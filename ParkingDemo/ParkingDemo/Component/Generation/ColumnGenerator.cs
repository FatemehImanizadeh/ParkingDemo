using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.ApplicationSettings;
using Rhino.Geometry;
using Rhino.Input.Custom;

namespace ParkingDemo.Component.Generation
{
    public class ColumnGenerator : GH_Component
    {
        public ColumnGenerator()
          : base("ColumnGridGenerator", "CGG",
              "generates the structure based on parking data",
                "ParkingDemo", "Generation")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking", "P", "parking", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("H", "H", "H", GH_ParamAccess.list);
            pManager.AddNumberParameter("V", "V", "V", GH_ParamAccess.list);
            pManager.AddGenericParameter("Parking", "P", "parking with grids", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var parking = new Parking();
            DA.GetData(0, ref parking);
            var mainpathpts = parking.PathPoints;
            var pathptslist = new DataTree<Point3d>(mainpathpts);
            pathptslist.Flatten();
            var pathptslistnew = pathptslist.Branch(0).Distinct().ToList();
            List<double> horizontalException;
            List<double> verticalException;
            ColumnGrid.GridException(pathptslistnew, out horizontalException, out verticalException);
            verticalException.Sort();
            horizontalException.Sort();
           var vEx = verticalException.Distinct().ToList();
           var hEx = horizontalException.Distinct().ToList();
            var outline = parking.Outline;
            var gridCoords = ColumnGrid.GridCoordinates(outline, verticalException, horizontalException);
            var hcord = gridCoords[0];
            var vcoord = gridCoords[1];
            var finalgrid = new List<List<double>>();
            finalgrid = ColumnGrid.GridGenerator2(gridCoords, ColumnGrid.OutlineGridCoordinates(outline), 7.5,7.5);
            finalgrid[0].Sort();
            finalgrid[1].Sort();
            var hgrid = finalgrid[0].Distinct().ToList();
            var vgrid = finalgrid[1].Distinct().ToList();
            DA.SetDataList(0, hgrid);
            DA.SetDataList(1, vgrid);
            
        }
        protected override System.Drawing.Bitmap Icon => ParkingDemo.Properties.Resources.ColumnGenerator;
        public override Guid ComponentGuid =>  new Guid("5C7545FD-F60D-4418-A0E0-08ECAAF5CBCC"); 
        
    }
}