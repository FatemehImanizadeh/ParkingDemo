using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

using ParkingDemo.Utils;



namespace ParkingDemo
{
    // ========================================================================
    // COMPONENT
    //
    // Pure preview component - it never bakes anything into the Rhino
    // document. Its only job is to always show a live OpenGL preview of the
    // generated parking (cars, graded cells, main path, excluded cells,
    // entrance cell, boundary wall), with per-element on/off buttons drawn
    // directly on the component.
    //
    // Baking is handled entirely by the separate BakeParkingResult
    // component - this one is preview-only, no BAKE button, no bake inputs.
    // ========================================================================

    public class PreviewParking : GH_Component
    {
        // ====================================================================
        // TOGGLE STATE
        //
        // These are plain fields (not GH input params) because they are set
        // by clicking the buttons drawn on the component itself, not by
        // wiring a boolean toggle from the canvas.
        // ====================================================================

        internal bool ShowCars = true;
        internal bool ShowGradientCells = true;
        internal bool ShowPath = true;
        internal bool ShowExcludedCells = true;
        internal bool ShowEntranceCell = true;
        internal bool ShowWalls = true;


        // ====================================================================
        // CACHED PREVIEW STATE
        //
        // Refreshed on every normal solve so DrawViewportMeshes/Wires always
        // have current data, without needing to re-run SolveInstance just
        // because a toggle button was clicked.
        // ====================================================================

        private Parking _previewParking;
        private IGH_Goo _carBlockGoo;


        public PreviewParking()
            : base(
                "Preview Parking Result",
                "PreviewPark",
                "Always-on live preview of the generated parking (cars, graded " +
                "cells, main circulation path, excluded cells, entrance cell, " +
                "boundary wall). Click the buttons on the component to toggle " +
                "each element on/off. This component does not bake anything - " +
                "use Bake Parking Result for that.",
                "ParkingDemo",
                "Analyse")
        {
        }


        // ====================================================================
        // INPUTS
        // ====================================================================

        protected override void RegisterInputParams(
            GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Parking",
                "P",
                "Generated Parking object to preview.",
                GH_ParamAccess.item);

            // Generic is intentional - see BakeParkingResult for why.
            pManager.AddGenericParameter(
                "Car Block",
                "Blk",
                "Reference an existing double-car block instance in Rhino. " +
                "Only needed if the Cars preview toggle is on.",
                GH_ParamAccess.item);

            pManager[1].Optional = true;
        }


        // ====================================================================
        // OUTPUTS
        // ====================================================================

        protected override void RegisterOutputParams(
            GH_OutputParamManager pManager)
        {
            // No outputs - this component only draws a live preview.
        }


        // ====================================================================
        // SOLVE INSTANCE
        // ====================================================================

        protected override void SolveInstance(
            IGH_DataAccess DA)
        {
            Parking parking = null;

            IGH_Goo carBlockGoo = null;


            if (!DA.GetData(0, ref parking))
            {
                _previewParking = null;

                return;
            }


            // Car Block is optional - only required if cars are toggled on.
            DA.GetData(1, ref carBlockGoo);


            if (parking == null)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "Parking input is empty.");

                _previewParking = null;

                return;
            }


            // ---------------------------------------------------------------
            // Build the preview geometry directly from the parking's raw
            // fields (CellsWithGrade, PathLines, ExcludeCells, EntryCell,
            // Outline, ...) - the exact same source data the original bake
            // logic reads from.
            //
            // If some other component already built parking.PreviewGeometry
            // (e.g. the generation component), it's reused as-is to avoid
            // recomputation. Otherwise it's built right here, so this
            // component never depends on anything else having populated it
            // first.
            // ---------------------------------------------------------------

            if (parking.PreviewGeometry == null)
            {
                RhinoDoc doc =
                    RhinoDoc.ActiveDoc;

                double tolerance =
                    doc != null
                    ? doc.ModelAbsoluteTolerance
                    : 0.001;

                ParkingPreviewGeometryBuilder.BuildAll(
                    parking,
                    tolerance);
            }


            _previewParking = parking;
            _carBlockGoo = carBlockGoo;
        }


        // ====================================================================
        // LIVE PREVIEW
        // ====================================================================

        public override BoundingBox ClippingBox
        {
            get
            {
                BoundingBox box = BoundingBox.Empty;

                if (_previewParking?.PreviewGeometry != null)
                {
                    ParkingPreviewGeometry pg =
                        _previewParking.PreviewGeometry;

                    GrowBox(ref box, pg.GradientCells);
                    GrowBox(ref box, pg.ExcludedCells);
                    GrowBox(ref box, pg.PathRibbons);
                    GrowBox(ref box, pg.Walls);

                    if (pg.EntranceCell?.Geometry != null)
                        box.Union(pg.EntranceCell.Geometry.GetBoundingBox(true));
                }

                return box;
            }
        }


        private static void GrowBox(
            ref BoundingBox box,
            List<GeometryColorPair> items)
        {
            if (items == null)
                return;

            foreach (GeometryColorPair item in items)
            {
                if (item?.Geometry != null)
                    box.Union(item.Geometry.GetBoundingBox(true));
            }
        }


        public override void DrawViewportMeshes(
            IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);

            if (_previewParking?.PreviewGeometry == null)
                return;

            ParkingPreviewGeometry pg =
                _previewParking.PreviewGeometry;

            if (ShowGradientCells)
                DrawShadedBreps(args, pg.GradientCells);

            if (ShowExcludedCells)
                DrawShadedBreps(args, pg.ExcludedCells);

            if (ShowPath)
                DrawShadedBreps(args, pg.PathRibbons);

            if (ShowEntranceCell && pg.EntranceCell != null)
                DrawShadedBreps(args, new List<GeometryColorPair> { pg.EntranceCell });

            if (ShowWalls)
                DrawShadedBreps(args, pg.Walls);

            if (ShowCars)
                DrawCarsPreview(args);
        }


        public override void DrawViewportWires(
            IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (_previewParking?.PreviewGeometry == null)
                return;

            ParkingPreviewGeometry pg =
                _previewParking.PreviewGeometry;

            // Only the path can fall back to a bare curve (when the ribbon
            // brep couldn't be built - see
            // ParkingPreviewGeometryBuilder.BuildContinuousPath), so that's
            // the only element that needs a wire pass.
            if (ShowPath)
                DrawWireCurves(args, pg.PathRibbons);
        }


        private static void DrawShadedBreps(
            IGH_PreviewArgs args,
            List<GeometryColorPair> items)
        {
            if (items == null)
                return;

            foreach (GeometryColorPair item in items)
            {
                if (item?.Geometry is Brep brep)
                {
                    DisplayMaterial material =
                        new DisplayMaterial(item.Color);

                    args.Display.DrawBrepShaded(
                        brep,
                        material);
                }
            }
        }


        private static void DrawWireCurves(
            IGH_PreviewArgs args,
            List<GeometryColorPair> items)
        {
            if (items == null)
                return;

            foreach (GeometryColorPair item in items)
            {
                if (item?.Geometry is Curve curve)
                {
                    args.Display.DrawCurve(
                        curve,
                        item.Color,
                        2);
                }
            }
        }


        /// <summary>
        /// Draws the parked cars live, without baking. Cars are Rhino block
        /// instances rather than raw geometry, so unlike the other elements
        /// they still need the Car Block reference at draw time; only the
        /// block definition's own geometry is duplicated and transformed
        /// per car - nothing is added to the document.
        /// </summary>
        private void DrawCarsPreview(
            IGH_PreviewArgs args)
        {
            if (_previewParking?.CarTransforms == null)
                return;

            if (_carBlockGoo == null)
                return;

            RhinoDoc doc =
                RhinoDoc.ActiveDoc;

            if (doc == null)
                return;

            InstanceDefinition carBlockDefinition =
                BakeParkingResult.ResolveCarBlockDefinition(
                    _carBlockGoo,
                    doc);

            if (carBlockDefinition == null)
                return;

            RhinoObject[] defObjects =
                carBlockDefinition.GetObjects();

            if (defObjects == null || defObjects.Length == 0)
                return;

            foreach (var branch in _previewParking.CarTransforms.Branches)
            {
                if (branch == null)
                    continue;

                foreach (Transform carTransform in branch)
                {
                    foreach (RhinoObject obj in defObjects)
                    {
                        GeometryBase geo = obj.Geometry;

                        if (geo == null)
                            continue;

                        Color objectColor =
                            obj.Attributes.DrawColor(doc);

                        if (geo is Brep sourceBrep)
                        {
                            Brep transformed =
                                (Brep)sourceBrep.Duplicate();

                            transformed.Transform(carTransform);

                            args.Display.DrawBrepShaded(
                                transformed,
                                new DisplayMaterial(objectColor));
                        }
                        else if (geo is Mesh sourceMesh)
                        {
                            Mesh transformed =
                                (Mesh)sourceMesh.Duplicate();

                            transformed.Transform(carTransform);

                            args.Display.DrawMeshShaded(
                                transformed,
                                new DisplayMaterial(objectColor));
                        }
                        else if (geo is Curve sourceCurve)
                        {
                            Curve transformed =
                                (Curve)sourceCurve.Duplicate();

                            transformed.Transform(carTransform);

                            args.Display.DrawCurve(
                                transformed,
                                objectColor);
                        }
                    }
                }
            }
        }


        // ====================================================================
        // CUSTOM UI
        // ====================================================================

        public override void CreateAttributes()
        {
            m_attributes =
                new PreviewParkingAttributes(
                    this);
        }


        // ====================================================================
        // ICON
        // ====================================================================

        protected override Bitmap Icon => Properties.Resources.BakeParking;


        // ====================================================================
        // GUID
        //
        // Different from BakeParkingResult's GUID - this is a separate,
        // new component.
        // ====================================================================

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid(
                    "C30019DB-2068-42C9-814A-25EE355A9A1C");
            }
        }
    }


    // ========================================================================
    // CUSTOM COMPONENT ATTRIBUTES
    //
    // Draws a 3x2 grid of small toggle buttons under the component body.
    // Clicking a button flips the corresponding Show* field on the owner
    // and just refreshes the preview draw (ExpirePreview) - no need to
    // re-run SolveInstance since no data changed.
    // ========================================================================

    public class PreviewParkingAttributes :
        GH_ComponentAttributes
    {
        private readonly RectangleF[] _toggleBounds =
            new RectangleF[6];

        private static readonly string[] ToggleLabels =
        {
            "Cars",
            "Gradient",
            "Path",
            "Excluded",
            "Entrance",
            "Walls"
        };

        private const int Columns = 2;
        private const int Rows = 3;

        private const float ButtonHeight = 20f;
        private const float ButtonSpacing = 4f;
        private const float BottomMargin = 6f;
        private const float SideMargin = 6f;


        public PreviewParkingAttributes(
            PreviewParking owner)
            : base(owner)
        {
        }


        // ====================================================================
        // LAYOUT
        // ====================================================================

        protected override void Layout()
        {
            base.Layout();


            RectangleF originalBounds =
                Bounds;


            float minimumWidth =
                190f;


            float newWidth =
                Math.Max(
                    originalBounds.Width,
                    minimumWidth);


            float gridHeight =
                (Rows * ButtonHeight) +
                ((Rows - 1) * ButtonSpacing);


            float additionalHeight =
                gridHeight +
                BottomMargin +
                8f;


            Bounds =
                new RectangleF(
                    originalBounds.X,
                    originalBounds.Y,
                    newWidth,
                    originalBounds.Height +
                    additionalHeight);


            // ---------------------------------------------------------------
            // Lay out the 3x2 button grid.
            // ---------------------------------------------------------------

            float gridTop =
                Bounds.Bottom -
                gridHeight -
                BottomMargin;

            float colWidth =
                (Bounds.Width -
                (SideMargin * 2f) -
                ((Columns - 1) * ButtonSpacing)) /
                Columns;

            for (int i = 0; i < ToggleLabels.Length; i++)
            {
                int row = i / Columns;
                int col = i % Columns;

                float x =
                    Bounds.Left +
                    SideMargin +
                    (col * (colWidth + ButtonSpacing));

                float y =
                    gridTop +
                    (row * (ButtonHeight + ButtonSpacing));

                _toggleBounds[i] =
                    new RectangleF(
                        x,
                        y,
                        colWidth,
                        ButtonHeight);
            }
        }


        // ====================================================================
        // RENDER
        // ====================================================================

        protected override void Render(
            GH_Canvas canvas,
            Graphics graphics,
            GH_CanvasChannel channel)
        {
            base.Render(
                canvas,
                graphics,
                channel);


            if (channel != GH_CanvasChannel.Objects)
                return;


            PreviewParking component =
                Owner as PreviewParking;

            if (component == null)
                return;


            bool[] states =
            {
                component.ShowCars,
                component.ShowGradientCells,
                component.ShowPath,
                component.ShowExcludedCells,
                component.ShowEntranceCell,
                component.ShowWalls
            };

            for (int i = 0; i < _toggleBounds.Length; i++)
            {
                DrawToggleButton(
                    graphics,
                    _toggleBounds[i],
                    ToggleLabels[i],
                    states[i]);
            }
        }


        private static void DrawToggleButton(
            Graphics graphics,
            RectangleF bounds,
            string label,
            bool isOn)
        {
            Color fillColor =
                isOn
                ? Color.FromArgb(72, 145, 68)
                : Color.FromArgb(90, 90, 90);

            Color borderColor =
                isOn
                ? Color.FromArgb(45, 95, 45)
                : Color.FromArgb(55, 55, 55);

            using (SolidBrush brush = new SolidBrush(fillColor))
            {
                graphics.FillRectangle(brush, bounds);
            }

            using (Pen borderPen = new Pen(borderColor, 1.2f))
            {
                graphics.DrawRectangle(
                    borderPen,
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    bounds.Height);
            }

            using (System.Drawing.Font font = new System.Drawing.Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            using (StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                graphics.DrawString(label, font, textBrush, bounds, format);
            }
        }


        // ====================================================================
        // MOUSE INPUT
        // ====================================================================

        public override GH_ObjectResponse RespondToMouseDown(
            GH_Canvas sender,
            GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < _toggleBounds.Length; i++)
                {
                    if (!_toggleBounds[i].Contains(e.CanvasLocation))
                        continue;

                    PreviewParking component =
                        Owner as PreviewParking;

                    if (component == null)
                        break;

                    switch (i)
                    {
                        case 0:
                            component.ShowCars = !component.ShowCars;
                            break;
                        case 1:
                            component.ShowGradientCells = !component.ShowGradientCells;
                            break;
                        case 2:
                            component.ShowPath = !component.ShowPath;
                            break;
                        case 3:
                            component.ShowExcludedCells = !component.ShowExcludedCells;
                            break;
                        case 4:
                            component.ShowEntranceCell = !component.ShowEntranceCell;
                            break;
                        case 5:
                            component.ShowWalls = !component.ShowWalls;
                            break;
                    }

                    // Only the display needs to refresh - no data changed,
                    // so there is no need to re-run SolveInstance.
                    component.ExpirePreview(true);

                    return GH_ObjectResponse.Handled;
                }
            }

            return base.RespondToMouseDown(sender, e);
        }
    }
}