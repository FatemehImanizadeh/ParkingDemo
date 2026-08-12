using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
namespace ParkingDemo.Utils
{
    
    public static class BakeResultsUtils
    {
        /// <summary>
        /// bake cells with a gradient regarding to their grade
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="parking"></param>
        /// <param name="layerIndex"></param>
        public static void BakeGradientCells(
     RhinoDoc doc,
     Parking parking,
     int layerIndex)
        {
            if (parking.CellsWithGrade == null)
                return;

            int gradeCount =
                parking.CellsWithGrade.BranchCount;

            if (gradeCount == 0)
                return;

            int maximumGrade =
                gradeCount - 1;

            for (int grade = 0;
                 grade < gradeCount;
                 grade++)
            {
                var cells =
                    parking.CellsWithGrade.Branch(grade);

                if (cells == null)
                    continue;

                double normalizedGrade;

                if (maximumGrade == 0)
                {
                    normalizedGrade = 0.0;
                }
                else
                {
                    normalizedGrade =
                        (double)grade /
                        (double)maximumGrade;
                }

                Color gradeColor =
                    GetParkingGradientColor(
                        normalizedGrade);

                ObjectAttributes attributes =
                    CreateColoredAttributes(
                        layerIndex,
                        gradeColor);

                foreach (Rectangle3d rectangle in cells)
                {
                    Brep surface =
                        CreateRectangleSurface(
                            rectangle);

                    if (surface != null)
                    {
                        doc.Objects.AddBrep(
                            surface,
                            attributes);
                    }
                }
            }
        }
        public static ObjectAttributes CreateColoredAttributes(
    int layerIndex,
    Color color)
        {
            ObjectAttributes attributes =
                new ObjectAttributes();

            attributes.LayerIndex =
                layerIndex;

            attributes.ColorSource =
                ObjectColorSource.ColorFromObject;

            attributes.ObjectColor =
                color;

            return attributes;
        }

        public static Color GetParkingGradientColor(
     double t)
        {
            t = Math.Max(
                0.0,
                Math.Min(1.0, t));

            Color yellow =
                Color.FromArgb(
                    255,
                    245,
                    75);

            Color orange =
                Color.FromArgb(
                    255,
                    150,
                    65);

            Color red =
                Color.FromArgb(
                    255,
                    70,
                    70);

            if (t <= 0.5)
            {
                double localT =
                    t / 0.5;

                return InterpolateColor(
                    yellow,
                    orange,
                    localT);
            }
            else
            {
                double localT =
                    (t - 0.5) / 0.5;

                return InterpolateColor(
                    orange,
                    red,
                    localT);
            }
        }
        public static Color InterpolateColor(
    Color a,
    Color b,
    double t)
        {
            int r =
                (int)(a.R +
                (b.R - a.R) * t);

            int g =
                (int)(a.G +
                (b.G - a.G) * t);

            int bl =
                (int)(a.B +
                (b.B - a.B) * t);

            return Color.FromArgb(
                r,
                g,
                bl);
        }
        public static Brep CreateRectangleSurface(
    Rectangle3d rectangle)
        {
            double tolerance =
                RhinoDoc.ActiveDoc != null
                ? RhinoDoc.ActiveDoc.ModelAbsoluteTolerance
                : 0.001;

            return CreateRectangleSurface(
                rectangle,
                tolerance);
        }

        /// <summary>
        /// Same as CreateRectangleSurface(rectangle) but does not depend on
        /// RhinoDoc.ActiveDoc, so it can be used from a pure geometry
        /// builder (see ParkingPreviewGeometryBuilder).
        /// </summary>
        public static Brep CreateRectangleSurface(
    Rectangle3d rectangle,
    double tolerance)
        {
            Curve boundary =
                rectangle.ToNurbsCurve();

            Brep[] breps =
                Brep.CreatePlanarBreps(
                    boundary,
                    tolerance);

            if (breps == null ||
                breps.Length == 0)
            {
                return null;
            }

            return breps[0];
        }

        /// <summary>
        /// Bakes a set of already-computed geometry/color pairs as-is.
        /// No geometry is recomputed here - this is the fast path used by
        /// the preview/bake component once ParkingPreviewGeometryBuilder
        /// has already built parking.PreviewGeometry.
        /// </summary>
        public static void BakeGeometryColorPairs(
    RhinoDoc doc,
    IEnumerable<GeometryColorPair> items,
    int layerIndex)
        {
            if (items == null)
                return;

            foreach (GeometryColorPair item in items)
            {
                if (item == null ||
                    item.Geometry == null)
                {
                    continue;
                }

                ObjectAttributes attributes =
                    CreateColoredAttributes(
                        layerIndex,
                        item.Color);

                if (item.Geometry is Brep brep)
                {
                    doc.Objects.AddBrep(
                        brep,
                        attributes);
                }
                else if (item.Geometry is Curve curve)
                {
                    doc.Objects.AddCurve(
                        curve,
                        attributes);
                }
            }
        }



        /// <summary>
        /// gray the excluded cells from the design space! 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="parking"></param>
        /// <param name="layerIndex"></param>
        public static void BakeExcludedCells(
        RhinoDoc doc,
        Parking parking,
        int layerIndex)
        {
            if (parking.ExcludeCells == null)
                return;

            Color excludedColor =
                Color.FromArgb(
                    80,
                    80,
                    80);

            ObjectAttributes attributes =
                CreateColoredAttributes(
                    layerIndex,
                    excludedColor);

            foreach (Rectangle3d rectangle
                in parking.ExcludeCells)
            {
                Brep surface =
                    CreateRectangleSurface(
                        rectangle);

                if (surface != null)
                {
                    doc.Objects.AddBrep(
                        surface,
                        attributes);
                }
            }
        }


        public static void BakeContinuousPath(
    RhinoDoc doc,
    Parking parking,
    int layerIndex,
    double width)
        {
            if (parking.PathLines == null)
                return;

            List<Curve> lineCurves =
                new List<Curve>();

            foreach (Line line in parking.PathLines)
            {
                if (!line.IsValid)
                    continue;

                lineCurves.Add(
                    new LineCurve(line));
            }

            if (lineCurves.Count == 0)
                return;

            Curve[] joinedCurves =
                Curve.JoinCurves(
                    lineCurves,
                    doc.ModelAbsoluteTolerance);

            if (joinedCurves == null)
                return;

            Color pathColor =
                Color.FromArgb(
                    60,
                    160,
                    160);

            ObjectAttributes attributes =
                CreateColoredAttributes(
                    layerIndex,
                    pathColor);

            foreach (Curve centerCurve in joinedCurves)
            {
                Brep ribbon =
                    CreatePathRibbon(
                        centerCurve,
                        width,
                        doc.ModelAbsoluteTolerance);

                if (ribbon != null)
                {
                    doc.Objects.AddBrep(
                        ribbon,
                        attributes);
                }
                else
                {
                    // Fallback
                    doc.Objects.AddCurve(
                        centerCurve,
                        attributes);
                }
            }
        }

        public static Brep CreatePathRibbon(
    Curve centerCurve,
    double width,
    double tolerance)
        {
            if (centerCurve == null)
                return null;

            double halfWidth =
                width * 0.5;

            Curve[] sideA =
                centerCurve.Offset(
                    Plane.WorldXY,
                    halfWidth,
                    tolerance,
                    CurveOffsetCornerStyle.Round);

            Curve[] sideB =
                centerCurve.Offset(
                    Plane.WorldXY,
                    -halfWidth,
                    tolerance,
                    CurveOffsetCornerStyle.Round);

            if (sideA == null ||
                sideB == null ||
                sideA.Length == 0 ||
                sideB.Length == 0)
            {
                return null;
            }

            Curve a = sideA[0];
            Curve b = sideB[0];

            List<Curve> boundary =
                new List<Curve>();

            boundary.Add(a);

            boundary.Add(
                new LineCurve(
                    a.PointAtEnd,
                    b.PointAtEnd));

            boundary.Add(b);

            boundary.Add(
                new LineCurve(
                    b.PointAtStart,
                    a.PointAtStart));

            Curve[] joined =
                Curve.JoinCurves(
                    boundary,
                    tolerance);

            if (joined == null ||
                joined.Length == 0)
            {
                return null;
            }

            Brep[] breps =
                Brep.CreatePlanarBreps(
                    joined,
                    tolerance);

            if (breps == null ||
                breps.Length == 0)
            {
                return null;
            }

            return breps[0];
        }



        public static void BakeEntranceCell(
    RhinoDoc doc,
    Parking parking,
    int layerIndex)
        {
            if (parking.EntryCell == null)
                return;

            if (parking.PlanPointsGrid == null)
                return;

            int row =
                parking.EntryCell.row;

            int col =
                parking.EntryCell.col;

            Point3d corner;

            try
            {
                corner =
                    parking.PlanPointsGrid
                    .Branch(row)[col]+ new Point3d(-2.5, -2.5, 0);
            }
            catch
            {
                return;
            }

            const double cellSize = 5.0;

            Plane plane =
                new Plane(
                    corner,
                    Vector3d.XAxis,
                    Vector3d.YAxis);

            Rectangle3d entranceRectangle =
                new Rectangle3d(
                    plane,
                    new Interval(
                        0,
                        cellSize),
                    new Interval(
                        0,
                        cellSize));

            Brep entranceSurface =
                CreateRectangleSurface(
                    entranceRectangle);

            if (entranceSurface == null)
                return;

            Color entranceColor =
                Color.FromArgb(
                    110,
                    220,
                    235);

            ObjectAttributes attributes =
                CreateColoredAttributes(
                    layerIndex,
                    entranceColor);

            doc.Objects.AddBrep(
                entranceSurface,
                attributes);
        }



        public static void BakeParkingWall(
    RhinoDoc doc,
    Parking parking,
    int layerIndex,
    double thickness)
        {
            if (parking.Outline == null)
                return;

            Curve outer =
                parking.Outline.DuplicateCurve();

            if (!outer.IsClosed)
                return;

            Curve inner =
                FindInnerOffset(
                    outer,
                    thickness,
                    doc.ModelAbsoluteTolerance);

            if (inner == null)
                return;

            Curve[] boundaries =
            {
        outer,
        inner
    };

            Brep[] wallBreps =
                Brep.CreatePlanarBreps(
                    boundaries,
                    doc.ModelAbsoluteTolerance);

            if (wallBreps == null)
                return;

            ObjectAttributes attributes =
                CreateColoredAttributes(
                    layerIndex,
                    Color.FromArgb(
                        35,
                        35,
                        35));

            foreach (Brep wall in wallBreps)
            {
                doc.Objects.AddBrep(
                    wall,
                    attributes);
            }
        }

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

            if (positive != null &&
                positive.Length > 0)
            {
                double area =
                    GetCurveArea(
                        positive[0]);

                if (area > 0 &&
                    area < outerArea)
                {
                    bestCurve =
                        positive[0];
                }
            }

            if (negative != null &&
                negative.Length > 0)
            {
                double area =
                    GetCurveArea(
                        negative[0]);

                if (area > 0 &&
                    area < outerArea)
                {
                    if (bestCurve == null ||
                        area >
                        GetCurveArea(bestCurve))
                    {
                        bestCurve =
                            negative[0];
                    }
                }
            }

            return bestCurve;
        }

        public static double GetCurveArea(
    Curve curve)
        {
            AreaMassProperties amp =
                AreaMassProperties.Compute(
                    curve);

            if (amp == null)
                return -1;

            double area =
                amp.Area;

            amp.Dispose();

            return area;
        }
    }

}
