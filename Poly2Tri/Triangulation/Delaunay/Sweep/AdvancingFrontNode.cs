
/// Changes from the Java version
///   Removed getters
///   Has* turned into attributes
/// Future possibilities
///   Comments!

namespace Poly2Tri {
	public class AdvancingFrontNode {
		public AdvancingFrontNode Next;
		public AdvancingFrontNode Prev;
		public double             Value;
		public TriangulationPoint Point;
		public DelaunayTriangle   Triangle;

		public AdvancingFrontNode(TriangulationPoint point) {
			this.Point = point;
			Value = point.X;
		}

		public bool HasNext { get { return Next != null; } }
		public bool HasPrev { get { return Prev != null; } }
	}
}
