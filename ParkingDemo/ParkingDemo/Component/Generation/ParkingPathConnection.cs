using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.Geometry;
using static ParkingDemo.ParkingUtils.PathInfo;

namespace ParkingDemo.Component.Generation
{
    public class ParkingPathConnection : GH_Component
    {
        public ParkingPathConnection()
          : base("ParkingPathConnection", "PC",
              "path conncetion ",
              "ParkingDemo", "Generation")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking", "Parking", "Parking", GH_ParamAccess.item); 
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Parking", "Parking", "Parking", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var parking = new Parking();
            DA.GetData(0,ref parking);
            var planMatrix = parking.PlanMatrix;
            var grid = parking.PlanPointsGrid;
            var parkingPaths = parking.ParkingPaths;
            var carTransforms = parking.CarTransforms;
            var mainPathPts = parking.PathPoints; 
            mainPathConnection.CreateConnectionPath( parking);
            var optimization = new Optimization();
            Optimization.OptimizationFunction(optimization, parking);
            DA.SetData(0, parking); 



        }
        protected override System.Drawing.Bitmap Icon => null; 
        public override Guid ComponentGuid => new Guid("CDA93991-B188-418E-83E9-B181EEA19F03"); }
       
    }
