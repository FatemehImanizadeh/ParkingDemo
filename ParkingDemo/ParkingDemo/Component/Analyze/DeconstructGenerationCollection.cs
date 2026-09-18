using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using ParkingDemo.Utils;
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
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel.Attributes;
using Rhino;

namespace ParkingDemo.Component.Analyze
{
    public class DeconstructGenerationCollection : GH_Component
    {
        private readonly List<GenerationCollection> _collections = new List<GenerationCollection>();
        private IReadOnlyList<ParkingExportRow> _exportRows = Array.Empty<ParkingExportRow>();
        private bool _exporting;

        internal bool CanExport => !Locked && !_exporting && _exportRows.Count > 0;

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
            GenerationCollection generations = null;
            if (!DA.GetData(0, ref generations) || generations?.parkings == null)
                return;
            // Item inputs can be solved more than once; collect each collection only once.
            if (!_collections.Contains(generations)) _collections.Add(generations);
            var ids = new List<Guid>();
            var scores = new List<double>();
            foreach(var parking in generations.parkings)
            {
                if (parking == null) continue;
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

        public override void ClearData()
        {
            _collections?.Clear();
            _exportRows = Array.Empty<ParkingExportRow>();
            base.ClearData();
        }

        protected override void BeforeSolveInstance()
        {
            _collections.Clear();
            _exportRows = Array.Empty<ParkingExportRow>();
            base.BeforeSolveInstance();
        }

        protected override void AfterSolveInstance()
        {
            _exportRows = ParkingExportRow.Snapshot(_collections.SelectMany(collection => collection.parkings));
            Message = _exportRows.Count + " options";
            base.AfterSolveInstance();
        }

        internal void ExportResults()
        {
            if (!CanExport) return;

            // Keep one immutable snapshot even if a scheduled generation runs while the dialog is open.
            var rows = _exportRows;
            string units = RhinoDoc.ActiveDoc?.ModelUnitSystem.ToString() ?? "model units";
            _exporting = true;
            try
            {
                using (var dialog = new SaveFileDialog
                {
                    Title = "Export parking results (CSV + PNG chart)",
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    AddExtension = true,
                    FileName = "ParkingResults_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".csv",
                    CheckPathExists = true,
                    OverwritePrompt = false,
                    RestoreDirectory = true
                })
                {
                    // No ExpireSolution call: a click exports once, without another generation/solve.
                    if (dialog.ShowDialog(Grasshopper.Instances.DocumentEditor) != DialogResult.OK) return;
                    ParkingResultsExporter.Export(rows, dialog.FileName, units);
                    Message = "Exported " + rows.Count + " options";
                    RhinoApp.WriteLine("Parking CSV: " + dialog.FileName);
                    RhinoApp.WriteLine("Parking chart: " + Path.ChangeExtension(dialog.FileName, ".png"));
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Parking export failed: " + ex.Message);
                Message = "Export failed";
                MessageBox.Show(Grasshopper.Instances.DocumentEditor, ex.Message,
                    "Parking export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _exporting = false;
                OnDisplayExpired(true);
            }
        }

        public override void CreateAttributes()
        {
            m_attributes = new DeconstructGenerationCollectionAttributes(this);
        }
        protected override System.Drawing.Bitmap Icon => ParkingDemo.Properties.Resources.DeconstructParkingCollection;
        public override Guid ComponentGuid  => new Guid("E6583702-C41A-4A28-BD9C-5ABCEE96F520");
    }

    internal sealed class DeconstructGenerationCollectionAttributes : GH_ComponentAttributes
    {
        private RectangleF _exportBounds;

        public DeconstructGenerationCollectionAttributes(DeconstructGenerationCollection owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();
            var body = Bounds;
            // Extend below the original body without moving any parameter grips.
            const float buttonWidth = 146;
            float width = Math.Max(body.Width, buttonWidth + 8);
            Bounds = new RectangleF(body.X - (width - body.Width) / 2, body.Y, width, body.Height + 28);
            _exportBounds = new RectangleF(Bounds.X + 4, Bounds.Bottom - 25, Bounds.Width - 8, 22);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel != GH_CanvasChannel.Objects) return;
            var component = (DeconstructGenerationCollection)Owner;
            using (var capsule = GH_Capsule.CreateTextCapsule(_exportBounds, _exportBounds,
                GH_Palette.Black, "Export CSV + chart"))
                capsule.Render(graphics, Selected, !component.CanExport, false);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && _exportBounds.Contains(e.CanvasLocation))
            {
                ((DeconstructGenerationCollection)Owner).ExportResults();
                return GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseDown(sender, e);
        }
    }
}
