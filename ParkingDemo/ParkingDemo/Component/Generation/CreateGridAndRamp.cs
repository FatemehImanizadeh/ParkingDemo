using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ParkingDemo;
using Grasshopper.Kernel.Parameters;
using System.Linq;
using static ParkingDemo.ParkingUtils;
using ParkingDemo.Utils;
using Eto.Forms;

namespace ParkingDemo
{
    public class CreateGrid : GH_Component
    {
        public CreateGrid()
          : base("OutlineGrid", "OutlineGrid",
              "Create a grid corresponding to outline bounding box",
              "ParkingDemo", "preinfo")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("AddRampToParking", "AR", "add ramp to parking in generation process", GH_ParamAccess.item, false);
            pManager.AddCurveParameter("outline", "outline", "parking internal outline", GH_ParamAccess.item);
            pManager.AddIntegerParameter("rampside", "Rside", "side of parking entrance", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("rampstartcellindex", "Rstartindex", "index of the ramp start cell based on the ramp i", GH_ParamAccess.item);
            Param_Integer param = pManager[2] as Param_Integer;
            param.AddNamedValue("north", 0);
            param.AddNamedValue("west", 1);
            param.AddNamedValue("east", 2);
            param.AddNamedValue("south", 3);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMatrixParameter("planto matrix", "plantomatrix", "this components converts the plan information to a matrix of numbers each presenting a functionality in parking: this matrix is the base input for all further calculations", GH_ParamAccess.item);
            pManager.AddPointParameter("Outline Pointgrid", "ptsgrid", "internal grid corresponding to plan cells of size 5*5", GH_ParamAccess.tree);
            pManager.AddCurveParameter("outline", "outline ", "plane outlin based on the center of Cplane origin", GH_ParamAccess.item);
            pManager.AddRectangleParameter("cells", "cells", "all cellular regions of plane with 5*5 m size", GH_ParamAccess.tree);
            pManager.AddNumberParameter("area", "area", "total internal outline area", GH_ParamAccess.item);
            pManager.AddPointParameter("sidecellsaddress", "side cells", "center of cells on the borders of the plan: each path contains cells of one side", GH_ParamAccess.tree);
            pManager.AddNumberParameter("rampinfo", "rampinfo", " ramp information: index0: side, index1: index of ramp start cell on selected side, index2: ramp type, index3: ramp orientation", GH_ParamAccess.list);
            pManager.AddNumberParameter("rampendcell", "rampendcell", "gives the cell cell row and col of the ramp end cell which is the parking start cell", GH_ParamAccess.list); 
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool addRamp = false;
            DA.GetData(0, ref addRamp); 
            var cir = new Circle(3);
            var cir2=  cir.ToNurbsCurve();
            Curve crv = cir2;
            DA.GetData(1, ref crv);
            int rampside = 0;
            DA.GetData(2, ref rampside);
            var temp_bbox = crv.GetBoundingBox(true);
            var minpt_0 = temp_bbox.Min;
            // transform the curve so that the center of bbox is on the origin
            var transformation_vec = new Vector3d(minpt_0 * -1);
            var trf = Rhino.Geometry.Transform.Translation(transformation_vec);
            crv.Transform(trf);
            var bbox = crv.GetBoundingBox(true);
            var minpt = bbox.Min;
            var maxpt = bbox.Max;
            var grid = new DataTree<Point3d>();
            var size = 5;
            //اون پایین مشخص میکنم که گرید نقاط دقیقا اندازه محدوده کرو ورودی باشه و برای این که کامل اونو در بر بگیره شاید یکی بیشتر/
            grid = ParkingUtils.CreateGrid((int)ParkingUtils.RoundUp.RoundTo(maxpt.Y, size) / size, (int)ParkingUtils.RoundUp.RoundTo(maxpt.X, size) / size, (int)size);
            var plantomatrix = ParkingUtils.GridToMatrix(grid, grid.BranchCount, grid.Branch(0).Count
      , crv);
            var cells = ParkingUtils.CellularOutline(grid, plantomatrix);
            var outline = ParkingUtils.outlinefromcells(cells);
            var area = cells.BranchCount * 25;

            var cellscount = cells.BranchCount;
            var ramptypes = new DataTree<Point3d>();
            ramptypes = ParkingUtils.Ramp.ramptypes();
            var ramporientations = new List<Transform>();
            ramporientations =  ParkingUtils.Ramp.ramporientations();
            ramporientations.ToList();
            var sideptsaddress = new DataTree<int[]>();
            var allsidepts = Ramp.OutlineSidesFinder(plantomatrix, grid, out sideptsaddress);
            var RampPossibleOptions = ParkingUtils.Ramp.RampPossibleOptions(plantomatrix, grid, sideptsaddress, allsidepts);
          
            var rampinfo = new List<int>();
            var firstpathcell = new int[2];
            //DA.GetData(2, ref cellindex);
            if (addRamp)
            ParkingUtils.Ramp.rampplacement(plantomatrix,RampPossibleOptions , sideptsaddress, ramptypes, ramporientations, rampside, out rampinfo, out firstpathcell);
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
            DA.SetData(0,  plantomatrix);
            DA.SetDataTree(1, grid);
            DA.SetData(2, outline);
            DA.SetDataTree(3, cells);
            DA.SetData(4, area);
            DA.SetDataTree(5, allsidepts);
            DA.SetDataList(6, rampinfo);
            DA.SetDataList(7, rampendcell) ;

            var parking = new Parking();
            parking.PlanMatrix = plantomatrix;
            parking.PlanPointsGrid = grid;
            parking.PlanCells = cells;
            parking.SidePoints = allsidepts;
            parking.RampEndCell = new ParkingUtils.PathInfo.Cell(rampendcell[0], rampendcell[1]);
            parking.PathStartCell = new ParkingUtils.PathInfo.Cell(rampendcell[0], rampendcell[1]);
        }
        protected       override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null; //Properties.Resources.grid_1;

            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("F6D91134-4E88-4243-B102-061869B01405"); }
        }
    }
}
