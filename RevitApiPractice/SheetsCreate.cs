using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Text.RegularExpressions;

namespace RevitApiPractice

{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SheetsCreate : IExternalCommand
    {
        private Document doc;
        private ExternalCommandData commandData = null;
        private Options opt;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            
            this.commandData = commandData;
            this.doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            FilteredElementCollector collectorp = new FilteredElementCollector(doc);
            collectorp.OfClass(typeof(ViewPlan));
            FilteredElementCollector collectors = new FilteredElementCollector(doc);
            collectors.OfClass(typeof(ViewSection));
            List<ViewPlan> ViewPlansp = collectorp.Cast<ViewPlan>().ToList();
            List<ViewSection> ViewPlanss = collectors.Cast<ViewSection>().ToList();
            List<ViewPlan> FloorViews = new List<ViewPlan>();
            List<ViewSection> ElevationViews = new List<ViewSection>();
            FilteredElementCollector collector1 = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_TitleBlocks);
            Element TB1 = null;
            Element TB2 = null;
            foreach (Element e in collector1)
            {
                if (e.Name.Contains("A4 sheet ground floor (1_100)"))
                {
                    TB1 = e;
                }
                if (e.Name.Contains("A4 sheet Elevation(1_100)"))
                {
                    TB2 = e;
                }
            }
            foreach (ViewPlan vp in ViewPlansp)
            {
                if(vp.ViewType==ViewType.FloorPlan)
                {
                    FloorViews.Add(vp);
                }
            }

            foreach (ViewSection vp in ViewPlanss)
            {
                if (vp.ViewType == ViewType.Elevation)
                {
                    ElevationViews.Add(vp);
                }
            }

            try
            {
                using (Transaction trans = new Transaction(doc, "Place Dimension1"))
                {
                    trans.Start();
                    ViewSheet viewSheet2 = ViewSheet.Create(doc, TB2.Id);

                    foreach (ViewPlan floorplan in FloorViews)
                    {
                        if (floorplan.Name == "Level 1" || floorplan.Name == "Level 2")
                        {
                            floorPlanSheet(floorplan, TB1);

                        }


                    }
                    int sh = 1;
                    foreach (ViewSection elevation in ElevationViews)
                    {
                        String st = elevation.Name;
                        if ((st == "North")||(st=="South")|| (st == "East")|| (st == "West"))
                        {
                            ElevationPlanSheet(elevation, viewSheet2, sh);
                            sh += 1;
                        }
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

            
            return Result.Succeeded;
        }
        public void floorPlanSheet(ViewPlan viewplan, Element tb)
        {
            ViewSheet viewSheet1 = ViewSheet.Create(doc, tb.Id);
            ElementId id = viewplan.Id;
            Viewport viewport= Viewport.Create(doc, viewSheet1.Id, id, new XYZ(0.4, 0.4, 0));
            
            viewSheet1.Print();
                
        }
        public void ElevationPlanSheet(ViewSection viewplan, ViewSheet viewSheet,int sh)
        {
            ElementId id = viewplan.Id;
            if(sh==1)
            {
                Viewport viewport = Viewport.Create(doc, viewSheet.Id, id, new XYZ(0.2, 0.55, 0));
            }
            if (sh == 2)
            {
                Viewport viewport = Viewport.Create(doc, viewSheet.Id, id, new XYZ(0.7, 0.55, 0));
            }
            if (sh == 3)
            {
                Viewport viewport = Viewport.Create(doc, viewSheet.Id, id, new XYZ(0.2, 0.25, 0));
            }
            if (sh == 4)
            {
                Viewport viewport = Viewport.Create(doc, viewSheet.Id, id, new XYZ(0.7, 0.25, 0));
            }

            if(sh==4)
            {
                viewSheet.Print();
            }
           

        }

    }
}
