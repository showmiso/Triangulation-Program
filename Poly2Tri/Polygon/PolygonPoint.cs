
/// Changes from the Java version
///   Replaced get/set Next/Previous with attributes
/// Future possibilities
///   Documentation!

namespace Poly2Tri {
	public class PolygonPoint : TriangulationPoint {
		public PolygonPoint( double x, double y ) : base(x, y) { }

		public PolygonPoint Next { get; set; }
		public PolygonPoint Previous { get; set; }
	}
}
