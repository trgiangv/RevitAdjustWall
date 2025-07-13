using Autodesk.Revit.DB;

namespace RevitAdjustWall.Extensions;

/// <summary>
///     Represent extension methods for the <see cref="Autodesk.Revit.DB.UnitUtils"/> class.
/// </summary>
public static class UnitExtensions
{
    /// <summary>
    ///     Converts millimeters to internal Revit format
    /// </summary>
    /// <returns>Value in feet</returns>
    public static double FromMillimeters(this double millimeters)
    {
        return UnitUtils.ConvertToInternalUnits(millimeters, UnitTypeId.Millimeters);
    }

    /// <summary>
    ///     Converts a Revit internal format value to millimeters
    /// </summary>
    /// <returns>Value in millimeters</returns>
    public static double ToMillimeters(this double feet)
    {
        return UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Millimeters);
    }
    

    /// <summary>
    ///     Converts degrees to internal Revit format
    /// </summary>
    /// <returns>Value in radians</returns>
    public static double FromDegrees(this double degrees)
    {
        return UnitUtils.ConvertToInternalUnits(degrees, UnitTypeId.Degrees);
    }

    /// <summary>
    ///     Converts a Revit internal format value to degrees
    /// </summary>
    /// <returns>Value in radians</returns>
    public static double ToDegrees(this double radians)
    {
        return UnitUtils.ConvertFromInternalUnits(radians, UnitTypeId.Degrees);
    }
}