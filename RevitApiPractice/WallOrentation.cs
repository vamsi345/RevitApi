using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitApiPractice

{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class WallOrentation : IExternalCommand
    {
        private Document doc;
        private ExternalCommandData commandData = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            this.commandData = commandData;
            this.doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            List<ElementId> elementIds = GetOutermostWalls(doc).Distinct().ToList();
           
           
            try
            {
                using (Transaction trans = new Transaction(doc, "Place Dimension"))
                {
                    trans.Start();
                    wallOrentation(elementIds);
                    
                    trans.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }

            commandData.Application.ActiveUIDocument.Selection.SetElementIds(elementIds);
            return Result.Succeeded;
        }
        public void wallOrentation(List<ElementId> WallIds)
        {
            Wall nWall = null;
            float nY = 0;
            float bO = 0;
            List<int> Ids = new List<int>();
            foreach(ElementId eleId in WallIds)
            {
                Wall wall = doc.GetElement(eleId) as Wall;
                LocationCurve Lc = wall.Location as LocationCurve;
                Curve C = Lc.Curve;
                Line line = C as Line;
                if((float)line.GetEndPoint(0).Y==(float)line.GetEndPoint(1).Y&&line.GetEndPoint(0).Y>nY)
                {
                    nWall = wall;
                    if(nWall.Orientation.Y!=1)
                    {
                        bO = (float)nWall.Orientation.Y;
                        nWall.Flip();
                    }
                    nY = (float)line.GetEndPoint(0).Y;
                }
            }
            Wall currentWall = nWall;
            int index = 0;
            List<Wall> walls=new List<Wall>();
           
            foreach (ElementId wallId1 in WallIds)
            {
                foreach(ElementId wallId in WallIds)
                {
                    Wall wall = doc.GetElement(wallId) as Wall;
                    LocationCurve Lc = wall.Location as LocationCurve;
                    Curve C = Lc.Curve;
                    Line line = C as Line;
                    IList<XYZ> tes = line.Tessellate();
                    LocationCurve CLc = currentWall.Location as LocationCurve;
                    Curve CC = CLc.Curve;
                    Line Cline = CC as Line;
                    IList<XYZ> Ctes = Cline.Tessellate();
                    IntersectionResultArray intersectionResultArray;
                    if (wall.Equals(currentWall))
                    {
                        continue;
                    }
                    if(walls.Contains(wall))
                    {
                        continue;
                    }
                    line.Intersect(Cline, out intersectionResultArray);
                    if (intersectionResultArray != null)
                    {
                       

                        double b1x = Cline.GetEndPoint(index).X - line.GetEndPoint(0).X;
                        double b1y = Cline.GetEndPoint(index).Y - line.GetEndPoint(0).Y;
                        double b2x = Cline.GetEndPoint(index).X - line.GetEndPoint(1).X;
                        double b2y = Cline.GetEndPoint(index).Y - line.GetEndPoint(1).Y;
                        bool b2 = Cline.GetEndPoint(index).X -line.GetEndPoint(1).X==0 && Cline.GetEndPoint(index).Y -line.GetEndPoint(1).Y==0;
                       if(Ids.Contains(wallId.IntegerValue))
                        {
                            continue;
                        }
                        if (currentWall.Orientation.Y == 1)
                        {
                            if(Cline.GetEndPoint(0).X>Cline.GetEndPoint(1).X)
                            {
                                index = 0;
                            }
                            else
                            {
                                index = 1;
                            }
                            if((float)Cline.GetEndPoint(index).X - (float)line.GetEndPoint(0).X != 0 && (float)Cline.GetEndPoint(index).Y - (float)line.GetEndPoint(0).Y!=0|| (float)Cline.GetEndPoint(index).X - (float)line.GetEndPoint(1).X != 0 && (float)Cline.GetEndPoint(index).Y - (float)line.GetEndPoint(1).Y != 0)
                            {
                                continue;
                            }
                            if (Cline.GetEndPoint(index).Y < line.GetEndPoint(0).Y || Cline.GetEndPoint(index).Y < line.GetEndPoint(1).Y)
                            {
                                if (wall.Orientation.X != -1)
                                {
                                    wall.Flip();
                                    currentWall = wall;
                                    Ids.Add(wallId.IntegerValue);
                                    break;
                                }
                                
                                currentWall = wall;
                                Ids.Add(wallId.IntegerValue);
                                break;
                            }
                            if (Cline.GetEndPoint(index).Y > line.GetEndPoint(0).Y || Cline.GetEndPoint(index).Y > line.GetEndPoint(1).Y)
                            {
                                if (wall.Orientation.X != 1)
                                {
                                    wall.Flip();
                                    walls.Add(currentWall);
                                    
                                    currentWall = wall;
                                    Ids.Add(wallId.IntegerValue);
                                    break;
                                }
                                
                                currentWall = wall;
                                Ids.Add(wallId.IntegerValue);
                                break;
                            }
                        }
                        if (currentWall.Orientation.X == 1)
                        {
                            if (Cline.GetEndPoint(0).Y < Cline.GetEndPoint(1).Y)
                            {
                                index = 0;
                            }
                            else
                            {
                                index = 1;
                            }
                            if ((float)Cline.GetEndPoint(index).X - (float)line.GetEndPoint(0).X != 0 && (float)Cline.GetEndPoint(index).Y - (float)line.GetEndPoint(0).Y != 0 || (float)Cline.GetEndPoint(index).X - (float)line.GetEndPoint(1).X != 0 && (float)Cline.GetEndPoint(index).Y - (float)line.GetEndPoint(1).Y != 0)
                            {
                                continue;
                            }
                            if (Cline.GetEndPoint(index).X < line.GetEndPoint(0).X || Cline.GetEndPoint(index).X < line.GetEndPoint(1).X)
                            {
                                if (wall.Orientation.Y != 1)
                                {
                                    wall.Flip();
                                    currentWall = wall;
                                    Ids.Add(wallId.IntegerValue);
                                    break;
                                }
                                currentWall = wall;
                                Ids.Add(wallId.IntegerValue);
                                break;
                            }
                            if (Cline.GetEndPoint(index).X > line.GetEndPoint(0).X || Cline.GetEndPoint(index).X > line.GetEndPoint(1).X)
                            {
                                if (wall.Orientation.X != -1)
                                {
                                   
                                    wall.Flip();
                                    currentWall = wall;
                                    Ids.Add(wallId.IntegerValue);
                                    break;
                                }
                                currentWall = wall;
                                Ids.Add(wallId.IntegerValue);
                                break;
                            }
                        }
                        if (currentWall.Orientation.Y == -1)
                        {
                            if (Cline.GetEndPoint(0).X < Cline.GetEndPoint(1).X)
                            {
                                index = 0;
                            }
                            else
                            {
                                index = 1;
                            }
                            if ((float)Cline.GetEndPoint(index).X - (float)line.GetEndPoint(0).X != 0 && (float)Cline.GetEndPoint(index).Y - (float)line.GetEndPoint(0).Y != 0 || (float)Cline.GetEndPoint(index).X - (float)line.GetEndPoint(1).X != 0 &&  (float)Cline.GetEndPoint(index).Y - (float)line.GetEndPoint(1).Y != 0)
                            {
                                continue;
                            }
                            if (Cline.GetEndPoint(index).Y < line.GetEndPoint(0).Y || Cline.GetEndPoint(index).Y < line.GetEndPoint(1).Y)
                            {
                                if (wall.Orientation.X != -1)
                                {
                                    wall.Flip();
                                    currentWall = wall;
                                    Ids.Add(wallId.IntegerValue);
                                    break;
                                }
                                currentWall = wall;
                                Ids.Add(wallId.IntegerValue);
                                break;
                            }
                            if (Cline.GetEndPoint(index).Y > line.GetEndPoint(0).Y || Cline.GetEndPoint(index).Y > line.GetEndPoint(1).Y)
                            {
                                if (wall.Orientation.X != 1)
                                {
                                    wall.Flip();
                                    currentWall = wall;
                                    Ids.Add(wallId.IntegerValue);
                                    break;
                                }
                                currentWall = wall;
                                Ids.Add(wallId.IntegerValue);
                                break;
                            }
                        }
                        if (currentWall.Orientation.X == -1)
                        {
                            if (Cline.GetEndPoint(0).Y > Cline.GetEndPoint(1).Y)
                            {
                                index = 0;
                            }
                            else
                            {
                                index = 1;
                            }
                            if ((float)Cline.GetEndPoint(index).X - (float)line.GetEndPoint(0).X != 0 && (float)Cline.GetEndPoint(index).Y - (float)line.GetEndPoint(0).Y != 0 || (float)Cline.GetEndPoint(index).X - (float)line.GetEndPoint(1).X != 0 && (float)Cline.GetEndPoint(index).Y - (float)line.GetEndPoint(1).Y != 0)
                            {
                                continue;
                            }
                            if (Cline.GetEndPoint(index).X < line.GetEndPoint(0).X || Cline.GetEndPoint(index).X < line.GetEndPoint(1).X)
                            {
                                if (wall.Orientation.Y != 1)
                                {
                                    wall.Flip();
                                    currentWall = wall;
                                    Ids.Add(wallId.IntegerValue);
                                    break;
                                }
                                currentWall = wall;
                                Ids.Add(wallId.IntegerValue);
                                break;
                            }
                            if (Cline.GetEndPoint(index).X > line.GetEndPoint(0).X || Cline.GetEndPoint(index).X > line.GetEndPoint(1).X)
                            {
                                if (wall.Orientation.X != -1)
                                {
                                    wall.Flip();
                                    
                                    Ids.Add(wallId.IntegerValue);
                                    currentWall = wall;
                                    break;
                                }
                                currentWall = wall;
                                Ids.Add(wallId.IntegerValue);
                                break;
                            }
                        }

                    }
                }
                
            }

        }
        public static List<ElementId> GetOutermostWalls(Document doc, View view = null)
        {
            double offset = 1000 / 304.8;
            List<Wall> wallList = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
            double maxX = -1D;
            double minX = -1D;
            double maxY = -1D;
            double minY = -1D;
            wallList.ForEach((wall) =>
            {
                Curve curve = (wall.Location as LocationCurve).Curve;
                XYZ xyz1 = curve.GetEndPoint(0);
                XYZ xyz2 = curve.GetEndPoint(1);

                double _minX = Math.Min(xyz1.X, xyz2.X);
                double _maxX = Math.Max(xyz1.X, xyz2.X);
                double _minY = Math.Min(xyz1.Y, xyz2.Y);
                double _maxY = Math.Max(xyz1.Y, xyz2.Y);

                if (curve.IsCyclic)
                {
                    Arc arc = curve as Arc;
                    double _radius = arc.Radius;
                    _maxX += _radius;
                    _minX -= _radius;
                    _maxY += _radius;
                    _minY += _radius;
                }

                if (minX == -1) minX = _minX;
                if (maxX == -1) maxX = _maxX;
                if (maxY == -1) maxY = _maxY;
                if (minY == -1) minY = _minY;

                if (_minX < minX) minX = _minX;
                if (_maxX > maxX) maxX = _maxX;
                if (_maxY > maxY) maxY = _maxY;
                if (_minY < minY) minY = _minY;
            });
            minX -= offset;
            maxX += offset;
            minY -= offset;
            maxY += offset;

            CurveArray curves = new CurveArray();
            Line line1 = Line.CreateBound(new XYZ(minX, maxY, 0), new XYZ(maxX, maxY, 0));
            Line line2 = Line.CreateBound(new XYZ(maxX, maxY, 0), new XYZ(maxX, minY, 0));
            Line line3 = Line.CreateBound(new XYZ(maxX, minY, 0), new XYZ(minX, minY, 0));
            Line line4 = Line.CreateBound(new XYZ(minX, minY, 0), new XYZ(minX, maxY, 0));
            curves.Append(line1); curves.Append(line2); curves.Append(line3); curves.Append(line4);

            using (TransactionGroup group = new TransactionGroup(doc))
            {

                Room newRoom = null;
                RoomTag tag1 = null;

                group.Start("find outermost walls");
                using (Transaction transaction = new Transaction(doc, "createNewRoomBoundaryLines"))
                {
                    transaction.Start();
                    if (view == null)
                        view = doc.ActiveView;
                    SketchPlane sketchPlane = SketchPlane.Create(doc, view.GenLevel.Id);

                    ModelCurveArray modelCaRoomBoundaryLines = doc.Create.NewRoomBoundaryLines(sketchPlane, curves, view);

                    XYZ point = new XYZ(minX + 600 / 304.8, maxY - 600 / 304.8, 0);

                    newRoom = doc.Create.NewRoom(view.GenLevel, new UV(point.X, point.Y));

                    if (newRoom == null)
                    {
                        string msg = "hh。";
                        TaskDialog.Show("xx", msg);
                        transaction.RollBack();
                        return null;
                    }
                    tag1 = doc.Create.NewRoomTag(new LinkElementId(newRoom.Id), new UV(point.X, point.Y), view.Id);
                    transaction.Commit();
                }
                List<ElementId> elementIds = DetermineAdjacentElementLengthsAndWallAreas(doc, newRoom);
                group.RollBack();
                return elementIds;
            }

        }
        static List<ElementId> DetermineAdjacentElementLengthsAndWallAreas(Document doc, Room room)
        {
            List<ElementId> elementIds = new List<ElementId>();

            IList<IList<BoundarySegment>> boundaries
              = room.GetBoundarySegments(new SpatialElementBoundaryOptions());

            int n = boundaries.Count;//.Size;

            int iBoundary = 0, iSegment;

            foreach (IList<BoundarySegment> b in boundaries)
            {
                ++iBoundary;
                iSegment = 0;
                foreach (BoundarySegment s in b)
                {
                    ++iSegment;
                    Element neighbour = doc.GetElement(s.ElementId);// s.Element;
                    Curve curve = s.GetCurve();//.Curve;
                    double length = curve.Length;

                    if (neighbour is Wall)
                    {
                        Wall wall = neighbour as Wall;

                        Parameter p = wall.get_Parameter(
                          BuiltInParameter.HOST_AREA_COMPUTED);

                        double area = p.AsDouble();

                        LocationCurve lc
                          = wall.Location as LocationCurve;

                        double wallLength = lc.Curve.Length;

                        elementIds.Add(wall.Id);
                    }
                }
            }
            return elementIds;
        }

    }
}
