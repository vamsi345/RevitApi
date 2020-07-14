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
    public class Dimensioning : IExternalCommand
    {
        private Document doc;
        private ExternalCommandData commandData = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            this.commandData = commandData;
            this.doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            List<ElementId> elementIds = GetOutermostWalls(doc).Distinct().ToList();
            FilteredElementCollector collector1 = new FilteredElementCollector(doc);
            FilteredElementCollector collector2 = new FilteredElementCollector(doc);
            List<Wall> wallList = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
            ElementCategoryFilter Windowfilter = new ElementCategoryFilter(BuiltInCategory.OST_Windows);
            ElementCategoryFilter Doorfilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            List<Element> openings = collector1.WherePasses(Windowfilter).WhereElementIsNotElementType().ToElements().ToList();
            List<Element> doors = collector2.WherePasses(Doorfilter).WhereElementIsNotElementType().ToElements().ToList();
            openings.AddRange(doors);
            View view = doc.ActiveView;
            
            ReferenceArrayArray dimensionArguments1 = OuterWallSeg(elementIds);
           
            List<Line> Dlines1 = DimensionLines(elementIds, 5);
           

            try
            {
                using (Transaction trans = new Transaction(doc, "Place Dimension"))
                {
                    int i = 0;
                    int j = 0;
                    int k = 0;
                    int p = 0;
                    trans.Start();
                    foreach (ReferenceArray rArray in dimensionArguments1)
                    {
                        Dimension dim = doc.Create.NewDimension(view, Dlines1[i], rArray);
                        i += 1;
                    }
                   


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
        public ReferenceArrayArray OuterWallSeg(List<ElementId> elementIds)
        {
            ReferenceArray nR = new ReferenceArray();
            ReferenceArray eR = new ReferenceArray();
            ReferenceArray sR = new ReferenceArray();
            ReferenceArray wR = new ReferenceArray();
            List<float> XN1 = new List<float>();
            List<float> XN2 = new List<float>();
            List<float> YE1 = new List<float>();
            List<float> YE2 = new List<float>();
            List<float> XS1 = new List<float>();
            List<float> XS2 = new List<float>();
            List<float> YW1 = new List<float>();
            List<float> YW2 = new List<float>();
            XN1.Add(0);
            XN2.Add(0);
            YE1.Add(0);
            YE2.Add(0);
            XS1.Add(0);
            XS2.Add(0);
            YW1.Add(0);
            YW2.Add(0);
            ReferenceArrayArray raa = new ReferenceArrayArray();
            foreach(ElementId eleId in elementIds)
            {
                Wall wall = doc.GetElement(eleId) as Wall;
                LocationCurve Lc = wall.Location as LocationCurve;
                Curve C = Lc.Curve;
                Line line1 = C as Line;
                XYZ orentation = wall.Orientation;
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.DetailLevel = ViewDetailLevel.Fine;
                List<Wall> walls = WallIntersect(wall, elementIds);
                int face = 5;
                int cond = 0;
                foreach (Wall wall1 in walls)
                {
                    LocationCurve Lc2 = wall1.Location as LocationCurve;
                    Curve line2 = Lc2.Curve;
                    GeometryElement gE = wall1.get_Geometry(opt);
                    foreach(GeometryObject gO in gE)
                    {
                        if (gO.GetType().Name != "Solid")
                        {
                            continue;
                        }
                        Solid solid = gO as Solid;
                        FaceArray faceArray = solid.Faces;
                        Face f = faceArray.get_Item(face);
                        EdgeArrayArray eAA = f.EdgeLoops;
                        foreach(EdgeArray eA in eAA)
                        {
                            if(orentation.Y==1)
                            {
                                if(XN1.Contains((float)line1.GetEndPoint(1).X)&&(float)line2.GetEndPoint(0).X == (float)line1.GetEndPoint(1).X|| XN2.Contains((float)line1.GetEndPoint(0).X) && (float)line2.GetEndPoint(0).X == (float)line1.GetEndPoint(0).X)
                                {

                                    XN1.Add((float)line1.GetEndPoint(0).X);
                                    XN2.Add((float)line1.GetEndPoint(1).X);
                                    continue;
                                }
                                else
                                {
                                    nR.Append(eA.get_Item(2).Reference);
                                }
                                XN1.Add((float)line1.GetEndPoint(0).X);
                                XN2.Add((float)line1.GetEndPoint(1).X);

                            }
                            if (orentation.X == 1)
                            {
                                if (YE1.Contains((float)line1.GetEndPoint(1).Y) && (float)line2.GetEndPoint(0).Y == (float)line1.GetEndPoint(1).Y || YE2.Contains((float)line1.GetEndPoint(0).Y) && (float)line2.GetEndPoint(0).Y == (float)line1.GetEndPoint(0).Y)
                                {
                                    YE1.Add((float)line1.GetEndPoint(0).Y);
                                    YE2.Add((float)line1.GetEndPoint(1).Y);
                                    continue;
                                }
                                else
                                {
                                    eR.Append(eA.get_Item(2).Reference);
                                }
                                YE1.Add((float)line1.GetEndPoint(0).Y);
                                YE2.Add((float)line1.GetEndPoint(1).Y);
                            }
                            if (orentation.Y == -1)
                            {
                                if (XS1.Contains((float)line1.GetEndPoint(1).X) && (float)line2.GetEndPoint(0).X == (float)line1.GetEndPoint(1).X || XS2.Contains((float)line1.GetEndPoint(0).X) && (float)line2.GetEndPoint(0).X == (float)line1.GetEndPoint(0).X)
                                {
                                    XS1.Add((float)line1.GetEndPoint(0).X);
                                    XS2.Add((float)line1.GetEndPoint(1).X);
                                    continue;
                                }
                                else
                                {
                                    sR.Append(eA.get_Item(2).Reference);
                                }
                                XS1.Add((float)line1.GetEndPoint(0).X);
                                XS2.Add((float)line1.GetEndPoint(1).X);
                            }
                            if (orentation.X ==-1)
                            {
                                if (YW1.Contains((float)line1.GetEndPoint(1).Y) && (float)line2.GetEndPoint(0).Y == (float)line1.GetEndPoint(1).Y || YW2.Contains((float)line1.GetEndPoint(0).Y) && (float)line2.GetEndPoint(0).Y == (float)line1.GetEndPoint(0).Y)
                                {
                                    YW1.Add((float)line1.GetEndPoint(0).Y);
                                    YW2.Add((float)line1.GetEndPoint(1).Y);
                                    continue;
                                }
                                else
                                {
                                    wR.Append(eA.get_Item(2).Reference);
                                }
                                YW1.Add((float)line1.GetEndPoint(0).Y);
                                YW2.Add((float)line1.GetEndPoint(1).Y);
                            }
                        }
                    }
                    face -= 1;
                }
            }
            raa.Append(nR);
            raa.Append(eR);
            raa.Append(sR);
            raa.Append(wR);
            return raa;
        }
        public List<Wall> WallIntersect(Wall wall, List<ElementId> eleIds)
        {
            List<Wall> walls = new List<Wall>(2);
            LocationCurve Lc1 = wall.Location as LocationCurve;
            Curve C1 = Lc1.Curve;
            Line L1 = C1 as Line;
            Wall walls1=null;
            Wall walls2 = null;
            foreach(ElementId eleId in eleIds)
            {
                
                IntersectionResultArray intersectionResultArray;
                Wall wall2 = doc.GetElement(eleId) as Wall;
                if (wall.Equals(wall2))
                {
                    continue;
                }
                LocationCurve Lc2 = wall2.Location as LocationCurve;
                Curve C2 = Lc2.Curve;
                Line L2 = C2 as Line;
                L1.Intersect(L2,out intersectionResultArray);
                if (intersectionResultArray!=null)
                {
                    if ((float)intersectionResultArray.get_Item(0).XYZPoint.X - (float)L1.GetEndPoint(0).X == 0 && (float)intersectionResultArray.get_Item(0).XYZPoint.Y - (float)L1.GetEndPoint(0).Y == 0)
                    {
                        walls1 = wall2;
                    }
                    if ((float)intersectionResultArray.get_Item(0).XYZPoint.X - (float)L1.GetEndPoint(1).X == 0 && (float)intersectionResultArray.get_Item(0).XYZPoint.Y - (float)L1.GetEndPoint(1).Y == 0)
                    {
                        walls2 = wall2;
                    }
                   
                }
                

            }
            walls.Add(walls1);
            walls.Add(walls2);
            return walls;
        }
        public ReferenceArrayArray wallCutDimension(List<ElementId> elementIds)
        {
            List<float> XN1 = new List<float>();
            List<float> XN2 = new List<float>();
            List<float> YE1 = new List<float>();
            List<float> YE2 = new List<float>();
            List<float> XS1 = new List<float>();
            List<float> XS2 = new List<float>();
            List<float> YW1 = new List<float>();
            List<float> YW2 = new List<float>();
            XN1.Add(0);
            XN2.Add(0);
            YE1.Add(0);
            YE2.Add(0);
            XS1.Add(0);
            XS2.Add(0);
            YW1.Add(0);
            YW2.Add(0);
            ReferenceArray nR = new ReferenceArray();
            ReferenceArray eR = new ReferenceArray();
            ReferenceArray sR = new ReferenceArray();
            ReferenceArray wR = new ReferenceArray();
            ReferenceArrayArray raa = new ReferenceArrayArray();
            foreach (ElementId elementId1 in elementIds)
            {
                Wall wall1 = doc.GetElement(elementId1) as Wall;
                LocationCurve Lc1 = wall1.Location as LocationCurve;
                XYZ orentation = wall1.Orientation;
                Curve c1 = Lc1.Curve;
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.DetailLevel = ViewDetailLevel.Fine;
                if (wall1.Orientation.X != -1 && wall1.Orientation.Y != -1)
                {

                }
                foreach (ElementId elementId2 in elementIds)
                {
                    Wall wall2 = doc.GetElement(elementId2) as Wall;
                    LocationCurve Lc2 = wall2.Location as LocationCurve;
                    Curve c2 = Lc2.Curve;
                    GeometryElement geomElem = wall2.get_Geometry(opt);
                    if (wall1.Equals(wall2))
                    {
                        continue;
                    }
                    IntersectionResultArray intersectionResultArray;
                    c1.Intersect(c2, out intersectionResultArray);
                    if (intersectionResultArray != null)
                    {
                        if (orentation.Y == 1)
                        {
                            foreach (GeometryObject gobj in geomElem)
                            {
                                if (gobj.GetType().Name != "Solid")
                                {
                                    continue;
                                }
                                Solid gsolid = gobj as Solid;
                                FaceArray fArray = gsolid.Faces;
                                if (XN1.Contains((float)c1.GetEndPoint(1).X) && (float)c2.GetEndPoint(0).X == (float)c1.GetEndPoint(1).X || XN2.Contains((float)c1.GetEndPoint(0).X) && (float)c2.GetEndPoint(0).X == (float)c1.GetEndPoint(0).X)
                                {
                                    continue;
                                    XN1.Add((float)c1.GetEndPoint(0).X);
                                    XN2.Add((float)c1.GetEndPoint(1).X);
                                }
                                else
                                {
                                    nR.Append(fArray.get_Item(1).Reference);
                                }
                                XN1.Add((float)c1.GetEndPoint(0).X);
                                XN2.Add((float)c1.GetEndPoint(1).X);
                            }
                        }
                        if (orentation.X == 1)
                        {
                            foreach (GeometryObject gobj in geomElem)
                            {
                                if (gobj.GetType().Name != "Solid")
                                {
                                    continue;
                                }
                                Solid gsolid = gobj as Solid;
                                FaceArray fArray = gsolid.Faces;
                                if (YE1.Contains((float)c1.GetEndPoint(1).Y) && (float)c2.GetEndPoint(0).Y == (float)c1.GetEndPoint(1).Y || YE2.Contains((float)c1.GetEndPoint(0).Y) && (float)c2.GetEndPoint(0).Y == (float)c1.GetEndPoint(0).Y)
                                {
                                    YE1.Add((float)c1.GetEndPoint(0).Y);
                                    YE2.Add((float)c1.GetEndPoint(1).Y);
                                    continue;
                                }
                                else
                                {
                                    eR.Append(fArray.get_Item(1).Reference);
                                }
                                YE1.Add((float)c1.GetEndPoint(0).Y);
                                YE2.Add((float)c1.GetEndPoint(1).Y);
                            }
                        }
                        if (orentation.Y == -1||orentation.X!=1|| orentation.X != -1 && orentation.Y != 1 || orentation.Y != -1)
                        {
                            foreach (GeometryObject gobj in geomElem)
                            {
                                if (gobj.GetType().Name != "Solid")
                                {
                                    continue;
                                }
                                Solid gsolid = gobj as Solid;
                                FaceArray fArray = gsolid.Faces;

                                if (XS1.Contains((float)c1.GetEndPoint(1).X) && (float)c2.GetEndPoint(0).X == (float)c1.GetEndPoint(1).X || XS2.Contains((float)c1.GetEndPoint(0).X) && (float)c2.GetEndPoint(0).X == (float)c1.GetEndPoint(0).X)
                                {
                                    XS1.Add((float)c1.GetEndPoint(0).X);
                                    XS2.Add((float)c1.GetEndPoint(1).X);
                                    continue;
                                }
                                else
                                {
                                    sR.Append(fArray.get_Item(1).Reference);
                                }
                                XS1.Add((float)c1.GetEndPoint(0).X);
                                XS2.Add((float)c1.GetEndPoint(1).X);
                            }
                        }
                        if (orentation.X == -1)
                        {
                            foreach (GeometryObject gobj in geomElem)
                            {
                                if (gobj.GetType().Name != "Solid")
                                {
                                    continue;
                                }
                                Solid gsolid = gobj as Solid;
                                FaceArray fArray = gsolid.Faces;
                                if (YW1.Contains((float)c1.GetEndPoint(1).Y) && (float)c2.GetEndPoint(0).Y == (float)c1.GetEndPoint(1).Y || YW2.Contains((float)c1.GetEndPoint(0).Y) && (float)c2.GetEndPoint(0).Y == (float)c1.GetEndPoint(0).Y)
                                {
                                    YW1.Add((float)c1.GetEndPoint(0).Y);
                                    YW2.Add((float)c1.GetEndPoint(1).Y);
                                    continue;
                                }
                                else
                                {
                                    wR.Append(fArray.get_Item(1).Reference);
                                }
                                YW1.Add((float)c1.GetEndPoint(0).Y);
                                YW2.Add((float)c1.GetEndPoint(1).Y);

                            }
                        }


                    }

                }
            }
            raa.Append(nR);
            raa.Append(eR);
            raa.Append(sR);
            raa.Append(wR);
            return raa;
        }
       
        public List<Line> DimensionLines(List<ElementId> eleId, double tf)
        {
            double yN = 0, xE = 0, yS = 0, xW = 0;
            Line line = null;
            Line nline = null;
            Line eline = null;
            Line wline = null;
            Line sline = null;
            foreach (ElementId element in eleId)
            {
                Element ele = doc.GetElement(element);
                Wall wall = ele as Wall;
                XYZ orentation = wall.Orientation;
                LocationCurve Lcurve = wall.Location as LocationCurve;
                Curve curve = Lcurve.Curve;
                line = curve as Line;
                if (orentation.Y == 1 || orentation.Y == -1)
                {
                    if (yN == 0)
                    {
                        yN = line.GetEndPoint(0).Y;
                        nline = line;
                        yS = line.GetEndPoint(0).Y;
                        sline = line;
                    }
                    if (line.GetEndPoint(0).Y > yN)
                    {
                        yN = line.GetEndPoint(0).Y;
                        nline = line;
                    }
                    if (line.GetEndPoint(0).Y < yS)
                    {
                        yS = line.GetEndPoint(0).Y;
                        sline = line;
                    }
                }
                if (orentation.X == 1 || orentation.X == -1)
                {
                    if (xE == 0)
                    {
                        xE = line.GetEndPoint(0).X;
                        eline = line;
                        xW = line.GetEndPoint(0).X;
                        wline = line;
                    }
                    if (line.GetEndPoint(0).X > xE)
                    {
                        xE = line.GetEndPoint(0).X;
                        eline = line;
                    }
                    if (line.GetEndPoint(0).X < xW)
                    {
                        xW = line.GetEndPoint(0).X;
                        wline = line;
                    }
                }
            }
            XYZ myN = new XYZ(0, tf, 0);
            Transform tfn = Transform.CreateTranslation(myN);
            Line myNewN = nline.CreateTransformed(tfn) as Line;
            XYZ myE = new XYZ(tf, 0, 0);
            Transform tfe = Transform.CreateTranslation(myE);
            Line myNewE = eline.CreateTransformed(tfe) as Line;
            XYZ myS = new XYZ(0, -tf, 0);
            Transform tfs = Transform.CreateTranslation(myS);
            Line myNewS = sline.CreateTransformed(tfs) as Line;
            XYZ myW = new XYZ(-tf, 0, 0);
            Transform tfw = Transform.CreateTranslation(myW);
            Line myNewW = wline.CreateTransformed(tfw) as Line;
            List<Line> lines = new List<Line>();
            lines.Add(myNewN);
            lines.Add(myNewE);
            lines.Add(myNewS);
            lines.Add(myNewW);
            return lines;
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
                        string msg = "创建房间失败。";
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
