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
    public class Dimensions : IExternalCommand
    {
        private Document doc;
        private ExternalCommandData commandData = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            this.commandData = commandData;
            this.doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            List<ElementId> elementIdss = GetOutermostWalls(doc);
            uidoc.Selection.SetElementIds(elementIdss);
            Selection selection = uidoc.Selection;
            List<ElementId> elementIds = uidoc.Selection.GetElementIds() as List<ElementId>;
            List<Line> Dlines = DimensionLines(elementIds);
            ReferenceArray rArray = new ReferenceArray();
            View view = doc.ActiveView;
            int opt = 0;
             try
                {
                    using (Transaction trans = new Transaction(doc, "Place Family"))
                    {
                   
                        trans.Start();
                    foreach (Line lin in Dlines)
                    {
                        opt += 1;
                        rArray = CreateDimension(elementIds, lin, opt);
                        
                        ReferenceArray rrr1 = new ReferenceArray();
                        rrr1.Append(rArray.get_Item(0));
                        if (opt == 1)
                        {
                            rrr1.Append(rArray.get_Item(0));
                            rrr1.Append(rArray.get_Item(rArray.Size));
                        }
                        else
                        {
                         rrr1.Append(rArray.get_Item(rArray.Size - 1));
                        }
                        Dimension dim = doc.Create.NewDimension(view, lin, rrr1);
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




            public ReferenceArray CreateDimension(List<ElementId> eleIds, Line line, int orient)
        {
            ReferenceArray rr1 = new ReferenceArray();
            foreach (ElementId eleId in eleIds)
            {
                Element ele = doc.GetElement(eleId);
                Wall wall = ele as Wall;
                XYZ orentation = wall.Orientation;
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.DetailLevel = ViewDetailLevel.Fine;
                GeometryElement geomElem = ele.get_Geometry(opt);
                if (orentation.Y == 1&& orient==1)
                {
                    foreach (GeometryObject gobj in geomElem)
                    {
                        Solid gsolid = gobj as Solid;
                        EdgeArray eArray = gsolid.Edges;
                        Edge e1 = eArray.get_Item(4);
                        Edge e2 = eArray.get_Item(6);
                        Reference r1 = e1.Reference;
                        Reference r2 = e2.Reference;

                        rr1.Append(r1);
                        rr1.Append(r2);

                    }
                }
                if (orentation.X == 1 && orient == 2)
                {
                    foreach (GeometryObject gobj in geomElem)
                    {
                        Solid gsolid = gobj as Solid;
                        EdgeArray eArray = gsolid.Edges;
                        Edge e1 = eArray.get_Item(4);
                        Edge e2 = eArray.get_Item(6);
                        Reference r1 = e1.Reference;
                        Reference r2 = e2.Reference;

                        rr1.Append(r1);
                        rr1.Append(r2);

                    }
                }
                if (orentation.Y == -1 && orient == 3)
                {
                    foreach (GeometryObject gobj in geomElem)
                    {
                        Solid gsolid = gobj as Solid;
                        EdgeArray eArray = gsolid.Edges;
                        Edge e1 = eArray.get_Item(4);
                        Edge e2 = eArray.get_Item(6);
                        Reference r1 = e1.Reference;
                        Reference r2 = e2.Reference;

                        rr1.Append(r1);
                        rr1.Append(r2);

                    }
                }
                if (orentation.X == -1 && orient == 4)
                {
                    foreach (GeometryObject gobj in geomElem)
                    {
                        Solid gsolid = gobj as Solid;
                        EdgeArray eArray = gsolid.Edges;
                        Edge e1 = eArray.get_Item(4);
                        Edge e2 = eArray.get_Item(6);
                        Reference r1 = e1.Reference;
                        Reference r2 = e2.Reference;

                        rr1.Append(r1);
                        rr1.Append(r2);

                    }
                }
            }
            return rr1;
        }
        public List<Line> DimensionLines(List<ElementId> eleId)
        {
            double  yN = 0, xE = 0, yS = 0, xW = 0;
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
                if(orentation.Y==1&& line.GetEndPoint(0).Y > yN)
                {
                    yN = line.GetEndPoint(0).Y;
                    nline = line;
                }
                if (orentation.X == 1&& line.GetEndPoint(0).X > xE)
                {
                    xE = line.GetEndPoint(0).X;
                    eline = line;
                }
                if (orentation.Y == -1&& line.GetEndPoint(0).Y < yS)
                {
                    yS = line.GetEndPoint(0).Y;
                    sline = line;
                }
                if (orentation.X == -1&& line.GetEndPoint(0).X < xW)
                {
                    xW = line.GetEndPoint(0).X;
                    wline = line;
                }
            }
            XYZ myN = new XYZ(0, 10, 0);
            Transform tfn = Transform.CreateTranslation(myN);
            Line myNewN = nline.CreateTransformed(tfn) as Line;
            XYZ myE = new XYZ(10, 0, 0);
            Transform tfe = Transform.CreateTranslation(myE);
            Line myNewE = eline.CreateTransformed(tfe) as Line;
            XYZ myS = new XYZ(0, -10, 0);
            Transform tfs = Transform.CreateTranslation(myS);
            Line myNewS = sline.CreateTransformed(tfs) as Line;
            XYZ myW = new XYZ(-10, 0, 0);
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
