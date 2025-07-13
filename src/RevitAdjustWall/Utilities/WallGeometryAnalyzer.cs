using System;
using Autodesk.Revit.DB;
using RevitAdjustWall.Extensions;

namespace RevitAdjustWall.Utilities;

/// <summary>
/// Utility class for analyzing wall geometry configurations, particularly L-shaped corners
/// </summary>
public static class WallGeometryAnalyzer
{
    /// <summary>
    /// Default tolerance for geometric calculations (1mm in Revit internal units)
    /// </summary>
    private static readonly double DefaultTolerance = 1.0.FromMillimeters();

    /// <summary>
    /// Tolerance for angle calculations (1 degree in radians)
    /// </summary>
    private const double AngleTolerance = Math.PI / 180.0;

    /// <summary>
    /// Represents the result of L-shape analysis between two walls
    /// </summary>
    public class LShapeAnalysisResult
    {
        public bool IsLShape { get; set; }
        public string Description { get; set; } = string.Empty;
        public XYZ? CornerPoint { get; set; }
        public double AngleBetweenWalls { get; set; }
        public bool AreWallsConnected { get; set; }
        public double DistanceBetweenWalls { get; set; }
    }

    /// <summary>
    /// Analyzes if two walls form an L-shape configuration based on their location line directions
    /// </summary>
    /// <param name="wall1">First wall</param>
    /// <param name="wall2">Second wall</param>
    /// <param name="tolerance">Distance tolerance for connection detection</param>
    /// <returns>Analysis result indicating if walls form an L-shape</returns>
    public static LShapeAnalysisResult AnalyzeLShapeConfiguration(Wall wall1, Wall wall2, double tolerance = -1)
    {
        if (tolerance < 0) tolerance = DefaultTolerance;
        
        var result = new LShapeAnalysisResult();
        
        // Get wall location lines
        var line1 = GetWallLocationLine(wall1);
        var line2 = GetWallLocationLine(wall2);
        
        if (line1 == null || line2 == null)
        {
            result.Description = "One or both walls do not have valid location lines";
            return result;
        }

        // Get direction vectors
        var direction1 = line1.Direction;
        var direction2 = line2.Direction;
        
        // Calculate angle between directions
        result.AngleBetweenWalls = direction1.AngleTo(direction2);
        
        // Check if walls are perpendicular (L-shape requirement)
        if (!direction1.IsPerpendicular(direction2, AngleTolerance))
        {
            result.Description = $"Walls are not perpendicular. Angle: {result.AngleBetweenWalls:F1}°";
            return result;
        }
        
        // Find potential corner point (intersection of infinite lines)
        var intersectionPoint = line1.Intersection(line2);
        if (intersectionPoint == null)
        {
            result.Description = "Wall direction lines do not intersect (parallel walls)";
            return result;
        }
        
        result.CornerPoint = intersectionPoint;
        
        // Check if walls are connected or close enough to form a continuous L-shape
        var connectionAnalysis = AnalyzeWallConnection(line1, line2, intersectionPoint, tolerance);
        result.AreWallsConnected = connectionAnalysis.AreConnected;
        result.DistanceBetweenWalls = connectionAnalysis.Distance;
        
        // Determine if this forms a valid L-shape
        result.IsLShape = result.AreWallsConnected && 
                         Math.Abs(result.AngleBetweenWalls - 90.0) < 1.0; // Within 1 degree of 90°
        
        if (result.IsLShape)
        {
            result.Description = $"Valid L-shape detected. Angle: {result.AngleBetweenWalls:F1}°, " +
                               $"Connection distance: {result.DistanceBetweenWalls.ToMillimeters():F1}mm";
        }
        else if (!result.AreWallsConnected)
        {
            result.Description = $"Walls are perpendicular but not connected. " +
                               $"Distance: {result.DistanceBetweenWalls.ToMillimeters():F1}mm";
        }
        
        return result;
    }

    /// <summary>
    /// Checks if two wall direction vectors form a continuous L-shape pattern
    /// </summary>
    /// <param name="direction1">Direction vector of first wall</param>
    /// <param name="direction2">Direction vector of second wall</param>
    /// <param name="tolerance">Angle tolerance in radians</param>
    /// <returns>True if directions form L-shape (perpendicular)</returns>
    public static bool AreDirectionsContinuousLShape(XYZ direction1, XYZ direction2, double tolerance = -1)
    {
        if (tolerance < 0) tolerance = AngleTolerance;
        
        // L-shape requires perpendicular directions
        return direction1.IsPerpendicular(direction2, tolerance);
    }

    /// <summary>
    /// Gets the location line of a wall
    /// </summary>
    /// <param name="wall">The wall</param>
    /// <returns>Location line or null if not available</returns>
    private static Line? GetWallLocationLine(Wall wall)
    {
        return wall.Location is LocationCurve { Curve: Line line }
            ? line 
            : null;
    }

    /// <summary>
    /// Analyzes the connection between two wall lines
    /// </summary>
    private static (bool AreConnected, double Distance) AnalyzeWallConnection(
        Line line1, Line line2, XYZ intersectionPoint, double tolerance)
    {
        // Check if intersection point is close to either wall's endpoints
        var line1Start = line1.GetEndPoint(0);
        var line1End = line1.GetEndPoint(1);
        var line2Start = line2.GetEndPoint(0);
        var line2End = line2.GetEndPoint(1);
        
        // Calculate distances from intersection to all endpoints
        var distances = new[]
        {
            intersectionPoint.DistanceTo(line1Start),
            intersectionPoint.DistanceTo(line1End),
            intersectionPoint.DistanceTo(line2Start),
            intersectionPoint.DistanceTo(line2End)
        };
        
        // Find minimum distance
        var minDistance = Math.Min(Math.Min(distances[0], distances[1]), 
                                  Math.Min(distances[2], distances[3]));
        
        // Walls are considered connected if intersection is close to any endpoint
        var areConnected = minDistance <= tolerance;
        
        return (areConnected, minDistance);
    }

    /// <summary>
    /// Determines which ends of the walls should be modified to create a proper L-shape connection
    /// </summary>
    /// <param name="wall1">First wall</param>
    /// <param name="wall2">Second wall</param>
    /// <param name="cornerPoint">The corner point where walls should meet</param>
    /// <returns>Tuple indicating which ends to modify (0=start, 1=end)</returns>
    public static (int Wall1End, int Wall2End) DetermineEndsToModify(Wall wall1, Wall wall2, XYZ cornerPoint)
    {
        var line1 = GetWallLocationLine(wall1);
        var line2 = GetWallLocationLine(wall2);
        
        if (line1 == null || line2 == null)
            return (-1, -1);
        
        // Find which end of each wall is closest to the corner point
        var wall1StartDist = cornerPoint.DistanceTo(line1.GetEndPoint(0));
        var wall1EndDist = cornerPoint.DistanceTo(line1.GetEndPoint(1));
        var wall1End = wall1StartDist < wall1EndDist ? 0 : 1;
        
        var wall2StartDist = cornerPoint.DistanceTo(line2.GetEndPoint(0));
        var wall2EndDist = cornerPoint.DistanceTo(line2.GetEndPoint(1));
        var wall2End = wall2StartDist < wall2EndDist ? 0 : 1;
        
        return (wall1End, wall2End);
    }
}
