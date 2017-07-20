// 함수 하나로 합치기
namespace Poly2Tri {
	public class DTSweepConstraint : TriangulationConstraint {
		/// <summary>
		/// Give two points in any order. Will always be ordered so
		/// that q.y > p.y and q.x > p.x if same y value 
		/// </summary>
        /// 임의의 순서로 두 점을 지정합니다.
        /// 항상 정렬되어 있을 때 같은 y값일 때 
        /// q.y > p.y, q.x > p.x 의 결과를 낼 수 있습니다.
        /// 
        public DTSweepConstraint(TriangulationPoint p1, TriangulationPoint p2, bool IsBEdge = false)
        {
            P = p1;
            Q = p2;
            if (p1.Y > p2.Y)
            {
                Q = p1;
                P = p2;
            }
            else if (p1.Y == p2.Y)
            {
                if (p1.X > p2.X)
                {
                    Q = p1;
                    P = p2;
                }
                else if (p1.X == p2.X)
                {
                    //logger.info( "Failed to create constraint {}={}", p1, p2 );
                    //throw new DuplicatePointException( p1 + "=" + p2 );
                    //return;
                }
            }
            if (IsBEdge)
                Q.AddBEdge(this);
            else Q.AddEdge(this);

        }

        //public DTSweepConstraint( TriangulationPoint p1, TriangulationPoint p2 ) {
        //    P = p1;
        //    Q = p2;
        //    if (p1.Y > p2.Y) {
        //        Q = p1;
        //        P = p2;
        //    } else if (p1.Y == p2.Y) {
        //        if (p1.X > p2.X) {
        //            Q = p1;
        //            P = p2;
        //        } else if (p1.X == p2.X) {
        //            //logger.info( "Failed to create constraint {}={}", p1, p2 );
        //            //throw new DuplicatePointException( p1 + "=" + p2 );
        //            //return;
        //        }
        //    }
        //    Q.AddEdge(this);
        //}
        //// Breakline
        //public DTSweepConstraint(TriangulationPoint p1, TriangulationPoint p2, int num)
        //{
        //    P = p1;
        //    Q = p2;
        //    if (p1.Y > p2.Y)
        //    {
        //        Q = p1;
        //        P = p2;
        //    }
        //    else if (p1.Y == p2.Y)
        //    {
        //        if (p1.X > p2.X)
        //        {
        //            Q = p1;
        //            P = p2;
        //        }
        //        else if (p1.X == p2.X)
        //        {
        //            //logger.info( "Failed to create constraint {}={}", p1, p2 );
        //            //throw new DuplicatePointException( p1 + "=" + p2 );
        //            //return;
        //        }
        //    }
        //    Q.AddBEdge(this);
        //}
	}
}
