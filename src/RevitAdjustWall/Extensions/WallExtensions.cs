using Autodesk.Revit.DB;
using RevitAdjustWall.Exceptions;

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
    
    /// <summary>
    ///     Extends the wall location curve by the specified value
    /// </summary>
    /// <param name="wall">The wall to extend</param>
    /// <param name="extendValue">The value to extend by</param>
    /// <param name="extendStart">True to extend the start, false to extend the end</param>
    /// <returns>The wall with the new location curve</returns>
    /// <exception cref="WallAdjustmentOperationException">
    ///     Wall location curve is not a line
    /// </exception>
    /// <exception cref="T:Autodesk.Revit.Exceptions.ArgumentsInconsistentException">
    ///    Curve length is too small for Revit's tolerance (as identified by Application.ShortCurveTolerance)
    /// </exception>
    public static void SetLocation(this Wall wall, double extendValue, bool extendStart = true)
    {
        if (wall.Location is not LocationCurve locationCurve) return;
        
        if (locationCurve.Curve is Line line)
        {
            locationCurve.Curve = line.Extend(extendValue, extendStart);
        }
        else
        {
            throw new WallAdjustmentOperationException("Wall location curve is not a line");
        }
    }
}