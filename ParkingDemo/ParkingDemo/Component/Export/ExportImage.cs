using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Eto.Threading;
using Grasshopper.Kernel;
using ParkingDemo.Utils;
using Rhino.Geometry;
using System.Threading; 
using System.Threading;
namespace ParkingDemo.components.Preview
{


    public class ExportImage : GH_Component
    {
        List<int> existingIndices = new ParkingPreview().ExistingIndices;
        bool Reset = false;
        string ParkingName;
        public ExportImage()
          : base("PrintResult", "PrintResult",
                  "PrintResult",
              "ParkingDemo", "Export")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Save Folder Path", "P", "folder location to save exports in there", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Outline BoundingRectangle", "BRec", "outline bounding rectangle",
                GH_ParamAccess.item);
            pManager.AddTextParameter("Parking Name", "PN", "parking name to be the title of file saved", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Reset Preview sheet", "Reset Preview", "reset preview size for export image results: if the plan area changes set the reset value to true to create a new layout of your plan ", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
           
            var bRec = new Rectangle3d(Plane.Unset, 10, 10);
            var parkingName =  "P";
            string folderPath = "";
            DA.GetData(0, ref folderPath);
            var resetPlan = false;
            DA.GetData(1, ref bRec);
            DA.GetData(2, ref parkingName);

            DA.GetData(3, ref resetPlan);
            if ( resetPlan)
            {
                
            if (parkingName != ParkingName)
            existingIndices.Clear();
                if (existingIndices.Count == 0) existingIndices.Add(0);
                var index = existingIndices.Max() + 1;
                existingIndices.Add(index);
               
                ParkingPreview.PrintResult( parkingName, folderPath, index, bRec, resetPlan);
                ParkingName = parkingName;  
            }
        }

        protected override System.Drawing.Bitmap Icon => ParkingDemo.Properties.Resources.ExportImage;

        public override Guid ComponentGuid
        {
            get { return new Guid("F21AEDDE-4A45-4165-A23A-16FE59852FA0"); }
        }
    }
}

