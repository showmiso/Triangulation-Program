
/**
 * Forces a triangle edge between two points p and q
 * when triangulating. For example used to enforce
 * Polygon Edges during a polygon triangulation.
 * 
 * @author Thomas Åhlén, thahlen@gmail.com
 */

namespace Poly2Tri {
	public class TriangulationConstraint {
		public TriangulationPoint P;
		public TriangulationPoint Q;

        // 내가 추가
        public TriangulationConstraint() { }
        public TriangulationConstraint(TriangulationPoint _P, TriangulationPoint _Q) {
            P = _P; Q = _Q;
        }

	}
}
