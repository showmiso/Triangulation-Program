using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace MainProgram
{
    // 삼각형
    public class MyTriangle
    {
        public Point3d pt1, pt2, pt3;

        public MyTriangle() { }
        public MyTriangle(Point3d _pt1, Point3d _pt2, Point3d _pt3)
        {
            pt1 = _pt1; pt2 = _pt2; pt3 = _pt3;
        }
    };

    // 점
    public class MyPoint
    {
        public double X, Y, Z;
        public MyPoint() { }
        public MyPoint(Point3d pt) { X = pt.X; Y = pt.Y; Z = pt.Z; }
        public MyPoint(double _X, double _Y, double _Z) { X = _X; Y = _Y; Z = _Z; }
    };
    
    class ACadUtils
    {
        public const double eps = 1e-5;

        // 객체 선택 함수
        public static PromptSelectionResult SelectPointFunc(Editor ed, String strToSelect, String strMsg)
        {
            TypedValue[] tvs = { new TypedValue(0, strToSelect) };
            SelectionFilter sf = new SelectionFilter(tvs);

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = strMsg;
            pso.MessageForRemoval = strMsg;
            pso.AllowDuplicates = false;
            PromptSelectionResult psr = null;

            try
            {
                psr = ed.GetSelection(pso, sf);
            }
            catch (System.Exception ex)
            {
                if (ex is Autodesk.AutoCAD.Runtime.Exception)
                {
                    Autodesk.AutoCAD.Runtime.Exception aEs =
                        ex as Autodesk.AutoCAD.Runtime.Exception;

                    if (aEs.ErrorStatus != Autodesk.AutoCAD.Runtime.ErrorStatus.OK)
                    {
                        ed.WriteMessage("\nKeyword Entered: {0}", ex.Message);
                    }
                }
            }

            return psr;
        }

        // 점 정렬 함수
        internal class Sort3DPoint : IComparer<Point3d>
        {
            public int Compare(Point3d a, Point3d b)
            {
                if ((a.X == b.X) && (a.Y == b.Y))
                    return 0;

                if ((a.X < b.X) || ((a.X == b.X) && (a.Y < b.Y)))
                    return -1;

                return 1;
            }
        }

        // 점 정렬
        public static void ArrayPointList(ref List<Point3d> ptList)
        {
            if (ptList == null) return;

            Point3dCollection pts = new Point3dCollection();
            foreach (Point3d pt in ptList)
                pts.Add(pt);

            Point3d[] raw3dpt = new Point3d[ptList.Count];
            pts.CopyTo(raw3dpt, 0);
            Array.Sort(raw3dpt, new Sort3DPoint());

            Point3dCollection Sortedpts = new Point3dCollection(raw3dpt);

            ptList.Clear();
            ptList = null;

            ptList = new List<Point3d>();

            for (int i = 0; i < Sortedpts.Count; i++)
                ptList.Add(Sortedpts[i]);
        }

        // lineList에 line이 들어있는지 확인하는 함수
        public static bool IsValueInList(Line line, List<Line> lineList)
        {
            for (int i = 0; i < lineList.Count; i++)
            {
                if (line == lineList[i])
                    return true;
            }
            return false;
        }

        // 두 점이 같은지 판단하는 함수
        public static bool IsSamePoint(Point3d p1, Point3d p2)
        {
            return IsSamePoint(p1.X, p1.Y, p2.X, p2.Y);
        }

        public static bool IsSamePoint(MyPoint p1, MyPoint p2)
        {
            return IsSamePoint(p1.X, p1.Y, p2.X, p2.Y);
        }

        public static bool IsSamePoint(double x1, double y1, double x2, double y2)
        {
            if ((Math.Abs(x1 - x2) <= eps) && ((Math.Abs(y1 - y2) <= eps)))
                return true;

            return false;
        }
    }
}
