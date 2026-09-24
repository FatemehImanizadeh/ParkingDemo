using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using ParkingDemo.Utils;
using Rhino;

namespace ParkingDemo.Component.Export
{
    /// <summary>Exports selected collection results only in response to the canvas button.</summary>
    public sealed class ExportTopParkingOptions : GH_Component
    {
        private List<Parking> _selected = new List<Parking>();
        private int _imageWidth = 2000;
        private bool _exporting;
        internal bool CanExport => !Locked && !_exporting && _selected.Count > 0;

        public ExportTopParkingOptions() : base("Export Top Parking Options", "TopExport",
            "Exports top options as individual PNGs in Images, a combined PNG overview and an editable Rhino file.",
            "ParkingDemo", "Export") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Generation Collection", "GC", "Connect one generated parking collection from SortResults.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Car Block (legacy)", "Blk", "Unused; cars_2/cars_3 are selected internally from cell size.", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddIntegerParameter("Top Count", "N", "Number of best-scoring valid options to export; fewer are used if the collection is smaller.", GH_ParamAccess.item, 10);
            pManager.AddIntegerParameter("PNG Width", "W", "Width of each annotated PNG in pixels, from 800 to 4096.", GH_ParamAccess.item, 2000);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Selected Options", "P", "Options that will be exported, in descending score order.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Scores", "S", "Scores of the selected options in export order.", GH_ParamAccess.list);
        }

        public override void ClearData()
        {
            _selected?.Clear();
            base.ClearData();
        }

        protected override void BeforeSolveInstance()
        {
            _selected.Clear();
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration > 0)
            {
                _selected.Clear();
                Message = "Connect one collection";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Supply one collection and one value for each setting per component.");
                return;
            }
            GenerationCollection collection = null;
            int count = 10;
            int width = 2000;
            if (!DA.GetData(0, ref collection) || collection?.parkings == null) return;
            if (!DA.GetData(2, ref count) || !DA.GetData(3, ref width)) return;
            if (count < 1 || width < 800 || width > 4096)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Top Count must be positive; PNG Width must be 800–4096 pixels.");
                return;
            }
            _selected = TopParkingScene.SelectBest(collection.parkings, count);
            _imageWidth = width;
            Message = _selected.Count + " options ready";
            if (_selected.Count == 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid options with finite scores are available.");
            else if (_selected.Count < count)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The collection contains fewer valid scored options than requested; all available options will be exported.");

            DA.SetDataList(0, _selected);
            DA.SetDataList(1, _selected.ConvertAll(parking => parking.Score));
        }

        internal void ExportResults()
        {
            if (!CanExport) return;
            var source = RhinoDoc.ActiveDoc;
            if (source == null)
            { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Open a Rhino document first."); return; }
            var selected = _selected.ToArray();
            int width = _imageWidth;
            _exporting = true;
            try
            {

                using (var dialog = new FolderBrowserDialog
                {
                    Description = "Choose a folder for the top parking PNGs and Rhino file. A new export subfolder will be created.",
                    ShowNewFolderButton = true
                })
                {
                    if (dialog.ShowDialog(Grasshopper.Instances.DocumentEditor) != DialogResult.OK) return;
                    // Export directly from the click handler: normal solves never write files or bake geometry.
                    string folder = TopParkingBatchExporter.Export(selected, source, dialog.SelectedPath, width);
                    Message = "Exported " + selected.Length;
                    RhinoApp.WriteLine("Top parking export: " + folder);
                    MessageBox.Show(Grasshopper.Instances.DocumentEditor,
                        selected.Length + " PNG files in Images, plus TopParkingOptions.png and TopParkingOptions.3dm were saved to:" + Environment.NewLine + folder,
                        "Parking export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Message = "Export failed";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                MessageBox.Show(Grasshopper.Instances.DocumentEditor, ex.Message,
                    "Parking export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { _exporting = false; OnDisplayExpired(true); }
        }

        public override void CreateAttributes() => m_attributes = new ExportTopParkingOptionsAttributes(this);
        protected override Bitmap Icon => Properties.Resources.ExportImage;
        public override Guid ComponentGuid => new Guid("A83C526F-29FB-46E1-BE20-582CA6E840FD");
    }

    internal sealed class ExportTopParkingOptionsAttributes : GH_ComponentAttributes
    {
        private RectangleF _button;
        public ExportTopParkingOptionsAttributes(ExportTopParkingOptions owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();
            var bounds = Bounds;
            // Give the full button label room in both icon and text display modes.
            // The original body and input/output grips remain in their normal positions.
            float width = Math.Max(150, bounds.Width);
            _button = new RectangleF(bounds.X + (bounds.Width - width) / 2, bounds.Bottom + 3, width, 22);
            Bounds = RectangleF.Union(bounds, _button);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel != GH_CanvasChannel.Objects) return;
            using (var button = GH_Capsule.CreateTextCapsule(_button, _button, GH_Palette.Black, "Export PNG + 3DM"))
                button.Render(graphics, Selected, !((ExportTopParkingOptions)Owner).CanExport, false);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && _button.Contains(e.CanvasLocation))
            { ((ExportTopParkingOptions)Owner).ExportResults(); return GH_ObjectResponse.Handled; }
            return base.RespondToMouseDown(sender, e);
        }
    }
}
