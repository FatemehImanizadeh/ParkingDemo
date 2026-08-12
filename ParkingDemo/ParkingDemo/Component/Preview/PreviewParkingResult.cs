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
    // ========================================================================

    public class PreviewParkingResult : GH_Component
    {
        /// <summary>
        /// Becomes true only when the user clicks the BAKE PARKING button.
        /// SolveInstance consumes it once and immediately resets it.
        /// </summary>
        internal bool BakeRequested { get; set; } = false;


        // ====================================================================
        // LIVE PREVIEW STATE
        //
        // These fields are refreshed on EVERY normal solve (bake button or
        // not) so DrawViewportMeshes/DrawViewportWires always have
        // something current to draw, using the geometry already stored on
        // parking.PreviewGeometry - nothing is recomputed here.
        // ====================================================================

        private Parking _previewParking;
        private IGH_Goo _carBlockGoo;

        private bool _showCars = true;
        private bool _showGradient = true;
        private bool _showPath = true;
        private bool _showExcluded = true;
        private bool _showEntrance = true;
        private bool _showWalls = true;


        public PreviewParkingResult()
            : base(
                "Bake Parking Result",
                "BakePark",
                "Always previews the generated parking (cars, graded cells, " +
                "main path, excluded cells, entrance cell, boundary wall) " +
                "using its precomputed geometry, and bakes the selected " +
                "elements into Rhino when the BAKE PARKING button is pressed.",
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
            // ---------------------------------------------------------------
            // Parking object
            // ---------------------------------------------------------------

            pManager.AddGenericParameter(
                "Parking",
                "P",
                "Generated Parking object to visualize and bake.",
                GH_ParamAccess.item);


            // ---------------------------------------------------------------
            // Car block
            // ---------------------------------------------------------------

            // Generic is intentional.
            //
            // Grasshopper's native Block Instance parameter may wrap
            // InstanceObject / InstanceReferenceGeometry inside its own Goo.
            //
            // Using Generic allows us to unwrap it ourselves reliably.
            pManager.AddGenericParameter(
                "Car Block",
                "Blk",
                "Reference an existing double-car block instance in Rhino. " +
                "The block definition will be used for all generated cars.",
                GH_ParamAccess.item);

            // Cars are only needed if cars are actually shown/baked.
            pManager[1].Optional = true;


            // ---------------------------------------------------------------
            // Selective show/bake toggles.
            //
            // Each toggle controls BOTH whether that element is drawn in
            // the always-on live preview AND whether it gets baked when
            // BAKE PARKING is pressed. All default to true.
            // ---------------------------------------------------------------

            pManager.AddBooleanParameter(
                "Bake Cars",
                "Cars",
                "Show/bake the parked cars.",
                GH_ParamAccess.item,
                true);
            pManager[2].Optional = true;

            pManager.AddBooleanParameter(
                "Bake Gradient Cells",
                "Grade",
                "Show/bake the grade-colored parking cells.",
                GH_ParamAccess.item,
                true);
            pManager[3].Optional = true;

            pManager.AddBooleanParameter(
                "Bake Path",
                "Path",
                "Show/bake the main circulation path.",
                GH_ParamAccess.item,
                true);
            pManager[4].Optional = true;

            pManager.AddBooleanParameter(
                "Bake Excluded Cells",
                "Excl",
                "Show/bake the excluded cells.",
                GH_ParamAccess.item,
                true);
            pManager[5].Optional = true;

            pManager.AddBooleanParameter(
                "Bake Entrance Cell",
                "Entry",
                "Show/bake the entrance cell.",
                GH_ParamAccess.item,
                true);
            pManager[6].Optional = true;

            pManager.AddBooleanParameter(
                "Bake Walls",
                "Wall",
                "Show/bake the parking boundary wall.",
                GH_ParamAccess.item,
                true);
            pManager[7].Optional = true;
        }


        // ====================================================================
        // OUTPUTS
        // ====================================================================

        protected override void RegisterOutputParams(
            GH_OutputParamManager pManager)
        {
            // No outputs are required.
            //
            // The purpose of this component is to bake the result
            // directly into the active Rhino document.
        }


        // ====================================================================
        // SOLVE INSTANCE
        // ====================================================================

        protected override void SolveInstance(
            IGH_DataAccess DA)
        {
            Parking parking = null;

            IGH_Goo carBlockGoo = null;

            bool bakeCars = true;
            bool bakeGradient = true;
            bool bakePath = true;
            bool bakeExcluded = true;
            bool bakeEntrance = true;
            bool bakeWalls = true;


            // Read Parking
            if (!DA.GetData(0, ref parking))
                return;


            // Car Block is optional - only needed if cars are shown/baked.
            DA.GetData(1, ref carBlockGoo);

            DA.GetData(2, ref bakeCars);
            DA.GetData(3, ref bakeGradient);
            DA.GetData(4, ref bakePath);
            DA.GetData(5, ref bakeExcluded);
            DA.GetData(6, ref bakeEntrance);
            DA.GetData(7, ref bakeWalls);


            if (parking == null)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "Parking input is empty.");

                _previewParking = null;

                return;
            }


            // ---------------------------------------------------------------
            // ALWAYS-ON LIVE PREVIEW.
            //
            // This runs on every normal solve, bake button or not. It just
            // caches what DrawViewportMeshes/DrawViewportWires need; no
            // geometry is recomputed here, they read parking.PreviewGeometry
            // directly (see ParkingPreviewGeometryBuilder in the generation
            // component).
            // ---------------------------------------------------------------

            _previewParking = parking;
            _carBlockGoo = carBlockGoo;

            _showCars = bakeCars;
            _showGradient = bakeGradient;
            _showPath = bakePath;
            _showExcluded = bakeExcluded;
            _showEntrance = bakeEntrance;
            _showWalls = bakeWalls;

            if (parking.PreviewGeometry == null)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Remark,
                    "Parking.PreviewGeometry is empty - nothing to preview " +
                    "or bake yet. Call ParkingPreviewGeometryBuilder.BuildAll(...) " +
                    "once in the component that generates this Parking object.");
            }


            // ---------------------------------------------------------------
            // IMPORTANT:
            //
            // Normal Grasshopper recomputation does NOT bake anything.
            // Baking only happens after clicking the button.
            // ---------------------------------------------------------------

            if (!BakeRequested)
                return;


            // Consume request immediately.
            //
            // Therefore another Grasshopper solution will not accidentally
            // bake the same parking a second time.
            BakeRequested = false;


            BakeParking(
                parking,
                carBlockGoo,
                bakeCars,
                bakeGradient,
                bakePath,
                bakeExcluded,
                bakeEntrance,
                bakeWalls);
        }


        // ====================================================================
        // MAIN BAKE ORCHESTRATOR
        // ====================================================================

        private void BakeParking(
            Parking parking,
            IGH_Goo carBlockGoo,
            bool bakeCars,
            bool bakeGradient,
            bool bakePath,
            bool bakeExcluded,
            bool bakeEntrance,
            bool bakeWalls)
        {
            RhinoDoc doc =
                RhinoDoc.ActiveDoc;


            if (doc == null)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Error,
                    "No active Rhino document was found.");

                return;
            }


            if (parking.PreviewGeometry == null)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Error,
                    "Parking.PreviewGeometry is empty - nothing to bake. " +
                    "Build it once in the generation component with " +
                    "ParkingPreviewGeometryBuilder.BuildAll(...).");

                return;
            }


            // ---------------------------------------------------------------
            // Resolve car block (only if cars are actually being baked)
            // ---------------------------------------------------------------

            InstanceDefinition carBlockDefinition = null;

            if (bakeCars)
            {
                carBlockDefinition =
                    ResolveCarBlockDefinition(
                        carBlockGoo,
                        doc);

                if (carBlockDefinition == null)
                {
                    AddRuntimeMessage(
                        GH_RuntimeMessageLevel.Error,
                        "Car Block must reference an existing block instance " +
                        "placed in the Rhino document.");

                    return;
                }
            }


            ParkingPreviewGeometry pg =
                parking.PreviewGeometry;


            // ===============================================================
            // CREATE / FIND LAYERS
            // ===============================================================

            int carsLayerIndex =
                EnsureChildLayer(doc, "Parking", "Cars");

            int gradientLayerIndex =
                EnsureChildLayer(doc, "Parking", "Gradient Cells");

            int pathLayerIndex =
                EnsureChildLayer(doc, "Parking", "Main Path");

            int excludedLayerIndex =
                EnsureChildLayer(doc, "Parking", "Excluded Cells");

            int entranceLayerIndex =
                EnsureChildLayer(doc, "Parking", "Entrance Cell");

            int wallLayerIndex =
                EnsureChildLayer(doc, "Parking", "Walls");


            // ===============================================================
            // BAKE EACH SELECTED ELEMENT USING ITS PRECOMPUTED GEOMETRY.
            //
            // Nothing below recomputes any geometry - it was all already
            // built once by ParkingPreviewGeometryBuilder and is reused
            // as-is here, exactly the same geometry the preview was
            // showing.
            // ===============================================================

            if (bakeCars)
            {
                BakeCars(
                    doc,
                    parking,
                    carBlockDefinition,
                    carsLayerIndex);
            }

            if (bakeGradient)
            {
                BakeResultsUtils.BakeGeometryColorPairs(
                    doc,
                    pg.GradientCells,
                    gradientLayerIndex);
            }

            if (bakePath)
            {
                BakeResultsUtils.BakeGeometryColorPairs(
                    doc,
                    pg.PathRibbons,
                    pathLayerIndex);
            }

            if (bakeExcluded)
            {
                BakeResultsUtils.BakeGeometryColorPairs(
                    doc,
                    pg.ExcludedCells,
                    excludedLayerIndex);
            }

            if (bakeEntrance && pg.EntranceCell != null)
            {
                BakeResultsUtils.BakeGeometryColorPairs(
                    doc,
                    new List<GeometryColorPair> { pg.EntranceCell },
                    entranceLayerIndex);
            }

            if (bakeWalls)
            {
                BakeResultsUtils.BakeGeometryColorPairs(
                    doc,
                    pg.Walls,
                    wallLayerIndex);
            }


            // ===============================================================
            // FINAL REDRAW
            // ===============================================================

            doc.Views.Redraw();


            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Remark,
                "Parking successfully baked.");
        }


        // ====================================================================
        // BAKE CARS
        // ====================================================================

        public static void BakeCars(
            RhinoDoc doc,
            Parking parking,
            InstanceDefinition carBlockDefinition,
            int carsLayerIndex)
        {
            if (parking.CarTransforms == null)
                return;


            ObjectAttributes attributes =
                new ObjectAttributes();


            attributes.LayerIndex =
                carsLayerIndex;


            foreach (var branch
                in parking.CarTransforms.Branches)
            {
                if (branch == null)
                    continue;


                foreach (Transform transform
                    in branch)
                {
                    doc.Objects.AddInstanceObject(
                        carBlockDefinition.Index,
                        transform,
                        attributes);
                }
            }
        }


        // ====================================================================
        // LAYER MANAGEMENT
        // ====================================================================

        /// <summary>
        /// Ensures:
        ///
        /// Parking
        ///    └── childName
        ///
        /// exists and returns the child layer index.
        /// Existing layers are reused.
        /// </summary>
        public static int EnsureChildLayer(
            RhinoDoc doc,
            string parentName,
            string childName)
        {
            // ---------------------------------------------------------------
            // Find parent
            // ---------------------------------------------------------------

            int parentIndex =
                doc.Layers.FindByFullPath(
                    parentName,
                    -1);


            // ---------------------------------------------------------------
            // Create parent if needed
            // ---------------------------------------------------------------

            if (parentIndex < 0)
            {
                Layer parentLayer =
                    new Layer();


                parentLayer.Name =
                    parentName;


                parentIndex =
                    doc.Layers.Add(
                        parentLayer);
            }


            Layer parent =
                doc.Layers[parentIndex];


            // ---------------------------------------------------------------
            // Find child
            // ---------------------------------------------------------------

            string childFullPath =
                parentName +
                "::" +
                childName;


            int childIndex =
                doc.Layers.FindByFullPath(
                    childFullPath,
                    -1);


            // ---------------------------------------------------------------
            // Create child if needed
            // ---------------------------------------------------------------

            if (childIndex < 0)
            {
                Layer childLayer =
                    new Layer();


                childLayer.Name =
                    childName;


                childLayer.ParentLayerId =
                    parent.Id;


                childIndex =
                    doc.Layers.Add(
                        childLayer);
            }


            return childIndex;
        }


        // ====================================================================
        // BLOCK RESOLUTION
        // ====================================================================

        /// <summary>
        /// Attempts to extract the InstanceDefinition from the Grasshopper
        /// input.
        ///
        /// Supported input representations:
        ///
        /// 1. Grasshopper Block Instance Goo
        /// 2. Rhino InstanceObject
        /// 3. Rhino InstanceReferenceGeometry
        /// 4. Referenced IGH_GeometricGoo
        /// 5. Guid pointing to an InstanceObject
        /// </summary>
        public static InstanceDefinition ResolveCarBlockDefinition(
            IGH_Goo goo,
            RhinoDoc doc)
        {
            if (goo == null)
                return null;


            // ---------------------------------------------------------------
            // Try unwrapping a Value property.
            //
            // Some GH Goo wrappers expose the Rhino object through Value,
            // but the exact wrapper class is not always public API.
            // ---------------------------------------------------------------

            object value =
                goo;


            var valueProperty =
                goo.GetType()
                   .GetProperty("Value");


            if (valueProperty != null)
            {
                object unwrapped =
                    valueProperty.GetValue(
                        goo);


                if (unwrapped != null)
                {
                    value =
                        unwrapped;
                }
            }


            // ---------------------------------------------------------------
            // Rhino InstanceObject
            // ---------------------------------------------------------------

            InstanceObject instanceObject =
                value as InstanceObject;


            if (instanceObject != null)
            {
                return
                    instanceObject.InstanceDefinition;
            }


            // ---------------------------------------------------------------
            // InstanceReferenceGeometry
            // ---------------------------------------------------------------

            InstanceReferenceGeometry instanceReference =
                value as InstanceReferenceGeometry;


            if (instanceReference != null)
            {
                return
                    doc.InstanceDefinitions.FindId(
                        instanceReference.ParentIdefId);
            }


            // ---------------------------------------------------------------
            // Try referenced Rhino object GUID
            // ---------------------------------------------------------------

            Guid objectId =
                Guid.Empty;


            IGH_GeometricGoo geometricGoo =
                goo as IGH_GeometricGoo;


            if (geometricGoo != null &&
                geometricGoo.ReferenceID != Guid.Empty)
            {
                objectId =
                    geometricGoo.ReferenceID;
            }
            else
            {
                Guid castId;


                if (goo.CastTo<Guid>(
                    out castId))
                {
                    objectId =
                        castId;
                }
            }


            // ---------------------------------------------------------------
            // Resolve GUID
            // ---------------------------------------------------------------

            if (objectId != Guid.Empty)
            {
                RhinoObject rhinoObject =
                    doc.Objects.Find(
                        objectId);


                InstanceObject referencedInstance =
                    rhinoObject as InstanceObject;


                if (referencedInstance != null)
                {
                    return
                        referencedInstance.InstanceDefinition;
                }
            }


            return null;
        }


        // ====================================================================
        // LIVE PREVIEW (always on, independent of the BAKE button)
        //
        // Everything below just DRAWS geometry that already lives on
        // parking.PreviewGeometry (built once by
        // ParkingPreviewGeometryBuilder.BuildAll in the generation
        // component). Nothing here recomputes geometry, and nothing here
        // touches doc.Objects - it's pure OpenGL preview via the GH
        // display pipeline.
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

            if (_showGradient)
                DrawShadedBreps(args, pg.GradientCells);

            if (_showExcluded)
                DrawShadedBreps(args, pg.ExcludedCells);

            if (_showPath)
                DrawShadedBreps(args, pg.PathRibbons);

            if (_showEntrance && pg.EntranceCell != null)
                DrawShadedBreps(args, new List<GeometryColorPair> { pg.EntranceCell });

            if (_showWalls)
                DrawShadedBreps(args, pg.Walls);

            if (_showCars)
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

            // Only the path can fall back to a bare curve (see
            // ParkingPreviewGeometryBuilder.BuildContinuousPath), so that's
            // the only one that needs a wire pass.
            if (_showPath)
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
        /// Draws the parked cars live, without baking. Cars are Rhino
        /// block instances rather than raw geometry, so unlike the other
        /// elements they still need the Car Block reference at draw time;
        /// only the block definition's own geometry is duplicated and
        /// transformed per car - nothing is added to the document.
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
                ResolveCarBlockDefinition(
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
                new PreviewParkingResultAttributes(
                    this);
        }


        // ====================================================================
        // ICON
        // ====================================================================

        protected override Bitmap Icon => Properties.Resources.preview;


        // ====================================================================
        // GUID
        // ====================================================================

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid(
                    "1C191F82-3204-42BC-A3D9-95592BB40D83");
            }
        }
    }


    // ========================================================================
    // CUSTOM COMPONENT ATTRIBUTES
    // ========================================================================

    public class PreviewParkingResultAttributes :
        GH_ComponentAttributes
    {
        private RectangleF _buttonBounds;


        // ---------------------------------------------------------------
        // BUTTON APPEARANCE
        // ---------------------------------------------------------------

        private const float ButtonHeight =
            36f;


        private const float BottomMargin =
            6f;


        private const float SideMargin =
            6f;


        public PreviewParkingResultAttributes(
            PreviewParkingResult owner)
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


            // ---------------------------------------------------------------
            // Make sure the component is wide enough for a large button.
            // ---------------------------------------------------------------

            float minimumWidth =
                155f;


            float newWidth =
                Math.Max(
                    originalBounds.Width,
                    minimumWidth);


            float additionalHeight =
                ButtonHeight +
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
            // Large BAKE PARKING button
            // ---------------------------------------------------------------

            _buttonBounds =
                new RectangleF(
                    Bounds.Left +
                    SideMargin,

                    Bounds.Bottom -
                    ButtonHeight -
                    BottomMargin,

                    Bounds.Width -
                    (SideMargin * 2f),

                    ButtonHeight);
        }


        // ====================================================================
        // RENDER
        // ====================================================================

        protected override void Render(
            GH_Canvas canvas,
            Graphics graphics,
            GH_CanvasChannel channel)
        {
            // Draw standard component first.
            base.Render(
                canvas,
                graphics,
                channel);


            if (channel !=
                GH_CanvasChannel.Objects)
            {
                return;
            }


            // ---------------------------------------------------------------
            // Large green button.
            //
            // We draw it manually instead of GH_Capsule.CreateTextCapsule
            // so its appearance is clearly different from a normal GH button.
            // ---------------------------------------------------------------

            Color buttonColor =
                Color.FromArgb(
                    72,
                    145,
                    68);


            Color hoverLikeBorder =
                Color.FromArgb(
                    45,
                    95,
                    45);


            Color textColor =
                Color.White;


            // ---------------------------------------------------------------
            // Background
            // ---------------------------------------------------------------

            using (SolidBrush brush =
                new SolidBrush(
                    buttonColor))
            {
                graphics.FillRectangle(
                    brush,
                    _buttonBounds);
            }


            // ---------------------------------------------------------------
            // Border
            // ---------------------------------------------------------------

            using (Pen borderPen =
                new Pen(
                    hoverLikeBorder,
                    1.5f))
            {
                graphics.DrawRectangle(
                    borderPen,
                    _buttonBounds.X,
                    _buttonBounds.Y,
                    _buttonBounds.Width,
                    _buttonBounds.Height);
            }


            // ---------------------------------------------------------------
            // Button text
            // ---------------------------------------------------------------

            using (System.Drawing.Font font =
                new System.Drawing.Font(
                    "Segoe UI",
                    10f,
                    FontStyle.Bold))
            {
                using (SolidBrush textBrush =
                    new SolidBrush(
                        textColor))
                {
                    using (StringFormat format =
                        new StringFormat())
                    {
                        format.Alignment =
                            StringAlignment.Center;


                        format.LineAlignment =
                            StringAlignment.Center;


                        graphics.DrawString(
                            "BAKE PARKING",
                            font,
                            textBrush,
                            _buttonBounds,
                            format);
                    }
                }
            }
        }


        // ====================================================================
        // MOUSE INPUT
        // ====================================================================

        public override GH_ObjectResponse RespondToMouseDown(
            GH_Canvas sender,
            GH_CanvasMouseEvent e)
        {
            if (e.Button ==
                    MouseButtons.Left &&
                _buttonBounds.Contains(
                    e.CanvasLocation))
            {
                PreviewParkingResult component =
                    Owner as PreviewParkingResult;


                if (component != null)
                {
                    component.BakeRequested =
                        true;


                    // Trigger one GH solution.
                    //
                    // BakeRequested is immediately consumed inside
                    // SolveInstance so this only bakes once.
                    component.ExpireSolution(
                        true);
                }


                return
                    GH_ObjectResponse.Handled;
            }


            return
                base.RespondToMouseDown(
                    sender,
                    e);
        }
    }
}