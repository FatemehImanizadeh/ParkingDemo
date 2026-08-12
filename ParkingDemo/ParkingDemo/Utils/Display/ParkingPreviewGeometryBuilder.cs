using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;

namespace ParkingDemo.Utils
{
    /// <summary>
    /// Builds the preview/bake geometry for a Parking object, WITHOUT baking
    /// anything into the Rhino document.
    ///
    /// This mirrors the logic in BakeResultsUtils (same rectangles, same
    /// colors, same offsets) but every method here just returns geometry +
    /// color pairs instead of calling doc.Objects.Add*.
    ///
    /// Usage: call BuildAll(...) exactly once, right after your generation
    /// component finishes computing a Parking layout. The result gets
    /// attached to parking.PreviewGeometry automatically, and from then on
    /// both the live GH preview and the Bake button reuse it directly.
    /// </summary>
    public static class ParkingPreviewGeometryBuilder
    {
        /// <summary>
        /// Builds every preview element and stores the result on
        /// parking.PreviewGeometry.
        /// </summary>
        /// <param name="parking">The generated parking object.</param>
        /// <param name="tolerance">Usually doc.ModelAbsoluteTolerance.</param>
        /// <param name="pathWidth">Same meaning as in BakeContinuousPath (model units).</param>
        /// <param name="wallThickness">Same meaning as in BakeParkingWall (model units).</param>
        public static ParkingPreviewGeometry BuildAll(
            Parking parking,
            double tolerance,
            double pathWidth = 0.30,
            double wallThickness = 0.20)
        {
            var preview = new ParkingPreviewGeometry
            {
                GradientCells = BuildGradientCells(parking, tolerance),
                ExcludedCells = BuildExcludedCells(parking, tolerance),
                PathRibbons = BuildContinuousPath(parking, pathWidth, tolerance),
                EntranceCell = BuildEntranceCell(parking, tolerance),
                Walls = BuildParkingWall(parking, wallThickness, tolerance)
            };

            parking.PreviewGeometry = preview;

            return preview;
        }

        public static List<GeometryColorPair> BuildGradientCells(
            Parking parking,
            double tolerance)
        {
            var result = new List<GeometryColorPair>();

            if (parking.CellsWithGrade == null)
                return result;

            int gradeCount = parking.CellsWithGrade.BranchCount;

            if (gradeCount == 0)
                return result;

            int maximumGrade = gradeCount - 1;

            for (int grade = 0; grade < gradeCount; grade++)
            {
                var cells = parking.CellsWithGrade.Branch(grade);

                if (cells == null)
                    continue;

                double normalizedGrade = maximumGrade == 0
                    ? 0.0
                    : (double)grade / maximumGrade;

                Color gradeColor = BakeResultsUtils.GetParkingGradientColor(normalizedGrade);

                foreach (Rectangle3d rectangle in cells)
                {
                    Brep surface = BakeResultsUtils.CreateRectangleSurface(rectangle, tolerance);

                    if (surface != null)
                        result.Add(new GeometryColorPair(surface, gradeColor));
                }
            }

            return result;
        }

        public static List<GeometryColorPair> BuildExcludedCells(
            Parking parking,
            double tolerance)
        {
            var result = new List<GeometryColorPair>();

            if (parking.ExcludeCells == null)
                return result;

            Color excludedColor = Color.FromArgb(80, 80, 80);

            foreach (Rectangle3d rectangle in parking.ExcludeCells)
            {
                Brep surface = BakeResultsUtils.CreateRectangleSurface(rectangle, tolerance);

                if (surface != null)
                    result.Add(new GeometryColorPair(surface, excludedColor));
            }

            return result;
        }

        public static List<GeometryColorPair> BuildContinuousPath(
            Parking parking,
            double width,
            double tolerance)
        {
            var result = new List<GeometryColorPair>();

            if (parking.PathLines == null)
                return result;

            var lineCurves = new List<Curve>();

            foreach (Line line in parking.PathLines)
            {
                if (!line.IsValid)
                    continue;

                lineCurves.Add(new LineCurve(line));
            }

            if (lineCurves.Count == 0)
                return result;

            Curve[] joinedCurves = Curve.JoinCurves(lineCurves, tolerance);

            if (joinedCurves == null)
                return result;

            Color pathColor = Color.FromArgb(60, 160, 160);

            foreach (Curve centerCurve in joinedCurves)
            {
                Brep ribbon = BakeResultsUtils.CreatePathRibbon(centerCurve, width, tolerance);

                if (ribbon != null)
                {
                    result.Add(new GeometryColorPair(ribbon, pathColor));
                }
                else
                {
                    // Same fallback as the original bake method: if the
                    // ribbon can't be built, keep the bare centerline.
                    result.Add(new GeometryColorPair(centerCurve, pathColor));
                }
            }

            return result;
        }

        public static GeometryColorPair BuildEntranceCell(
            Parking parking,
            double tolerance)
        {
            if (parking.EntryCell == null)
                return null;

            if (parking.PlanPointsGrid == null)
                return null;

            int row = parking.EntryCell.row;
            int col = parking.EntryCell.col;

            Point3d corner;

            try
            {
                corner = parking.PlanPointsGrid.Branch(row)[col] + new Point3d(-2.5, -2.5, 0);
            }
            catch
            {
                return null;
            }

            const double cellSize = 5.0;

            Plane plane = new Plane(corner, Vector3d.XAxis, Vector3d.YAxis);

            Rectangle3d entranceRectangle = new Rectangle3d(
                plane,
                new Interval(0, cellSize),
                new Interval(0, cellSize));

            Brep entranceSurface = BakeResultsUtils.CreateRectangleSurface(entranceRectangle, tolerance);

            if (entranceSurface == null)
                return null;

            Color entranceColor = Color.FromArgb(110, 220, 235);

            return new GeometryColorPair(entranceSurface, entranceColor);
        }

        public static List<GeometryColorPair> BuildParkingWall(
            Parking parking,
            double thickness,
            double tolerance)
        {
            var result = new List<GeometryColorPair>();

            if (parking.Outline == null)
                return result;

            Curve outer = parking.Outline.DuplicateCurve();

            if (!outer.IsClosed)
                return result;

            Curve inner = BakeResultsUtils.FindInnerOffset(outer, thickness, tolerance);

            if (inner == null)
                return result;

            Curve[] boundaries = { outer, inner };

            Brep[] wallBreps = Brep.CreatePlanarBreps(boundaries, tolerance);

            if (wallBreps == null)
                return result;

            Color wallColor = Color.FromArgb(35, 35, 35);

            foreach (Brep wall in wallBreps)
                result.Add(new GeometryColorPair(wall, wallColor));

            return result;
        }
    }
}
