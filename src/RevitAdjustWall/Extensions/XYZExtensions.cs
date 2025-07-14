using System;
using Autodesk.Revit.DB;

namespace RevitAdjustWall.Extensions;

public static class XyzExtensions
{
    /// <summary>
    /// Checks if two vectors are parallel within a specified tolerance.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <param name="tolerance">The tolerance for parallel checking (default: 1e-9).</param>
    /// <returns>True if the vectors are parallel, otherwise false.</returns>
    public static bool IsParallel(this XYZ vector1, XYZ vector2, double tolerance = 1e-9)
    {
        var crossProduct = vector1.Normalize().CrossProduct(vector2.Normalize());
        return crossProduct.GetLength() < tolerance;
    }

    /// <summary>
    /// Checks if two vectors are perpendicular within a specified tolerance.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <param name="tolerance">The tolerance for the dot product (default: 1e-9).</param>
    /// <returns>True if the vectors are perpendicular, otherwise false.</returns>
    public static bool IsPerpendicular(this XYZ vector1, XYZ vector2, double tolerance = 1e-9)
    {
        var dotProduct = vector1.Normalize().DotProduct(vector2.Normalize());
        return Math.Abs(dotProduct) < tolerance;
    }

    /// <summary>
    /// Checks if two vectors point in the same direction (parallel and same orientation).
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <param name="tolerance">The tolerance for parallel checking (default: 1e-9).</param>
    /// <returns>True if vectors are parallel and point in the same direction.</returns>
    public static bool IsSameDirection(this XYZ vector1, XYZ vector2, double tolerance = 1e-9)
    {
        if (!vector1.IsParallel(vector2, tolerance)) return false;
        var dotProduct = vector1.Normalize().DotProduct(vector2.Normalize());
        return dotProduct > (1.0 - tolerance);
    }
}