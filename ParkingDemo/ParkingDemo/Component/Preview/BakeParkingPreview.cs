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
    public class BakeParkingPreview : GH_Component
    {
        // Set to true by the custom attributes when the "Bake Parking" button is clicked.
        // SolveInstance reads it, runs the bake once, then resets it so it only fires
        // on an actual click and not on every normal solve.
        internal bool BakeRequested { get; set; } = false;

        public BakeParkingPreview()
          : base("Bake Parking Preview", "BakePark",
              "Builds the plan preview of a parking (cars, path cells, path lines) and bakes it into the Rhino document",
              "ParkingDemo", "Analyse")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking", "P", "Parking to preview / bake", GH_ParamAccess.item);
            // Generic on purpose: Grasshopper's native "Block Instance" param wraps its own
            // Goo type that does NOT satisfy a strict Param_Geometry cast to GeometryBase
            // (that's the "Invalid cast: Block Instance -> GeometryBase" error). Generic
            // accepts it - and a plain referenced geometry or a Guid - and we unwrap it
            // ourselves in ResolveCarBlockDefinition.
            pManager.AddGenericParameter("Car Block", "Blk", "Double-car block instance (reference an existing block placed in Rhino) used to place a car at each occupied lot", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            // Intentionally empty for now - this component's main job is the bake side effect.
            // We can add an output (e.g. a "Baked" bool, or the GUIDs of baked objects) once
            // the baking logic itself is in place.
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Parking parking = null;
            DA.GetData(0, ref parking);

            IGH_Goo carBlockGoo = null;
            DA.GetData(1, ref carBlockGoo);

            if (parking == null) return;

            if (BakeRequested)
            {
                BakeRequested = false; // consume the click so it only bakes once
                BakeParking(parking, carBlockGoo);
            }
        }

        private void BakeParking(Parking parking, IGH_Goo carBlockGoo)
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return;

            var idef = ResolveCarBlockDefinition(carBlockGoo, doc);
            if (idef == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Car Block input must reference an existing block instance placed in the Rhino document.");
                return;
            }

            int carsLayerIndex = EnsureChildLayer(doc, "Parking", "Cars");
            int cellsLayerIndex = EnsureChildLayer(doc, "Parking", "Path Cells");
            int linesLayerIndex = EnsureChildLayer(doc, "Parking", "Path Lines");

            // Cars: one block instance per car transform.
            if (parking.CarTransforms != null)
            {
                var attributes = new ObjectAttributes { LayerIndex = carsLayerIndex };
                foreach (var branch in parking.CarTransforms.Branches)
                    foreach (var xform in branch)
                        doc.Objects.AddInstanceObject(idef.Index, xform, attributes);
            }

            // Path cells: the graded path cells, baked as closed rectangle curves.
            if (parking.CellsWithGrade != null)
            {
                var attributes = new ObjectAttributes { LayerIndex = cellsLayerIndex };
                foreach (var branch in parking.CellsWithGrade.Branches)
                    foreach (var rec in branch)
                        doc.Objects.AddCurve(rec.ToNurbsCurve(), attributes);
            }

            // Path main lines.
            if (parking.PathLines != null)
            {
                var attributes = new ObjectAttributes { LayerIndex = linesLayerIndex };
                foreach (var line in parking.PathLines)
                    doc.Objects.AddLine(line, attributes);
            }

            doc.Views.Redraw();
        }

        // Ensures a "parentName::childName" layer pair exists and returns the child's index,
        // creating the parent first if needed. Re-uses existing layers on repeated bakes
        // instead of duplicating them.
        private static int EnsureChildLayer(RhinoDoc doc, string parentName, string childName)
        {
            int parentIndex = doc.Layers.FindByFullPath(parentName, -1);
            if (parentIndex < 0)
                parentIndex = doc.Layers.Add(new Layer { Name = parentName });

            var parent = doc.Layers[parentIndex];

            string childFullPath = parentName + "::" + childName;
            int childIndex = doc.Layers.FindByFullPath(childFullPath, -1);
            if (childIndex < 0)
                childIndex = doc.Layers.Add(new Layer { Name = childName, ParentLayerId = parent.Id });

            return childIndex;
        }

        // Unwraps whatever Goo the "Car Block" input arrives as into an InstanceDefinition.
        // Handles three cases, in order:
        //   1. Grasshopper's native "Block Instance" param - its Goo boxes an
        //      Rhino.DocObjects.InstanceObject (or, on some versions, an
        //      InstanceReferenceGeometry) in a "Value" property, reached via reflection
        //      since the exact wrapper type isn't public API we can reference directly.
        //   2. Any referenced geometry Goo - IGH_GeometricGoo exposes ReferenceID, the GUID
        //      of the Rhino object it was picked from.
        //   3. A plain Guid (or anything CastTo<Guid> can produce) pointing at the instance.
        // In cases 2 and 3 we look the object up in the document and read its
        // InstanceDefinition off the resulting InstanceObject.
        private static InstanceDefinition ResolveCarBlockDefinition(IGH_Goo goo, RhinoDoc doc)
        {
            if (goo == null) return null;

            object value = goo;
            var valueProp = goo.GetType().GetProperty("Value");
            if (valueProp != null)
            {
                var unwrapped = valueProp.GetValue(goo);
                if (unwrapped != null) value = unwrapped;
            }

            if (value is InstanceObject instObj)
                return instObj.InstanceDefinition;

            if (value is InstanceReferenceGeometry instRef)
                return doc.InstanceDefinitions.FindId(instRef.ParentIdefId);

            Guid id = Guid.Empty;
            if (goo is IGH_GeometricGoo geoGoo && geoGoo.ReferenceID != Guid.Empty)
                id = geoGoo.ReferenceID;
            else if (goo.CastTo(out Guid castId))
                id = castId;

            if (id != Guid.Empty)
            {
                var rhinoObj = doc.Objects.Find(id) as InstanceObject;
                if (rhinoObj != null)
                    return rhinoObj.InstanceDefinition;
            }

            return null;
        }

        public override void CreateAttributes()
        {
            m_attributes = new BakeParkingPreviewAttributes(this);
        }

        protected override Bitmap Icon => null;

        public override Guid ComponentGuid => new Guid("d4759445-c0dd-412d-b8a5-41acd73bc5e1");
    }

    // Custom attributes: draws an extra "Bake Parking" button under the component body
    // and turns a click on it into BakeRequested = true + ExpireSolution(true).
    public class BakeParkingPreviewAttributes : GH_ComponentAttributes
    {
        private RectangleF _buttonBounds;

        public BakeParkingPreviewAttributes(BakeParkingPreview owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();

            var bounds = GH_Convert.ToRectangle(Bounds);
            bounds.Height += 22; // room for the button strip
            Bounds = bounds;

            _buttonBounds = new RectangleF(Bounds.X + 4, Bounds.Bottom - 20, Bounds.Width - 8, 18);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                var capsule = GH_Capsule.CreateTextCapsule(_buttonBounds, _buttonBounds, GH_Palette.Black, "Bake Parking");
                capsule.Render(graphics, Selected, Owner.Locked, false);
                capsule.Dispose();
            }
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && _buttonBounds.Contains(e.CanvasLocation))
            {
                var comp = (BakeParkingPreview)Owner;
                comp.BakeRequested = true;
                comp.ExpireSolution(true);
                return GH_ObjectResponse.Handled;
            }

            return base.RespondToMouseDown(sender, e);
        }
    }
}