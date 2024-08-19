using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.ApplicationSettings;
using Rhino.Geometry;
using Rhino.PlugIns;

namespace ParkingDemo.Component.Analyze
{
    public class SetOptimizationWeights : GH_Component
    {
        public SetOptimizationWeights()
          : base("SetOptimizationWeights", "SOW",
              "set weight for all accessable parameters in optimizatoin for each parking in generation Collection",
             "ParkingDemo", "Analyse")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking Colleciton", "PC", "parking collection to reparametrize its optimization based on user input data", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number of Parkings", "NP", " wight for number of parkings(a value between 0 and 1", GH_ParamAccess.item);
            pManager.AddNumberParameter("Total Path Length", "PL", " wight for total parking path lengths(a value between 0 and 1", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum Non-functional  Cells", "NC", " wight for number of cells with no functionality(a value between 0 and 1", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking Colleciton", "PC", "parking collection to reparametrize its optimization based on user input data", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var generations = new GenerationCollection();
            DA.GetData(0, ref generations);
            var parkingNumW = 1.0;
            var pathLenW = .2;
            var nonFuncW = 1.0;
            DA.GetData(1, ref parkingNumW); 
            DA.GetData(2, ref pathLenW);
            DA.GetData(3, ref nonFuncW);
            var optimization = new Optimization();
            optimization.LotNumW = parkingNumW;
            optimization.PathLenW = pathLenW;
            optimization.NonFuncW = nonFuncW;
            Parallel.ForEach(generations.parkings, parking => { Optimization.OptimizationFunction(optimization, parking);  });
            DA.SetData(0, generations);
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("B36E7B5B-3C76-44F8-8C3F-242A5CDF76DF"); }
        }
    }
}