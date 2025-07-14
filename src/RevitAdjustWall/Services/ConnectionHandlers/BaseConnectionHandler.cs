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
    public abstract bool CanHandle(List<Wall> walls, out XYZ? foundConnectionPoint);

    /// <summary>
    /// Calculates new wall endpoints based on gap distance for this connection type
    /// </summary>
    public abstract Dictionary<Wall, Line> CalculateAdjustment(
        List<Wall> walls, XYZ connectionPoint, WallConnectionType connectionType, double gapDistance);
    
    /// <summary>
    /// Gets the line from a wall's location curve
    /// </summary>
    protected static Line? GetWallLine(Wall wall)
    {
        return wall.Location is LocationCurve { Curve: Line line } ? line : null;
    }

    /// <summary>
    /// Calculates the angle between two wall directions
    /// </summary>
    private static double GetAngleBetweenWalls(Wall wall1, Wall wall2)
    {
        var line1 = GetWallLine(wall1);
        var line2 = GetWallLine(wall2);
        
        if (line1 == null || line2 == null)
            return 0.0;

        return line1.Direction.AngleTo(line2.Direction);
    }

    /// <summary>
    /// Checks if two walls are perpendicular within tolerance
    /// </summary>
    protected static bool AreWallsPerpendicular(Wall wall1, Wall wall2)
    {
        var angle = GetAngleBetweenWalls(wall1, wall2);
        return Math.Abs(angle - PerpendicularAngle) < AngleTolerance;
    }

    /// <summary>
    /// Checks if two walls are parallel (or opposite) within tolerance
    /// </summary>
    protected static bool AreWallsParallel(Wall wall1, Wall wall2)
    {
        var angle = GetAngleBetweenWalls(wall1, wall2);
        return Math.Abs(angle - ParallelAngle) < AngleTolerance ||
               Math.Abs(angle - OppositeAngle) < AngleTolerance;
    }
    
    /// <summary>
    /// Finds all potential connection points from walls
    /// </summary>
    public static XYZ? FindConnectionPoint(List<Wall> walls)
    {
        if (walls.Count is < WallConnection.MinWallsForConnection or > WallConnection.MaxWallsForConnection)
            return null;

        return walls.Count switch
        {
            WallConnection.MinWallsForConnection => FindConnection2Walls(walls[0], walls[1]),
            WallConnection.MaxWallsForConnection => FindConnection3Walls(walls[0], walls[1], walls[2]),
            _ => null
        };
    }

    private static XYZ? FindConnection2Walls(Wall wall1, Wall wall2)
    {
        var line1 = GetWallLine(wall1);
        var line2 = GetWallLine(wall2);
        
        if (line1 == null || line2 == null)
            return null;

        return line1.Intersection(line2);
    }

    private static XYZ? FindConnection3Walls(Wall wall1, Wall wall2, Wall wall3)
    {
        var line1 = GetWallLine(wall1);
        var line2 = GetWallLine(wall2);
        var line3 = GetWallLine(wall3);

        if (line1 == null || line2 == null || line3 == null)
            return null;
        
        
        var line1Line2Collinear = AreLinesInline(line1, line2);
        var line1Line3Collinear = AreLinesInline(line1, line3);
        var line2Line3Collinear = AreLinesInline(line2, line3);

        // Count collinear pairs - should be exactly 1 for valid Tri-Shape
        var collinearPairCount = (line1Line2Collinear ? 1 : 0) +
                                (line1Line3Collinear ? 1 : 0) +
                                (line2Line3Collinear ? 1 : 0);

        if (collinearPairCount != 1)
            return null;
        
        if (line1Line2Collinear)
        {
            return line3.Intersection(line1) ?? line3.Intersection(line2);
        }

        if (line1Line3Collinear)
        {
            return line2.Intersection(line1) ?? line2.Intersection(line3);
        }
        
        return line1.Intersection(line2) ?? line1.Intersection(line3);
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
    
    /// <summary>
    /// Gets the wall thickness at the connection point
    /// </summary>
    protected static double GetWallThickness(Wall wall)
    {
        return wall.Width;
    }
}
