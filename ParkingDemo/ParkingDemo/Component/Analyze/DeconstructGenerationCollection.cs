using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.Geometry;

namespace ParkingDemo.Component.Analyze
{
    public class DeconstructGenerationCollection : GH_Component
    {
        public DeconstructGenerationCollection()
          : base("DeconstructGenerationCollection", "DGC",
              "gets all parking guids inside a parking generation ",
                "ParkingDemo", "parking")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Generation Collection", "GC", "single generation collection for a parking", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Ids", "Ids", "Ids", GH_ParamAccess.list);
            pManager.AddNumberParameter("Scores", "Scores", "Scores", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var generations = new GenerationCollection();
            DA.GetData(0, ref generations);
            var ids = new List<Guid>();
            var scores = new List<double>();
            foreach(var parking in generations.parkings)
            {
                ids.Add(parking.ParkingID);
                scores.Add(parking.Score);
            }
            DA.SetDataList(0 , ids);
            DA.SetDataList(1 , scores);
        }
        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid  => new Guid("E6583702-C41A-4A28-BD9C-5ABCEE96F520"); }
        
    }
