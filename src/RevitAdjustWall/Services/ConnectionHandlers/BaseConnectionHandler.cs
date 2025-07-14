using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitAdjustWall.Extensions;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services.ConnectionHandlers;

/// <summary>
/// Base class for connection handlers providing common functionality
/// Implements shared geometric analysis and utility methods
/// </summary>
public abstract class BaseConnectionHandler : IConnectionHandler
{
    private static readonly double AngleTolerance = 1.0.FromDegrees();
    private static readonly double PerpendicularAngle = 90.0.FromDegrees();
    private static readonly double ParallelAngle = 0.0.FromDegrees();
    private static readonly double OppositeAngle = 180.0.FromDegrees();
    
    /// <summary>
    /// Gets the connection type this handler manages
    /// </summary>
    public abstract WallConnectionType ConnectionType { get; }

    /// <summary>
    /// Determines if this handler can process the given wall configuration
    /// </summary>
    public abstract bool CanHandle(List<WallInfo> walls, out XYZ? foundConnectionPoint);

    /// <summary>
    /// Calculates new wall endpoints based on gap distance for this connection type
    /// </summary>
    public abstract Dictionary<WallInfo, Line> CalculateAdjustment(
        List<WallInfo> walls, XYZ connectionPoint, double gapDistance);
    

    /// <summary>
    /// Calculates the angle between two wall directions
    /// </summary>
    private static double GetAngleBetweenWalls(WallInfo wall1, WallInfo wall2)
    {
        var line1 = wall1.Line;
        var line2 = wall2.Line;

        return line1.Direction.AngleTo(line2.Direction);
    }

    /// <summary>
    /// Checks if two walls are perpendicular within tolerance
    /// </summary>
    protected static bool AreWallsPerpendicular(WallInfo wall1, WallInfo wall2)
    {
        var angle = GetAngleBetweenWalls(wall1, wall2);
        return Math.Abs(angle - PerpendicularAngle) < AngleTolerance;
    }

    /// <summary>
    /// Checks if two walls are parallel (or opposite) within tolerance
    /// </summary>
    protected static bool AreWallsParallel(WallInfo wall1, WallInfo wall2)
    {
        var angle = GetAngleBetweenWalls(wall1, wall2);
        return Math.Abs(angle - ParallelAngle) < AngleTolerance ||
               Math.Abs(angle - OppositeAngle) < AngleTolerance;
    }
    

    protected static bool AreLinesInline(Line line1, Line line2)
    {
        var dir1 = line1.Direction.Normalize();
        var dir2 = line2.Direction.Normalize();
        
        var isParallel = dir1.CrossProduct(dir2).IsZeroLength();
        if (!isParallel) return false;
        
        var p1 = line1.GetEndPoint(0);
        var p2 = line2.GetEndPoint(0);
        
        var vec = p2 - p1;
        return vec.CrossProduct(dir1).IsZeroLength();
    }


    /// <summary>
    /// Gets the endpoint of a wall that is closest to the connection point
    /// </summary>
    protected static XYZ? GetClosestEndpoint(Line line, XYZ? connectionPoint)
    {
        if (connectionPoint == null) return null;

        var start = line.GetEndPoint(0);
        var end = line.GetEndPoint(1);

        return start.DistanceTo(connectionPoint) <= end.DistanceTo(connectionPoint) ? start : end;
    }

    protected static bool IsPointOnLine(XYZ point, Line line, double tolerance = 1e-6)
    {
        var start = line.GetEndPoint(0);
        var end = line.GetEndPoint(1);

        // Direction vector of the line
        var lineVec = end - start;
        var pointVec = point - start;

        // Check if vectors are parallel: cross product == zero vector
        var cross = lineVec.CrossProduct(pointVec);
        if (cross.GetLength() > tolerance)
            return false;

        // Check if point lies within the segment bounds using dot product
        var dot = pointVec.DotProduct(lineVec);
        return !(dot < -tolerance) && !(dot > lineVec.DotProduct(lineVec) + tolerance);
    }
}
