using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.Geometry;

namespace ParkingDemo.components.Preview
{
    

    public class PrintResult : GH_Component
    {
        List<int> existingIndices = new ParkingPreview().ExistingIndices;
       
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
            if (existingIndices.Count == 0) existingIndices.Add(0);
            var index = existingIndices.Max () + 1 ;
            existingIndices.Add(index);
            var resetPlan = false; 
            var bRec = new Rectangle3d(Plane.Unset, 10, 10);
            DA.GetData(0, ref bRec);
            DA.GetData(1, ref resetPlan); 
            ParkingPreview.PrintResult("testname", index, bRec,  resetPlan); 
        }

        protected override System.Drawing.Bitmap Icon => null;

        public override Guid ComponentGuid
        {
            get { return new Guid("F21AEDDE-4A45-4165-A23A-16FE59852FA0"); }
        }
    }
}

