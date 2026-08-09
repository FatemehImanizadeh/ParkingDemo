using System;
using System.Drawing;
using System.Windows.Forms;

using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

using ParkingDemo.Utils;



namespace ParkingDemo
{
    // ========================================================================
    // COMPONENT
    // ========================================================================

    public class BakeParkingResult : GH_Component
    {
        /// <summary>
        /// Becomes true only when the user clicks the BAKE PARKING button.
        /// SolveInstance consumes it once and immediately resets it.
        /// </summary>
        internal bool BakeRequested { get; set; } = false;


        public BakeParkingResult()
            : base(
                "Bake Parking Result",
                "BakePark",
                "Bakes the generated parking visualization into Rhino, including " +
                "cars, graded cells, main circulation path, excluded cells, " +
                "entrance cell and parking boundary wall.",
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


            // Read Parking
            if (!DA.GetData(0, ref parking))
                return;


            // Read Car Block
            if (!DA.GetData(1, ref carBlockGoo))
                return;


            if (parking == null)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "Parking input is empty.");

                return;
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
                carBlockGoo);
        }


        // ====================================================================
        // MAIN BAKE ORCHESTRATOR
        // ====================================================================

        private void BakeParking(
            Parking parking,
            IGH_Goo carBlockGoo)
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


            // ---------------------------------------------------------------
            // Resolve car block
            // ---------------------------------------------------------------

            InstanceDefinition carBlockDefinition =
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


            // ===============================================================
            // CREATE / FIND LAYERS
            // ===============================================================

            int carsLayerIndex =
                EnsureChildLayer(
                    doc,
                    "Parking",
                    "Cars");


            int gradientLayerIndex =
                EnsureChildLayer(
                    doc,
                    "Parking",
                    "Gradient Cells");


            int pathLayerIndex =
                EnsureChildLayer(
                    doc,
                    "Parking",
                    "Main Path");


            int excludedLayerIndex =
                EnsureChildLayer(
                    doc,
                    "Parking",
                    "Excluded Cells");


            int entranceLayerIndex =
                EnsureChildLayer(
                    doc,
                    "Parking",
                    "Entrance Cell");


            int wallLayerIndex =
                EnsureChildLayer(
                    doc,
                    "Parking",
                    "Walls");


            // ===============================================================
            // 1. CARS
            // ===============================================================

            BakeCars(
                doc,
                parking,
                carBlockDefinition,
                carsLayerIndex);


            // ===============================================================
            // 2. GRADED PARKING CELLS
            //
            // CellsWithGrade:
            //
            // Branch 0 → Grade 0
            // Branch 1 → Grade 1
            // Branch 2 → Grade 2
            // ...
            //
            // BakeResultsUtils is responsible for converting the grade
            // to the yellow → orange → red visualization.
            // ===============================================================

            BakeResultsUtils.BakeGradientCells(
                doc,
                parking,
                gradientLayerIndex);


            // ===============================================================
            // 3. CONTINUOUS MAIN PATH
            // ===============================================================

            // Width in Rhino model units.
            //
            // If your Rhino file uses meters:
            // 0.30 = 30 cm
            //
            // You can change this independently from the cell size.
            const double pathWidth = 0.30;


            BakeResultsUtils.BakeContinuousPath(
                doc,
                parking,
                pathLayerIndex,
                pathWidth);


            // ===============================================================
            // 4. EXCLUDED CELLS
            // ===============================================================

            BakeResultsUtils.BakeExcludedCells(
                doc,
                parking,
                excludedLayerIndex);


            // ===============================================================
            // 5. ENTRANCE CELL
            //
            // Light blue / cyan visualization.
            // ===============================================================

            BakeResultsUtils.BakeEntranceCell(
                doc,
                parking,
                entranceLayerIndex);


            // ===============================================================
            // 6. PARKING OUTLINE WALL
            //
            // 20 cm wall wh en Rhino model units = meters.
            // ===============================================================

            const double wallThickness = 0.20;


            BakeResultsUtils.BakeParkingWall(
                doc,
                parking,
                wallLayerIndex,
                wallThickness);


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
        // CUSTOM UI
        // ====================================================================

        public override void CreateAttributes()
        {
            m_attributes =
                new BakeParkingResultAttributes(
                    this);
        }


        // ====================================================================
        // ICON
        // ====================================================================

        protected override Bitmap Icon
        {
            get
            {
                return null;

                // Later you can replace this with:
                //
                // return Properties.Resources.YourBakeIcon;
            }
        }


        // ====================================================================
        // GUID
        // ====================================================================

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid(
                    "67F0D003-5A65-4B39-B730-FC89E3028011");
            }
        }  
}



    // ========================================================================
    // CUSTOM COMPONENT ATTRIBUTES
    // ========================================================================

    public class BakeParkingResultAttributes :
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


        public BakeParkingResultAttributes(
            BakeParkingResult owner)
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
                BakeParkingResult component =
                    Owner as BakeParkingResult;


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