using System;
using Autodesk.Revit.DB;

namespace RevitAdjustWall.Extensions;

public static class XyzExtensions
{
    
    /// <summary>
    ///     Project a point to a plane
    /// </summary>
    /// <param name="xyzPoint">source point</param>
    /// <param name="plane">plane to project</param>
    /// <returns>a point projected to the plane</returns>
    public static XYZ ProjectToPlane(this XYZ xyzPoint, Plane plane)
    {
        var vecPoToPlaneOrigin = plane.Origin - xyzPoint;
        if (Math.Abs(vecPoToPlaneOrigin.DotProduct(plane.Normal)) > 1e-6)
        {
            return xyzPoint + plane.Normal * vecPoToPlaneOrigin.DotProduct(plane.Normal);
        }
        return xyzPoint;
    }
    
    /// <summary>
    /// Projects a point to a plane along a specified direction.
    /// </summary>
    /// <param name="xyzPoint">The point to project.</param>
    /// <param name="planeOrigin">The origin of the plane.</param>
    /// <param name="planeNormal">The normal vector of the plane.</param>
    /// <param name="projectDirection">The direction along which to project the point.</param>
    /// <returns>The projected point on the plane.</returns>
    public static XYZ ProjectToPlane(this XYZ xyzPoint, XYZ planeOrigin, XYZ planeNormal, XYZ projectDirection)
    {
         // Calculate the vector from the plane origin to the point
         var v = xyzPoint - planeOrigin;
         
         // Calculate the distance along projectDirection to the plane
         var distanceAlongVToPlane = -v.DotProduct(planeNormal) / projectDirection.DotProduct(planeNormal);
         
         var projectedPoint = xyzPoint + distanceAlongVToPlane * projectDirection;
         
         return projectedPoint;
    }
    
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

    /// <summary>
    /// Checks if two vectors point in opposite directions (parallel but opposite orientation).
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <param name="tolerance">The tolerance for parallel checking (default: 1e-9).</param>
    /// <returns>True if vectors are parallel and point in opposite directions.</returns>
    public static bool IsOppositeDirection(this XYZ vector1, XYZ vector2, double tolerance = 1e-9)
    {
        if (!vector1.IsParallel(vector2, tolerance)) return false;
        var dotProduct = vector1.Normalize().DotProduct(vector2.Normalize());
        return dotProduct < -(1.0 - tolerance);
    }

    /// <summary>
    /// Calculates the angle between two vectors in radians.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The angle between the vectors in radians (0 to π).</returns>
    public static double Angle(this XYZ vector1, XYZ vector2)
    {
        var v1Normalized = vector1.Normalize();
        var v2Normalized = vector2.Normalize();
        
        return v1Normalized.AngleTo(v2Normalized);
    }
    
    /// <summary>
    /// Calculates the angle between two vectors in radians.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The angle between the vectors on a plane defined by the planeNormal vector (0 to 2π).</returns>
    public static double Angle(this XYZ vector1, XYZ vector2, XYZ planeNormal)
    {
        var v1Normalized = vector1.Normalize();
        var v2Normalized = vector2.Normalize();

        return v1Normalized.AngleOnPlaneTo(v2Normalized, planeNormal);
    }
}