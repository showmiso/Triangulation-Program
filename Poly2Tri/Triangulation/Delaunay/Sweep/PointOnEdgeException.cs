
using System;

namespace Poly2Tri {
	public class PointOnEdgeException : NotImplementedException {
		public readonly TriangulationPoint A,B,C;

		public PointOnEdgeException( string message, TriangulationPoint a, TriangulationPoint b, TriangulationPoint c )
			: base(message)
		{
			A=a;
			B=b;
			C=c;
		}
	}
}
