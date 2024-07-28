using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using ParkingDemo.Utils;

namespace ParkingDemo.Component.Analyze
{
    public class DeconstrucParking : GH_Component
    {
        public DeconstrucParking()
          : base("DeconstrucParking", "DP",
              "deconstruct a parking to extract all its features for plan visualization",
              "ParkingDemo", "parking")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking", "Parking", "Parking", GH_ParamAccess.item);

        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Score", "Score", "Score", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var parking = new Parking();
            DA.GetData(0, ref parking);
            var score = parking.Score;
            var lotCount = parking.LotNumber;
            DA.SetData(0, score);
        }
        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid
        {
            get { return new Guid("F9866FC2-9736-4AEC-A651-E538FE7EB3F5"); }
        }
    }
}