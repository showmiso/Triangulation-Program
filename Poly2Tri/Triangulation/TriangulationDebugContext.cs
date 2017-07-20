﻿
namespace Poly2Tri {
	public abstract class TriangulationDebugContext {
		protected TriangulationContext _tcx;

		public TriangulationDebugContext(TriangulationContext tcx) {
			_tcx = tcx;
		}

		public abstract void Clear();
	}
}
