using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Parameters;
using System.Linq;
using static ParkingDemo.ParkingUtils;
using ParkingDemo.Utils;
using Eto.Forms;
using System.Threading.Tasks;

namespace ParkingDemo.Component.Start
{
    public class CreateGrid : GH_Component
    {
        public CreateGrid()
          : base("StartGeneration", "StartGeneration",
              "create parking generation pre info and places ramp information in the output in case the ramp placement option is active",
              "ParkingDemo", "Start")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("AddRampToParking", "AR", "add ramp to parking in generation process", GH_ParamAccess.item, false);
            pManager.AddCurveParameter("Outline", "O", "parking internal outline", GH_ParamAccess.item);
            pManager.AddCurveParameter("Exclude Boundary", "E", "parking Exclude", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Entrance Side", "ES", "side of parking entrance", GH_ParamAccess.item);
            Param_Integer param = pManager[3] as Param_Integer;
            param.AddNamedValue("north", 0);
            param.AddNamedValue("west", 1);
            param.AddNamedValue("east", 2);
            param.AddNamedValue("south", 3);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
           /* pManager.AddMatrixParameter("planto matrix", "plantomatrix", "this components converts the plan information to a matrix of numbers each presenting a functionality in parking: this matrix is the base input for all further calculations", GH_ParamAccess.item);
            pManager.AddPointParameter("Outline Pointgrid", "ptsgrid", "internal grid corresponding to plan cells of size 5*5", GH_ParamAccess.tree);
            pManager.AddCurveParameter("outline", "outline ", "plane outlin based on the center of Cplane origin", GH_ParamAccess.item);
            pManager.AddRectangleParameter("cells", "cells", "all cellular regions of plane with 5*5 m size", GH_ParamAccess.tree);
            pManager.AddNumberParameter("area", "area", "total internal outline area", GH_ParamAccess.item);
            pManager.AddPointParameter("sidecellsaddress", "side cells", "center of cells on the borders of the plan: each path contains cells of one side", GH_ParamAccess.tree);
            pManager.AddNumberParameter("rampinfo", "rampinfo", " ramp information: index0: side, index1: index of ramp start cell on selected side, index2: ramp type, index3: ramp orientation", GH_ParamAccess.list);
            pManager.AddNumberParameter("rampendcell", "rampendcell", "gives the cell cell row and col of the ramp end cell which is the parking start cell", GH_ParamAccess.list);*/
            pManager.AddGenericParameter("Parking", "P", "parking with initial informatin created and stored in it", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool addRamp = false;
            DA.GetData(0, ref addRamp);
            var cir = new Circle(3);
            var cir2 = cir.ToNurbsCurve();
            Curve crv = cir2;
            var excludeCrvs = new List<Curve>(); 
            DA.GetData(1, ref crv);
            if(!DA.GetDataList(2, excludeCrvs)) return;
            int rampside = 0;
            DA.GetData(3, ref rampside);
            var temp_bbox = crv.GetBoundingBox(true);
            var minpt_0 = temp_bbox.Min;
            // transform the curve so that the center of bbox is on the origin
            var transformation_vec = new Vector3d(minpt_0 * -1);
            var trf = Transform.Translation(transformation_vec);
            crv.Transform(trf);
            foreach(var crvex in excludeCrvs) { crvex.Transform(trf); }
            var bbox = crv.GetBoundingBox(true); 
            var minpt = bbox.Min;
            var maxpt = bbox.Max;
            var grid = new DataTree<Point3d>();
            var size = 5;
            //اون پایین مشخص میکنم که گرید نقاط دقیقا اندازه محدوده کرو ورودی باشه و برای این که کامل اونو در بر بگیره شاید یکی بیشتر/
            grid = ParkingUtils.CreateGrid((int)RoundUp.RoundTo(maxpt.Y, size) / size, (int)RoundUp.RoundTo(maxpt.X, size) / size, size);
            var plantomatrix = GridToMatrix3(grid, grid.BranchCount, grid.Branch(0).Count, crv, excludeCrvs);
            var cells = CellularOutline(grid, plantomatrix);
            var outline = OutlineFromCells(cells);
            var ramptypes = Ramp.ramptypes();
           var  ramporientations = Ramp.ramporientations();
            ramporientations.ToList();
            var sideptsaddress = new DataTree<int[]>();
            var allsidepts = Ramp.OutlineSidesFinder(plantomatrix, grid, out sideptsaddress);
            var RampPossibleOptions = Ramp.RampPossibleOptions(plantomatrix, grid, sideptsaddress, allsidepts);
            var rampinfo = new List<int>();
            var firstpathcell = new int[2];
            //DA.GetData(2, ref cellindex);
            if (addRamp)
                Ramp.rampplacement(plantomatrix, RampPossibleOptions, sideptsaddress, ramptypes, ramporientations, rampside, out rampinfo, out firstpathcell);
            else
            {
                var sidePtsSideAddress = sideptsaddress.Branch(rampside);
                var ran = new Random();
                var ranIndex = ran.Next(sidePtsSideAddress.Count);
                firstpathcell = sidePtsSideAddress[ranIndex];
            }
            var rampendcell = new List<int>();
            rampendcell.Add(firstpathcell[0]);
            rampendcell.Add(firstpathcell[1]);
            ResetMatrixElementsAfterRamp(plantomatrix);

            /*  DA.SetData(0, plantomatrix);
              DA.SetDataTree(1, grid);
              DA.SetData(2, outline);
              DA.SetDataTree(3, cells);
              DA.SetData(4, area);
              DA.SetDataTree(5, allsidepts);
              DA.SetDataList(6, rampinfo);
              DA.SetDataList(7, rampendcell);*/
            var parking = new Parking();
            parking.PlanMatrix = plantomatrix;
            parking.PlanPointsGrid = grid;
            parking.Outline = outline;
            parking.PlanCells = cells;
            parking.SidePoints = allsidepts;
            parking.RampEndCell = new PathInfo.Cell(rampendcell[0], rampendcell[1]);
            parking.PathStartCell = new PathInfo.Cell(rampendcell[0], rampendcell[1]);
            parking.RampInfo = rampinfo;
            DA.SetData(0, parking);
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.StartGeneration;

        public override Guid ComponentGuid
        {
            get { return new Guid("F6D91134-4E88-4243-B102-061869B01405"); }
        }
    } 
}
