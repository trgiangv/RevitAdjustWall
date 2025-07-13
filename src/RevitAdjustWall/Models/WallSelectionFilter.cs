using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitAdjustWall.Models;

/// <summary>
/// Selection filter to allow only wall elements
/// </summary>
internal class WallSelectionFilter : ISelectionFilter
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