using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;


namespace ParkingDemo
{
    public class ParkingPathsTest : GH_Component
    {
        public ParkingPathsTest()
          : base("ParkingPaths", "parkingpaths",
              "to test the created parkinfo class in code performance",
              "ParkingDemo", "parking")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("output", "output", "output", GH_ParamAccess.item); 
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var cells  = new List<ParkingUtils.PathInfo.Cell>();

            var parkingpath1 = new ParkingUtils.PathInfo.ParkingPath();
            var parkingpath2 = new ParkingUtils.PathInfo.ParkingPath(cells, 0, ParkingUtils.PathInfo.PathType.ConnectionPath); 
            cells.Add(new ParkingUtils.PathInfo.Cell(0, 0, parkingpath1)); 
            cells.Add(new ParkingUtils.PathInfo.Cell(0, 1, parkingpath1));
            cells.Add(new ParkingUtils.PathInfo.Cell(0, 2, parkingpath1));
            parkingpath1.cells = cells; 
            var str = parkingpath2.ToString();
            DA.SetData(0,  str); 
        }
        protected override System.Drawing.Bitmap Icon => null; 

        public override Guid ComponentGuid
        {
            get { return new Guid("233D0023-E15D-404D-8C04-3DC811B6B948"); }
        }
    }
}