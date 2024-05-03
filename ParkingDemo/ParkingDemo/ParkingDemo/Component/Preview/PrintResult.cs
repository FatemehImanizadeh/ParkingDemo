using System;
using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.Geometry;

namespace ParkingDemo.components.Preview
{
    public class PrintResult : GH_Component
    {
        public PrintResult()
          : base("PrintResult", "PrintResult",
                  "PrintResult",
              "ParkingDemo", "Preview")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddRectangleParameter("Outline BoundingRectangle", "BRec", "outline bounding rectangle",
                GH_ParamAccess.item);
            pManager.AddBooleanParameter("Reset Preview sheet", "Reset Preview", "reset preview size for export image results: if the plan area changes set the reset value to true to create a new layout of your plan ", GH_ParamAccess.item);
            
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }
        protected override void SolveInstance(IGH_DataAccess DA)

        {
            var resetPlan = false; 
            var bRec = new Rectangle3d(Plane.Unset, 10, 10);
            DA.GetData(0, ref bRec);
            DA.GetData(1, ref resetPlan); 
            var indx = new Random().Next(0,100);
            
            ParkingPreview.PrintResult("testname", indx, bRec,  resetPlan); 
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid
        {
            get { return new Guid("F21AEDDE-4A45-4165-A23A-16FE59852FA0"); }
        }
    }
}

