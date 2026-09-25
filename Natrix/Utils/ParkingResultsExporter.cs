using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Natrix.Utils
{
    internal static class ParkingResultsExporter
    {
        /// <summary>Creates a CSV and matching PNG with four metric panels and a parallel coordinates chart.</summary>
        public static void Export(IReadOnlyList<ParkingExportRow> rows, string csvPath, string modelUnits)
        {
            if (rows == null || rows.Count == 0)
                throw new InvalidOperationException("There are no generated parking options to export.");
            if (!string.Equals(Path.GetExtension(csvPath), ".csv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a filename ending in .csv.", nameof(csvPath));

            string chartPath = Path.ChangeExtension(csvPath, ".png");
            if (File.Exists(csvPath) || File.Exists(chartPath))
                throw new IOException("A CSV or PNG with this name already exists. Choose a new filename.");

            // Render first: a chart failure must not leave a CSV that looks like a complete export.
            byte[] png;
            using (var chart = CreateChart(rows, modelUnits))
            using (var buffer = new MemoryStream())
            {
                chart.Save(buffer, ImageFormat.Png);
                png = buffer.ToArray();
            }

            bool createdCsv = false;
            bool createdChart = false;
            try
            {
                // CreateNew also protects existing files if another process writes after the check.
                using (var csv = new FileStream(csvPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    createdCsv = true;
                    using (var chart = new FileStream(chartPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        createdChart = true;
                        using (var writer = new StreamWriter(csv, new UTF8Encoding(true), 1024, true))
                            writer.Write(ParkingExportRow.ToCsv(rows));
                        chart.Write(png, 0, png.Length);
                    }
                }
            }
            catch
            {
                // Only remove files created by this attempt, never pre-existing user files.
                if (createdChart) TryDelete(chartPath);
                if (createdCsv) TryDelete(csvPath);
                throw;
            }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        internal static Bitmap CreateChart(IReadOnlyList<ParkingExportRow> rows, string modelUnits)
        {
            var bitmap = new Bitmap(1800, 2000);
            bitmap.SetResolution(150, 150);
            try
            {
                using (var graphics = Graphics.FromImage(bitmap))
                using (var titleFont = new Font("Segoe UI", 30, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var textFont = new Font("Segoe UI", 18, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var ink = new SolidBrush(Color.FromArgb(30, 44, 60)))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    graphics.Clear(Color.FromArgb(245, 247, 250));
                    graphics.DrawString("Parking options | ranked results", titleFont, ink, 44, 25);
                    graphics.DrawString(rows.Count.ToString(CultureInfo.InvariantCulture) +
                        " options  |  Rank 1 = highest score  |  All options shown in CSV order",
                        textFont, ink, 46, 76);

                    DrawPanel(graphics, rows, new RectangleF(40, 120, 850, 450),
                        "Number of parkings", "Parking count", row => row.ParkingCount,
                        Color.FromArgb(25, 120, 145));
                    DrawPanel(graphics, rows, new RectangleF(910, 120, 850, 450),
                        "Parking score", "Score", row => row.Score,
                        Color.FromArgb(112, 76, 175));
                    DrawPanel(graphics, rows, new RectangleF(40, 590, 850, 450),
                        "Average path length", "Distance (" + (string.IsNullOrWhiteSpace(modelUnits) ? "model units" : modelUnits) + ")",
                        row => row.AveragePathLength, Color.FromArgb(205, 120, 32));
                    DrawPanel(graphics, rows, new RectangleF(910, 590, 850, 450),
                        "Average turns", "Turns per parking route", row => row.AverageTurns,
                        Color.FromArgb(44, 140, 95));

                    DrawParallelCoordinates(graphics, rows, new RectangleF(40, 1060, 1720, 770), modelUnits);

                    graphics.DrawString("Distances = existing path grades x grid-cell size, in Rhino model units.",
                        textFont, ink, 46, 1860);
                    graphics.DrawString("Counts and turns follow the generator's existing analysis convention. Each panel has its own vertical scale.",
                        textFont, ink, 46, 1892);
                    graphics.DrawString("Unavailable values are blank in the CSV and omitted from the corresponding plot.",
                        textFont, ink, 46, 1924);
                }
                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        // Axis order is also used by the chart's labels and direction indicators.
        internal static double?[] ParallelValues(ParkingExportRow row) => new double?[]
        {
            row.ParkingCount, row.AveragePathLength, row.MaximumPathLength,
            row.AverageTurns, row.MaximumTurns, row.Score
        };

        // Returns distance from the top of an axis: 0 = better, 1 = worse.
        // Constant axes sit at the midpoint, and missing values break the polyline.
        internal static double? ParallelPosition(double? value, double min, double max, bool higherIsBetter)
        {
            if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value)) return null;
            if (max == min) return 0.5;
            double fraction = (value.Value - min) / (max - min);
            fraction = Math.Max(0, Math.Min(1, fraction));
            return higherIsBetter ? 1 - fraction : fraction;
        }

        private static void DrawParallelCoordinates(Graphics graphics, IReadOnlyList<ParkingExportRow> rows,
            RectangleF panel, string modelUnits)
        {
            string units = string.IsNullOrWhiteSpace(modelUnits) ? "model units" : modelUnits;
            string[] labels = { "Parking count", "Average distance", "Maximum distance", "Average turns", "Maximum turns", "Score" };
            string[] axisUnits = { "spaces", units, units, "turns", "turns", "score" };
            bool[] higherIsBetter = { true, false, false, false, false, true };
            Color[] colors = { Color.FromArgb(0, 105, 150), Color.FromArgb(205, 93, 0),
                Color.FromArgb(136, 75, 170), Color.FromArgb(0, 145, 115), Color.FromArgb(190, 65, 115) };
            var values = rows.Select(ParallelValues).ToArray();
            var minimums = new double[6];
            var maximums = new double[6];
            var available = new bool[6];
            for (int axis = 0; axis < 6; axis++)
            {
                var finite = values.Select(row => row[axis]).Where(value => value.HasValue &&
                    !double.IsNaN(value.Value) && !double.IsInfinity(value.Value)).Select(value => value.Value).ToArray();
                available[axis] = finite.Length > 0;
                minimums[axis] = available[axis] ? finite.Min() : 0;
                maximums[axis] = available[axis] ? finite.Max() : 0;
            }

            // Rows are the same score-ranked snapshot used by the CSV and other panels.
            // A result with no finite score cannot be one of the highlighted best options.
            var highlighted = Enumerable.Range(0, rows.Count).Where(index => rows[index].Score.HasValue).Take(5).ToArray();
            var plot = new RectangleF(panel.X + 110, panel.Y + 220, panel.Width - 280, 420);
            using (var heading = new Font("Segoe UI", 25, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var font = new Font("Segoe UI", 17, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var small = new Font("Segoe UI", 15, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var ink = new SolidBrush(Color.FromArgb(45, 58, 74)))
            using (var axisPen = new Pen(Color.FromArgb(180, 192, 204), 1.5f))
            using (var backgroundPen = new Pen(Color.FromArgb(35, 110, 120, 130), 1))
            using (var centered = new StringFormat { Alignment = StringAlignment.Center })
            {
                graphics.FillRectangle(Brushes.White, panel);
                graphics.DrawString("All parameters | parallel coordinates", heading, ink, panel.X + 22, panel.Y + 18);
                graphics.DrawString("Each line is one option. Better values are at the top of every axis; scales use this collection's observed range.",
                    font, ink, panel.X + 22, panel.Y + 57);

                // Legend identifies highlighted options by their CSV rank.
                for (int i = 0; i < highlighted.Length; i++)
                {
                    float x = panel.X + 24 + i * 205;
                    using (var pen = new Pen(colors[i], 3))
                        graphics.DrawLine(pen, x, panel.Y + 107, x + 35, panel.Y + 107);
                    graphics.DrawString("Rank " + rows[highlighted[i]].Rank.ToString(CultureInfo.InvariantCulture),
                        font, ink, x + 45, panel.Y + 95);
                }
                graphics.DrawLine(backgroundPen, panel.X + 1110, panel.Y + 107, panel.X + 1145, panel.Y + 107);
                graphics.DrawString("Other options", font, ink, panel.X + 1155, panel.Y + 95);

                for (int axis = 0; axis < 6; axis++)
                {
                    float x = plot.Left + plot.Width * axis / 5;
                    graphics.DrawString(labels[axis], font, ink, new RectangleF(x - 120, panel.Y + 147, 240, 25), centered);
                    graphics.DrawString(axisUnits[axis] + (higherIsBetter[axis] ? " | higher is better" : " | lower is better"),
                        small, ink, new RectangleF(x - 135, panel.Y + 177, 270, 25), centered);
                    graphics.DrawLine(axisPen, x, plot.Top, x, plot.Bottom);
                }

                // Draw the full population first, then highlighted lines back-to-front so Rank 1 remains visible.
                for (int row = 0; row < rows.Count; row++)
                    if (!highlighted.Contains(row))
                        DrawParallelLine(graphics, values[row], minimums, maximums, higherIsBetter, plot, backgroundPen, false);
                for (int i = highlighted.Length - 1; i >= 0; i--)
                    using (var pen = new Pen(colors[i], 2.5f))
                        DrawParallelLine(graphics, values[highlighted[i]], minimums, maximums, higherIsBetter, plot, pen, true);

                // Original-unit tick labels overlay lines on white backing for readability.
                for (int axis = 0; axis < 6; axis++)
                {
                    float x = plot.Left + plot.Width * axis / 5;
                    int ticks = !available[axis] || minimums[axis] == maximums[axis] ? 1 : 5;
                    for (int tick = 0; tick < ticks; tick++)
                    {
                        double position = ticks == 1 ? 0.5 : tick / 4.0;
                        double fraction = higherIsBetter[axis] ? 1 - position : position;
                        double value = minimums[axis] + (maximums[axis] - minimums[axis]) * fraction;
                        string label = available[axis] ? value.ToString("G4", CultureInfo.InvariantCulture) : "N/A";
                        float y = plot.Top + (float)position * plot.Height;
                        graphics.DrawLine(axisPen, x - 4, y, x + 4, y);
                        var size = graphics.MeasureString(label, small);
                        graphics.FillRectangle(Brushes.White, x + 7, y - size.Height / 2, size.Width, size.Height);
                        graphics.DrawString(label, small, ink, x + 7, y - size.Height / 2);
                    }
                }
                graphics.DrawString("Top five scored options are highlighted. Legend ranks match the CSV; equal scores retain collection order.",
                    font, ink, panel.X + 22, panel.Y + 678);
                graphics.DrawString("Constant axes use the midpoint. Missing metrics break the line. Identical option profiles overlap.",
                    font, ink, panel.X + 22, panel.Y + 710);
            }
        }

        private static void DrawParallelLine(Graphics graphics, double?[] values, double[] minimums,
            double[] maximums, bool[] higherIsBetter, RectangleF plot, Pen pen, bool highlighted)
        {
            PointF? previous = null;
            using (var brush = new SolidBrush(pen.Color))
                for (int axis = 0; axis < values.Length; axis++)
                {
                    double? position = ParallelPosition(values[axis], minimums[axis], maximums[axis], higherIsBetter[axis]);
                    if (!position.HasValue) { previous = null; continue; }
                    var point = new PointF(plot.Left + plot.Width * axis / 5, plot.Top + (float)position.Value * plot.Height);
                    if (previous.HasValue) graphics.DrawLine(pen, previous.Value, point);
                    float radius = highlighted ? 4 : 1.5f;
                    graphics.FillEllipse(brush, point.X - radius, point.Y - radius, radius * 2, radius * 2);
                    previous = point;
                }
        }

        private static void DrawPanel(Graphics graphics, IReadOnlyList<ParkingExportRow> rows,
            RectangleF panel, string title, string units, Func<ParkingExportRow, double?> selector, Color color)
        {
            using (var heading = new Font("Segoe UI", 24, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var font = new Font("Segoe UI", 16, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var ink = new SolidBrush(Color.FromArgb(45, 58, 74)))
            using (var gridPen = new Pen(Color.FromArgb(223, 230, 237)))
            using (var linePen = new Pen(color, 2.5f))
            using (var pointBrush = new SolidBrush(color))
            using (var rightAligned = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            using (var centered = new StringFormat { Alignment = StringAlignment.Center })
            {
                graphics.FillRectangle(Brushes.White, panel);
                graphics.DrawString(title, heading, ink, panel.X + 22, panel.Y + 16);
                graphics.DrawString(units, font, ink, panel.X + 22, panel.Y + 50);
                var plot = new RectangleF(panel.X + 95, panel.Y + 93, panel.Width - 125, panel.Height - 160);
                var values = rows.Select(selector).ToArray();
                var finite = values.Where(value => value.HasValue &&
                    !double.IsNaN(value.Value) && !double.IsInfinity(value.Value)).Select(value => value.Value).ToArray();
                double min = finite.Length == 0 ? 0 : Math.Min(0, finite.Min());
                double max = finite.Length == 0 ? 1 : Math.Max(0, finite.Max());
                double range = max - min;
                if (range == 0) max = min + 1;
                else { max += range * 0.08; if (min < 0) min -= range * 0.08; }

                for (int tick = 0; tick <= 4; tick++)
                {
                    float y = plot.Bottom - plot.Height * tick / 4;
                    double value = min + (max - min) * tick / 4;
                    graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                    graphics.DrawString(value.ToString("0.##", CultureInfo.InvariantCulture), font, ink,
                        new RectangleF(panel.X + 4, y - 12, 80, 24), rightAligned);
                }
                int tickCount = Math.Min(6, rows.Count);
                for (int tick = 0; tick < tickCount; tick++)
                {
                    int index = tickCount == 1 ? 0 : (int)Math.Round((rows.Count - 1.0) * tick / (tickCount - 1));
                    float x = XPosition(index, rows.Count, plot);
                    graphics.DrawString(rows[index].Rank.ToString(CultureInfo.InvariantCulture), font, ink,
                        new RectangleF(x - 40, plot.Bottom + 10, 80, 24), centered);
                }
                graphics.DrawString("Option rank (highest score first)", font, ink,
                    new RectangleF(plot.Left, plot.Bottom + 38, plot.Width, 25), centered);

                PointF? previous = null;
                for (int index = 0; index < values.Length; index++)
                {
                    double? value = values[index];
                    if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
                    {
                        previous = null;
                        continue;
                    }
                    var point = new PointF(XPosition(index, rows.Count, plot),
                        plot.Bottom - (float)((value.Value - min) / (max - min)) * plot.Height);
                    if (previous.HasValue) graphics.DrawLine(linePen, previous.Value, point);
                    graphics.FillEllipse(pointBrush, point.X - 3, point.Y - 3, 6, 6);
                    previous = point;
                }
                if (finite.Length == 0)
                    graphics.DrawString("No available values", font, ink, plot, centered);
            }
        }

        private static float XPosition(int index, int count, RectangleF plot) =>
            count <= 1 ? plot.Left + plot.Width / 2 : plot.Left + plot.Width * index / (count - 1);
    }
}
