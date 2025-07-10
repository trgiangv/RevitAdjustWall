using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

namespace RevitAdjustWall.Commands;

[Transaction(TransactionMode.Manual)]
public class JoinTwoWallsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        Document doc = uidoc.Document;

        try
        {
            // Prompt user to select two walls
            Reference ref1 = uidoc.Selection.PickObject(ObjectType.Element, new WallSelectionFilter(), "Select first wall");
            Reference ref2 = uidoc.Selection.PickObject(ObjectType.Element, new WallSelectionFilter(), "Select second wall");

            Wall wall1 = doc.GetElement(ref1) as Wall;
            Wall wall2 = doc.GetElement(ref2) as Wall;

            if (wall1 == null || wall2 == null)
            {
                TaskDialog.Show("Error", "Selection is not a wall.");
                return Result.Failed;
            }

            using (Transaction trans = new Transaction(doc, "Join Walls"))
            {
                trans.Start();

                // Allow wall joins at both ends
                WallUtils.AllowWallJoinAtEnd(wall1, 0);
                WallUtils.AllowWallJoinAtEnd(wall1, 1);
                WallUtils.AllowWallJoinAtEnd(wall2, 0);
                WallUtils.AllowWallJoinAtEnd(wall2, 1);

                // Join the walls
                if (!JoinGeometryUtils.AreElementsJoined(doc, wall1, wall2))
                {
                    JoinGeometryUtils.JoinGeometry(doc, wall1, wall2);
                }

                trans.Commit();
            }

            TaskDialog.Show("Success", "Walls joined successfully.");
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
