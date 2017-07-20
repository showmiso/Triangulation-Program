
using System.Collections.Generic;

namespace Poly2Tri {
	public class TriangulationPoint {
		// List of edges this point constitutes an upper ending point (CDT)
		public List<DTSweepConstraint> Edges { get; private set; }
        public List<DTSweepConstraint> BEdges { get; private set; }

		public TriangulationPoint( double x, double y ) { X=x; Y=y; }

		public override string ToString() {
			return "[" + X + "," + Y + "]";
		}

		public double X,Y;
        // float으로 변경해준다. OnPaint에서 출력할때?
		public float Xf { get { return (float)X; } set { X=value; } }
		public float Yf { get { return (float)Y; } set { Y=value; } }

        // 엣지를 추가한다.
		public void AddEdge(DTSweepConstraint e) {
			if (Edges == null) 
                Edges = new List<DTSweepConstraint>();
			Edges.Add(e);
		}

        // 엣지리스트가 null이면 return false
		public bool HasEdges { get { return Edges != null; } }

        // DTSweepConstraint와 같이 수정한다.???
        // Breakline
        // 엣지를 추가한다.
        public void AddBEdge(DTSweepConstraint e)
        {
            if (BEdges == null)
                BEdges = new List<DTSweepConstraint>();
            BEdges.Add(e);
        }

        // 엣지리스트가 null이면 return false
        public bool HasBEdges { get { return BEdges != null; } }
	}
}