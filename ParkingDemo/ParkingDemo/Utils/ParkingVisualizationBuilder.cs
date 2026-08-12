using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper;

namespace ParkingDemo.Utils
{
    // ========================================================================
    // DATA CARRIERS
    // ========================================================================

    /// <summary>
    /// A single flat cell rectangle paired with the color it should be
    /// drawn or baked with.
    /// </summary>
    public struct ColoredRectangle
    {
        public Rectangle3d Rectangle;
        public Color Color;

        public ColoredRectangle(Rectangle3d rectangle, Color color)
        {
            Rectangle = rectangle;
            Color = color;
        }
    }


    /// <summary>
    /// Every piece of geometry + color needed to visualize a Parking
    /// result, computed once and shared by both the Bake component and
    /// the Preview component.
    ///
    /// IMPORTANT: this class contains no RhinoDoc references and writes
    /// nothing to the document. It is pure geometry + color data.
    /// </summary>
    public class ParkingVisualizationData
    {
        public List<ColoredRectangle> GradientCells = new List<ColoredRectangle>();
        public List<ColoredRectangle> ExcludedCells = new List<ColoredRectangle>();
        public ColoredRectangle? EntranceCell;

        public List<Curve> PathCenterlines = new List<Curve>();
        public Color PathColor;
        public double PathWidth;

        public Curve WallOuter;
        public Curve WallInner;
        public Color WallColor;

        // TODO (ramp): once ramp geometry/coloring rules are provided,
        // add e.g.:
        //   public List<ColoredRectangle> RampCells;
        // built the same way as GradientCells but with a white -> light
        // blue ramp instead of the yellow -> orange -> red one.

        public DataTree<Transform> CarTransforms;
    }


    // ========================================================================
    // BUILDER
    // ========================================================================

    /// <summary>
    /// Turns a Parking result into plain geometry + color data
    /// (ParkingVisualizationData). Performs NO Rhino document writes.
    ///
    /// Used by:
    ///   - BakeParkingResult    -> converts the data into real Rhino objects
    ///   - PreviewParkingResult -> converts the data into viewport-only
    ///                             meshes/curves, redrawn every solve
    ///
    /// Keeping this logic in one place means the preview and the bake
    /// will always show exactly the same geometry and colors.
    /// </summary>
    public static class ParkingVisualizationBuilder
    {
        // ---------------------------------------------------------------
        // COLORS
        //
        // Centralised here so Preview and Bake can never drift apart.
        // ---------------------------------------------------------------

        public static readonly Color ExcludedCellColor =
            Color.FromArgb(80, 80, 80);

        public static readonly Color EntranceCellColor =
            Color.FromArgb(110, 220, 235);

        public static readonly Color PathColorDefault =
            Color.FromArgb(60, 160, 160);

        public static readonly Color WallColorDefault =
            Color.FromArgb(35, 35, 35);

        public const double EntranceCellSize = 5.0;


        // ---------------------------------------------------------------
        // MAIN ENTRY POINT
        // ---------------------------------------------------------------

        public static ParkingVisualizationData Build(
            Parking parking,
            double pathWidth,
            double wallThickness,
            double tolerance)
        {
            var data = new ParkingVisualizationData();

            if (parking == null)
                return data;

            data.GradientCells = BuildGradientCells(parking);
            data.ExcludedCells = BuildExcludedCells(parking);
            data.EntranceCell = BuildEntranceCell(parking);

            data.PathCenterlines = BuildPathCenterlines(parking, tolerance);
            data.PathColor = PathColorDefault;
            data.PathWidth = pathWidth;

            Curve outer;
            Curve inner;

            BuildWallBoundaries(
                parking,
                wallThickness,
                tolerance,
                out outer,
                out inner);

            data.WallOuter = outer;
            data.WallInner = inner;
            data.WallColor = WallColorDefault;

            data.CarTransforms = parking.CarTransforms;

            return data;
        }


        // ---------------------------------------------------------------
        // GRADIENT CELLS
        //
        // CellsWithGrade:
        //   Branch 0 -> Grade 0
        //   Branch 1 -> Grade 1
        //   ...
        // ---------------------------------------------------------------

        public static List<ColoredRectangle> BuildGradientCells(Parking parking)
        {
            var result = new List<ColoredRectangle>();

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

                double normalizedGrade =
                    maximumGrade == 0
                        ? 0.0
                        : (double)grade / maximumGrade;

                Color gradeColor = GetParkingGradientColor(normalizedGrade);

                foreach (Rectangle3d rectangle in cells)
                {
                    result.Add(new ColoredRectangle(rectangle, gradeColor));
                }
            }

            return result;
        }


        // ---------------------------------------------------------------
        // EXCLUDED CELLS
        // ---------------------------------------------------------------

        public static List<ColoredRectangle> BuildExcludedCells(Parking parking)
        {
            var result = new List<ColoredRectangle>();

            if (parking.ExcludeCells == null)
                return result;

            foreach (Rectangle3d rectangle in parking.ExcludeCells)
            {
                result.Add(new ColoredRectangle(rectangle, ExcludedCellColor));
            }

            return result;
        }


        // ---------------------------------------------------------------
        // ENTRANCE CELL
        // ---------------------------------------------------------------

        public static ColoredRectangle? BuildEntranceCell(Parking parking)
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
                corner =
                    parking.PlanPointsGrid.Branch(row)[col] +
                    new Point3d(-2.5, -2.5, 0);
            }
            catch
            {
                return null;
            }

            Plane plane =
                new Plane(corner, Vector3d.XAxis, Vector3d.YAxis);

            Rectangle3d rectangle =
                new Rectangle3d(
                    plane,
                    new Interval(0, EntranceCellSize),
                    new Interval(0, EntranceCellSize));

            return new ColoredRectangle(rectangle, EntranceCellColor);
        }


        // ---------------------------------------------------------------
        // PATH CENTERLINES
        // ---------------------------------------------------------------

        public static List<Curve> BuildPathCenterlines(
            Parking parking,
            double tolerance)
        {
            var result = new List<Curve>();

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

            Curve[] joined = Curve.JoinCurves(lineCurves, tolerance);

            if (joined != null)
                result.AddRange(joined);

            return result;
        }


        // ---------------------------------------------------------------
        // WALL BOUNDARIES (outer outline + inner offset)
        // ---------------------------------------------------------------

        public static void BuildWallBoundaries(
            Parking parking,
            double thickness,
            double tolerance,
            out Curve outer,
            out Curve inner)
        {
            outer = null;
            inner = null;

            if (parking.Outline == null)
                return;

            Curve outerCurve = parking.Outline.DuplicateCurve();

            if (!outerCurve.IsClosed)
                return;

            Curve innerCurve =
                FindInnerOffset(outerCurve, thickness, tolerance);

            if (innerCurve == null)
                return;

            outer = outerCurve;
            inner = innerCurve;
        }


        // ---------------------------------------------------------------
        // COLOR HELPERS
        // ---------------------------------------------------------------

        public static Color GetParkingGradientColor(double t)
        {
            t = Math.Max(0.0, Math.Min(1.0, t));

            Color yellow = Color.FromArgb(255, 245, 75);
            Color orange = Color.FromArgb(255, 150, 65);
            Color red = Color.FromArgb(255, 70, 70);

            if (t <= 0.5)
            {
                double localT = t / 0.5;
                return InterpolateColor(yellow, orange, localT);
            }
            else
            {
                double localT = (t - 0.5) / 0.5;
                return InterpolateColor(orange, red, localT);
            }
        }

        public static Color InterpolateColor(Color a, Color b, double t)
        {
            int r = (int)(a.R + (b.R - a.R) * t);
            int g = (int)(a.G + (b.G - a.G) * t);
            int bl = (int)(a.B + (b.B - a.B) * t);

            return Color.FromArgb(r, g, bl);
        }


        // ---------------------------------------------------------------
        // GEOMETRY HELPERS
        // (moved here from BakeResultsUtils - doc-independent)
        // ---------------------------------------------------------------

        public static Curve FindInnerOffset(
            Curve outer,
            double thickness,
            double tolerance)
        {
            Curve[] positive =
                outer.Offset(
                    Plane.WorldXY,
                    thickness,
                    tolerance,
                    CurveOffsetCornerStyle.Sharp);

            Curve[] negative =
                outer.Offset(
                    Plane.WorldXY,
                    -thickness,
                    tolerance,
                    CurveOffsetCornerStyle.Sharp);

            Curve bestCurve = null;
            double outerArea = GetCurveArea(outer);

            if (positive != null && positive.Length > 0)
            {
                double area = GetCurveArea(positive[0]);

                if (area > 0 && area < outerArea)
                    bestCurve = positive[0];
            }

            if (negative != null && negative.Length > 0)
            {
                double area = GetCurveArea(negative[0]);

                if (area > 0 && area < outerArea)
                {
                    if (bestCurve == null || area > GetCurveArea(bestCurve))
                        bestCurve = negative[0];
                }
            }

            return bestCurve;
        }

        public static double GetCurveArea(Curve curve)
        {
            AreaMassProperties amp = AreaMassProperties.Compute(curve);

            if (amp == null)
                return -1;

            double area = amp.Area;
            amp.Dispose();

            return area;
        }
    }
}
