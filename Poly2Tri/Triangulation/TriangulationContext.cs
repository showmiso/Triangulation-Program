
using System.Collections.Generic;

namespace Poly2Tri {
	public abstract class TriangulationContext {
        // DTSweepContext가 상속하는 추상 클래스
		public TriangulationDebugContext DebugContext { get; protected set; }

        // 삼각형
		public readonly List<DelaunayTriangle> Triangles = new List<DelaunayTriangle>();
        // 점
		public readonly List<TriangulationPoint> Points = new List<TriangulationPoint>();
		//public TriangulationMode TriangulationMode { get; protected set; }
		public Triangulatable Triangulatable { get; private set; }      // Polygon, PointSet

		public int StepCount { get; private set; }

		public void Done() {
			StepCount++;
		}

        //// TriangulationAlgorithm == DTSweep
		//public abstract TriangulationAlgorithm Algorithm { get; }

        // Polygon의 삼각망 생성을 준비한다.
		public virtual void PrepareTriangulation(Triangulatable t) {
			Triangulatable = t;
			//TriangulationMode = t.TriangulationMode;
			t.Prepare(this);
		}

        public abstract TriangulationConstraint NewConstraint(TriangulationPoint a, TriangulationPoint b, bool IsBEdge = false);
        
        //// DTSweepConstraint와 함께 수정!!!!!!!!!!!!!
        //public abstract TriangulationConstraint NewConstraint(TriangulationPoint a, TriangulationPoint b);
        //// Breakline
        //public abstract TriangulationConstraint NewConstraint(TriangulationPoint a, TriangulationPoint b, int num);

		public void Update(string message) {}

		public virtual void Clear() {
			Points.Clear();
            //if (DebugContext != null) 
            //    DebugContext.Clear();
			StepCount = 0;
		}

		public virtual bool IsDebugEnabled { get; protected set; }

		public DTSweepDebugContext DTDebugContext { get { return DebugContext as DTSweepDebugContext; } }
	}
}