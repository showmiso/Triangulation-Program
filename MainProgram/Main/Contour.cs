using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace MainProgram
{
    public class Contour
    {
        public static Document doc = null;
        public static Database db = null;
        public static Editor ed = null;
        public static Transaction acTrans = null;

        private double zMin = 0.0;
        private double zMax = 0.0;
        private double zInvertal = 0.0;
        private const double eps = 1e-6;

        private static List<Line> AllList = null;           // 모든 Line을 보관하는 List
        private static List<Polyline> ctPList = null;       // Contour PolyLine List

        [CommandMethod("MCT")]
        public void Main()
        {
            doc = Application.DocumentManager.MdiActiveDocument;
            db = doc.Database;
            ed = doc.Editor;

            List<MyTriangle> triList = null;     // 3DPoly의 정점 List

            Init(ref triList);

            if (triList == null)
                return;

            Process(triList);

            RenderContour();

        }// method Main end

        // 초기화 
        private void Init(ref List<MyTriangle> triList)
        {
            // 삼각망 받기
            PromptSelectionResult psr = ACadUtils.SelectPointFunc(ed, "POLYLINE", "\nSelect Contour Line : ");

            if (psr == null || psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("삼각망이 선택되지 않았습니다.");
                return;
            }

            SelectionSet ss = psr.Value;

            ObjectId[] plId = ss.GetObjectIds();
            acTrans = db.TransactionManager.StartTransaction();

            List<Polyline3d> plList = new List<Polyline3d>();      // 삼각망 List

            if (triList == null)
                triList = new List<MyTriangle>();

            using (acTrans)
            {
                BlockTable acBlkTbl;
                acBlkTbl = (BlockTable)acTrans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = (BlockTableRecord)acTrans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                for (int i = 0; i < ss.Count; i++)
                {
                    Entity ent = (Entity)acTrans.GetObject(plId[i], OpenMode.ForRead);

                    Polyline3d plEnt = (Polyline3d)acTrans.GetObject(plId[i], OpenMode.ForRead);

                    if (plEnt.Closed == true)
                    {
                        plList.Add(plEnt);
                    }

                }// for i end

                // Trianglelist로 분해
                for (int i = 0; i < plList.Count; i++)
                {
                    // 폴리선 폭파
                    DBObjectCollection acObjColl = new DBObjectCollection();
                    plList[i].Explode(acObjColl);

                    // 3개일 경우만 돈다.
                    if (acObjColl.Count == 3)
                    {
                        // 3번 돌겠지
                        int k = 0;
                        MyTriangle tri = new MyTriangle();

                        foreach (DBObject obj in acObjColl)
                        {
                            // 블록 테이블에 추가
                            Entity ent = (Entity)obj;
                            acBlkTblRec.AppendEntity(ent);
                            acTrans.AddNewlyCreatedDBObject(ent, true);

                            Line plEnt = (Line)acTrans.GetObject(ent.ObjectId, OpenMode.ForRead);

                            if (k == 0)
                                tri.pt1 = plEnt.StartPoint;
                            else if (k == 1)
                                tri.pt2 = plEnt.StartPoint;
                            else if (k == 2)
                            {
                                tri.pt3 = plEnt.StartPoint;
                                triList.Add(tri);
                            }

                            k++;
                        }// foreach DBObject end
                    }
                    else
                    {
                        // 3이 아닌 3DPoly라인이라면, 리스트에서 삭제한다.
                        plList.RemoveAt(i--);
                    }

                }// 

                //acTrans.Commit();

            }// using end

            // z 최대 최소값 구하고, 
            if (triList == null) return;

            zMin = triList[0].pt1.Z;
            zMax = zMin;

            for (int i = 0; i < triList.Count; i++)
            {
                if (zMin > triList[i].pt1.Z) zMin = triList[i].pt1.Z;
                if (zMin > triList[i].pt2.Z) zMin = triList[i].pt2.Z;
                if (zMin > triList[i].pt3.Z) zMin = triList[i].pt3.Z;

                if (zMax < triList[i].pt1.Z) zMax = triList[i].pt1.Z;
                if (zMax < triList[i].pt2.Z) zMax = triList[i].pt2.Z;
                if (zMax < triList[i].pt3.Z) zMax = triList[i].pt3.Z;
            }

            if (zMax == zMin)
            {
                ed.WriteMessage("\n평면에는 등고선을 그릴 수 없습니다.");
                return;
            }

            // z interval을 입력받는다.
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nInput Contour Inverval Value : ");
            PromptDoubleResult pdr = ed.GetDouble(pdo);

            if (pdr.Status != PromptStatus.OK)
                return;

            if (pdr.Value == 0.0)
            {
                ed.WriteMessage("\n0일 때는 등고선을 그릴 수 없습니다.");
                return;
            }

            zInvertal = pdr.Value;

            // 최소 z값이 z interval에 못미친다면, 값을 증가시켜 새로운 interval value를 적용시킨다.
            if ((zMin % zInvertal) != 0.0)
            {
                if (zMin < 0.0)
                    zMin = zMin - (zMin % zInvertal);
                else zMin = (zMin - (zMin % zInvertal)) + zInvertal;
            }

        }// method Init end

        private void Process(List<MyTriangle> triList)
        {
            List<Line> ctLList = new List<Line>();     // Contour Line List
            if (ctPList == null)
                ctPList = new List<Polyline>();

            double x1, y1, z1, x2, y2, z2;
            double d1, mx, my;

            // zMin값이 zMax값에 도달할 때까지 반복한다.
            while (zMin <= zMax)
            {
                foreach (MyTriangle tri in triList)
                {
                    Point3d[] ptArr = new Point3d[3];
                    ptArr[0] = new Point3d(tri.pt1.X, tri.pt1.Y, tri.pt1.Z);
                    ptArr[1] = new Point3d(tri.pt2.X, tri.pt2.Y, tri.pt2.Z);
                    ptArr[2] = new Point3d(tri.pt3.X, tri.pt3.Y, tri.pt3.Z);

                    // 점 개수 List
                    List<Point3d> ptList = new List<Point3d>();

                    for (int i = 0; i < 3; i++)
                    {
                        // 0 1, 1 2, 2 0
                        Line line = new Line(ptArr[i], ptArr[(i + 1) % 3]);

                        x1 = line.StartPoint.X; y1 = line.StartPoint.Y; z1 = line.StartPoint.Z;
                        x2 = line.EndPoint.X; y2 = line.EndPoint.Y; z2 = line.EndPoint.Z;

                        // if this triangle side intersect contour level.
                        // 현재 삼각형 side의 등고선 레벨이 교차한다면, 
                        // 측면 contour level의 교차를 계산하고, 개별 contour line에 점을 추가한다.
                        if ((zMin >= Math.Min(z1, z2)) && (zMin <= Math.Max(z1, z2)) && (z1 != z2))
                        {
                            d1 = (zMin - z1) / (z2 - z1);
                            mx = x1 + (d1 * (x2 - x1));
                            my = y1 + (d1 * (y2 - y1));

                            // compare with point(s) already added to single contour line

                            // 점 리스트에 추가한다.
                            bool bCheck = false;
                            for (int j = 0; j < ptList.Count; j++)
                            {
                                if (ACadUtils.IsSamePoint(ptList[j].X, ptList[j].Y, mx, my) == true)
                                {
                                    bCheck = true;
                                    break;
                                }
                            }

                            if (bCheck == false)
                            {
                                // ptList에 추가
                                ptList.Add(new Point3d(mx, my, 0.0));
                            }

                        }// if end

                    }// for i end

                    // Contour line이 2점을 가지고 있다면, 콘타라인 리스트에 추가한다.
                    if (ptList.Count == 2)
                    {
                        // contList에 line으로 추가
                        Line line = new Line(new Point3d(ptList[0].X, ptList[0].Y, ptList[0].Z),
                                                new Point3d(ptList[1].X, ptList[1].Y, ptList[1].Z));

                        ctLList.Add(line);
                        // ptList Clear
                        ptList.Clear();
                    }// if end

                }// foreach MyTriangle end

                // 현재까지 생성된 라인을 연결하여, 폴리라인으로 만든다.
                LinesToPolyLine(ctLList, zMin);
                //Polyline pline = LinesToPolyLine(ctLList, zMin);
                //if (pline != null)
                //    ctPList.Add(pline);

                // 다시 콘타 리스트 초기화
                ctLList.Clear();

                // 값 증가
                zMin += zInvertal;

            }// while end

            LineToPLine();
        }

        // 등고선 그리기
        private void RenderContour()
        {
            if (ctPList == null)
                return;

            acTrans = db.TransactionManager.StartTransaction();

            using (acTrans)
            {
                BlockTable acBlkTbl;
                acBlkTbl = (BlockTable)acTrans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = (BlockTableRecord)acTrans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (Polyline pl in ctPList)
                {
                    acBlkTblRec.AppendEntity(pl);
                    acTrans.AddNewlyCreatedDBObject(pl, true);
                }

                ed.WriteMessage("\n폴리곤 생성 완료");
                acTrans.Commit();

            }// using end

            ed.Regen();

            if (ctPList != null)
            {
                ctPList.Clear();
                ctPList = null;
            }
            if (AllList != null)
            {
                AllList.Clear();
                AllList = null;
            }

        }// method RenderContour end

        // 중간 정산.
        // lineList 하나씩 복사하는 함수
        private void LinesToPolyLine(List<Line> ctLList, double zEle)
        {
            if (AllList == null)
                AllList = new List<Line>();

            // 라인리스트 복사
            foreach (Line line in ctLList)
            {
                Line newline = new Line(
                    new Point3d(line.StartPoint.X, line.StartPoint.Y, zEle),
                    new Point3d(line.EndPoint.X, line.EndPoint.Y, zEle));

                AllList.Add(newline);
            }
        }

        private void LineToPLine()
        {
            List<Polyline> plineList = new List<Polyline>();
            Point3d p1, p2, EndPoint;
            List<Point3d> ptpList;

            // 여기는 Line끼리 연결
            // 전체 라인리스트를 돌면서 높이가 같은 점을 찾는다. 
            for (int i = 0; i < AllList.Count; i++)
            {
                ptpList = new List<Point3d>();
                ptpList.Add(AllList[i].StartPoint);
                EndPoint = AllList[i].EndPoint;
                ptpList.Add(EndPoint);

                // 함수로 뺄까말까ㅠㅠ
                for (int j = 0; j < AllList.Count; j++)
                {
                    // 높이가 같은 것을 찾는다.
                    if (i != j && AllList[i].StartPoint.Z == AllList[j].StartPoint.Z)
                    {
                        // 높이가 같을 때 점이 같은지 확인한다.
                        p1 = EndPoint;
                        p2 = AllList[j].StartPoint;

                        if (ACadUtils.IsSamePoint(p1, p2))
                        {
                            ptpList.Add(p2);
                            EndPoint = AllList[j].EndPoint;
                            ptpList.Add(EndPoint);
                            AllList.RemoveAt(j--);
                            j = 0;
                        }

                        p1 = EndPoint;
                        p2 = AllList[j].EndPoint;

                        if (ACadUtils.IsSamePoint(p1, p2))
                        {
                            ptpList.Add(p2);
                            EndPoint = AllList[j].StartPoint;
                            ptpList.Add(EndPoint);
                            AllList.RemoveAt(j--);
                            j = 0;
                        }
                    }

                }// for j end

                // 2개인 선은 다시 체크해서 반복해준다. 
                if (ptpList.Count == 2)
                {
                    ptpList.Clear();
                    ptpList.Add(AllList[i].EndPoint);
                    EndPoint = AllList[i].StartPoint;
                    ptpList.Add(EndPoint);

                    for (int j = 0; j < AllList.Count; j++)
                    {
                        // 높이가 같은 것을 찾는다.
                        if (i != j && AllList[i].StartPoint.Z == AllList[j].StartPoint.Z)
                        {
                            // 높이가 같을 때 점이 같은지 확인한다.
                            p1 = EndPoint;
                            p2 = AllList[j].StartPoint;

                            if (ACadUtils.IsSamePoint(p1, p2))
                            {
                                ptpList.Add(p2);
                                EndPoint = AllList[j].EndPoint;
                                ptpList.Add(EndPoint);
                                AllList.RemoveAt(j--);
                                j = 0;
                            }

                            p1 = EndPoint;
                            p2 = AllList[j].EndPoint;

                            if (ACadUtils.IsSamePoint(p1, p2))
                            {
                                ptpList.Add(p2);
                                EndPoint = AllList[j].StartPoint;
                                ptpList.Add(EndPoint);
                                AllList.RemoveAt(j--);
                                j = 0;
                            }
                        }
                    }// for j end
                }

                AllList.RemoveAt(i--);

                // lineList를 pline으로 바꾼다.
                Polyline pline = new Polyline();

                for (int k = 0; k < ptpList.Count; k++)
                {
                    pline.AddVertexAt(k, new Point2d(ptpList[k].X, ptpList[k].Y), 0, 0, 0);
                }

                pline.Elevation = ptpList[0].Z;

                // 만약 시작 정점과 끝 정점의 값이 같다면, 닫힌 정점이다.
                if (pline.GetPoint2dAt(0) == pline.GetPoint2dAt(pline.NumberOfVertices - 1))
                    pline.Closed = true;

                // 지금까지 만들어진 pline을 추가한다.
                plineList.Add(pline);

            }// for i end

            //////////////////////////////////////////////////////////////////////////
            // 여기는 pline끼리 연결
            // pline을 돌면서 해보자 더럽고 치사하지만
            for (int i = 0; i < plineList.Count; i++)
            {
                for (int j = 0; j < plineList.Count; j++)
                {
                    Polyline pline = null;

                    if (i != j && plineList[i].Elevation == plineList[j].Elevation)
                    {
                        if (plineList[i].GetPoint2dAt(0) == plineList[j].GetPoint2dAt(0))
                        {
                            pline = ConnectPolyline(plineList[i], 0, plineList[j], 0, plineList[i].StartPoint.Z);
                        }
                        else if (plineList[i].GetPoint2dAt(0) == plineList[j].GetPoint2dAt(plineList[j].NumberOfVertices - 1))
                        {
                            pline = ConnectPolyline(plineList[i], 0, plineList[j], 1, plineList[i].StartPoint.Z);
                        }
                        else if (plineList[i].GetPoint2dAt(plineList[i].NumberOfVertices - 1) == plineList[j].GetPoint2dAt(0))
                        {
                            pline = ConnectPolyline(plineList[i], 1, plineList[j], 0, plineList[i].StartPoint.Z);
                        }
                        else if (plineList[i].GetPoint2dAt(plineList[i].NumberOfVertices - 1) == plineList[j].GetPoint2dAt(plineList[j].NumberOfVertices - 1))
                        {
                            pline = ConnectPolyline(plineList[i], 1, plineList[j], 1, plineList[i].StartPoint.Z);
                        }

                        // 
                        if (pline != null)
                        {
                            plineList.Add(pline);

                            if (i < j)
                            {
                                plineList.RemoveAt(i);
                                plineList.RemoveAt(--j);
                            }
                            else
                            {
                                plineList.RemoveAt(i);
                                plineList.RemoveAt(j--);
                            }
                        }
                    }

                }// for j end

            }// for i end

            // plineList 복사
            foreach (Polyline pline in plineList)
            {
                ctPList.Add(pline);
            }
            plineList.Clear();
            plineList = null;
        }

        private Polyline ConnectPolyline(Polyline p1, int idx1, Polyline p2, int idx2, double dElevation)
        {
            Polyline pline = new Polyline();

            // p1과 p2를 연결한다. 중복점은 생략하고, GetPoint2dAt과 AddVertexAt으로 연결한다.

            int n = 0;

            if (idx1 == 0)
            {
                for (int i = p1.NumberOfVertices - 1; i > 0; i--)
                {
                    pline.AddVertexAt(n++, new Point2d(p1.GetPoint2dAt(i).X, p1.GetPoint2dAt(i).Y), 0, 0, 0);
                }
            }
            else
            {
                for (int i = 0; i < p1.NumberOfVertices - 1; i++)
                {
                    pline.AddVertexAt(n++, new Point2d(p1.GetPoint2dAt(i).X, p1.GetPoint2dAt(i).Y), 0, 0, 0);
                }
            }

            if (idx2 == 0)
            {
                for (int i = 0; i < p2.NumberOfVertices; i++)
                {
                    pline.AddVertexAt(n++, new Point2d(p2.GetPoint2dAt(i).X, p2.GetPoint2dAt(i).Y), 0, 0, 0);
                }
            }
            else
            {
                for (int i = p2.NumberOfVertices - 1; i >= 0; i--)
                {
                    pline.AddVertexAt(n++, new Point2d(p2.GetPoint2dAt(i).X, p2.GetPoint2dAt(i).Y), 0, 0, 0);
                }
            }

            // 고도 설정
            pline.Elevation = dElevation;

            // 닫힌 폴리선 설정
            if (pline.GetPoint2dAt(0) == pline.GetPoint2dAt(pline.NumberOfVertices - 1))
            {
                pline.Closed = true;
            }

            return pline;
        }

    }
}
