using System.Collections.Generic;

namespace Poly2Tri {
	public class DTSweepContext : TriangulationContext {
		// Inital triangle factor, seed triangle will extend 30% of 
		// PointSet width to both left and right.
        // 삼각형의 요소를 초기화한다. 
        // 시드 삼각형 좌우의 점 폭의 30%를 연장한다.
		private readonly float ALPHA = 0.3f;

		public AdvancingFront     Front;
		public TriangulationPoint Head { get; set; }
		public TriangulationPoint Tail { get; set; }

        // 둘 다 DTSweep에서 사용하는 변수이다.
        // left, right, bottom과 width, leftHighest를 가지고 있다. 
		public DTSweepBasin     Basin     = new DTSweepBasin();
        // ConstrainedEdge와 bool을 가지고 있는 Edge
		public DTSweepEdgeEvent EdgeEvent = new DTSweepEdgeEvent();

        // 점을 어떻게 정렬한다, 이런 것 같은데 요상하게 된다.
		private DTSweepPointComparator _comparator = new DTSweepPointComparator();

		public DTSweepContext() {
			Clear();
		}

        //public override bool IsDebugEnabled { get {
        //    return base.IsDebugEnabled;
        //} protected set {
        //    if (value && DebugContext == null) DebugContext = new DTSweepDebugContext(this);
        //    base.IsDebugEnabled = value;
        //}}

        // 안 불려짐
		public void RemoveFromList( DelaunayTriangle triangle ) {
			Triangles.Remove(triangle);
			// TODO: remove all neighbor pointers to this triangle
			//        for( int i=0; i<3; i++ )
			//        {
			//            if( triangle.neighbors[i] != null )
			//            {
			//                triangle.neighbors[i].clearNeighbor( triangle );
			//            }
			//        }
			//        triangle.clearNeighbors();
		}

        // 내가 임의로 만든 함수.
        private bool IsCanBeTriangle(DelaunayTriangle triangle)
        {
            TriangulationPoint p1 = triangle.Points[0];
            TriangulationPoint p2 = triangle.Points[1];
            TriangulationPoint p3 = triangle.Points[2];

            // 값이 아닐때만 삼각망에 넣는다.
            if (TriangulationUtil.IsSamePoint(p1, Head)
                || TriangulationUtil.IsSamePoint(p2, Head)
                || TriangulationUtil.IsSamePoint(p3, Head)
                || TriangulationUtil.IsSamePoint(p1, Tail)
                || TriangulationUtil.IsSamePoint(p2, Tail)
                || TriangulationUtil.IsSamePoint(p3, Tail))
                return false;

            //if ((p1 != Head) && (p2 != Head) && (p3 != Head)
            //    && (p1 != Tail) && (p2 != Tail) && (p3 != Tail))
            //    return true;
            return true;
        }

        // MeshClean와 MeshCleanReq는 초기 설정을 위한 함수입니다. 
        // 제약된 엣지(Breakline)을 사용해 내부 삼각형을 수집합니다.
		public void MeshClean( DelaunayTriangle triangle ) {
            //MeshCleanReq(triangle);
            //int num = Triangulatable.Triangles.Count;

            List<DelaunayTriangle> triList = new List<DelaunayTriangle>();

            MakeDynamicArray(triangle, ref triList);

            // 삼각형 재생성
            MeshCleanReq(triList);
		}

        private void MeshCleanReq(List<DelaunayTriangle> triList)
        {
            foreach (DelaunayTriangle triangle in triList)
            {
                if (triangle != null && !triangle.IsInterior && IsCanBeTriangle(triangle) == true)
                {
                    triangle.IsInterior = true;
                    Triangulatable.AddTriangle(triangle);
                }
            }
        }

        private void MakeDynamicArray(DelaunayTriangle triangle, ref List<DelaunayTriangle> triList)
        {
            // triangle이 null이 아니고, 
            // check되지 않았고, 최대 삼각형에 속하지 않는다면, 
            int head = 0;

            if (triangle != null && triangle.IsChecked == false)// && IsCanBeTriangle(triangle) == true)
            {
                triangle.IsChecked = true;
                triList.Add(triangle);
            }

            while (head < triList.Count)
            {
                // triangle이 null이 아니고, check되지 않았고, 
                // 최대 삼각형에 속하지 않는다면, EdgeIsConstrained가 아니라면, 
                for (int i = 0; i < 3; i++)
                    if (triangle.Neighbors[i] != null 
                        && triangle.Neighbors[i].IsChecked == false 
                        //&& IsCanBeTriangle(triangle.Neighbors[i]) == true
                        && triangle.EdgeIsConstrained[i] == false)
                    {
                        triangle.Neighbors[i].IsChecked = true;
                        triList.Add(triangle.Neighbors[i]);
                    }

                if (head + 1 >= triList.Count) break;

                triangle = triList[++head];
            }
        }

        //// 재귀 함수
        //// Triangulate -> FinalizationPolygon -> MeshClean 에서 불려진다. 
        //private void MeshCleanReq( DelaunayTriangle triangle ) {
        //    if (triangle != null && !triangle.IsInterior) {
        //        triangle.IsInterior = true;
        //        // Polygon에 triangle을 추가한다.
        //        // 이 부분에서 Head와 Tail이 포함된 삼각형을 삭제해준다.
        //        if (IsCanBeTriangle(triangle))
        //            Triangulatable.AddTriangle(triangle);

        //        for (int i = 0; i < 3; i++)
        //            if (!triangle.EdgeIsConstrained[i])
        //            {
        //                MeshCleanReq(triangle.Neighbors[i]);
        //            }
        //    }
        //}

		public override void Clear() {
			base.Clear();
			Triangles.Clear();
		}

        // 노드 추가라는데 내부에 아무 것도 없음
		public void AddNode( AdvancingFrontNode node ) {
			//        Console.WriteLine( "add:" + node.key + ":" + System.identityHashCode(node.key));
			//        m_nodeTree.put( node.getKey(), node );
			Front.AddNode(node);
		}

        // 내부에 아무 것도 없음
		public void RemoveNode( AdvancingFrontNode node ) {
			//        Console.WriteLine( "remove:" + node.key + ":" + System.identityHashCode(node.key));
			//        m_nodeTree.delete( node.getKey() );
			Front.RemoveNode(node);
		}

        // 해당 점을 가진 노드를 반환합니다.
		public AdvancingFrontNode LocateNode( TriangulationPoint point ) {
			return Front.LocateNode(point);
		}

        // 처음 Triangulate 생성할 때, 불려지는 함수로
        // AdvancingFront Front를 초기화한다.
		public void CreateAdvancingFront() {
			AdvancingFrontNode head, tail, middle;
			// Initial triangle
            // DelaunayTriangle 초기화하고, Triangles에 iTriangle을 추가한다. 
			DelaunayTriangle iTriangle = new DelaunayTriangle(Points[0], Tail, Head);
			Triangles.Add(iTriangle);

			head = new AdvancingFrontNode(iTriangle.Points[1]);
			head.Triangle = iTriangle;
			middle = new AdvancingFrontNode(iTriangle.Points[0]);
			middle.Triangle = iTriangle;
			tail = new AdvancingFrontNode(iTriangle.Points[2]);

            // head와 tail로 Front 초기화하고, middle을 추가한다.
			Front = new AdvancingFront(head, tail);
			Front.AddNode(middle);

			// TODO: I think it would be more intuitive if head is middles next and not previous
			//       so swap head and tail
            // head가 middle의 이전이 아니라 다음인게 더 직관적인 것 같다.
            // 그래서 head와 tail을 swap했다.
			Front.Head.Next = middle;
			middle.Next = Front.Tail;
			middle.Prev = Front.Head;
			Front.Tail.Prev = middle;
		}

		/// <summary>
		/// Try to map a node to all sides of this triangle that don't have 
		/// a neighbor.
		/// </summary>
        /// 이웃을 가지지 않는 삼각형의 모든 측면에 노드를 mapping하는 함수
        // neighbor를 가지지 않는 삼각형에 node를 연결하는 함수이다.
		public void MapTriangleToNodes( DelaunayTriangle t ) {
			for (int i = 0; i < 3; i++)
			    if (t.Neighbors[i] == null)
                {
                    // 이웃 삼각형이 없는 index(Edge)라면, 
                    TriangulationPoint pt = t.Points[i];                // 현재 점을 구하고, 
                    TriangulationPoint point = t.PointCWFrom(pt);       // 그 전?점을 구하고, 
				    AdvancingFrontNode n = Front.LocatePoint(point);    // 그걸로 된 노드를 구해서, 
				    if (n != null) 
                        n.Triangle = t;     // 그 노드에 현재 삼각형을 추가한다.
    			}
		}

        // 삼각망 생성을 준비한다.
		public override void PrepareTriangulation( Triangulatable t ) {
			base.PrepareTriangulation(t);

			double xmax, xmin;
			double ymax, ymin;

			xmax = xmin = Points[0].X;
			ymax = ymin = Points[0].Y;

			// Calculate bounds. Should be combined with the sorting
            // 경계를 계산합니다. 정렬과 결합되어 있어야 합니다.
			foreach (TriangulationPoint p in Points) {
				if (p.X > xmax) xmax = p.X;
				if (p.X < xmin) xmin = p.X;
				if (p.Y > ymax) ymax = p.Y;
				if (p.Y < ymin) ymin = p.Y;
			}

			double deltaX = ALPHA * (xmax - xmin);
			double deltaY = ALPHA * (ymax - ymin);

            // 점을 계산한다.
			TriangulationPoint p1 = new TriangulationPoint(xmax + deltaX, ymin - deltaY);
			TriangulationPoint p2 = new TriangulationPoint(xmin - deltaX, ymin - deltaY);

			Head = p1;
			Tail = p2;

			//long time = System.nanoTime();
			// Sort the points along y-axis
            // 
			Points.Sort(_comparator);
			//logger.info( "Triangulation setup [{}ms]", ( System.nanoTime() - time ) / 1e6 );
		}

        // 삼각망 마무으리
        // 여기에 안들어온다.
		public void FinalizeTriangulation() {
			Triangulatable.AddTriangles(Triangles);
			Triangles.Clear();
		}

        public override TriangulationConstraint NewConstraint(TriangulationPoint a, TriangulationPoint b, bool IsBEdge = false) {
            return new DTSweepConstraint(a, b, IsBEdge);
        }

        //// DTSweepConstraint와 함께 수정!!!!!!!!!!!!!
        //public override TriangulationConstraint NewConstraint( TriangulationPoint a, TriangulationPoint b ) {
        //    return new DTSweepConstraint(a, b);
        //}
        
        //// Breakline 
        //public override TriangulationConstraint NewConstraint(TriangulationPoint a, TriangulationPoint b, int num)
        //{
        //    return new DTSweepConstraint(a, b, num);
        //}

		//public override TriangulationAlgorithm Algorithm { get { return TriangulationAlgorithm.DTSweep; }}
	}
}
