using Autodesk.Revit.DB;

namespace RevitAdjustWall.Models;

public class WallInfo(Wall wall)
{
    public Line Line { get; } = (((LocationCurve)wall.Location).Curve as Line)!;
    public double Thickness { get; } = wall.Width;
    public double HalfThickness => Thickness / 2.0;
    
    /// <summary>
    ///     Sets the wall location curve
    /// </summary>
    /// <param name="newLocation">The new location curve</param>
    /// <returns>The wall with the new location curve</returns>
    public void SetLocation(Curve newLocation)
    {
        if (wall.Location is LocationCurve locationCurve)
        {
            locationCurve.Curve = newLocation;
        }
    }
}