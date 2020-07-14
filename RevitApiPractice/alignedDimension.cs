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
    class alignedDimension : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                ReferenceArray raa = new ReferenceArray();
                View view = doc.ActiveView;
                //Pick Object
                for (int i=0;i<=1;i++)
                {
                    Reference pickedObj = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);

                    //Retrieve Element
                    ElementId eleId = pickedObj.ElementId;
                   
                    Element ele = doc.GetElement(eleId);
                    
                    Wall wall = ele as Wall;
                    XYZ orentation = wall.Orientation;
                    LocationCurve Lcurve = wall.Location as LocationCurve;
                    Curve curve = Lcurve.Curve;
                    Line lin = curve as Line;
                    Line line = Line.CreateBound(new XYZ(lin.GetEndPoint(0).X, lin.GetEndPoint(0).Y + 5, 0), new XYZ(lin.GetEndPoint(1).X, lin.GetEndPoint(1).Y + 5, 0));
                    Options opt = new Options();
                    opt.ComputeReferences = true;
                    opt.DetailLevel = ViewDetailLevel.Fine;

                    GeometryElement geomElem = wall.get_Geometry(opt);
                    foreach (GeometryObject gobj in geomElem)
                    {
                        Solid gsolid = gobj as Solid;
                        FaceArray fArray = gsolid.Faces;
                        Face f1 = fArray.get_Item(0);
                        
                        Reference r1 = f1.Reference;
                        

                        raa.Append(r1);
                        

                    }
                }
               

                //Get Element Type
               

                //Display Element Id
                using (Transaction trans = new Transaction(doc, "Place Dimension"))
                {
                    trans.Start();
                    Dimension dim = doc.Create.NewAlignment(view, raa.get_Item(0), raa.get_Item(1));
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
