using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GH_IO.Types;
using Rhino.Render;
using System.Diagnostics;
namespace ParkingDemo
{
    public class PathFinder : GH_Component
    {
        public PathFinder()
          : base("PathFinder", "PathFinder",
              "computes the optional paths to access lots in parking and outputs parking informaiton for visualization",
              "ParkingDemo", "parking")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("iteration", "iter", "the iteration for locating parking lots in plan", GH_ParamAccess.item);
            pManager.AddIntegerParameter("pathstartcell", "startcell", "row and column information of parking entrance cell based on ramp information", GH_ParamAccess.list);
            pManager.AddMatrixParameter("planmatrix", "planmatrix", "the matrix corresponding to plan while each item represents a function in parking", GH_ParamAccess.item);
            pManager.AddRectangleParameter("plancells", "cells", "plan cells of size 5*5 m in datatree that the firs element represents cellrow and the second represents cell column", GH_ParamAccess.tree);
            pManager.AddPointParameter("grid", "grid", "rectangular grid corresponding to plan bounding box(first element of grid paths representscorresponding row in plan matrix ", GH_ParamAccess.tree);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("mainpathpoints", "mainpathpts", "gives the center point of all path cells in parking", GH_ParamAccess.tree);
            pManager.AddTransformParameter("lotinformation", "lotinfo", "gives the parking lots information in plan", GH_ParamAccess.tree);
            pManager.AddGenericParameter("parkingpaths", "parkingpaths", "parkingpaths", GH_ParamAccess.list);
            pManager.AddMatrixParameter("planmatrix", "planmatrix", "planmatrix", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var plantomatrix = new Matrix(2, 2);
            GH_Structure<Grasshopper.Kernel.Types.GH_Rectangle> ghcells = new GH_Structure<GH_Rectangle>();
            GH_Structure<GH_Point> ghgrid = new GH_Structure<GH_Point>();
            var parkingstartcell = new List<int>();
            int iter = 3;
            DA.GetData(0, ref iter);
            DA.GetDataList(1, parkingstartcell);
            DA.GetData(2, ref plantomatrix);
            DA.GetDataTree<GH_Rectangle>(3, out ghcells);
            DA.GetDataTree<GH_Point>(4, out ghgrid);
            var firstpathcell = new int[2] { parkingstartcell[0], parkingstartcell[1] };
            var grid = new DataTree<Point3d>();
            for (int i = 0; i < ghgrid.Paths.Count; i++)
            {
                var path = ghgrid.Paths[i];
                for (int j = 0; j < ghgrid.get_Branch(path).Count; j++)
                {
                    var ghpt = ghgrid.get_DataItem(path, j);
                    grid.Add(new Point3d(ghpt.Value), path);
                }
            }
            var cells = new DataTree<Rectangle3d>();
            for (int i = 0; i < cells.Paths.Count; i++)
            {
                var path = ghcells.Paths[i];
                for (int j = 0; j < ghcells.get_Branch(path).Count; j++)
                {
                    var ghcell = ghcells.get_DataItem(path, j);
                    var rec = ghcell.Value;
                    cells.Add(rec, path);
                }
            }
            var cellscount = cells.BranchCount;
            var cartransforms = new DataTree<Transform>();
            var mainpathpts = new DataTree<Point3d>();
            var pathindex = 0;
            var currentpathitemcount = 0;
            var pathptsloc = new DataTree<int[]>();
            int startcellfindingattemt = 0;
            var parkingpath = new ParkingUtils.PathInfo.ParkingPath();
            var parkingpaths = new List<ParkingUtils.PathInfo.ParkingPath>();
            // bool gno = GH_Convert(ghgrid, ref grid, GH_Conversion.Both);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            ParkingUtils.pathfinder(plantomatrix, firstpathcell, grid, ref cartransforms, mainpathpts, ref pathindex, ref currentpathitemcount, pathptsloc, ref startcellfindingattemt, ref parkingpaths);
            int iteration = 0;
            var ts = stopwatch.ElapsedMilliseconds.ToString();
            Debug.WriteLine(ts + "ms");
            while (ParkingUtils.emptycell(plantomatrix) > Math.Max(cellscount / 10, 4) && iteration <300 && startcellfindingattemt < 100)
            {
                iteration++;
                ParkingUtils.pathfinder(plantomatrix, firstpathcell, grid, ref cartransforms, mainpathpts, ref pathindex, ref currentpathitemcount, pathptsloc, ref startcellfindingattemt, ref parkingpaths);
            }
          ParkingUtils.mainPathConnection.CreateConnectionPath(plantomatrix, grid, parkingpaths, cartransforms, mainpathpts);
            DA.SetDataTree(0, mainpathpts);
            DA.SetDataTree(1, cartransforms);
            DA.SetDataList(2, parkingpaths);
            DA.SetData(3, plantomatrix);


        }


        protected override System.Drawing.Bitmap Icon => null;//Resources.connection1_;
        public override Guid ComponentGuid
        {
            get { return new Guid("02AE87AE-E0AA-4D5C-B1C4-8213A002D4EE"); }
        }
    }
}