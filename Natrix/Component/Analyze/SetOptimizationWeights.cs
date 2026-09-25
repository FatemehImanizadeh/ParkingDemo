using System;
using System.Linq;
using Grasshopper.Kernel;
using Natrix.Utils;

namespace Natrix.Component.Analyze
{
    public class SetOptimizationWeights : GH_Component
    {
        public SetOptimizationWeights() : base("SetOptimizationWeights", "SOW",
            "Rescores and sorts a collection using nonnegative relative weights; zero disables a criterion.",
            "Natrix", "Analyse") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking Collection", "PC", "Collection to rescore.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number of Parkings", "NP", "Capacity weight.", GH_ParamAccess.item, 0.50);
            pManager.AddNumberParameter("Average Path Length", "PL", "Mean travel-distance weight.", GH_ParamAccess.item, 0.20);
            // Keep the old socket index so existing wires cannot silently become turn weights.
            pManager.AddNumberParameter("Legacy Non-functional Cells", "NC", "Unused legacy input; has no effect.", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Average Turns", "T", "Mean turn-count weight.", GH_ParamAccess.item, 0.30);
            pManager.AddNumberParameter("Path Length StdDev", "SD-L", "Weight for uniform travel distances; lower spread is better.", GH_ParamAccess.item, 0.15);
            pManager.AddNumberParameter("Turns StdDev", "SD-T", "Weight for uniform turn counts; lower spread is better.", GH_ParamAccess.item, 0.15);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking Collection", "PC", "Rescored options, highest score first.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GenerationCollection source = null;
            if (!DA.GetData(0, ref source) || source?.parkings == null) return;
            double lots = 0.50, length = 0.20, turns = 0.30, pathSpread = 0.15, turnSpread = 0.15;
            if (!DA.GetData(1, ref lots) || !DA.GetData(2, ref length) || !DA.GetData(4, ref turns) ||
                !DA.GetData(5, ref pathSpread) || !DA.GetData(6, ref turnSpread)) return;
            var weights = new Optimization { LotNumW = lots, PathLenW = length, DirShiftW = turns,
                PathStdDevW = pathSpread, TurnsStdDevW = turnSpread };
            // Keep upstream scores intact; geometry is shared and never modified here.
            var result = new GenerationCollection { parkings = source.parkings.Where(p => p != null)
                .Select(p => p.CopyForScoring()).ToList() };
            try { Optimization.OptimizationFunction(weights, result.parkings); }
            catch (ArgumentException ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); return; }
            if (result.parkings.Any(p => !p.HasValidScore))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Some options have invalid or missing metrics and are unscored. Regenerate them to calculate enabled statistics.");
            result.parkings = result.parkings.OrderByDescending(p => p.HasValidScore).ThenByDescending(p => p.Score).ToList();
            DA.SetData(0, result);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.DeconstructParkingCollection;
        public override Guid ComponentGuid => new Guid("B36E7B5B-3C76-44F8-8C3F-242A5CDF76DF");
    }
}
