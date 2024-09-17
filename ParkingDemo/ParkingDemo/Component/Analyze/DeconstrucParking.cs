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
              "ParkingDemo", "Analyse")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking", "Parking", "Parking", GH_ParamAccess.item);

        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Score", "Score", "Score", GH_ParamAccess.item);
            pManager.AddMatrixParameter("Plan Matrix", "PM", "parking plan matrix", GH_ParamAccess.item);
            pManager.AddPointParameter("PathPoints", "Ppts", "parking path points", GH_ParamAccess.tree);
            pManager.AddTransformParameter("LotInformations", "LI", "parking lots information", GH_ParamAccess.tree);
            pManager.AddRectangleParameter("Cells", "cells", "all cellular regions of plane with 5*5 m size", GH_ParamAccess.tree);
            pManager.AddPointParameter("Side Cells Address", "side cells", "center of cells on the borders of the plan: each path contains cells of one side", GH_ParamAccess.tree);
            pManager.AddRectangleParameter("EXcluded Cells", "EC", "excluded cells in design process", GH_ParamAccess.list);
            pManager.AddRectangleParameter("Parking Entrance Cell", "PE", "parking entrance cell", GH_ParamAccess.item); 
            pManager.AddCurveParameter("Plan Outline", "O", "plan outline", GH_ParamAccess.item);
            pManager.AddNumberParameter("RampInfo", "rampinfo", " ramp information: index0: side, index1: index of ramp start cell on selected side, index2: ramp type, index3: ramp orientation", GH_ParamAccess.list);
            pManager.AddLineParameter("PathLines", "PL", "parking path lines", GH_ParamAccess.list);
            pManager.AddNumberParameter("Generation Time", "GT", "generation time", GH_ParamAccess.item) ;
            pManager.AddNumberParameter("Path Direction Shift", "PDS", "path direction shift", GH_ParamAccess.item);
            pManager.AddNumberParameter("Avg Direction Shift", "ADS", "average path direction shift", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Cells With Grade", "CWG", "path cells with grade", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Lot Count", "LC", "lot count", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var parking = new Parking();
            DA.GetData(0, ref parking);
            var score = parking.Score;
            var planMatrix = parking.PlanMatrix;
            var pathPts = parking.PathPoints;
            var lotInfo = parking.CarTransforms;
            var cells = parking.PlanCells;
            var sideCellsAddress = parking.SidePoints;
            var rampInfo = parking.RampInfo;
            var outline = parking.Outline;
            var lotCount = parking.LotNumber;
            DA.SetData(0, score);
            DA.SetData(1, planMatrix);
            if(pathPts != null)
            DA.SetDataTree(2, pathPts);
            if(lotInfo != null)
            DA.SetDataTree(3, lotInfo);
            DA.SetDataTree(4, cells);
            DA.SetDataTree(5, sideCellsAddress);
            DA.SetDataList(6, parking.ExcludeCells);
            DA.SetData(7, parking.ParkingEntranceCell);
            DA.SetData(8, outline);
            DA.SetDataList(9, rampInfo);
            DA.SetDataList(10, parking.PathLines);
            DA.SetData(11, parking.GenerationTime);
            DA.SetData(12, parking.PathDirectionShift); 
            DA.SetData(13, (float)parking.TotalDirShift/ parking.LotNumber);
            DA.SetDataTree(14, parking.CellsWithGrade); 
            DA.SetData(15, parking.LotNumber);
        }
        protected override System.Drawing.Bitmap Icon => ParkingDemo.Properties.Resources.DeconstructParking;
        public override Guid ComponentGuid
        {
            get { return new Guid("F9866FC2-9736-4AEC-A651-E538FE7EB3F5"); }
        }
    }
}