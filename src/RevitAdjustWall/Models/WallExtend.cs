using Autodesk.Revit.DB;

namespace RevitAdjustWall.Models;

public class WallExtend
{
    public XYZ? ExtendPoint { get; set; }
    public XYZ? Direction { get; set; }
    public double Value { get; set; }
    public Line NewLocation { get; set; }
}