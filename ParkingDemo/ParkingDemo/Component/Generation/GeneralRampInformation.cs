using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ParkingDemo.Component.Generation
{
    public class GeneralRampInformation : GH_Component
    {
        public GeneralRampInformation()
          : base("GeneralRampInformation", "Nickname",
              "Description",
             "ParkingDemo", "preinfo")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("RampTypes", "RT", "all predefined ramp types for the project", GH_ParamAccess.tree);
            pManager.AddTransformParameter("RampOrientations", "RO", " all predefined ramp orientations for the project", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var ramptype = ParkingUtils.Ramp.ramptypes();
            var rampOrientations = ParkingUtils.Ramp.ramporientations();
            DA.SetDataTree(0, ramptype);
            DA.SetDataList(1, rampOrientations);
        }
        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid => new Guid("ABDCE035-8B1B-4742-AF66-282E713CC1FD"); 
       
    }
}