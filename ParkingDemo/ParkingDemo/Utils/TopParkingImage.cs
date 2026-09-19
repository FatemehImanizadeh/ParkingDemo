using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using Rhino.Geometry;

namespace ParkingDemo.Utils
{
    /// <summary>Renders an isolated World-XY plan, independent of the active viewport and other document objects.</summary>
    internal static class TopParkingImage
    {
        internal sealed class Primitive
        {
            public Point3d[] Points;
            public Color Color;
            public bool Filled;
        }

        internal static List<Primitive> Tessellate(IEnumerable<TopParkingScene.Part> parts, double tolerance)
        {
            var result = new List<Primitive>();
            foreach (var part in parts)
            {
                if (part.Geometry is Curve curve)
                {
                    Polyline polyline;
                    if (!curve.TryGetPolyline(out polyline))
                        using (var approximation = curve.ToPolyline(tolerance, 0.1, 0, 0))
                        {
                            if (approximation == null || !approximation.TryGetPolyline(out polyline))
                                throw new InvalidOperationException("Cannot draw a curve in section " + part.Section + ".");
                        }
                    result.Add(new Primitive { Points = polyline.ToArray(), Color = part.Color });
                }
                else if (part.Geometry is Mesh mesh) AddMesh(result, mesh, part.Color);
                else if (part.Geometry is Brep brep)
                {
                    var meshes = Mesh.CreateFromBrep(brep, MeshingParameters.FastRenderMesh);
                    if (meshes == null || meshes.Length == 0)
                        throw new InvalidOperationException("Cannot mesh a surface in section " + part.Section + ".");
                    try { foreach (var piece in meshes) AddMesh(result, piece, part.Color); }
                    finally { foreach (var piece in meshes) piece.Dispose(); }
                }
            }
            return result;
        }

        private static void AddMesh(List<Primitive> result, Mesh mesh, Color color)
        {
            foreach (var face in mesh.Faces)
            {
                result.Add(new Primitive
                {
                    Filled = true, Color = color,
                    Points = new[] { (Point3d)mesh.Vertices[face.A], (Point3d)mesh.Vertices[face.B], (Point3d)mesh.Vertices[face.C] }
                });
                if (face.IsQuad)
                    result.Add(new Primitive
                    {
                        Filled = true, Color = color,
                        Points = new[] { (Point3d)mesh.Vertices[face.A], (Point3d)mesh.Vertices[face.C], (Point3d)mesh.Vertices[face.D] }
                    });
            }
        }

        public static void Save(TopParkingScene scene, IReadOnlyList<Primitive> carTemplate,
            string path, int width, string units, double tolerance)
        {
            int height = (int)(width * 0.85);
            using (var bitmap = new Bitmap(width, height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var title = new Font("Segoe UI", width * 0.020f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var font = new Font("Segoe UI", width * 0.012f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var ink = new SolidBrush(Color.FromArgb(35, 45, 55)))
            {
                bitmap.SetResolution(200, 200);
                graphics.Clear(Color.White);
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                float margin = width * 0.03f;
                graphics.DrawString("Parking option " + scene.Rank + " | Top view", title, ink, margin, margin * 0.45f);
                var plot = new RectangleF(margin, width * 0.085f, width - margin * 2, width * 0.48f);

                var bounds = scene.Bounds;
                if (carTemplate.Count > 0)
                    foreach (var transform in scene.Cars)
                        foreach (var primitive in carTemplate)
                            foreach (var point in primitive.Points)
                            { var moved = point; moved.Transform(transform); bounds.Union(moved); }
                double scale = Math.Min(plot.Width / Math.Max(tolerance, bounds.Max.X - bounds.Min.X),
                    plot.Height / Math.Max(tolerance, bounds.Max.Y - bounds.Min.Y));
                Func<Point3d, PointF> project = point => new PointF(
                    plot.Left + plot.Width / 2 + (float)((point.X - bounds.Center.X) * scale),
                    plot.Top + plot.Height / 2 - (float)((point.Y - bounds.Center.Y) * scale));

                // Match Preview Parking Result's section order, including the path/entrance overlays.
                foreach (var section in scene.Parts.GroupBy(part => part.Section))
                    Draw(graphics, Tessellate(section, tolerance), Transform.Identity, project, width);
                foreach (var transform in scene.Cars)
                    Draw(graphics, carTemplate, transform, project, width);

                string[] description = scene.Description(units);
                float detailsTop = width * 0.59f;
                float lineHeight = width * 0.025f;
                for (int i = 0; i < description.Length; i++)
                    graphics.DrawString(description[i], font, ink,
                        margin + (i % 2) * width * 0.48f, detailsTop + (i / 2) * lineHeight);

                float legendY = width * 0.71f;
                DrawKey(graphics, font, Color.FromArgb(60, 160, 160), "Circulation", margin, legendY, width);
                DrawKey(graphics, font, Color.FromArgb(110, 220, 235), "Entrance", width * 0.25f, legendY, width);
                DrawKey(graphics, font, Color.FromArgb(80, 80, 80), "Excluded", width * 0.46f, legendY, width);
                DrawKey(graphics, font, Color.FromArgb(35, 35, 35), "Walls", width * 0.67f, legendY, width);
                float gradientY = width * 0.748f;
                for (int i = 0; i < 100; i++)
                    using (var color = new SolidBrush(BakeResultsUtils.GetParkingGradientColor(i / 99.0)))
                        graphics.FillRectangle(color, margin + i * width * 0.003f, gradientY, width * 0.003f + 1, width * 0.012f);
                graphics.DrawString("Path grades: near entrance  >  farther away", font, ink, width * 0.35f, gradientY - width * 0.003f);
                string footer = "Gross: generated outline. Net: outline minus excluded cells. Distances: grid grades x cell size.";
                graphics.DrawString(footer, font, ink, margin, width * 0.786f);
                if (carTemplate.Count == 0)
                    graphics.DrawString("Car block not supplied; cars are omitted.", font, ink, margin, width * 0.813f);
                bitmap.Save(path, ImageFormat.Png);
            }
        }

        private static void Draw(Graphics graphics, IEnumerable<Primitive> primitives, Transform transform,
            Func<Point3d, PointF> project, int width)
        {
            var projected = primitives.Select(primitive => new Primitive
            {
                Filled = primitive.Filled, Color = primitive.Color,
                Points = primitive.Points.Select(point => { point.Transform(transform); return point; }).ToArray()
            }).ToList();
            // Top view: low faces first; curve detail is drawn after shaded faces.
            foreach (var primitive in projected.Where(p => p.Points.Length >= 2)
                .OrderBy(p => p.Filled ? 0 : 1).ThenBy(p => p.Points.Average(v => v.Z)))
            {
                var points = primitive.Points.Select(project).ToArray();
                if (primitive.Filled)
                {
                    graphics.SmoothingMode = SmoothingMode.None;
                    using (var brush = new SolidBrush(primitive.Color)) graphics.FillPolygon(brush, points);
                }
                else
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var pen = new Pen(primitive.Color, Math.Max(1, width / 1600f))) graphics.DrawLines(pen, points);
                }
            }
        }

        private static void DrawKey(Graphics graphics, Font font, Color color, string label, float x, float y, int width)
        {
            using (var brush = new SolidBrush(color))
                graphics.FillRectangle(brush, x, y, width * 0.012f, width * 0.012f);
            graphics.DrawString(label, font, Brushes.Black, x + width * 0.018f, y - width * 0.002f);
        }
    }
}
