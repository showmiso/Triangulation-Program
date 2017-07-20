using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace MainProgram
{
    public class ConvexHull
    {
        private static List<Point3d> ptList = null;
        private static List<Point3d> convexList = null;

        private static double cross(Point3d O, Point3d A, Point3d B)
        {
            return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
        }

        private static Point3d[] CopyOfRange(Point3d[] src, int start, int end)
        {
            int len = end - start;
            Point3d[] dest = new Point3d[len];
            Array.Copy(src, start, dest, 0, len);
            return dest;
        }

        public static List<Point3d> ComputeConvexHull(List<Point3d> _ptList)
        {
            if (_ptList == null)
                return null;
            ptList = _ptList;

            int k = 0;
            // 안될 것 같은데...
            Point3d[] ptArr = new Point3d[2 * ptList.Count];

            // Build Lower Hull
            for (int i = 0; i < ptList.Count; ++i)
            {
                while (k >= 2 && cross(ptArr[k - 2], ptArr[k - 1], ptList[i]) <= 0)
                    k--;
                ptArr[k++] = ptList[i];
            }

            // Build Upper Hull
            for (int i = ptList.Count - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && cross(ptArr[k - 2], ptArr[k - 1], ptList[i]) <= 0)
                    k--;
                ptArr[k++] = ptList[i];
            }

            if (k > 1)
                ptArr = CopyOfRange(ptArr, 0, k - 1);

            if (convexList == null)
                convexList = new List<Point3d>();

            Point3d pt;
            for (int i = 0; i < k - 1; i++)
            {
                pt = new Point3d(ptArr[i].X, ptArr[i].Y, ptArr[i].Z);
                convexList.Add(pt);
            }

            return convexList;
        }
    }
}
