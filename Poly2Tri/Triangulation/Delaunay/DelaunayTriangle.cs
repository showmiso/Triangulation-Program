/// Changes from the Java version
///   attributification
/// Future possibilities
///   Flattening out the number of indirections
///     Replacing arrays of 3 with fixed-length arrays?
///     Replacing bool[3] with a bit array of some sort?
///     Bundling everything into an AoS mess?
///     Hardcode them all as ABC ?

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Poly2Tri {
	public class DelaunayTriangle {
		public FixedArray3<TriangulationPoint> Points;          // 딱 3개의 점
		public FixedArray3<DelaunayTriangle  > Neighbors;       // 딱 3개의 이웃 델로니 삼각형
        // 3개의 엣지가 Constrained인지, Delaunay인지 판단하는 변수
        // Constrained : BreakLine
        // Delaunay : 일반 삼각망
        public FixedBitArray3 EdgeIsConstrained, EdgeIsDelaunay;    
        public bool IsInterior { get; set; }
        public bool IsChecked { get; set; }

        // 내가 임의로 만든 생성자
        public DelaunayTriangle() { }
        public void InitDelaunayTriangle(TriangulationPoint p1, TriangulationPoint p2, TriangulationPoint p3)
        {
            Points[0] = p1;
            Points[1] = p2;
            Points[2] = p3;
        }

		public DelaunayTriangle(TriangulationPoint p1, TriangulationPoint p2, TriangulationPoint p3) {
			Points[0] = p1;
			Points[1] = p2;
			Points[2] = p3;
		}

        // 삼각형의 3점 중 p점의 index를 구한다.
		public int IndexOf(TriangulationPoint p) {
			int i = Points.IndexOf(p);
			if (i==-1) 
                throw new Exception("Calling index with a point that doesn't exist in triangle");
			return i;
		}

		public int IndexCWFrom (TriangulationPoint p) { return (IndexOf(p)+2)%3; }
		public int IndexCCWFrom(TriangulationPoint p) { return (IndexOf(p)+1)%3; }

        // 점을 포함하는지 확인한다.
		public bool Contains(TriangulationPoint p) { return Points.Contains(p); }

		/// <summary>
		/// Update neighbor pointers
		/// </summary>
		/// <param name="p1">Point 1 of the shared edge</param>
		/// <param name="p2">Point 2 of the shared edge</param>
		/// <param name="t">This triangle's new neighbor</param>
		private void MarkNeighbor( TriangulationPoint p1, TriangulationPoint p2, DelaunayTriangle t ) {
			int i = EdgeIndex(p1,p2);
			if ( i==-1 ) 
                throw new Exception( "Error marking neighbors -- t doesn't contain edge p1-p2!" );
			Neighbors[i] = t;
		}

		/// <summary>
		/// Exhaustive search to update neighbor pointers
		/// </summary>
        // 
		public void MarkNeighbor( DelaunayTriangle t ) {
			// Points of this triangle also belonging to t
			bool a = t.Contains(Points[0]);
			bool b = t.Contains(Points[1]);
			bool c = t.Contains(Points[2]);

			if (b&&c) { 
                Neighbors[0]=t; 
                t.MarkNeighbor(Points[1],Points[2],this); 
            } else if (a&&c) { 
                Neighbors[1]=t; 
                t.MarkNeighbor(Points[0],Points[2],this); 
            } else if (a&&b) { 
                Neighbors[2]=t; 
                t.MarkNeighbor(Points[0],Points[1],this); 
            }
			else throw new Exception( "Failed to mark neighbor, doesn't share an edge!");
		}

		/// <param name="t">Opposite triangle</param>
		/// <param name="p">The point in t that isn't shared between the triangles</param>
        // t안에 점은 삼각형 사이에 공유되지 않습니다.
		public TriangulationPoint OppositePoint(DelaunayTriangle t, TriangulationPoint p) {
			Debug.Assert(t != this, "self-pointer error");
			return PointCWFrom(t.PointCWFrom(p));
		}
		
        // EdgeEvent, RotateTrianglePair 함수에서 쓰임
        // (Points.IndexOf(point)+1)%3의 인덱스를 가진 이웃 삼각형을 return한다. 
		public DelaunayTriangle NeighborCWFrom    (TriangulationPoint point) { return Neighbors[(Points.IndexOf(point)+1)%3]; }
		public DelaunayTriangle NeighborCCWFrom   (TriangulationPoint point) { return Neighbors[(Points.IndexOf(point)+2)%3]; }
        // FlipEdgeEvent, FlipScanEdgeEvent 함수에서 쓰임 
		public DelaunayTriangle NeighborAcrossFrom(TriangulationPoint point) { return Neighbors[ Points.IndexOf(point) ]; }

        // 많이 쓰임
        public TriangulationPoint PointCWFrom(TriangulationPoint point) { return Points[(IndexOf(point) + 2) % 3]; }        // 이 전점
		public TriangulationPoint PointCCWFrom(TriangulationPoint point) { return Points[(IndexOf(point)+1)%3]; }           // 이 후점

        // DTSweep의 RotateTrianglePair의 Assist 함수
        // 0 1 2 -> 1 2 0 으로 점 순서를 변경한다.
		private void RotateCW() {
			var t = Points[2];
			Points[2] = Points[1];
			Points[1] = Points[0];
			Points[0] = t;
		}

        // DTSweep의 RotateTrianglePair의 Assist 함수
		/// <summary>
		/// Legalize triangle by rotating clockwise around oPoint
		/// </summary>
		/// <param name="oPoint">The origin point to rotate around</param>
		/// <param name="nPoint">???</param>
        // 삼각형의 IndexCCWFrom(oPoint)번째 인덱스를 nPoint로 바꾼다.
        public void Legalize(TriangulationPoint oPoint, TriangulationPoint nPoint) {
			RotateCW();
			Points[IndexCCWFrom(oPoint)] = nPoint;
		}

		public override string ToString() { return Points[0] + "," + Points[1] + "," + Points[2]; }

		/// <summary>
		/// Finalize edge marking
		/// </summary>
        /// 엣지 마킹 마무리
        // 이웃 엣지에 표시한다. 
		public void MarkNeighborEdges() {
			for (int i = 0; i < 3; i++) 
                if ( EdgeIsConstrained[i] && Neighbors[i] != null ) 
                {
				    Neighbors[i].MarkConstrainedEdge(Points[(i+1)%3], Points[(i+2)%3]);
			    }
		}

		public void MarkEdge(DelaunayTriangle triangle) {
			for (int i = 0; i < 3; i++) 
                if ( EdgeIsConstrained[i] ) 
                {
				    triangle.MarkConstrainedEdge(Points[(i+1)%3], Points[(i+2)%3]);
			    }
		}

		public void MarkEdge(List<DelaunayTriangle> tList) {
			foreach ( DelaunayTriangle t in tList )
			    for ( int i = 0; i < 3; i++ )
			        if ( t.EdgeIsConstrained[i] )
			        {
				        MarkConstrainedEdge( t.Points[(i+1)%3], t.Points[(i+2)%3] );
			        }
		}

		public void MarkConstrainedEdge(int index) {
			EdgeIsConstrained[index] = true;
		}

		public void MarkConstrainedEdge(DTSweepConstraint edge) {
			MarkConstrainedEdge(edge.P, edge.Q);
		}

		/// <summary>
		/// Mark edge as constrained
		/// </summary>
        /// BreakLine이라고 설정한다.
		public void MarkConstrainedEdge(TriangulationPoint p, TriangulationPoint q) {
            // 두 점의 엣지 인덱스가 -1이 아니라면, 
			int i = EdgeIndex(p,q);
			if ( i != -1 ) 
                EdgeIsConstrained[i] = true;
		}

		public double Area() {
			double b = Points[0].X - Points[1].X;
			double h = Points[2].Y - Points[1].Y;

			return Math.Abs((b * h * 0.5f));
		}

		public TriangulationPoint Centroid() {
			double cx = (Points[0].X + Points[1].X + Points[2].X) / 3f;
			double cy = (Points[0].Y + Points[1].Y + Points[2].Y) / 3f;
			return new TriangulationPoint(cx, cy);
		}

		/// <summary>
		/// Get the index of the neighbor that shares this edge (or -1 if it isn't shared)
		/// </summary>
		/// <returns>index of the shared edge or -1 if edge isn't shared</returns>
        /// 엣지를 공유하는 이웃의 인덱스를 가져옵니다.
        /// 엣지를 공유하지 않는 경우 -1을 return하고, 그 외에는 공유한 엣지의 인덱스를 return합니다.
        /// 굉장히 중요한 함수!!!
        // p1, p2의 삼각형 에서의 인덱스를 얻고, 
        // 인덱스 값에 따라서 어떤 엣지인지, 그 엣지의 인덱스를 return한다.
		public int EdgeIndex(TriangulationPoint p1, TriangulationPoint p2) {
            // 두 점의 인덱스를 구하고, 
			int i1 = Points.IndexOf(p1);
			int i2 = Points.IndexOf(p2);

			// Points of this triangle in the edge p1-p2
            // p1-p2 엣지 삼각형의 점
			bool a = (i1==0 || i2==0);
			bool b = (i1==1 || i2==1);
			bool c = (i1==2 || i2==2);

			if (b&&c) return 0;
			if (a&&c) return 1;
			if (a&&b) return 2;
			return -1;
		}

		public bool GetConstrainedEdgeCCW   ( TriangulationPoint p ) { return EdgeIsConstrained[(IndexOf(p)+2)%3]; }
		public bool GetConstrainedEdgeCW    ( TriangulationPoint p ) { return EdgeIsConstrained[(IndexOf(p)+1)%3]; }
		public bool GetConstrainedEdgeAcross( TriangulationPoint p ) { return EdgeIsConstrained[ IndexOf(p)     ]; }
		public void SetConstrainedEdgeCCW   ( TriangulationPoint p, bool ce ) { EdgeIsConstrained[(IndexOf(p)+2)%3] = ce; }
		public void SetConstrainedEdgeCW    ( TriangulationPoint p, bool ce ) { EdgeIsConstrained[(IndexOf(p)+1)%3] = ce; }
		public void SetConstrainedEdgeAcross( TriangulationPoint p, bool ce ) { EdgeIsConstrained[ IndexOf(p)     ] = ce; }

		public bool GetDelaunayEdgeCCW   ( TriangulationPoint p ) { return EdgeIsDelaunay[(IndexOf(p)+2)%3]; }
		public bool GetDelaunayEdgeCW    ( TriangulationPoint p ) { return EdgeIsDelaunay[(IndexOf(p)+1)%3]; }
		public bool GetDelaunayEdgeAcross( TriangulationPoint p ) { return EdgeIsDelaunay[ IndexOf(p)     ]; }
		public void SetDelaunayEdgeCCW   ( TriangulationPoint p, bool ce ) { EdgeIsDelaunay[(IndexOf(p)+2)%3] = ce; }
		public void SetDelaunayEdgeCW    ( TriangulationPoint p, bool ce ) { EdgeIsDelaunay[(IndexOf(p)+1)%3] = ce; }
		public void SetDelaunayEdgeAcross( TriangulationPoint p, bool ce ) { EdgeIsDelaunay[ IndexOf(p)     ] = ce; }
	}
}
