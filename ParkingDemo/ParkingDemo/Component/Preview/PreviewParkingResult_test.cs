using System;
using System.Drawing;

using Grasshopper.Kernel;

using Rhino.Display;
using Rhino.Geometry;

using ParkingDemo.Utils;
using Rhino.DocObjects;
using ParkingDemo.Properties;

namespace ParkingDemo
{
    // ========================================================================
    // TEMPORARY PREVIEW TEST COMPONENT
    //
    // This component is only useful while developing the display system.
    //
    // Final architecture:
    // this preview code will move into BakeParkingResult.
    // ========================================================================

    public class PreviewParkingResult_test :
        GH_Component
    {
        private ParkingDisplayData _displayData;


        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public PreviewParkingResult_test()
            : base(
                "Preview Parking Result",
                "PreviewPark",
                "Displays ParkingDisplayData directly in the Rhino viewport " +
                "without baking geometry.",
                "ParkingDemo",
                "Analyse")
        {
        }


        // ====================================================================
        // INPUT
        // ====================================================================

        protected override void RegisterInputParams(
            GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Display Data",
                "D",
                "Parking display geometry produced by ParkingDisplayBuilder.",
                GH_ParamAccess.item);
        }


        // ====================================================================
        // OUTPUT
        // ====================================================================

        protected override void RegisterOutputParams(
            GH_OutputParamManager pManager)
        {
        }


        // ====================================================================
        // SOLVE INSTANCE
        // ====================================================================

        protected override void SolveInstance(
            IGH_DataAccess DA)
        {
            ParkingDisplayData displayData =
                null;


            if (!DA.GetData(
                0,
                ref displayData))
            {
                _displayData =
                    null;

                return;
            }


            _displayData =
                displayData;


            if (_displayData == null)
                return;


            _displayData.RebuildClippingBox();


            ExpirePreview(
                true);
        }


        // ====================================================================
        // PREVIEW CAPABLE
        // ====================================================================

        public override bool IsPreviewCapable
        {
            get
            {
                return
                    true;
            }
        }


        // ====================================================================
        // CLIPPING BOX
        // ====================================================================

        public override BoundingBox ClippingBox
        {
            get
            {
                if (_displayData == null)
                {
                    return
                        BoundingBox.Empty;
                }


                return
                    _displayData.ClippingBox;
            }
        }

        // ====================================================================
        // SHADED PREVIEW
        // ====================================================================

        public override void DrawViewportMeshes(
            IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(
                args);


            if (_displayData == null)
                return;


            foreach (ParkingDisplayItem item
                in _displayData.AllGeometry)
            {
                if (item == null ||
                    item.Geometry == null ||
                    !item.DrawFill)
                {
                    continue;
                }


                DisplayMaterial material =
                    new DisplayMaterial(
                        item.FillColor,
                        0.0);


                // -----------------------------------------------------------
                // Mesh
                // -----------------------------------------------------------

                Mesh mesh =
                    item.Geometry as Mesh;


                if (mesh != null)
                {
                    args.Display.DrawMeshShaded(
                        mesh,
                        material);

                    continue;
                }


                // -----------------------------------------------------------
                // Brep
                // -----------------------------------------------------------

                Brep brep =
                    item.Geometry as Brep;


                if (brep != null)
                {
                    args.Display.DrawBrepShaded(
                        brep,
                        material);
                }
            }
        }
        // ====================================================================
        // WIRE PREVIEW
        // ====================================================================

        public override void DrawViewportWires(
            IGH_PreviewArgs args)
        {
            base.DrawViewportWires(
                args);


            if (_displayData == null)
                return;


            // ---------------------------------------------------------------
            // Parking geometry
            // ---------------------------------------------------------------

            foreach (ParkingDisplayItem item
                in _displayData.AllGeometry)
            {
                if (item == null ||
                    item.Geometry == null ||
                    !item.DrawWire)
                {
                    continue;
                }


                DrawGeometryWires(
                    args,
                    item);
            }


            // ---------------------------------------------------------------
            // Cars
            // ---------------------------------------------------------------

            DrawCars(
                args);
        }


        // ====================================================================
        // DRAW GEOMETRY WIRES
        // ====================================================================

        private static void DrawGeometryWires(
            IGH_PreviewArgs args,
            ParkingDisplayItem item)
        {
            // ---------------------------------------------------------------
            // Curve
            // ---------------------------------------------------------------

            Curve curve =
                item.Geometry as Curve;


            if (curve != null)
            {
                args.Display.DrawCurve(
                    curve,
                    item.WireColor,
                    item.WireThickness);

                return;
            }


            // ---------------------------------------------------------------
            // Brep
            // ---------------------------------------------------------------

            Brep brep =
                item.Geometry as Brep;


            if (brep != null)
            {
                foreach (BrepEdge edge
                    in brep.Edges)
                {
                    args.Display.DrawCurve(
                        edge,
                        item.WireColor,
                        item.WireThickness);
                }


                return;
            }


            // ---------------------------------------------------------------
            // Mesh
            // ---------------------------------------------------------------

            Mesh mesh =
                item.Geometry as Mesh;


            if (mesh != null)
            {
                args.Display.DrawMeshWires(
                    mesh,
                    item.WireColor);
            }
        }

        // ====================================================================
        // DRAW CARS
        // ====================================================================

        private void DrawCars(
            IGH_PreviewArgs args)
        {
            if (_displayData == null)
                return;


            if (_displayData.CarDefinition == null)
                return;


            if (_displayData.CarTransforms == null)
                return;


            // ---------------------------------------------------------------
            // Get the original Rhino objects which belong to the block
            // definition.
            //
            // Nothing is copied or baked here.
            // ---------------------------------------------------------------

            RhinoObject[] blockObjects =
                _displayData.CarDefinition.GetObjects();


            if (blockObjects == null ||
                blockObjects.Length == 0)
            {
                return;
            }


            // ---------------------------------------------------------------
            // Every CarTransform represents one generated car location.
            // ---------------------------------------------------------------

            foreach (Transform carTransform
                in _displayData.CarTransforms)
            {
                foreach (RhinoObject blockObject
                    in blockObjects)
                {
                    if (blockObject == null)
                        continue;


                    args.Display.DrawObject(
                        blockObject,
                        carTransform);
                }
            }
        }
        // ====================================================================
        // ICON
        // ====================================================================

        protected override Bitmap Icon => Resources.preview;


        // ====================================================================
        // GUID
        // ====================================================================

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid(
                    "E1C23152-73ED-4C85-AD44-8506E45BBA02");
            }
        }
    }
}