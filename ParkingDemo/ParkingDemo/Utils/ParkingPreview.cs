using Rhino.Commands;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Geometry.Voronoi;
using Rhino.Display;
using System.IO;
using Grasshopper.Kernel.Parameters.Hints;
using Rhino.Geometry;
using Rhino.Render;
namespace ParkingDemo.Utils
{
    internal class ParkingPreview
    {
        public List<int> ExistingIndices = new List<int>();

        public static void PrintResult(string DocumentName, int Index, Rectangle3d PlanArea, bool resetPlan)
        {
            // Set up export options
            var doc = RhinoDoc.ActiveDoc;
            RhinoPageView viewRectangle;
            try
            {
                viewRectangle = doc.Views.GetPageViews()[0];
                var detail2 = viewRectangle.GetDetailViews()[0];
                doc.Objects.Delete(detail2.Id, true);
            }
            catch
            {
                viewRectangle = doc.Views.AddPageView($"{DocumentName}_{Index}", PlanArea.Width, PlanArea.Height);
            }
            var width = (int)PlanArea.Width;
            var height = (int)PlanArea.Height;
            viewRectangle.PageWidth = PlanArea.Width;
            viewRectangle.PageHeight = PlanArea.Height;
            var corner0 = new Point2d(PlanArea.Corner(3).X, PlanArea.Corner(3).Y);
            var corner1 = new Point2d(PlanArea.Corner(1).X, PlanArea.Corner(1).Y);
            viewRectangle.AddDetailView(DocumentName, corner0, corner1, DefinedViewportProjection.Top);
            var detail = viewRectangle.GetDetailViews()[0];
            detail.Viewport.SetCameraLocation(PlanArea.Center, true);
            detail.DetailGeometry.IsProjectionLocked = true;

            detail.Geometry.Scale(1);
            var vec = new Vector3d(-PlanArea.Corner(0));
            detail.DetailGeometry.Transform(Transform.Translation(vec));
            detail.DetailGeometry.SetScale(1, doc.ModelUnitSystem, 1, doc.PageUnitSystem);
            detail.CommitChanges();
            detail.CommitViewportChanges();
         




            var view_capture = new ViewCapture
            {
                Width = width * 13,
                Height = height * 13,
                ScaleScreenItems = true,
                DrawAxes = false,
                DrawGrid = false,
                DrawGridAxes = false,
                TransparentBackground = true,
            };
            view_capture.TransparentBackground = true;
            var bitmapnew = view_capture.CaptureToBitmap(viewRectangle);
            Size size = new Size(width * 10, height * 10);
            var sizeNew = new Size(width * 10, height * 10);
            var image2 =
                viewRectangle.GetPreviewImage(size, false);

            image2.SetResolution(2000 * width, 2000 * height);
            image2.MakeTransparent();
            // image.Save(fileName);
            if (null != image2)
            {
                string filename2;
                
                filename2 = $"G:\\archtech\\thesis\\FatemeThesis\\Visualization\\imcap\\imcap6\\{DocumentName}_{Index.ToString()}.png";

                if(!File.Exists(filename2))
                filename2 = $"D:\\FatemeThesis\\Visualization\\imcap\\imcap6\\{DocumentName}_{Index.ToString()}.png";
                image2.Save(filename2);
            }
            // Change filename and format here
            //   var path = $"C:\\ParkingDemo\\Result";
            //  var filename = Path.Combine(path, "SampleCsViewCapture.png");
            //bitmapnew.Save(filename2, System.Drawing.Imaging.ImageFormat.Jpeg);
            doc.Objects.Delete(viewRectangle.ActiveDetailId, true);
        }
      
            /*
            var size = view.Size;
            var bitmapOptions = new Rhino.Display.ViewCaptureSettings(view, 300); // 300 dpi
            // bitmapOptions.SetModelScaleToFit(true);
            bitmapOptions.SetLayout(Size.Subtract(Size.Empty, Size.Empty), view.ScreenRectangle );
            bitmapOptions.DrawBackground = false;
            bitmapOptions.DrawWallpaper = false;
            // Capture viewport to bitmap
            var bitmap = view.CaptureToBitmap();
            if (bitmap == null)
            {
                RhinoApp.WriteLine("Failed to capture viewport to bitmap.");
            }
            // Export bitmap to a special format
            try
            {
                string filename = $"C:\\ParkingDemo\\Result\\{DocumentName}_{Index.ToString()}.png"; // Change filename and format here
                bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Png); // Change ImageFormat to desired special format
                RhinoApp.WriteLine("Export successful. File saved to: " + filename);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("Error exporting file: " + ex.Message);
            }
            finally
            {
                bitmap.Dispose();
            }*/
        }
}

