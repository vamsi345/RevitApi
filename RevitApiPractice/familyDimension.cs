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
    class familyDimension : IExternalCommand
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
                Element ele = doc.GetElement(eleId);
                ReferenceArray raa = new ReferenceArray();
                Wall wall=ele as Wall;
                
                XYZ orentation = wall.Orientation;
                LocationCurve Lcurve = wall.Location as LocationCurve;
                
                Curve curve = Lcurve.Curve;
                Line lin = curve as Line;
                Line line = Line.CreateBound(new XYZ(lin.GetEndPoint(0).X, lin.GetEndPoint(0).Y + 5, 0), new XYZ(lin.GetEndPoint(1).X, lin.GetEndPoint(1).Y + 5, 0));
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.DetailLevel = ViewDetailLevel.Fine;


                using (Transaction trans = new Transaction(doc, "Place Dimension1"))
                {
                    
                    trans.Start();
                    GeometryElement gE = wall.get_Geometry(opt);
                    foreach (GeometryObject go in gE)
                    {
                        if (go.GetType().Name!= "Solid")
                        {
                            continue;
                        }
                        Solid solid = go as Solid;
                        EdgeArray edgeArray = solid.Edges;
                        foreach(Edge edge in edgeArray)
                        {
                            if(edge.ApproximateLength>=lin.ApproximateLength)
                            {
                                raa.Append(edge.GetEndPointReference(0));
                                raa.Append(edge.GetEndPointReference(1));
                                break;
                            }
                        }
                        
                    }
                    
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
    }
}
