
/// Changes from the Java version
///   Removed BST code, but not all artifacts of it
/// Future possibilities
///   Eliminate Add/RemoveNode ?
///   Comments comments and more comments!

using System.Text;
using System;

namespace Poly2Tri {
	/**
	 * @author Thomas Åhlen (thahlen@gmail.com)
	 */
	public class AdvancingFront {
		public AdvancingFrontNode Head;
		public AdvancingFrontNode Tail;
		protected AdvancingFrontNode Search;

		public AdvancingFront( AdvancingFrontNode head, AdvancingFrontNode tail ) {
			this.Head = head;
			this.Tail = tail;
			this.Search = head;
			AddNode(head);
			AddNode(tail);
		}

		public void AddNode( AdvancingFrontNode node ) { }
		public void RemoveNode( AdvancingFrontNode node ) { }

        // node head에서 tail로 가면서, "->"를 추가한다.
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			AdvancingFrontNode node = Head;
			while (node != Tail) {
				sb.Append(node.Point.X).Append("->");
				node = node.Next;
			}
			sb.Append(Tail.Point.X);
			return sb.ToString();
		}

		/// <summary>
		/// MM:  This seems to be used by LocateNode to guess a position in the implicit linked list of AdvancingFrontNodes near x
		///      Removed an overload that depended on this being exact
		/// </summary>
		private AdvancingFrontNode FindSearchNode( double x ) {
			return Search;
		}

		/// <summary>
		/// We use a balancing tree to locate a node smaller or equal to given key value (in theory)
		/// </summary>
        /// 노드가 작거나 지정된 키값과 동일하게 위치하기 위해 balance tree를 사용합니다.
        /// 해당 점을 가진 노드를 반환합니다.
		public AdvancingFrontNode LocateNode( TriangulationPoint point ) {
			return LocateNode(point.X);
		}

		private AdvancingFrontNode LocateNode( double x ) {
			AdvancingFrontNode node = FindSearchNode(x);
			if (x < node.Value) {
				while ((node = node.Prev) != null)
					if (x >= node.Value) {
						Search = node;
						return node;
					}
			} else {
				while ((node = node.Next) != null)
					if (x < node.Value) {
						Search = node.Prev;
						return node.Prev;
					}
			}
			return null;
		}

		/// <summary>
		/// This implementation will use simple node traversal algorithm to find a point on the front
		/// </summary>
		public AdvancingFrontNode LocatePoint( TriangulationPoint point ) {
			double px = point.X;
			AdvancingFrontNode node = FindSearchNode(px);
			double nx = node.Point.X;

			if (px == nx) {
				if (point != node.Point) {
					// We might have two nodes with same x value for a short time
					if (point == node.Prev.Point) {
						node = node.Prev;
					} else if (point == node.Next.Point) {
						node = node.Next;
					} else {
						throw new Exception("Failed to find Node for given afront point");
					}
				}
			} else if (px < nx) {
				while ((node = node.Prev) != null) 
                    if (point == node.Point) 
                        break;
			} else {
				while ((node = node.Next) != null) 
                    if (point == node.Point) 
                        break;
			}
			Search = node;
			return node;
		}
	}
}
