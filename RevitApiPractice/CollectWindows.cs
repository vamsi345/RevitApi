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
    public class CollectWindows : IExternalCommand
    {
        private Document doc;
        private UIDocument uidoc;
        private ExternalCommandData commandData = null;
        private Options opt;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            this.opt = new Options();
            opt.ComputeReferences = true;
            opt.DetailLevel = ViewDetailLevel.Fine;
            this.commandData = commandData;
            this.doc = commandData.Application.ActiveUIDocument.Document;
            this.uidoc = commandData.Application.ActiveUIDocument;
            List<ElementId> elementIds = GetOutermostWalls(doc).Distinct().ToList();
            FilteredElementCollector collector1 = new FilteredElementCollector(doc);
            FilteredElementCollector collector2 = new FilteredElementCollector(doc);
            FilteredElementCollector collector3 = new FilteredElementCollector(doc);
            List<Wall> wallList = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
            ElementCategoryFilter Windowfilter = new ElementCategoryFilter(BuiltInCategory.OST_Windows);
            ElementCategoryFilter Doorfilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            ElementCategoryFilter Columnfilter = new ElementCategoryFilter(BuiltInCategory.OST_Columns);
            List<Element> openings = collector1.WherePasses(Windowfilter).WhereElementIsNotElementType().ToElements().ToList();
            List<Element> doors = collector2.WherePasses(Doorfilter).WhereElementIsNotElementType().ToElements().ToList();
            List<Element> columns = collector3.WherePasses(Columnfilter).WhereElementIsNotElementType().ToElements().ToList();
            openings.AddRange(doors);
            View view = doc.ActiveView;
            ReferenceArrayArray dimensionArguments1 = WHDimension(elementIds);
            ReferenceArrayArray dimensionArguments2 = OuterWallSeg(elementIds);
            ReferenceArrayArray dimensionArguments3 = segDimensions(elementIds, wallList);
            ReferenceArrayArray dimensionArguments4 = OpenDimension(openings, elementIds,dimensionArguments2);
            int tf = 0;
            int val;
            val = DimlineTrans(columns);
            if (val!=0)
            {
                List<Line> Dlinesnul = DimensionLines(elementIds,0);

                tf = val-(int)Dlinesnul[0].GetEndPoint(0).Y;
                tf = tf * (tf / tf);
            }
            List<Line> Dlines1 = DimensionLines(elementIds, tf+20);
            List<Line> Dlines2 = DimensionLines(elementIds, tf+15);
            List<Line> Dlines3 = DimensionLines(elementIds, tf+10);
            List<Line> Dlines4 = DimensionLines(elementIds, tf+5);

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
                    foreach (ReferenceArray rArray in dimensionArguments2)
                    {
                        Dimension dim = doc.Create.NewDimension(view, Dlines2[j], rArray);
                        j += 1;
                    }
                    foreach (ReferenceArray rArray in dimensionArguments3)
                    {
                        Dimension dim = doc.Create.NewDimension(view, Dlines3[k], rArray);
                        k += 1;
                    }
                    foreach (ReferenceArray rArray in dimensionArguments4)
                    {
                        Dimension dim = doc.Create.NewDimension(view, Dlines4[p], rArray);
                        p += 1;
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
        public int DimlineTrans(List<Element> columns)
        {
            int tf = 0;
            foreach(Element column in columns)
            {
                FamilyInstance fi = column as FamilyInstance;
                LocationPoint locationPoint = fi.Location as LocationPoint;
                XYZ Lpoint = locationPoint.Point;
                double value1 = Lpoint.X*(Lpoint.X / Lpoint.X);
                double value2 = Lpoint.Y * (Lpoint.Y / Lpoint.Y);
                if(value1>value2)
                {
                    if(value1>tf)
                    {
                        tf = (int)value1;
                    }
                }
                else
                {
                    if(value2>tf)
                    {
                        tf = (int)value2;
                    }
                }
            }
            return tf;
        }
        public ReferenceArrayArray WHDimension(List<ElementId> eleIds)
        {
            ReferenceArrayArray raa = new ReferenceArrayArray();
            List<Wall> nWalls = new List<Wall>();
            List<Wall> eWalls = new List<Wall>();
            List<Wall> sWalls = new List<Wall>();
            List<Wall> wWalls = new List<Wall>();
            Line line = null;
            double xN1 = 0, xN2 = 0, yE1 = 0, yE2 = 0, yW1 = 0, yW2 = 0, xS1 = 0, xS2 = 0;
            int n = 1, e = 1, w = 1, s = 1;
            foreach (ElementId eleId in eleIds)
            {
                Element ele = doc.GetElement(eleId);
                Wall wall = ele as Wall;
                XYZ orentation = wall.Orientation;
                LocationCurve Lcurve = wall.Location as LocationCurve;
                Curve curve = Lcurve.Curve;
                line = curve as Line;
                if (orentation.Y == 1)
                {
                    if (n == 1)
                    {
                        nWalls.Add(wall);
                        nWalls.Add(wall);
                        xN1 = line.GetEndPoint(0).X;
                        xN2 = line.GetEndPoint(1).X;
                        n = 2;

                    }
                    if (line.GetEndPoint(0).X < xN1)
                    {
                        nWalls.RemoveAt(0);
                        xN1 = line.GetEndPoint(0).X;
                        nWalls.Insert(0, wall);
                    }
                    if (line.GetEndPoint(1).X > xN2)
                    {
                        nWalls.RemoveAt(1);
                        xN2 = line.GetEndPoint(1).X;
                        nWalls.Insert(1, wall);
                    }

                }
                if (orentation.X == 1)
                {
                    if (e == 1)
                    {
                        eWalls.Add(wall);
                        eWalls.Add(wall);
                        yE1 = line.GetEndPoint(0).Y;
                        yE2 = line.GetEndPoint(1).Y;
                        e = 2;
                    }
                    if (line.GetEndPoint(0).Y > yE1)
                    {

                        eWalls.RemoveAt(0);
                        yE1 = line.GetEndPoint(0).Y;
                        eWalls.Insert(0, wall);
                    }
                    if (line.GetEndPoint(1).Y < yE2)
                    {
                        eWalls.RemoveAt(1);
                        yE2 = line.GetEndPoint(1).Y;
                        eWalls.Insert(1, wall);
                    }
                }
                if (orentation.Y == -1)
                {
                    if (s == 1)
                    {
                        sWalls.Add(wall);
                        sWalls.Add(wall);
                        xS1 = line.GetEndPoint(0).X;
                        xS2 = line.GetEndPoint(1).X;
                        s = 2;
                    }
                    if (line.GetEndPoint(0).X > xS1)
                    {
                        sWalls.RemoveAt(0);
                        xS1 = line.GetEndPoint(0).X;
                        sWalls.Insert(0, wall);
                    }
                    if (line.GetEndPoint(1).X < xS2)
                    {
                        sWalls.RemoveAt(1);
                        xS2 = line.GetEndPoint(1).X;
                        sWalls.Insert(1, wall);
                    }

                }
                if (orentation.X == -1)
                {
                    if (w == 1)
                    {
                        wWalls.Add(wall);
                        wWalls.Add(wall);
                        yW1 = line.GetEndPoint(0).Y;
                        yW2 = line.GetEndPoint(1).Y;
                        w = 2;
                    }
                    if (line.GetEndPoint(0).Y < yW1)
                    {

                        wWalls.RemoveAt(0);
                        yW1 = line.GetEndPoint(0).Y;
                        wWalls.Insert(0, wall);
                    }
                    if (line.GetEndPoint(1).Y > yW2)
                    {
                        wWalls.RemoveAt(1);
                        yW2 = line.GetEndPoint(1).Y;
                        wWalls.Insert(1, wall);
                    }
                }

            }
            List<List<Wall>> AllWalls = new List<List<Wall>>();
            AllWalls.Add(nWalls);
            AllWalls.Add(eWalls);
            AllWalls.Add(sWalls);
            AllWalls.Add(wWalls);

            foreach (List<Wall> walls in AllWalls)
            {
                ReferenceArray referenceArray = new ReferenceArray();
                int i = 0;
                foreach (Wall lwall in walls)
                {
                    LocationCurve Lcurve = lwall.Location as LocationCurve;
                    Curve curve = Lcurve.Curve;
                    Line line1 = curve as Line;
                    foreach (ElementId AllWall in eleIds)
                    {
                        Element ele = doc.GetElement(AllWall);
                        Wall wall = ele as Wall;
                        IntersectionResultArray intersectionResultArray;
                        LocationCurve AllLcurve = wall.Location as LocationCurve;
                        Curve Allcurve = AllLcurve.Curve;
                        Line AllLine = Allcurve as Line;
                        if (wall.Id.IntegerValue == lwall.Id.IntegerValue)
                        {
                            continue;
                        }
                        line1.Intersect(AllLine, out intersectionResultArray);
                        if (intersectionResultArray != null)
                        {
                            if ((float)line1.GetEndPoint(i).X == (float)intersectionResultArray.get_Item(0).XYZPoint.X && (float)line1.GetEndPoint(i).Y == (float)intersectionResultArray.get_Item(0).XYZPoint.Y)
                            {
                                GeometryElement geomEle = wall.get_Geometry(opt);
                                foreach (GeometryObject gobj in geomEle)
                                {
                                    if (gobj.GetType().Name != "Solid")
                                    {
                                        continue;
                                    }
                                    Solid gsolid = gobj as Solid;
                                    FaceArray faceArray = gsolid.Faces;
                                    IList<Reference> sideFaces = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior);
                                    Face face1 = uidoc.Document.GetElement(sideFaces[0]).GetGeometryObjectFromReference(sideFaces[0]) as Face;
                                    if (gobj.GetType().Name != "Solid")
                                    {
                                        continue;
                                    }
                                    Face face = null;
                                    double area = face1.Area;
                                    foreach (Face f in faceArray)
                                    {
                                        if (f.Area == area)
                                        {
                                            face = f;
                                        }
                                    }

                                    if (faceArray.get_Item(0).Area == faceArray.get_Item(1).Area && faceArray.get_Item(0).Area == area)
                                    {
                                        face = faceArray.get_Item(1);
                                    }
                                    EdgeArrayArray eAA = face.EdgeLoops;
                                    EdgeArray eA = eAA.get_Item(0);
                                    List<Edge> vedges = new List<Edge>();
                                    List<Edge> edg = new List<Edge>();
                                    foreach (Edge edge in eA)
                                    {
                                        Line line2 = edge.AsCurve() as Line;
                                        if ((int)line2.GetEndPoint(0).Z != 0 && (int)line2.GetEndPoint(1).Z == 0 || (int)line2.GetEndPoint(0).Z == 0 && (int)line2.GetEndPoint(1).Z != 0)
                                        {
                                            vedges.Add(edge);
                                        }
                                    }
                                    edg = vedges.OrderByDescending(vedge => vedge.ApproximateLength).ToList();
                                    Line l1 = edg[0].AsCurve() as Line;
                                    Line l2 = edg[1].AsCurve() as Line;
                                    if(i==0)
                                    {
                                        referenceArray.Append(edg[1].Reference);
                                    }
                                    else
                                    {
                                        referenceArray.Append(edg[0].Reference);
                                    }
                                }
                                i += 1;
                                break;
                            }

                        }

                    }


                }
                raa.Append(referenceArray);
            }
            return raa;
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
            XN1.Add(700);
            XN2.Add(700);
            YE1.Add(700);
            YE2.Add(700);
            XS1.Add(700);
            XS2.Add(700);
            YW1.Add(700);
            YW2.Add(700);
            ReferenceArrayArray raa = new ReferenceArrayArray();
            foreach (ElementId eleId in elementIds)
            {
                Wall wall = doc.GetElement(eleId) as Wall;
                LocationCurve Lc = wall.Location as LocationCurve;
                Curve C = Lc.Curve;
                Line line1 = C as Line;
                XYZ orentation = wall.Orientation;
                List<Wall> walls = WallIntersect(wall, elementIds);
                int cond = 0;
                foreach (Wall wall1 in walls)
                {
                    LocationCurve Lc2 = wall1.Location as LocationCurve;
                    Curve line2 = Lc2.Curve;
                    GeometryElement gE = wall1.get_Geometry(opt);
                    foreach (GeometryObject gO in gE)
                    {
                        if (gO.GetType().Name != "Solid")
                        {
                            continue;
                        }
                        Solid solid = gO as Solid;
                        FaceArray faceArray = solid.Faces;
                        IList<Reference> sideFaces = HostObjectUtils.GetSideFaces(wall1, ShellLayerType.Exterior);
                        Face face1 = uidoc.Document.GetElement(sideFaces[0]).GetGeometryObjectFromReference(sideFaces[0]) as Face;
                        if (gO.GetType().Name != "Solid")
                        {
                            continue;
                        }
                        Face face = null;
                        double area = face1.Area;
                        foreach (Face f in faceArray)
                        {
                            if (f.Area == area)
                            {
                                face = f;
                            }
                        }
                        
                        if (faceArray.get_Item(0).Area == faceArray.get_Item(1).Area && faceArray.get_Item(0).Area == area)
                        {
                            face = faceArray.get_Item(1);
                        }
                        EdgeArrayArray eAA = face.EdgeLoops;
                        EdgeArray eA = eAA.get_Item(0);
                        List<Edge> vedges = new List<Edge>();
                        List<Edge> edg = new List<Edge>();
                        foreach (Edge edge in eA)
                        {
                            Line line = edge.AsCurve() as Line;
                            if ((int)line.GetEndPoint(0).Z != 0 && (int)line.GetEndPoint(1).Z == 0 || (int)line.GetEndPoint(0).Z == 0 && (int)line.GetEndPoint(1).Z != 0)
                            {
                                vedges.Add(edge);
                            }
                        }
                        edg = vedges.OrderByDescending(vedge => vedge.ApproximateLength).ToList();
                        Line l1 = edg[0].AsCurve() as Line;
                        Line l2 = edg[1].AsCurve() as Line;
                        if (orentation.Y == 1)
                        {
                            if (XN1.Contains((float)line1.GetEndPoint(1).X) && (float)line2.GetEndPoint(0).X == (float)line1.GetEndPoint(1).X || XN2.Contains((float)line1.GetEndPoint(0).X) && (float)line2.GetEndPoint(0).X == (float)line1.GetEndPoint(0).X)
                            {

                                XN1.Add((float)line1.GetEndPoint(0).X);
                                XN2.Add((float)line1.GetEndPoint(1).X);
                                continue;
                            }
                            else
                            {
                                if(l1.GetEndPoint(0).X> l2.GetEndPoint(0).X)
                                {
                                    if (cond == 0)
                                    {
                                        nR.Append(edg[1].Reference);
                                    }
                                    else
                                    {
                                        nR.Append(edg[0].Reference);
                                    }
                                }
                                else
                                {
                                    if (cond == 0)
                                    {
                                        nR.Append(edg[0].Reference);
                                    }
                                    else
                                    {
                                        nR.Append(edg[1].Reference);
                                    }
                                }
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
                                if (l1.GetEndPoint(0).Y < l2.GetEndPoint(0).Y)
                                {
                                    if (cond == 0)
                                    {
                                        eR.Append(edg[1].Reference);
                                    }
                                    else
                                    {
                                        eR.Append(edg[0].Reference);
                                    }
                                }
                                else
                                {
                                    if (cond == 0)
                                    {
                                        eR.Append(edg[0].Reference);
                                    }
                                    else
                                    {
                                        eR.Append(edg[1].Reference);
                                    }
                                }
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
                                if (l1.GetEndPoint(0).X < l2.GetEndPoint(0).X)
                                {
                                    if(cond==0)
                                    {
                                        sR.Append(edg[1].Reference);
                                    }
                                    else
                                    {
                                        sR.Append(edg[0].Reference);
                                    }
                                    
                                }
                                else
                                {
                                    if (cond == 0)
                                    {
                                        sR.Append(edg[0].Reference);
                                    }
                                    else
                                    {
                                        sR.Append(edg[1].Reference);
                                    }
                                    
                                }
                                cond += 1;
                            }
                            XS1.Add((float)line1.GetEndPoint(0).X);
                            XS2.Add((float)line1.GetEndPoint(1).X);
                        }
                        if (orentation.X == -1)
                        {
                            if (YW1.Contains((float)line1.GetEndPoint(1).Y) && (float)line2.GetEndPoint(0).Y == (float)line1.GetEndPoint(1).Y || YW2.Contains((float)line1.GetEndPoint(0).Y) && (float)line2.GetEndPoint(0).Y == (float)line1.GetEndPoint(0).Y)
                            {
                                YW1.Add((float)line1.GetEndPoint(0).Y);
                                YW2.Add((float)line1.GetEndPoint(1).Y);
                                continue;
                            }
                            else
                            {
                                if (l1.GetEndPoint(0).Y > l2.GetEndPoint(0).Y)
                                {
                                    if (cond == 0)
                                    {
                                        wR.Append(edg[0].Reference);
                                    }
                                    else
                                    {
                                        wR.Append(edg[1].Reference);
                                    }
                                }
                                else
                                {
                                    if (cond == 0)
                                    {
                                        wR.Append(edg[0].Reference);
                                    }
                                    else
                                    {
                                        wR.Append(edg[1].Reference);
                                    }
                                }
                            }
                            YW1.Add((float)line1.GetEndPoint(0).Y);
                            YW2.Add((float)line1.GetEndPoint(1).Y);
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
        public List<Wall> WallIntersect(Wall wall, List<ElementId> eleIds)
        {
            List<Wall> walls = new List<Wall>(2);
            LocationCurve Lc1 = wall.Location as LocationCurve;
            Curve C1 = Lc1.Curve;
            Line L1 = C1 as Line;
            Wall walls1 = null;
            Wall walls2 = null;
            foreach (ElementId eleId in eleIds)
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
                L1.Intersect(L2, out intersectionResultArray);
                if (intersectionResultArray != null)
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
        public ReferenceArrayArray segDimensions(List<ElementId> OuterWalls, List<Wall> AllWalls)
        {
            ReferenceArray nR = new ReferenceArray();
            ReferenceArray eR = new ReferenceArray();
            ReferenceArray sR = new ReferenceArray();
            ReferenceArray wR = new ReferenceArray();
            List<int> xN = new List<int>();
            List<int> yE = new List<int>();
            List<int> xS = new List<int>();
            List<int> yW = new List<int>();
            xN.Add(700);
            yE.Add(700);
            xS.Add(700);
            yW.Add(700);
            ReferenceArrayArray raa = new ReferenceArrayArray();
            foreach (ElementId Outerelement in OuterWalls)
            {
                Element ele = doc.GetElement(Outerelement);
                Wall wall = ele as Wall;
                XYZ orentation = wall.Orientation;
                LocationCurve Lcurve = wall.Location as LocationCurve;
                Curve curve = Lcurve.Curve;
                Line line = curve as Line;
                foreach (Wall AllWall in AllWalls)
                {
                    if(AllWall.Orientation.X!=1&& AllWall.Orientation.X != -1&& AllWall.Orientation.Y != 1 && AllWall.Orientation.Y != -1)
                    {
                        continue;
                    }
                    if (Outerelement == AllWall.Id)
                    {
                        continue;
                    }
                    IntersectionResultArray intersectionResultArray;
                    LocationCurve AllLcurve = AllWall.Location as LocationCurve;
                    Curve Allcurve = AllLcurve.Curve;
                    Line AllLine = Allcurve as Line;
                    line.Intersect(AllLine, out intersectionResultArray);
                    if (intersectionResultArray == null)
                    {
                        continue;
                    }
                    if (intersectionResultArray != null)
                    {
                        IList<Reference> Faces1 = HostObjectUtils.GetSideFaces(AllWall, ShellLayerType.Exterior);
                        
                        IList<Reference> Faces2 = HostObjectUtils.GetSideFaces(AllWall, ShellLayerType.Interior);
                        
                        if (orentation.Y == 1)
                        {
                            if (xN.Contains((int)line.GetEndPoint(0).X) && xN.Contains((int)AllLine.GetEndPoint(1).X))
                            {
                                continue;

                            }
                            else
                            {
                                nR.Append(Faces1[0]);
                                nR.Append(Faces2[0]);
                            }
                            xN.Add((int)line.GetEndPoint(1).X);
                        }
                        if (orentation.X == 1)
                        {
                            if (yE.Contains((int)line.GetEndPoint(0).Y) && yE.Contains((int)AllLine.GetEndPoint(1).Y))
                            {
                                continue;

                            }
                            else
                            {
                                eR.Append(Faces1[0]);
                                eR.Append(Faces2[0]);
                            }
                            yE.Add((int)line.GetEndPoint(1).Y);
                        }
                        if (orentation.Y == -1)
                        {
                            if (xS.Contains((int)line.GetEndPoint(0).X) && xS.Contains((int)AllLine.GetEndPoint(1).X))
                            {
                                continue;

                            }
                            else
                            {
                                sR.Append(Faces1[0]);
                                sR.Append(Faces2[0]);
                            }
                            xS.Add((int)line.GetEndPoint(1).X);
                        }
                        if (orentation.X == -1)
                        {
                            if (yW.Contains((int)line.GetEndPoint(0).Y) && yW.Contains((int)AllLine.GetEndPoint(1).Y))
                            {
                                continue;

                            }
                            else
                            {
                                wR.Append(Faces1[0]);
                                wR.Append(Faces2[0]);
                            }
                            yW.Add((int)line.GetEndPoint(1).Y);
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
        public ReferenceArrayArray OpenDimension(List<Element> openings, List<ElementId> elementIds, ReferenceArrayArray BoundDimension)
        {
            ReferenceArray nR = new ReferenceArray();
            ReferenceArray eR = new ReferenceArray();
            ReferenceArray sR = new ReferenceArray();
            ReferenceArray wR = new ReferenceArray();
            ReferenceArrayArray raa = new ReferenceArrayArray();
            List<float> OpenWidth = new List<float>();
            foreach (Element element in openings)
            {
                FamilyInstance opening = element as FamilyInstance;
                if (opening.Category.Name == "Doors")
                {
                    if (!OpenWidth.Contains((float)opening.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble()))
                    {
                        OpenWidth.Add((float)opening.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble());
                    }

                }
                if (opening.Category.Name == "Windows")
                {
                    if (!OpenWidth.Contains((float)opening.Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble()))
                    {
                        OpenWidth.Add((float)opening.Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble());
                    }
                }
            }
            foreach (ElementId eleId in elementIds)
            {
                Wall wall = doc.GetElement(eleId) as Wall;
                XYZ orentation = wall.Orientation;
                LocationCurve Lcurve = wall.Location as LocationCurve;
                Curve curve = Lcurve.Curve;
                Line line = curve as Line;
                GeometryElement gEle = wall.get_Geometry(opt);
                foreach (GeometryObject gobj in gEle)
                {
                    if (gobj.GetType().Name != "Solid")
                    {
                        continue;
                    }
                    Solid gsolid = gobj as Solid;
                    FaceArray faceArray = gsolid.Faces;
                    IList<Reference> sideFaces = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior);
                    Face face1 = uidoc.Document.GetElement(sideFaces[0]).GetGeometryObjectFromReference(sideFaces[0]) as Face;
                    
                    Face face = null;
                    double area = face1.Area;
                    foreach (Face f in faceArray)
                    {
                        if (f.Area == area)
                        {
                            face = f;
                        }
                    }

                    if (faceArray.get_Item(0).Area == faceArray.get_Item(1).Area && faceArray.get_Item(0).Area == area)
                    {
                        face = faceArray.get_Item(1);
                    }
                    EdgeArrayArray edgeArrayArray = face.EdgeLoops;
                    List<Edge> ed = new List<Edge>();
                    foreach (EdgeArray edgeArray in edgeArrayArray)
                    {
                        foreach (Edge edge1 in edgeArray)
                        {
                            ReferenceArray gf = new ReferenceArray();
                            Curve c1 = edge1.AsCurve();
                            foreach (Edge edge2 in edgeArray)
                            {
                                Curve c2 = edge2.AsCurve();
                                if (ed.Contains(edge1) || ed.Contains(edge2))
                                {
                                    continue;
                                }
                                if (edge1.ApproximateLength != edge2.ApproximateLength)
                                {
                                    if ((float)edge1.ApproximateLength != (float)edge2.ApproximateLength)
                                    {

                                        continue;
                                    }
                                }

                                XYZ pt1 = c1.GetEndPoint(0);
                                if (OpenWidth.Contains((float)c2.Distance(pt1)))
                                {
                                    gf.Append(edge1.Reference);
                                    gf.Append(edge2.Reference);
                                    using (Transaction trans = new Transaction(doc, "Place1 Dimension"))
                                    {
                                        trans.Start();
                                        Dimension dim1 = doc.Create.NewDimension(doc.ActiveView, line, gf);
                                        float dimVal1 = (float)dim1.Value;
                                        String dimValStr1 = dim1.ValueString;
                                        if (dim1.Value == 0 && dim1.ValueString == "0")
                                        {
                                            doc.Delete(dim1.Id);
                                            gf.Clear();
                                        }
                                        else if (dim1.Value != 0)
                                        {
                                            doc.Delete(dim1.Id);
                                            if (orentation.Y == 1)
                                            {
                                                nR.Append(edge1.Reference);
                                                nR.Append(edge2.Reference);
                                            }
                                            if (orentation.X == 1)
                                            {
                                                eR.Append(edge1.Reference);
                                                eR.Append(edge2.Reference);
                                            }
                                            if (orentation.Y == -1)
                                            {
                                                sR.Append(edge1.Reference);
                                                sR.Append(edge2.Reference);
                                            }
                                            if (orentation.X == -1)
                                            {
                                                wR.Append(edge1.Reference);
                                                wR.Append(edge2.Reference);
                                            }
                                            ed.Add(edge1);
                                            ed.Add(edge2);
                                        }

                                        trans.Commit();
                                    }
                                    break;
                                }



                            }

                        }
                    }

                }
            }
            int pole = 1;
            foreach (ReferenceArray ra in BoundDimension)
            {
                foreach (Reference r in ra)
                {
                    if (pole == 1)
                    {
                        nR.Append(r);
                    }
                    if (pole == 2)
                    {
                        eR.Append(r);
                    }
                    if (pole == 3)
                    {
                        sR.Append(r);
                    }
                    if (pole == 4)
                    {
                        wR.Append(r);
                    }
                }
                pole += 1;
            }

            raa.Append(nR);
            raa.Append(eR);
            raa.Append(sR);
            raa.Append(wR);
            return raa;

        }
        public List<Line> DimensionLines(List<ElementId> eleId, double tf)
        {
            double yN = -1, xE = -1, yS = -1, xW = -1;
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
                    if (yN == -1)
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
                    if (xE == -1)
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
