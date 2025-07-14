using Autodesk.Revit.DB;

namespace RevitAdjustWall.Extensions;

public static class WallExtensions
{
    /// <summary>
    ///     Sets the wall location curve
    /// </summary>
    /// <param name="wall">The wall to set the location for</param>
    /// <param name="newLocation">The new location curve</param>
    /// <returns>The wall with the new location curve</returns>
    public static void SetLocation(this Wall wall, Curve newLocation)
    {
        if (wall.Location is LocationCurve locationCurve)
        {
            locationCurve.Curve = newLocation;
        }
    }
}