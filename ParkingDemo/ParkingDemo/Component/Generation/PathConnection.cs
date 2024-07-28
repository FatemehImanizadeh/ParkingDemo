using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ParkingDemo.Component.Generation
{
    public class PathConnection : GH_Component
    {
        public PathConnection()
          : base("PathConnection", "PathConnection",
              "get location of 2 cells and gives the lotgain value based on nbasedmethod",
              "ParkingDemo", "preinfo")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("n1", "n1", "cell1row", GH_ParamAccess.item);
            pManager.AddIntegerParameter("m1", "m1", "cell1col", GH_ParamAccess.item);
            pManager.AddIntegerParameter("n2", "n2", "cell2row", GH_ParamAccess.item);
            pManager.AddIntegerParameter("m2", "m2", "cell2col", GH_ParamAccess.item);
            pManager.AddMatrixParameter("matrix", "mtx", "plan information matrix", GH_ParamAccess.item);

        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("lotgainnum", "lotgain", "the total number of new lots according to bridge btw last paths(the value may be negative if there are more alternatives lost)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("ispathpossible", "pathpossible", "it false there is ramp in one or more of the bridge cells or there are cells outside the plan in this bridge path and the path is not pissible", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int n1 = 0; int m1 = 0; int n2 = 0; int m2 = 0;
            var mtx = new Matrix(2, 2);
            DA.GetData(0, ref n1);
            DA.GetData(1, ref m1);
            DA.GetData(2, ref n2);
            DA.GetData(3, ref m2);
            DA.GetData(4, ref mtx);
            bool ispathpossible;
            var parkingPath = new ParkingUtils.PathInfo.ParkingPath();
            var p1 = new ParkingUtils.PathInfo.Cell(n1, m1, parkingPath);
            var p2 = new ParkingUtils.PathInfo.Cell(n2, m2, parkingPath);
            var lotgain = ParkingUtils.mainPathConnection.LotGain(p1, p2, mtx, false, out ispathpossible);
            DA.SetData(0, lotgain);
            DA.SetData(1, ispathpossible);
        }
        protected override System.Drawing.Bitmap Icon => null;
        public override Guid ComponentGuid
        {
            get { return new Guid("1B1404CD-D890-45A1-AF51-7F61FE9599F3"); }
        }
    }
}

