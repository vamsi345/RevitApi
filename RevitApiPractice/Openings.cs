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
    [TransactionAttribute(TransactionMode.Manual)]
    class Openings : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                //Pick Object
                Reference pickedObj = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);

                //Retrieve Element
                ElementId eleId = pickedObj.ElementId;
                View view = doc.ActiveView;
                Element ele = doc.GetElement(eleId) as Element;
                ReferenceArray raa = new ReferenceArray();
                Wall wall = ele as Wall;
                XYZ orentation = wall.Orientation;
                LocationCurve Lcurve = wall.Location as LocationCurve;
                Curve curve = Lcurve.Curve;
                Line lin = curve as Line;
                Line line = Line.CreateBound(new XYZ(lin.GetEndPoint(0).X, lin.GetEndPoint(0).Y + 5, 0), new XYZ(lin.GetEndPoint(1).X, lin.GetEndPoint(1).Y + 5, 0));
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.DetailLevel = ViewDetailLevel.Fine;
                GeometryElement gEle = ele.get_Geometry(opt);
                List<float> width1 = new List<float>();

                FilteredElementCollector collector1 = new FilteredElementCollector(doc);
                FilteredElementCollector collector2 = new FilteredElementCollector(doc);
                ElementCategoryFilter Windowfilter = new ElementCategoryFilter(BuiltInCategory.OST_Windows);
                ElementCategoryFilter Doorfilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
                List<Element> openings = collector1.WherePasses(Windowfilter).WhereElementIsNotElementType().ToElements().ToList();
                List<Element> doors = collector2.WherePasses(Doorfilter).WhereElementIsNotElementType().ToElements().ToList();
                openings.AddRange(doors);

                foreach(Element element in openings)
                {
                    FamilyInstance opening = element as FamilyInstance;
                    if (opening.Category.Name == "Doors")
                    {
                        if(!width1.Contains((float)opening.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble()))
                        {
                            width1.Add((float)opening.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble());
                        }
                       
                    }
                    if (opening.Category.Name == "Windows")
                    {
                        if (!width1.Contains((float)opening.Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble()))
                        {
                            width1.Add((float)opening.Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble());
                        }
                    }
                }
                ReferenceArrayArray referenceArrayArray = new ReferenceArrayArray();
                foreach (GeometryObject gobj in gEle)
                {
                    Solid gsolid = gobj as Solid;
                    FaceArray fArray = gsolid.Faces;
                    Face f1 = fArray.get_Item(0);
                    EdgeArrayArray edgeArrayArray= f1.EdgeLoops;
                    List<Edge> ed1 = new List<Edge>();
                    List<Edge> ed2 = new List<Edge>();
                    foreach (EdgeArray edgeArray in edgeArrayArray)
                    {
                        foreach(Edge edge1 in edgeArray)
                        {
                            ReferenceArray gf = new ReferenceArray();
                            Curve c1 = edge1.AsCurve();
                            foreach (Edge edge2 in edgeArray)
                            {
                                Curve c2 = edge2.AsCurve();
                                if(ed1.Contains(edge1)||ed1.Contains(edge2))
                                {
                                    continue;
                                }
                                if(edge1.ApproximateLength!=edge2.ApproximateLength)
                                {
                                    continue;
                                }
                                
                                XYZ pt1 = c1.GetEndPoint(0);
                                float dist1= (float)c2.Distance(pt1);
                                float distance = (float)pt1.DistanceTo(c2.GetEndPoint(0));
                                    if (width1.Contains((float)c2.Distance(pt1)))
                                    {
                                        gf.Append(edge1.Reference);
                                        gf.Append(edge2.Reference);
                                        using (Transaction trans = new Transaction(doc, "Place1 Dimension"))
                                        {
                                            trans.Start();


                                            Dimension dim1 = doc.Create.NewDimension(view, line, gf);
                                            float dimVal1 = (float)dim1.Value;
                                            String dimValStr1 = dim1.ValueString;
                                            if (dim1.Value == 0 && dim1.ValueString=="0")
                                            {
                                                doc.Delete(dim1.Id);
                                                gf.Clear();
                                            }
                                            else if (dim1.Value != 0)
                                            {
                                                doc.Delete(dim1.Id);
                                                raa.Append(edge1.Reference);
                                                raa.Append(edge2.Reference);
                                                ed1.Add(edge1);
                                                ed1.Add(edge2);
                                            }

                                            trans.Commit();
                                        }
                                        break;
                                    }
                                
                               

                            }
                          
                        }
                    }

                }

                using (Transaction trans = new Transaction(doc, "Place Dimension"))
                {
                    trans.Start();
                    

                        Dimension dim = doc.Create.NewDimension(view, line, raa);
                       

                    trans.Commit();
                }
                return Result.Succeeded;

            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }


        }
        public ReferenceArrayArray OpeningDimension(List<Element> openings, List<ElementId> eleIds, View view, Document doc, List<Line> Dlines, ReferenceArrayArray BoundDimension)
        {
            ReferenceArray nR = new ReferenceArray();
            ReferenceArray eR = new ReferenceArray();
            ReferenceArray sR = new ReferenceArray();
            ReferenceArray wR = new ReferenceArray();
            ReferenceArrayArray rpp = new ReferenceArrayArray();
            Options opt = new Options();
            opt.ComputeReferences = true;
            opt.DetailLevel = ViewDetailLevel.Fine;
            int q = 0;
            int pole = 1;
            foreach (ReferenceArray ra in BoundDimension)
            {

                foreach (Reference r in ra)
                {

                    Wall BWall = doc.GetElement(r.ElementId) as Wall;
                    GeometryElement gE = BWall.get_Geometry(opt);
                    foreach (GeometryObject gO in gE)
                    {
                        Solid gS = gO as Solid;
                        FaceArray fA = gS.Faces;
                        Face f = fA.get_Item(5);
                        EdgeArrayArray eAA = f.EdgeLoops;
                        foreach (EdgeArray eA in eAA)
                        {
                            if (pole == 1)
                            {
                                nR.Append(eA.get_Item(2).Reference);
                                break;
                            }
                            if (pole == 2)
                            {
                                eR.Append(eA.get_Item(2).Reference);
                                break;
                            }
                            if (pole == 3)
                            {
                                sR.Append(eA.get_Item(2).Reference);
                                break;
                            }
                            if (pole == 4)
                            {
                                wR.Append(eA.get_Item(2).Reference);
                                break;
                            }

                        }


                    }

                }
                pole += 1;
            }

            foreach (Element ele in openings)
            {
                double width = 0;
                FamilyInstance opening = ele as FamilyInstance;
                Wall wall = opening.Host as Wall;
                XYZ orentation = wall.Orientation;
                LocationCurve Lcurve = wall.Location as LocationCurve;
                Curve curve = Lcurve.Curve;
                Line line = curve as Line;
                if (orentation.Y == 1)
                {
                    line = Dlines[0];
                }
                if (orentation.X == 1)
                {
                    line = Dlines[1];
                }
                if (orentation.Y == -1)
                {
                    line = Dlines[2];
                }
                if (orentation.X == -1)
                {
                    line = Dlines[3];
                }
                if (!eleIds.Contains(wall.Id))
                {
                    continue;
                }
                if (opening.Category.Name == "Doors")
                {
                    width = opening.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble();
                }
                if (opening.Category.Name == "Windows")
                {
                    width = opening.Symbol.get_Parameter(BuiltInParameter.WINDOW_WIDTH).AsDouble();
                }
                Transform trans = Transform.Identity;
                GeometryElement gEle = opening.get_Geometry(opt);
                foreach (GeometryInstance gIns in gEle)
                {
                    GeometryElement gInsEle = gIns.GetSymbolGeometry();
                    ReferenceArray checkRef = new ReferenceArray();
                    foreach (GeometryObject gObj in gInsEle)
                    {
                        if (gObj.GetType().Name != "Solid")
                        {
                            continue;
                        }
                        Solid solid = gObj as Solid;
                        EdgeArray edgeArray = solid.Edges;
                        FaceArray faces = solid.Faces;
                        foreach (Edge edge1 in edgeArray)
                        {
                            Curve c1 = edge1.AsCurve();
                            foreach (Edge edge2 in edgeArray)
                            {
                                Curve c2 = edge2.AsCurve();
                                if (edge1.Equals(edge2))
                                {
                                    continue;
                                }
                                else
                                {
                                    if (c1.GetEndPoint(0).Y == c2.GetEndPoint(0).Y || c1.GetEndPoint(0).Y == c2.GetEndPoint(1).Y)
                                    {
                                        if ((float)c1.Distance(c2.GetEndPoint(0)) == (float)width || (float)c1.Distance(c2.GetEndPoint(1)) == (float)width)
                                        {
                                            checkRef.Append(edge1.Reference);
                                            checkRef.Append(edge2.Reference);




                                            Dimension dim = doc.Create.NewDimension(view, line, checkRef);
                                            float pp = (float)dim.Value;
                                            float kk = (float)width;
                                            String st = dim.ValueString;
                                            if ((float)dim.Value != (float)width)
                                            {
                                                doc.Delete(dim.Id);
                                                checkRef.Clear();
                                            }
                                            else if ((float)dim.Value == (float)width)
                                            {
                                                doc.Delete(dim.Id);
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


                                            }


                                            if (!checkRef.IsEmpty)
                                            {
                                                break;
                                            }
                                        }

                                    }
                                }
                            }
                            if (!checkRef.IsEmpty)
                            {
                                break;
                            }
                        }
                        if (!checkRef.IsEmpty)
                        {
                            break;
                        }
                    }
                    break;
                }
                if (ele.Id.IntegerValue == 241184)
                {
                    String dp = "0";
                }

            }
            int t = 0;
            rpp.Append(nR);
            rpp.Append(eR);
            rpp.Append(sR);
            rpp.Append(wR);

            Dimension dim1 = doc.Create.NewDimension(view, Dlines[0], nR);
            Dimension dim2 = doc.Create.NewDimension(view, Dlines[1], eR);
            Dimension dim3 = doc.Create.NewDimension(view, Dlines[2], sR);
            Dimension dim4 = doc.Create.NewDimension(view, Dlines[3], wR);
            return rpp;

        }

    }

}
