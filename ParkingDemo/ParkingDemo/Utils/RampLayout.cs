using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rhino.Geometry;

namespace ParkingDemo.Utils
{
    // Placed ramp data, ordered from the outside landing to the parking entrance.
    public sealed class RampLayout
    {
        public int TypeIndex { get; private set; }
        public int OrientationIndex { get; private set; }
        public int EntranceSide { get; private set; }
        public int SideCellIndex { get; private set; }
        public IReadOnlyList<Rectangle3d> Cells { get; private set; }
        public IReadOnlyList<Point3d> Centers { get; private set; }
        public Point3d End => Centers[Centers.Count - 1];

        public static RampLayout FromParking(Parking parking)
        {
            if (parking.RampInfo == null || parking.RampInfo.Count < 4 || parking.RampEndCell == null) return null;
            var info = parking.RampInfo;
            var orientation = ParkingUtils.Ramp.ramporientations()[info[3]];
            var offsets = ParkingUtils.Ramp.ramptypes().Branch(info[2]).Select(point =>
            { point.Transform(orientation); return point; }).ToArray();
            var last = offsets[offsets.Length - 1];
            // Match rampplacement's integer offsets exactly, including mirrored orientations.
            int row = parking.RampEndCell.row + (int)last.Y;
            int col = parking.RampEndCell.col - (int)last.X;
            return new RampLayout
            {
                TypeIndex = info[2], OrientationIndex = info[3], EntranceSide = info[0], SideCellIndex = info[1],
                Centers = offsets.Select(p => parking.PlanPointsGrid.Branch(row - (int)p.Y)[col + (int)p.X]).ToArray(),
                Cells = offsets.Select(p => parking.PlanCells.Branch(row - (int)p.Y, col + (int)p.X)[0]).ToArray()
            };
        }

        public static List<GeometryColorPair> Build(Parking parking, double tolerance)
        {
            var result = new List<GeometryColorPair>();
            var ramp = parking.Ramp;
            if (ramp == null) return result;
            double size = parking.CellSize;
            var ink = Color.FromArgb(55, 75, 85);
            // The final cell is already drawn as the parking entrance; avoid overlapping fills.
            for (int i = 0; i < ramp.Cells.Count - 1; i++)
            {
                double t = (double)i / (ramp.Cells.Count - 2);
                var color = Color.FromArgb((int)(230 - 80 * t), (int)(228 + 7 * t), (int)(225 + 20 * t));
                var surface = BakeResultsUtils.CreateRectangleSurface(ramp.Cells[i], tolerance);
                if (surface != null) result.Add(new GeometryColorPair(surface, color));
                result.Add(new GeometryColorPair(ramp.Cells[i].ToNurbsCurve(), ink));
            }
            // Ratios of cell size: smaller spacing means more arrows.
            const double arrowHalfWidth = 0.09, arrowDepth = 0.055, arrowSpacing = 0.125;
            const double cornerRadius = 0.22;
            using (var polyline = new PolylineCurve(ramp.Centers))
            {
                // Round the centerline itself; offsetting would move it away from the ramp center.
                var centerline = Curve.CreateFilletCornersCurve(polyline, size * cornerRadius, tolerance, 0.01)
                    ?? polyline.DuplicateCurve();
                centerline.Transform(Transform.Translation(0, 0, size * 0.001));
                result.Add(new GeometryColorPair(centerline, ink));
                int divisions = System.Math.Max(2, (int)System.Math.Ceiling(centerline.GetLength() / (size * arrowSpacing)));
                var parameters = centerline.DivideByCount(divisions, false);
                if (parameters != null)
                    foreach (double parameter in parameters)
                    {
                        var direction = centerline.TangentAt(parameter);
                        if (!direction.Unitize()) continue;
                        var across = new Vector3d(-direction.Y, direction.X, 0);
                        var tip = centerline.PointAt(parameter);
                        var back = tip - direction * (size * arrowDepth);
                        result.Add(new GeometryColorPair(new PolylineCurve(new[]
                            { back + across * (size * arrowHalfWidth), tip, back - across * (size * arrowHalfWidth) }), ink));
                    }
            }
            return result;
        }
    }
}
