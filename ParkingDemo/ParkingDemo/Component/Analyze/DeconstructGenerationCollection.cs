﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Runtime;
using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using Rhino.DocObjects;
using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;

namespace ParkingDemo.Component.Analyze
{
    public class DeconstructGenerationCollection : GH_Component
    {
        public DeconstructGenerationCollection()
          : base("Deconstruct Parking Collection", "DPC",
              "gets all parking guids inside a parking generation with select parking component all parkings inside collection are accessible ",
                "ParkingDemo", "Analyse")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Generation Collection", "GC", "single generation collection for a parking", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Ids", "Ids", "Ids", GH_ParamAccess.list);
            pManager.AddNumberParameter("Scores", "Scores", "Scores", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var generations = new GenerationCollection();
            DA.GetData(0, ref generations);
            var ids = new List<Guid>();
            var scores = new List<double>();
            foreach(var parking in generations.parkings)
            {
                ids.Add(parking.ParkingID);
                scores.Add(parking.Score);

            }
           
           /* RhinoList<System.Object> IDs = new RhinoList<System.Object>(ids);
            RhinoList<double> SCores = new RhinoList<double>(scores);
            IDs.Sort(SCores.ToArray());
            SCores.Sort();*/
            DA.SetDataList(0 , ids);
            DA.SetDataList(1 , scores);
        }
        protected override System.Drawing.Bitmap Icon => ParkingDemo.Properties.Resources.DeconstructParkingCollection;
        public override Guid ComponentGuid  => new Guid("E6583702-C41A-4A28-BD9C-5ABCEE96F520"); }
        
    }
