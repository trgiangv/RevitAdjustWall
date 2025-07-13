using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Diagnostics;
using System.Linq;
using RevitAdjustWall.Extensions;
using RevitAdjustWall.Services.ConnectionHandlers;
using RevitAdjustWall.Utilities;

namespace RevitAdjustWall.Commands;

[Transaction(TransactionMode.Manual)]
public class JoinTwoWallsCommand : IExternalCommand
{
    public static UIApplication Uiapp = null!;
    public static UIDocument? Uidoc => Uiapp.ActiveUIDocument;
    public static Document? Doc => Uidoc?.Document;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    { 
        Uiapp = commandData.Application;

        try
        {
           var eles = Uidoc.Selection.PickElementsByRectangle(new WallSelectionFilter(), "Select first wall");
           
           var connection = BaseConnectionHandler.FindConnectionPoint(eles.Cast<Wall>().ToList());
           var cornerhandler = new CornerConnectionHandler();
           
           Trace.Write(cornerhandler.CanHandle(eles.Cast<Wall>().ToList(), out var connectionPoint));
           Trace.Write(connectionPoint);

            
            
            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            // User canceled the operation
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", "An error occurred: " + ex.Message);
            message = ex.Message;
            return Result.Failed;
        }
    }

    private static void CornerJoin()
    {
        if (Doc == null || Uidoc == null) return;

        var ref1 = Uidoc.Selection.PickObject(ObjectType.Element, new WallSelectionFilter(), "Select first wall");
        var ref2 = Uidoc.Selection.PickObject(ObjectType.Element, new WallSelectionFilter(), "Select second wall");

        var wall1 = Doc.GetElement(ref1) as Wall;
        var wall2 = Doc.GetElement(ref2) as Wall;
        
        
        //
        //
        // var gap = 20.0.FromMillimeters();
        // var newEnd1MovedDist = thickness2 / 2 + gap;
        // var newStart2MovedDist = thickness1 / 2 + gap;
        //
        // var intersection = line1.Intersection(line2);
        // var newEnd1 = intersection + line1.Direction * thickness2 / 2;
        // var newLine1 = Line.CreateBound(line1Redirected.GetEndPoint(0), newEnd1);
        //     
        // var newStart2 = intersection + line2.Direction * (thickness1 / 2 + gap);
        // var newLine2 = Line.CreateBound(newStart2, line2Redirected.GetEndPoint(1));
        //     
        // using var transaction = new Transaction(Doc, "Join Two Walls");
        // transaction.Start();
        //     
        // var wallLocationCurve = wall1.Location as LocationCurve;
        // wallLocationCurve.Curve = newLine1;
        //     
        // wallLocationCurve = wall2.Location as LocationCurve;
        // wallLocationCurve.Curve = newLine2;
        //     
        // transaction.Commit();
    }

    // Selection filter to allow only walls
    public class WallSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Wall;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
