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
    class RoofPlacement : IExternalCommand
    {
        private Document doc;
        private ExternalCommandData commandData = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            this.commandData = commandData;
            this.doc = commandData.Application.ActiveUIDocument.Document;
            DBConnect dbc = new DBConnect();
            String st = dbc.Select("Bungalow");
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            FamilySymbol symbol = collector.OfClass(typeof(FamilySymbol))
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .First(x => x.Name ==st );
            try
            {
                using (Transaction trans = new Transaction(doc, "Place roof"))
                {
                    trans.Start();
                    if (!symbol.IsActive)
                    {
                        symbol.Activate();
                    }

                    doc.Create.NewFamilyInstance(new XYZ(0, 0, 0), symbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

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
