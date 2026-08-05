using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;

using Rhino.Geometry;

using static ParkingDemo.ParkingUtils;
using ParkingDemo.Utils;
using ParkingDemo.Component.Start;

namespace ParkingDemo.Component.GUI
{
    public enum ParkingEntranceSide
    {
        North = 0,
        West = 1,
        East = 2,
        South = 3
    }

    public enum GenerationInterval
    {
        Ms500 = 500,
        Ms750 = 750,
        Sec1 = 1000,
        Sec15 = 1500,
        Sec2 = 2000,
        Sec3 = 3000
    }

    public class StartGenerationAdv : GH_Component
    {
        private readonly Random _random = new Random();

        private bool _isAutoRunning;
        private bool _solutionScheduled;
        private bool _componentIsAlive = true;

        public bool AddRamp { get; private set; }

        public ParkingEntranceSide EntranceSide { get; private set; }
            = ParkingEntranceSide.North;

        public GenerationInterval Interval { get; private set; }
            = GenerationInterval.Sec1;

        public bool IsAutoRunning => _isAutoRunning;

        public int IntervalMilliseconds => (int)Interval;

        public StartGenerationAdv()
            : base(
                "Start Generation",
                "Start",
                "Creates the initial parking information and repeatedly " +
                "generates new configurations when auto-run is enabled.",
                "ParkingDemo",
                "Start")
        {
        }

        public override void CreateAttributes()
        {
            m_attributes = new StartGenerationAttributes(this);
        }

        protected override void RegisterInputParams(
            GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter(
                "Outline",
                "O",
                "Parking internal outline.",
                GH_ParamAccess.item);

            pManager.AddCurveParameter(
                "Exclude Boundaries",
                "E",
                "Optional boundaries excluded from the parking plan.",
                GH_ParamAccess.list);

            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(
            GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Parking",
                "P",
                "Generated parking object.",
                GH_ParamAccess.item);
        }

        public void SetAddRamp(bool value)
        {
            if (AddRamp == value)
                return;

            RecordUndoEvent("Change ramp option");

            AddRamp = value;

            ExpireSolution(true);
        }

        public void SetEntranceSide(ParkingEntranceSide side)
        {
            if (EntranceSide == side)
                return;

            RecordUndoEvent("Change entrance side");

            EntranceSide = side;

            ExpireSolution(true);
        }

        public void SetInterval(GenerationInterval interval)
        {
            if (Interval == interval)
                return;

            RecordUndoEvent("Change generation interval");

            Interval = interval;

            /*
             * The next schedule will use the new interval.
             * We do not force an immediate recomputation here.
             */
            OnDisplayExpired(true);
        }

        public void StartGeneration()
        {
            if (_isAutoRunning)
                return;

            RecordUndoEvent("Start automatic generation");

            _isAutoRunning = true;

            Message = $"Running · {FormatInterval()}";

            ExpireSolution(true);
            OnDisplayExpired(true);
        }

        public void StopGeneration()
        {
            if (!_isAutoRunning)
                return;

            RecordUndoEvent("Stop automatic generation");

            _isAutoRunning = false;
            _solutionScheduled = false;

            Message = "Stopped";

            OnDisplayExpired(true);
        }

        public string FormatInterval()
        {
            return Interval switch
            {
                GenerationInterval.Ms500 => "500 ms",
                GenerationInterval.Ms750 => "750 ms",
                GenerationInterval.Sec1 => "1 s",
                GenerationInterval.Sec15 => "1.5 s",
                GenerationInterval.Sec2 => "2 s",
                GenerationInterval.Sec3 => "3 s",
                _ => "1 s"
            };
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve sourceOutline = null;

            if (!DA.GetData(0, ref sourceOutline))
            {
                StopGenerationBecauseOfError(
                    "A parking outline must be connected.");

                return;
            }

            if (sourceOutline == null || !sourceOutline.IsValid)
            {
                StopGenerationBecauseOfError(
                    "The connected parking outline is invalid.");

                return;
            }

            var sourceExcludeCurves = new List<Curve>();
            DA.GetDataList(1, sourceExcludeCurves);

            /*
             * Never transform geometry received directly from upstream
             * components. Work with duplicates.
             */

            Curve crv = sourceOutline.DuplicateCurve();

            var excludeCrvs = sourceExcludeCurves
                .Where(curve => curve != null && curve.IsValid)
                .Select(curve => curve.DuplicateCurve())
                .ToList();

            BoundingBox temporaryBoundingBox =
                crv.GetBoundingBox(true);

            Point3d minimumPoint =
                temporaryBoundingBox.Min;

            var transformationVector = new Vector3d(
                -minimumPoint.X,
                -minimumPoint.Y,
                -minimumPoint.Z);

            Transform translation =
                Transform.Translation(transformationVector);

            crv.Transform(translation);

            foreach (Curve excludeCurve in excludeCrvs)
            {
                excludeCurve.Transform(translation);
            }

            BoundingBox boundingBox =
                crv.GetBoundingBox(true);

            Point3d maximumPoint =
                boundingBox.Max;

            const int cellSize = 5;

            DataTree<Point3d> grid = ParkingUtils.CreateGrid(
                (int)RoundUp.RoundTo(
                    maximumPoint.Y,
                    cellSize) / cellSize,

                (int)RoundUp.RoundTo(
                    maximumPoint.X,
                    cellSize) / cellSize,

                cellSize);

            Matrix planToMatrix;

            List<Rectangle3d> excludeCells;

            planToMatrix = GridToMatrixWithExcludeCrvs(
                grid,
                grid.BranchCount,
                grid.Branch(0).Count,
                crv,
                excludeCrvs,
                out excludeCells);

            var cells =
                CellularOutline(grid, planToMatrix);

            var outline =
                OutlineFromCells(cells);

            var rampTypes =
                Ramp.ramptypes();

            var rampOrientations =
                Ramp.ramporientations();

            var sidePointsAddress =
                new DataTree<int[]>();

            var allSidePoints =
                Ramp.OutlineSidesFinder(
                    planToMatrix,
                    grid,
                    out sidePointsAddress);

            var rampPossibleOptions =
                Ramp.RampPossibleOptions(
                    planToMatrix,
                    grid,
                    sidePointsAddress,
                    allSidePoints);

            var rampInfo =
                new List<int>();

            var firstPathCell =
                new int[2];

            int entranceSideIndex =
                (int)EntranceSide;

            if (AddRamp)
            {
                Ramp.rampplacement(
                    planToMatrix,
                    rampPossibleOptions,
                    sidePointsAddress,
                    rampTypes,
                    rampOrientations,
                    entranceSideIndex,
                    out rampInfo,
                    out firstPathCell);
            }
            else
            {
                var selectedSideCells =
                    sidePointsAddress.Branch(
                        entranceSideIndex);

                if (selectedSideCells == null ||
                    selectedSideCells.Count == 0)
                {
                    StopGenerationBecauseOfError(
                        $"No valid entrance was found on the " +
                        $"{EntranceSide} side.");

                    return;
                }

                int randomIndex =
                    _random.Next(selectedSideCells.Count);

                firstPathCell =
                    selectedSideCells[randomIndex];
            }

            if (firstPathCell == null ||
                firstPathCell.Length < 2)
            {
                StopGenerationBecauseOfError(
                    "The generation process did not return a valid entrance cell.");

                return;
            }

            ResetMatrixElementsAfterRamp(planToMatrix);

            var parking = new Parking
            {
                ExcludeCells = excludeCells,
                PlanMatrix = planToMatrix,
                PlanPointsGrid = grid,
                Outline = outline,
                PlanCells = cells,
                SidePoints = allSidePoints,

                RampEndCell = new PathInfo.Cell(
                    firstPathCell[0],
                    firstPathCell[1]),

                PathStartCell = new PathInfo.Cell(
                    firstPathCell[0],
                    firstPathCell[1]),

                CurrentStartCell = new PathInfo.Cell(
                    firstPathCell[0],
                    firstPathCell[1]),

                EntryCell = new PathInfo.Cell(
                    firstPathCell[0],
                    firstPathCell[1]),

                RampInfo = rampInfo
            };

            DA.SetData(0, parking);

            if (_isAutoRunning)
            {
                Message = $"Running · {FormatInterval()}";
                ScheduleNextGeneration();
            }
            else
            {
                Message = "Stopped";
            }
        }

        private void ScheduleNextGeneration()
        {
            if (!_componentIsAlive ||
                !_isAutoRunning ||
                _solutionScheduled)
            {
                return;
            }

            GH_Document document =
                OnPingDocument();

            if (document == null)
                return;

            _solutionScheduled = true;

            document.ScheduleSolution(
                IntervalMilliseconds,
                scheduledDocument =>
                {
                    _solutionScheduled = false;

                    if (!_componentIsAlive ||
                        !_isAutoRunning ||
                        scheduledDocument == null ||
                        OnPingDocument() != scheduledDocument)
                    {
                        return;
                    }

                    /*
                     * The document will start its scheduled solution.
                     * Expiring this component ensures it recomputes
                     * and reads its current connected inputs.
                     */

                    ExpireSolution(false);
                });
        }

        private void StopGenerationBecauseOfError(
            string message)
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Error,
                message);

            _isAutoRunning = false;
            _solutionScheduled = false;

            Message = "Error";

            OnDisplayExpired(true);
        }

        public override void RemovedFromDocument(
            GH_Document document)
        {
            _componentIsAlive = false;
            _isAutoRunning = false;
            _solutionScheduled = false;

            base.RemovedFromDocument(document);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean(
                "AddRamp",
                AddRamp);

            writer.SetInt32(
                "EntranceSide",
                (int)EntranceSide);

            writer.SetInt32(
                "GenerationInterval",
                (int)Interval);

            /*
             * Usually it is safer not to resume automatic execution
             * immediately when a GH file is opened.
             */
            writer.SetBoolean(
                "WasRunning",
                _isAutoRunning);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            AddRamp =
                reader.ItemExists("AddRamp") &&
                reader.GetBoolean("AddRamp");

            if (reader.ItemExists("EntranceSide"))
            {
                EntranceSide =
                    (ParkingEntranceSide)
                    reader.GetInt32("EntranceSide");
            }

            if (reader.ItemExists("GenerationInterval"))
            {
                int savedInterval =
                    reader.GetInt32("GenerationInterval");

                if (Enum.IsDefined(
                    typeof(GenerationInterval),
                    savedInterval))
                {
                    Interval =
                        (GenerationInterval)savedInterval;
                }
            }

            /*
             * Do not automatically restart a timer when the file opens.
             * The user explicitly presses Start again.
             */

            _isAutoRunning = false;
            _solutionScheduled = false;
            Message = "Stopped";

            return base.Read(reader);
        }

        protected override Bitmap Icon =>
            Properties.Resources.StartGeneration;

        public override Guid ComponentGuid =>
            new Guid(
                "F6D91134-4E88-4243-B102-061869B01405");
    }
}