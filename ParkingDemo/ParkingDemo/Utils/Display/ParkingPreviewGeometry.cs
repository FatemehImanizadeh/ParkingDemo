using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;

namespace ParkingDemo.Utils
{
    /// <summary>
    /// Pairs a single piece of geometry (Brep or Curve, typically) with the
    /// color it should be shown / baked with. This is the atomic unit that
    /// both the live GH preview and the Bake command consume.
    /// </summary>
    public class GeometryColorPair
    {
        public GeometryBase Geometry { get; set; }
        public Color Color { get; set; }

        public GeometryColorPair() { }

        public GeometryColorPair(GeometryBase geometry, Color color)
        {
            Geometry = geometry;
            Color = color;
        }
    }

    /// <summary>
    /// Holds all the precomputed preview/bake geometry for a Parking object.
    ///
    /// This is meant to be built EXACTLY ONCE, right after the parking layout
    /// itself is generated (see ParkingPreviewGeometryBuilder.BuildAll), and
    /// stored on Parking.PreviewGeometry.
    ///
    /// From that point on:
    ///  - the preview/bake component just reads these lists to draw the
    ///    live OpenGL preview (no recomputation on every redraw)
    ///  - the same lists are baked as-is when the user presses the Bake
    ///    button (no recomputation on bake either)
    ///
    /// Cars are intentionally NOT included here: they are Rhino block
    /// instances (InstanceObject), not raw geometry, so they still need the
    /// Car Block reference at preview/bake time. Parking.CarTransforms
    /// already carries the placement data for them.
    /// </summary>
    public class ParkingPreviewGeometry
    {
        public List<GeometryColorPair> GradientCells { get; set; } = new List<GeometryColorPair>();
        public List<GeometryColorPair> ExcludedCells { get; set; } = new List<GeometryColorPair>();
        public List<GeometryColorPair> PathRibbons { get; set; } = new List<GeometryColorPair>();
        public GeometryColorPair EntranceCell { get; set; }
        public List<GeometryColorPair> Walls { get; set; } = new List<GeometryColorPair>();
    }
}
