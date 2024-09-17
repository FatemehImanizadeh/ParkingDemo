using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.Geometry;

namespace ParkingDemo.Component.Analyze
{
    public class GetPathLength : GH_Component
    {
        
        public GetPathLength()
          : base("GetPathLength", "PL",
              "max length by the cars to get to their lot",
              "ParkingDemo", "Analyse")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking", "Parking", "Parking", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Max Length", "ML", "max length", GH_ParamAccess.item);
            pManager.AddNumberParameter("Avg Length", "Avg", "average length", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var parking = new Parking();
            DA.GetData(0, ref parking);
            var maxLength = PathLength.GetPathLength(parking); 
            DA.SetData(0, maxLength);
            DA.SetData(1, (float)parking.TotalLengthGrade/parking.LotNumber); 
        }
        protected override System.Drawing.Bitmap Icon => null; 
       
        public override Guid ComponentGuid =>new Guid("FAE325E6-BA36-45CF-BCC5-12E76F7315AE"); 
      
    }
}