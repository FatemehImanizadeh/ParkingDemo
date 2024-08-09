using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.Geometry;

namespace ParkingDemo.Component.Analyze
{
    public class SelectParking : GH_Component
    {
        public SelectParking()
          : base("SelectParking", "SP",
              "select parking from generation collection based on parking guid",
               "ParkingDemo", "Analyse")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Generations", "Gs", "a collection of parking generations created in a single process", GH_ParamAccess.item);
            pManager.AddGenericParameter("ParkingId", "PId", "parking id to select the parking form the generation parking ids", GH_ParamAccess.item); 
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking", "P", "selected parking", GH_ParamAccess.item); 
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var generations = new GenerationCollection();
            DA.GetData(0, ref generations);
            var parkingId = new Guid();
            DA.GetData(1, ref parkingId);
            var parking = new Parking();
            foreach(var p in generations.parkings)
            {
               if (p.ParkingID == parkingId ) { DA.SetData(0, p);break; };
            }
        }
        protected override System.Drawing.Bitmap Icon => ParkingDemo.Properties.Resources.SelectParking;
    
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7D90FC55-9AB0-45D7-AAAE-FEB5ADECB799"); }
        }
    }
}