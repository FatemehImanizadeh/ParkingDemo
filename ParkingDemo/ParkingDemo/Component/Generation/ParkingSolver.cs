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
using ParkingDemo.Utils;
using System.Threading;

namespace ParkingDemo
{
    public class ParkingSolver : GH_Component
    {
        public Optimization Optimizaton = new Optimization();
        public GenerationCollection Generations = new GenerationCollection();
        public ParkingSolver()
          : base("PathFinder", "PathFinder",
              "computes the optional paths to access lots in parking and outputs parking informaiton for visualization",
              "ParkingDemo", "Generation")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("ResetGeneration", "Reset", "reset previous generation so than a new parking collection would be created based on upcoming iterations", GH_ParamAccess.item);
           /* pManager.AddIntegerParameter("pathstartcell", "startcell", "row and column information of parking entrance cell based on ramp information", GH_ParamAccess.list);
            pManager.AddMatrixParameter("planmatrix", "planmatrix", "the matrix corresponding to plan while each item represents a function in parking", GH_ParamAccess.item);
            pManager.AddRectangleParameter("plancells", "cells", "plan cells of size 5*5 m in datatree that the firs element represents cellrow and the second represents cell column", GH_ParamAccess.tree);
            pManager.AddPointParameter("grid", "grid", "rectangular grid corresponding to plan bounding box(first element of grid paths representscorresponding row in plan matrix ", GH_ParamAccess.tree);*/
            pManager.AddGenericParameter("Parking", "P", "parking basic data greated from the outline grid component", GH_ParamAccess.item);

        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
           /* pManager.AddPointParameter("mainpathpoints", "mainpathpts", "gives the center point of all path cells in parking", GH_ParamAccess.tree);
            pManager.AddTransformParameter("lotinformation", "lotinfo", "gives the parking lots information in plan", GH_ParamAccess.tree);
            pManager.AddGenericParameter("parkingpaths", "parkingpaths", "parkingpaths", GH_ParamAccess.list);
            pManager.AddMatrixParameter("planmatrix", "planmatrix", "planmatrix", GH_ParamAccess.item);*/
            pManager.AddGenericParameter("Parking", "Parking", "Parking", GH_ParamAccess.item);
            pManager.AddGenericParameter("GenerationCollection", "GenerationCollection", "GenerationCollection", GH_ParamAccess.list);
            // pManager.AddNumberParameter("Score", "Score", "Score", GH_ParamAccess.item);
        }          
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var generationReset = false;
            DA.GetData(0, ref generationReset);
            var parking = new Parking();
            DA.GetData(1,  ref parking);
            if(parking != null)
            {
                var grid = parking.PlanPointsGrid;
                var cellstart = parking.PathStartCell;
                var cellEnd = parking.RampEndCell;
                var firstpathcell = new int[2] { cellstart.row, cellstart.col };
                var plantomatrix = parking.PlanMatrix;
                var cells = parking.PlanCells;
                ///
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
                while (ParkingUtils.emptycell(plantomatrix) > Math.Max(cellscount / 10, 4) && iteration < 400 && startcellfindingattemt < 100)
                {
                    iteration++;
                    ParkingUtils.pathfinder(plantomatrix, firstpathcell, grid, ref cartransforms, mainpathpts, ref pathindex, ref currentpathitemcount, pathptsloc, ref startcellfindingattemt, ref parkingpaths);
                }
                mainPathConnection.CreateConnectionPath(plantomatrix, grid, parkingpaths, cartransforms, mainpathpts);
                /*  DA.SetDataTree(0, mainpathpts);
                  DA.SetDataTree(1, cartransforms);
                  DA.SetDataList(2, parkingpaths);
                  DA.SetData(3, plantomatrix);*/
                /////
                var emptyCells = ParkingUtils.emptycell(plantomatrix);
                parking.PlanMatrix = plantomatrix;
                parking.CarTransforms = cartransforms;
                parking.PathPoints = mainpathpts;
                parking.PathCellNumber = mainpathpts.DataCount;
                parking.LotNumber = cartransforms.DataCount;
                parking.EmptyCells = emptyCells;
                var num2 = grid.DataCount;
                parking.PlanCellNum = num2;
                if (generationReset)
                    Generations = new GenerationCollection();
                Optimization.CalculateOptimizationScore(parking);
                Generations.parkings.Add(parking);
                DA.SetData(0, parking);
                DA.SetData(1, Generations);
            }
        }
        protected override System.Drawing.Bitmap Icon => ParkingDemo.Properties.Resources.ParkingSolver;
        public override Guid ComponentGuid
        {
            get { return new Guid("02AE87AE-E0AA-4D5C-B1C4-8213A002D4EE"); }
        }
    }
}