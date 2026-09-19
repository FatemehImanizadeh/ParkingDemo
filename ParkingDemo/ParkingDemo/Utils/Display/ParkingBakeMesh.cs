using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino;
using Rhino.Geometry;

namespace ParkingDemo.Utils
{
    /// <summary>One selectable mesh per section, with independent vertex colors per piece.</summary>
    internal static class ParkingBakeMesh
    {
        public static Mesh Create(IEnumerable<GeometryColorPair> items, double tolerance, double pathWidth = 0.30)
        {
            var result = new Mesh();
            try
            {
                foreach (var item in items)
                {
                    if (item?.Geometry == null) continue;
                    if (item.Geometry is Mesh mesh)
                    {
                        using (var copy = mesh.DuplicateMesh()) Append(result, copy, item.Color);
                    }
                    else if (item.Geometry is Brep brep)
                        AppendBrep(result, brep, item.Color);
                    else if (item.Geometry is Curve curve)
                    {
                        // The preview builder keeps a centerline when its ribbon fails.
                        // Give that fallback the configured width of the plan path.
                        Polyline points;
                        if (!curve.TryGetPolyline(out points))
                        {
                            using (var polyline = curve.ToPolyline(tolerance, 0.1, 0, 0))
                            {
                                if (polyline == null || !polyline.TryGetPolyline(out points))
                                    throw new InvalidOperationException("Unable to mesh a parking path centerline.");
                            }
                        }
                        for (int i = 1; i < points.Count; i++)
                        {
                            var side = Vector3d.CrossProduct(points[i] - points[i - 1], Vector3d.ZAxis);
                            if (!side.Unitize()) continue;
                            side *= pathWidth * 0.5;
                            using (var strip = new Mesh())
                            {
                                strip.Vertices.Add(points[i - 1] + side);
                                strip.Vertices.Add(points[i - 1] - side);
                                strip.Vertices.Add(points[i] - side);
                                strip.Vertices.Add(points[i] + side);
                                strip.Faces.AddFace(0, 1, 2, 3);
                                Append(result, strip, item.Color);
                            }
                        }
                    }
                    else throw new InvalidOperationException("Unsupported parking mesh geometry.");
                }
                // Do not weld: neighboring grades need separate colors at shared positions.
                result.Normals.ComputeNormals();
                result.Compact();
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        private static void AppendBrep(Mesh target, Brep brep, Color color)
        {
            var pieces = Mesh.CreateFromBrep(brep, MeshingParameters.FastRenderMesh);
            if (pieces == null || pieces.Length == 0)
                throw new InvalidOperationException("Unable to mesh a parking surface.");
            try { foreach (var piece in pieces) Append(target, piece, color); }
            finally { foreach (var piece in pieces) piece?.Dispose(); }
        }

        private static void Append(Mesh target, Mesh piece, Color color)
        {
            if (piece == null || piece.Faces.Count == 0)
                throw new InvalidOperationException("A parking surface produced an empty mesh.");
            piece.VertexColors.CreateMonotoneMesh(color);
            target.Append(piece);
        }

        public static void Bake(RhinoDoc doc, IEnumerable<GeometryColorPair> items, int layer, double pathWidth = 0.30)
        {
            if (items == null) return;
            using (var mesh = Create(items, doc.ModelAbsoluteTolerance, pathWidth))
            using (var attributes = BakeResultsUtils.CreateColoredAttributes(layer, Color.White))
            {
                if (mesh.Faces.Count > 0 && doc.Objects.AddMesh(mesh, attributes) == Guid.Empty)
                    throw new InvalidOperationException("Rhino could not add the parking mesh.");
            }
        }
    }
}
