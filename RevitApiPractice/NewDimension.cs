using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace RevitApiPractice
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class NewDimension : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get UIDocument
            UIDocument uidoc = commandData.Application.ActiveUIDocument;

            //Get Document
            Document doc = uidoc.Document;

            //Get Level
            

            //Create Points 
           

            //Create Curves
           

           

            try
            {
                using (Transaction trans = new Transaction(doc, "Place Family"))
                {
                    Reference pickedObj = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
                    ElementId eleId = pickedObj.ElementId;
                    Element ele = doc.GetElement(eleId);
					FamilyInstance inst = ele as FamilyInstance;
					
					ElementId eTypeId = ele.GetTypeId();
                    ElementType eType = doc.GetElement(eTypeId) as ElementType;
                    Options opt = new Options();
                    opt.ComputeReferences = true;
                    opt.DetailLevel = ViewDetailLevel.Fine;
                    opt.IncludeNonVisibleObjects = true;
					

					GeometryElement geomElem = inst.GetOriginalGeometry(opt);
					GeometryObject geomObj=geomElem.First();
					Solid solid=geomObj as Solid;
					FaceArray fa = solid.Faces;
					Face f1 = fa.get_Item(0);
					Face f2 = fa.get_Item(1);
					XYZ pt1 = new XYZ(-45, 43, 0);
                    XYZ pt2 = new XYZ(37, 43, 0);
                    Line line = Line.CreateBound(pt1, pt2);
                    ReferenceArray rr1 = new ReferenceArray();
                    
                    View view = doc.ActiveView;
					Reference r1 = f1.Reference;
					Reference r2 = f2.Reference;
					rr1.Append(r1);
					rr1.Append(r2);
					trans.Start();

                    Dimension dim = doc.Create.NewDimension(view, line, rr1);


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
		public enum SpecialReferenceType
		{
			Left = 0,
			CenterLR = 1,
			Right = 2,
			Front = 3,
			CenterFB = 4,
			Back = 5,
			Bottom = 6,
			CenterElevation = 7,
			Top = 8
		}


		public static Reference GetSpecialFamilyReference(FamilyInstance inst, SpecialReferenceType refType)
		{
			Reference indexRef = null;

			int idx = (int)refType;


			if (inst != null)
			{
				Document dbDoc = inst.Document;

				Options geomOptions = dbDoc.Application.Create.NewGeometryOptions();

				if (geomOptions != null)
				{
					geomOptions.ComputeReferences = true;
					geomOptions.DetailLevel = ViewDetailLevel.Undefined;
					geomOptions.IncludeNonVisibleObjects = true;
				}

				GeometryElement gElement = inst.get_Geometry(geomOptions);
				GeometryInstance gInst = gElement.First() as GeometryInstance;

				String sampleStableRef = null;

				if (gInst != null)
				{
					GeometryElement gSymbol = gInst.GetSymbolGeometry();

					if (gSymbol != null)
					{
						foreach (GeometryObject geomObj in gSymbol)
						{
							if (geomObj is Solid)
							{
								Solid solid = geomObj as Solid;

								if (solid.Faces.Size > 0)
								{
									Face face = solid.Faces.get_Item(0);
									sampleStableRef = face.Reference.ConvertToStableRepresentation(dbDoc);
									break;
								}
							}
							else if (geomObj is Curve)
							{
								Curve curve = geomObj as Curve;

								sampleStableRef = curve.Reference.ConvertToStableRepresentation(dbDoc);
								break;
							}
							else if (geomObj is Point)
							{
								Point point = geomObj as Point;

								sampleStableRef = point.Reference.ConvertToStableRepresentation(dbDoc);
								break;
							}
						}
					}

					if (sampleStableRef != null)
					{
						String[] refTokens = sampleStableRef.Split(new char[] { ':' });

						String customStableRef = refTokens[0] + ":" + refTokens[1] + ":" + refTokens[2] + ":" + refTokens[3] + ":" + idx.ToString();

						indexRef = Reference.ParseFromStableRepresentation(dbDoc, customStableRef);

						GeometryObject geoObj = inst.GetGeometryObjectFromReference(indexRef);

						if (geoObj != null)
						{
							String finalToken = "";

							if (geoObj is Edge)
							{
								finalToken = ":LINEAR";
							}

							if (geoObj is Face)
							{
								finalToken = ":SURFACE";
							}

							customStableRef += finalToken;

							indexRef = Reference.ParseFromStableRepresentation(dbDoc, customStableRef);
						}
						else
						{
							indexRef = null;
						}
					}
				}
				else
				{
					throw new Exception("No Symbol Geometry found...");
				}


			}


			return indexRef;
		}
	}
}
