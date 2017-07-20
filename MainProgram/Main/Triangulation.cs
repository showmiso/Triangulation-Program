using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace MainProgram
{
    public class Triangulation
    {
        public static Document doc = null;
        public static Database db = null;
        public static Editor ed = null;
        public static Transaction acTrans = null;

        [CommandMethod("CTT")]
        public void Main()
        {
            doc = Application.DocumentManager.MdiActiveDocument;
            db = doc.Database;
            ed = doc.Editor;

            List<Point3d> ptList = null;         // 정점 리스트
            List<Line> brkLList = null;          // 브레이크 라인 리스트

            List<Point3d> convexList = null;
            List<Point3d> steinerList = null;

            List<MyTriangle> triList = null;     // 그릴 삼각형 리스트

            Input(ref ptList, ref brkLList);

            if (ptList == null)
                return;

            // 초기화 전에 convexHull을 계산한다.
            Compute(ptList, ref convexList, ref steinerList);

            if (convexList == null || steinerList == null)
                return;

            Process(convexList, steinerList, brkLList, ptList, ref triList);

            Render(triList);
        }

        private void Input(ref List<Point3d> ptList, ref List<Line> brkLList)
        {
            // 정점 정보를 받고,
            PromptSelectionResult psr = ACadUtils.SelectPointFunc(ed, "POINT", "\nSelect Point : ");

            if (psr == null || psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("점이 선택되지 않았습니다.");
                return;
            }

            SelectionSet ss = psr.Value;
            int nPts = ss.Count;
            ed.WriteMessage("\n★★★ {0}", nPts);

            if (nPts < 3)
            {
                ed.WriteMessage("Minimum 3 points must be selected!");
                return;
            }

            // 정점 정보 받기
            ObjectId[] ptId = ss.GetObjectIds();
            acTrans = db.TransactionManager.StartTransaction();

            ptList = new List<Point3d>();

            using (acTrans)
            {
                DBPoint ptEnt;
                int k = 0;
                int nCnt = 0;   // 중복점 찾기 위한 Count

                for (int i = 0; i < nPts; i++)
                {
                    ptEnt = (DBPoint)acTrans.GetObject(ptId[k], OpenMode.ForRead, false);

                    Point3d pt = new Point3d(ptEnt.Position[0], ptEnt.Position[1], ptEnt.Position[2]);

                    ptList.Add(pt);

                    // 중복 점 처리
                    for (int j = 0; j < ptList.Count - 1; j++)
                    {
                        // 지금 추가한 점과 같은 점이 있다면, 삭제한다.
                        if ((pt.X == ptList[j].X) && (pt.Y == ptList[j].Y)) //  
                        {
                            ptList.RemoveAt(j--);
                            nCnt++;
                        }
                    }// for j end

                    k++;
                }// for i end

                nPts -= nCnt;       // 현재 점 개수 갱신

                // 현재 좌표 및 중복 좌표 출력
                ed.WriteMessage("\n 중복 좌표 수는 {0} 이고, 현재 좌표 수는 {1} 입니다.", nCnt, nPts);

                //acTrans.Commit();
            }// using end

            // 점 정렬
            ACadUtils.ArrayPointList(ref ptList);

            ////////////////////////////////////////////////
            // 브레이크 라인 정보 받아서 두 점 인덱스로 엣지를 추가한다.
            // 엣지는 엣지 리스트로 관리한다. 
            acTrans = db.TransactionManager.StartTransaction();

            List<Polyline3d> brkPList = new List<Polyline3d>();

            // BreakLine 정렬
            using (acTrans)
            {
                BlockTable acBlkTbl;
                acBlkTbl = (BlockTable)acTrans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = (BlockTableRecord)acTrans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                psr = ACadUtils.SelectPointFunc(ed, "POLYLINE", "\nSelect Break Line : ");

                if (psr == null 
                 || psr.Status == PromptStatus.Error
                 || psr.Status == PromptStatus.Cancel)
                {
                    // 취소할 수 있다.
                    ed.WriteMessage("\nBreakLine을 선택하지 않았습니다.");
                }
                else
                {
                    // 선택한 폴리선 정보 받기
                    ss = psr.Value;
                    ObjectId[] bkId = ss.GetObjectIds();

                    for (int i = 0; i < ss.Count; i++)
                    {
                        Entity ent = (Entity)acTrans.GetObject(bkId[i], OpenMode.ForRead);

                        if (ent is Polyline3d)
                        {
                            Polyline3d plEnt = (Polyline3d)acTrans.GetObject(bkId[i], OpenMode.ForRead);
                            brkPList.Add(plEnt);
                        }
                    }// for i end

                    if (brkPList != null)
                        brkLList = new List<Line>();

                    // 폴리선 분해해서 라인 리스트에 추가
                    foreach (Polyline3d pl in brkPList)
                    {
                        // 폴리선 폭파
                        DBObjectCollection acObjColl = new DBObjectCollection();
                        pl.Explode(acObjColl);

                        foreach (DBObject obj in acObjColl)
                        {
                            // 이렇게 블록 테이블에 추가한 라인들은 모두 나중에 삭제해야함.
                            // 블록 테이블에 추가
                            Entity ent = (Entity)obj;
                            acBlkTblRec.AppendEntity(ent);
                            acTrans.AddNewlyCreatedDBObject(ent, true);

                            if (ent is Line)
                            {
                                Line plEnt = (Line)acTrans.GetObject(ent.ObjectId, OpenMode.ForRead);
                                brkLList.Add(plEnt);
                            }

                        }// foreach DBObject end
                    }// foreach polyline3d end
                }// else end
            }// using end
        }

        private void Compute(List<Point3d> ptList, ref List<Point3d> convexList, ref List<Point3d> steinerList)
        {
            convexList = ConvexHull.ComputeConvexHull(ptList);

            if (convexList == null)
                return;

            // 만약 외부점과 steiner점이 중복되도 된다면, 필요없는 부분이다.
            if (steinerList == null)
                steinerList = new List<Point3d>(ptList);

            // convexLists와 steinerList를 분리한다.
            int idx;
            for (int i = 0; i < convexList.Count; i++)
            {
                idx = steinerList.BinarySearch(0, steinerList.Count, convexList[i], new ACadUtils.Sort3DPoint());
                steinerList.RemoveAt(idx);
            }
        }

        private void Process(List<Point3d> convexList, List<Point3d> steinerList, List<Line> brkLList, List<Point3d> ptList, ref List<MyTriangle> triList)
        {
            ///////////////////////////////////////////////////////////////////
            // PolygonInfo로 정점 정보를 보낸다.

            if (convexList == null)
                return;

            // convexList, steinerList, brkLList 정보를 넘기고, 
            List<MyPoint> points = new List<MyPoint>();
            List<MyPoint> steinerpoint = null;
            List<MyPoint> breaklinepoint = null;
            // 
            foreach (Point3d pt in convexList)
                points.Add(new MyPoint(pt));

            if (steinerList != null)
            {
                steinerpoint = new List<MyPoint>();
                foreach (Point3d pt in steinerList)
                    steinerpoint.Add(new MyPoint(pt));
            }

            if (brkLList != null)
            {
                breaklinepoint = new List<MyPoint>();
                foreach (Line line in brkLList)
                {
                    breaklinepoint.Add(new MyPoint(line.StartPoint));
                    breaklinepoint.Add(new MyPoint(line.EndPoint));
                }
            }

            /*
            // PolygonInfo로 정점 정보를 보낸다.
            MainProcess process1 = new MainProcess(points, steinerpoint, breaklinepoint, true);
            MainProcess process2 = new MainProcess(points, steinerpoint, breaklinepoint, false);
            
            ///////////////////////////////////////////////////////////////////
            // Triangle정보를 받는다.
            List<MyTriangle> triList1 = process1.GetTriangles();
            List<MyTriangle> triList2 = process2.GetTriangles();

            if (triList == null)
                triList = new List<MyTriangle>();

            foreach (MyTriangle tri2 in triList2)
            {
                foreach (MyTriangle tri1 in triList1)
                {
                    if (tri2 != tri1)
                    {
                        triList.Add(tri2);
                    }
                    else
                    {
                        //triList.Add(tri1);
                    }
                }
            }
            */

            MainProcess process = new MainProcess(points, steinerpoint, breaklinepoint, true);
            triList = process.GetTriangles();

            // Z값 설정
            // 전체 점을 돌면서, x, y가 같은 점을 찾고, z값을 넣는다.

            List<Point3d> ptZList = new List<Point3d>();
            Point3d point;
            for (int i = 0; i < ptList.Count; i++)
            {
                point = new Point3d(ptList[i].X, ptList[i].Y, 0.0);
                ptZList.Add(point);
            }

            List<MyTriangle> triZList = new List<MyTriangle>();

            int idx1 = 0, idx2 = 0, idx3 = 0;
            Point3d pt1, pt2, pt3;

            foreach (MyTriangle tri in triList)
            {
                idx1 = ptZList.BinarySearch(0, ptZList.Count, tri.pt1, new ACadUtils.Sort3DPoint());
                idx2 = ptZList.BinarySearch(0, ptZList.Count, tri.pt2, new ACadUtils.Sort3DPoint());
                idx3 = ptZList.BinarySearch(0, ptZList.Count, tri.pt3, new ACadUtils.Sort3DPoint());

                pt1 = new Point3d(ptList[idx1].X, ptList[idx1].Y, ptList[idx1].Z);
                pt2 = new Point3d(ptList[idx2].X, ptList[idx2].Y, ptList[idx2].Z);
                pt3 = new Point3d(ptList[idx3].X, ptList[idx3].Y, ptList[idx3].Z);

                triZList.Add(new MyTriangle(pt1, pt2, pt3));
            }

            triList = triZList;
            
            // Clear 
            if (ptList != null)
            {
                ptList.Clear(); 
                ptList = null;
            }
            if (convexList != null)
            {
                convexList.Clear(); 
                convexList = null;
            }
            if (steinerList != null)
            {
                steinerList.Clear(); 
                steinerList = null;
            }
            if (brkLList != null)
            {
                brkLList.Clear(); 
                brkLList = null;
            }
        }

        private void Render(List<MyTriangle> triList)
        {
            if (triList == null)
                return;

            acTrans = db.TransactionManager.StartTransaction();

            using (acTrans)
            {
                BlockTable acBlkTbl;
                acBlkTbl = (BlockTable)acTrans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = (BlockTableRecord)acTrans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                //Point3d p1, p2, p3;
                // 그린다.
                foreach (MyTriangle tri in triList)
                {
                    //p1 = new Point3d(tri.pt1.X, tri.pt1.Y, tri.pt1.Z);
                    //p2 = new Point3d(tri.pt2.X, tri.pt2.Y, tri.pt2.Z);
                    //p3 = new Point3d(tri.pt3.X, tri.pt3.Y, tri.pt3.Z);
                    
                    Polyline3d poly = new Polyline3d(
                        Poly3dType.SimplePoly,
                        new Point3dCollection(new[] { 
                            new Point3d(tri.pt1.X, tri.pt1.Y, tri.pt1.Z), 
                            new Point3d(tri.pt2.X, tri.pt2.Y, tri.pt2.Z), 
                            new Point3d(tri.pt3.X, tri.pt3.Y, tri.pt3.Z) }),
                        false);
                    poly.Closed = true;
                    acBlkTblRec.AppendEntity(poly);
                    acTrans.AddNewlyCreatedDBObject(poly, true);
                }

                acTrans.Commit();
            }// using end

            Application.UpdateScreen();

            triList.Clear(); triList = null;
        }

    }
}
