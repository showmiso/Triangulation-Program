/// Changes from the Java version
///   Polygon constructors spruced up, checks for 3+ polys
///   Naming of everything
///   getTriangulationMode() -> TriangulationMode { get; }
///   Exceptions replaced
/// Future possibilities
///   We have a lot of Add/Clear methods -- we may prefer to just expose the container
///   Some self-explanatory methods may deserve commenting anyways

// 생성자에서 점 개수를 확인하고, 생성자를 public으로 해놓았다.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Poly2Tri {
	public class Polygon : Triangulatable {
		protected List<TriangulationPoint> _points;
        protected List<TriangulationPoint> _steinerPoints;
		protected List<Polygon> _holes;           
		protected List<DelaunayTriangle> _triangles;
        //protected PolygonPoint _last;                       // 안쓰임
        //protected List<TriangulationPoint> _tcxPoints;

        protected List<TriangulationPoint> _edgespoints;
        protected List<TriangulationConstraint> _edges;
        //public int[] _index;
        //protected ConstrainedPointSet _cps;

        ///// <summary>
        ///// Create a polygon from a list of at least 3 points with no duplicates.
        ///// </summary>
        ///// <param name="points">A list of unique points</param>
        //public Polygon(IList<PolygonPoint> points)
        //{
        //    if (points.Count < 3)
        //        throw new ArgumentException("List has fewer than 3 points", "points");

        //    // Lets do one sanity check that first and last point hasn't got same position
        //    // Its something that often happen when importing polygon data from other formats
        //    // 첫번째 점과 마지막 점이 같은지 확인하고, 같다면 마지막 점을 지운다.
        //    if (points[0].Equals(points[points.Count - 1]))
        //        points.RemoveAt(points.Count - 1);

        //    _points.AddRange(points.Cast<TriangulationPoint>());
        //}

        public Polygon(IList<PolygonPoint> points, IList<PolygonPoint> steinerpoints, IList<PolygonPoint> edgespoints)
        {
            if (points == null || points.Count < 3)
                throw new ArgumentException("List has fewer than 3 points", "points");

            // Lets do one sanity check that first and last point hasn't got same position
            // Its something that often happen when importing polygon data from other formats
            // 첫번째 점과 마지막 점이 같은지 확인하고, 같다면 마지막 점을 지운다.
            if (points[0].Equals(points[points.Count - 1]))
                points.RemoveAt(points.Count - 1);

            _points = new List<TriangulationPoint>();
            _points.AddRange(points.Cast<TriangulationPoint>());

            //
            if (steinerpoints != null && steinerpoints.Count > 0)
            {
                _steinerPoints = new List<TriangulationPoint>();
                _steinerPoints.AddRange(steinerpoints.Cast<TriangulationPoint>());
            }
            //
            if (edgespoints != null && edgespoints.Count > 0)
            {
                _edgespoints = new List<TriangulationPoint>();
                _edgespoints.AddRange(edgespoints.Cast<TriangulationPoint>());
            }
        }

        ///// <summary>
        ///// Create a polygon from a list of at least 3 points with no duplicates.
        ///// </summary>
        ///// <param name="points">A list of unique points.</param>
        //public Polygon(IEnumerable<PolygonPoint> points)
        //    : this((points as IList<PolygonPoint>) ?? points.ToArray()) { }

        ///// <summary>
        ///// Create a polygon from a list of at least 3 points with no duplicates.
        ///// </summary>
        ///// <param name="points">A list of unique points.</param>
        //public Polygon( params PolygonPoint[] points ) 
        //    : this((IList<PolygonPoint>)points) { }

		//public TriangulationMode TriangulationMode { get { return TriangulationMode.Polygon; } }

		public void AddSteinerPoint( TriangulationPoint point ) {
			if (_steinerPoints == null) 
                _steinerPoints = new List<TriangulationPoint>();
			_steinerPoints.Add(point);
		}

		public void AddSteinerPoints( List<TriangulationPoint> points ) {
			if (_steinerPoints == null) 
                _steinerPoints = new List<TriangulationPoint>();
			_steinerPoints.AddRange(points);
		}

		public void ClearSteinerPoints() {
			if (_steinerPoints != null) 
                _steinerPoints.Clear();
		}

		/// <summary>
		/// Add a hole to the polygon.
		/// </summary>
		/// <param name="poly">A subtraction polygon fully contained inside this polygon.</param>
		public void AddHole( Polygon poly ) {
			if (_holes == null) 
                _holes = new List<Polygon>();
			_holes.Add(poly);
			// XXX: tests could be made here to be sure it is fully inside
			//        addSubtraction( poly.getPoints() );
		}

        ///// <summary>
        ///// Inserts newPoint after point.
        ///// </summary>
        ///// <param name="point">The point to insert after in the polygon</param>
        ///// <param name="newPoint">The point to insert into the polygon</param>
        //public void InsertPointAfter( PolygonPoint point, PolygonPoint newPoint ) {
        //    // Validate that 
        //    int index = _points.IndexOf(point);
        //    if (index == -1) throw new ArgumentException("Tried to insert a point into a Polygon after a point not belonging to the Polygon", "point");
        //    newPoint.Next = point.Next;
        //    newPoint.Previous = point;
        //    point.Next.Previous = newPoint;
        //    point.Next = newPoint;
        //    _points.Insert(index + 1, newPoint);
        //}

        ///// <summary>
        ///// Inserts list (after last point in polygon?)
        ///// </summary>
        ///// <param name="list"></param>
        //public void AddPoints( IEnumerable<PolygonPoint> list ) {
        //    PolygonPoint first;
        //    foreach (PolygonPoint p in list) {
        //        p.Previous = _last;
        //        if (_last != null) {
        //            p.Next = _last.Next;
        //            _last.Next = p;
        //        }
        //        _last = p;
        //        _points.Add(p);
        //    }
        //    first = (PolygonPoint)_points[0];
        //    _last.Next = first;
        //    first.Previous = _last;
        //}

        ///// <summary>
        ///// Adds a point after the last in the polygon.
        ///// </summary>
        ///// <param name="p">The point to add</param>
        //public void AddPoint( PolygonPoint p ) {
        //    p.Previous = _last;
        //    p.Next = _last.Next;
        //    _last.Next = p;
        //    _points.Add(p);
        //}

        ///// <summary>
        ///// Removes a point from the polygon.
        ///// </summary>
        ///// <param name="p"></param>
        //public void RemovePoint( PolygonPoint p ) {
        //    PolygonPoint next, prev;

        //    next = p.Next;
        //    prev = p.Previous;
        //    prev.Next = next;
        //    next.Previous = prev;
        //    _points.Remove(p);
        //}

        //// 엣지 추가
        //public void AddEdge(TriangulationPoint x, TriangulationPoint y)
        //{
        //    AddEdge(new TriangulationConstraint(x, y));
        //}

        //public void AddEdge(TriangulationConstraint tct)
        //{
        //    if (_edges == null)
        //        _edges = new List<TriangulationConstraint>();
        //    _edges.Add(tct);
        //}

		public IList<TriangulationPoint> Points { get { return _points; } }
		public IList<DelaunayTriangle> Triangles { get { return _triangles; } }
        public IList<Polygon> Holes { get { return _holes; } }
        public IList<TriangulationConstraint> Edges { get { return _edges; } }
        public IList<TriangulationPoint> SteinerPoints { get { return _steinerPoints; } }
        //public IList<TriangulationPoint> tcxPoints { get { return _tcxPoints; } }

        public void AddTriangle( DelaunayTriangle t ) {
			_triangles.Add(t);
		}

		public void AddTriangles( IEnumerable<DelaunayTriangle> list ) {
			_triangles.AddRange(list);
		}

		public void ClearTriangles() {
			if (_triangles != null) 
                _triangles.Clear();
		}

		/// <summary>
		/// Creates constraints and populates the context with points
		/// </summary>
		/// <param name="tcx">The context</param>
        /// 제약 조건을 생성하고, 점들의 Context를 채웁니다.
		public void Prepare( TriangulationContext tcx ) {
			if (_triangles == null) {
				_triangles = new List<DelaunayTriangle>(_points.Count);
			} else {
				_triangles.Clear();
			}

            // Outer constraints
            // 여기가 엣지로 설정하는 부분
            // DTSweepConstraint의 DTSweepConstraint함수로 간다.
            for (int i = 0; i < _points.Count - 1; i++)
                tcx.NewConstraint(_points[i], _points[i + 1]);
            tcx.NewConstraint(_points[0], _points[_points.Count - 1]);
			tcx.Points.AddRange(_points);

            //// 브레이크 라인 설정
            //if (_edges != null)
            //{
            //    for (int i = 0; i < _edges.Count; i++) {
            //        tcx.NewConstraint(_edges[i].P, _edges[i].Q);
            //    }
            //    tcx.Points.AddRange(_points);
            //}

            // 점 추가한 것도 아닌데 왜 여기서 오류가 발생하는 거지?
            //// 브레이크 라인 설정
            //if (_index != null)
            //{
            //    _cps = new ConstrainedPointSet(_points, _index);
            //    _cps.Prepare(tcx);
            //}

			// Hole constraints
			if (_holes != null) {
				foreach (Polygon p in _holes) {
					for (int i = 0; i < p._points.Count - 1; i++) 
                        tcx.NewConstraint(p._points[i], p._points[i + 1]);
					tcx.NewConstraint(p._points[0], p._points[p._points.Count - 1]);
					tcx.Points.AddRange(p._points);
				}
			}

            // 내부 점을 추가한 다음에
			if (_steinerPoints != null) {
				tcx.Points.AddRange(_steinerPoints);
            }

            //// 인덱스 정보를 넣는다.
            //if (_index != null) {
                
            //    // 인덱스 정보로 생성된 점 정보를 
            //    // List<TriangulationConstraint> _edges에 가지고 있는다. 
            //    if (_edges == null)
            //        _edges = new List<TriangulationConstraint>();

            //    for (int i = 0; i < _index.Length; i += 2) {
            //        tcx.NewConstraint(tcx.Points[_index[i]], tcx.Points[_index[i + 1]], true);

            //        _edges.Add(new TriangulationConstraint(tcx.Points[_index[i]], tcx.Points[_index[i + 1]]));
            //    }
            //}

            // points와 steinerpoint가 분리되어 있기 때문에 이렇게 정점으로 찾아야한다.
            if (_edgespoints != null)
            {
                if (_edges == null)
                    _edges = new List<TriangulationConstraint>();

                int a, b;
                for (int i = 0; i < _edgespoints.Count - 1; i += 2)
                {
                    a = -1;
                    b = -1;
                    for (int j = 0; j < tcx.Points.Count; j++)
                    {
                        if (TriangulationUtil.IsSamePoint(_edgespoints[i], tcx.Points[j]))
                            a = j;
                        else if (TriangulationUtil.IsSamePoint(_edgespoints[i + 1], tcx.Points[j]))
                            b = j;

                        if (a != -1 && b != -1)
                        {
                            tcx.NewConstraint(tcx.Points[a], tcx.Points[b], true);
                            _edges.Add(new TriangulationConstraint(tcx.Points[a], tcx.Points[b]));
                            break;
                        }
                    }
                }
            }

            //_tcxPoints = tcx.Points;

		}

	}
}
