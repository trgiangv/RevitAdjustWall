using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitAdjustWall.Extensions;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services.ConnectionHandlers;

/// <summary>
/// Handler for Tri-Shape wall connections
/// Manages exactly 3 walls meeting at a single connection point
/// Specifically handles the case where 2 walls are inline and 1 wall is perpendicular
/// </summary>
public class TriShapeConnectionHandler : BaseConnectionHandler
{
    public override WallConnectionType ConnectionType => WallConnectionType.TriShape;
    
    /// <summary>
    /// Determines if this handler can process the given wall configuration
    /// Tri-Shape connections require exactly 3 walls: 2 inline walls and 1 perpendicular cross wall
    /// </summary>
    public override bool CanHandle(List<Wall> walls, out XYZ? foundConnectionPoint)
    {
        foundConnectionPoint = null;

        // 1. Verify exactly 3 walls are provided
        if (walls.Count != WallConnection.MaxWallsForConnection)
        {
            return false;
        }

        var wall1 = walls[0];
        var wall2 = walls[1];
        var wall3 = walls[2];

        // Get wall lines
        var line1 = GetWallLine(wall1);
        var line2 = GetWallLine(wall2);
        var line3 = GetWallLine(wall3);

        if (line1 == null || line2 == null || line3 == null)
        {
            return false;
        }

        // 2. Determine which walls are parallel (inline) and which is the cross wall
        // Use AreWallsParallel to match the logic in CalculateAdjustment
        var walls12Parallel = AreWallsParallel(wall1, wall2);
        var walls13Parallel = AreWallsParallel(wall1, wall3);
        var walls23Parallel = AreWallsParallel(wall2, wall3);

        // Count parallel pairs - should be exactly 1 for valid Tri-Shape
        var parallelPairCount = (walls12Parallel ? 1 : 0) +
                               (walls13Parallel ? 1 : 0) +
                               (walls23Parallel ? 1 : 0);

        if (parallelPairCount != 1)
        {
            return false;
        }

        // 3. Find the connection point using a more flexible approach
        var connectionPoint = FindTriShapeConnectionPoint(walls);
        if (connectionPoint == null)
        {
            return false;
        }

        Wall crossWall;
        Wall inlineWall1, inlineWall2;
        Line crossLine, inlineLine1, inlineLine2;

        if (walls12Parallel)
        {
            // Wall1 and Wall2 are inline, Wall3 is cross
            inlineWall1 = wall1;
            inlineWall2 = wall2;
            crossWall = wall3;
            inlineLine1 = line1;
            inlineLine2 = line2;
            crossLine = line3;
        }
        else if (walls13Parallel)
        {
            // Wall1 and Wall3 are inline, Wall2 is cross
            inlineWall1 = wall1;
            inlineWall2 = wall3;
            crossWall = wall2;
            inlineLine1 = line1;
            inlineLine2 = line3;
            crossLine = line2;
        }
        else // walls23Parallel
        {
            // Wall2 and Wall3 are inline, Wall1 is cross
            inlineWall1 = wall2;
            inlineWall2 = wall3;
            crossWall = wall1;
            inlineLine1 = line2;
            inlineLine2 = line3;
            crossLine = line1;
        }

        // 4. Verify the cross wall is perpendicular to the inline walls
        if (!AreWallsPerpendicular(crossWall, inlineWall1))
        {
            return false;
        }

        // 5. Basic validation: ensure we have a reasonable connection point
        // For Tri-Shape, we're more permissive about the exact geometric constraints
        // The key requirement is: 2 parallel walls + 1 perpendicular wall + valid connection point

        foundConnectionPoint = connectionPoint;
        return true;
    }

    /// <summary>
    /// Finds connection point specifically for Tri-Shape configurations
    /// Uses a more flexible approach than the base FindConnectionPoint method
    /// </summary>
    private static XYZ? FindTriShapeConnectionPoint(List<Wall> walls)
    {
        if (walls.Count != 3) return null;

        var line1 = GetWallLine(walls[0]);
        var line2 = GetWallLine(walls[1]);
        var line3 = GetWallLine(walls[2]);

        if (line1 == null || line2 == null || line3 == null) return null;

        // Try to find intersection points between all pairs
        var intersection12 = line1.Intersection(line2);
        var intersection13 = line1.Intersection(line3);
        var intersection23 = line2.Intersection(line3);

        // For Tri-Shape, we expect at least one valid intersection
        // Return the first valid intersection found
        if (intersection12 != null) return intersection12;
        if (intersection13 != null) return intersection13;
        if (intersection23 != null) return intersection23;

        return null;
    }


    public override Dictionary<Wall, Line> CalculateAdjustment(
        List<Wall> walls, XYZ connectionPoint, WallConnectionType connectionType, double gapDistance)
    {
        var adjustmentData = new Dictionary<Wall, Line>();

        var wall1 = walls[0];
        var wall2 = walls[1];
        var wall3 = walls[2];

        var line1 = GetWallLine(wall1);
        var line2 = GetWallLine(wall2);
        var line3 = GetWallLine(wall3);

        if (line1 == null || line2 == null || line3 == null)
            return adjustmentData;

        // Determine which walls are inline and which is the cross wall
        var walls12Inline = AreWallsParallel(wall1, wall2);
        var walls13Inline = AreWallsParallel(wall1, wall3);
        var walls23Inline = AreWallsParallel(wall2, wall3);

        Wall crossWall;
        Wall inlineWall1, inlineWall2;
        Line crossLine, inlineLine1, inlineLine2;

        if (walls12Inline)
        {
            inlineWall1 = wall1; inlineWall2 = wall2; crossWall = wall3;
            inlineLine1 = line1; inlineLine2 = line2; crossLine = line3;
        }
        else if (walls13Inline)
        {
            inlineWall1 = wall1; inlineWall2 = wall3; crossWall = wall2;
            inlineLine1 = line1; inlineLine2 = line3; crossLine = line2;
        }
        else if (walls23Inline)
        {
            inlineWall1 = wall2; inlineWall2 = wall3; crossWall = wall1;
            inlineLine1 = line2; inlineLine2 = line3; crossLine = line1;
        }
        else
        {
            // Fallback: no inline walls detected, return original lines
            adjustmentData[wall1] = line1;
            adjustmentData[wall2] = line2;
            adjustmentData[wall3] = line3;
            return adjustmentData;
        }

        // STEP 1: Find the true connection point where cross wall intersects inline walls
        var trueConnectionPoint = FindTrueConnectionPoint(crossLine, inlineLine1, inlineLine2);
        if (trueConnectionPoint == null)
        {
            // Fallback to original connection point if intersection not found
            trueConnectionPoint = connectionPoint;
        }

        // STEP 2: Adjust the cross wall position
        // Move the cross wall back by the gap distance from the inline walls
        var adjustedCrossWall = AdjustCrossWallWithGap(crossLine, trueConnectionPoint, gapDistance);

        // STEP 3: Adjust the inline walls to meet at the gap center
        // The gap center is at the original true connection point
        // The inline walls should extend to meet at this point
        var adjustedInlineWall1 = AdjustInlineWallToGapCenter(inlineLine1, trueConnectionPoint);
        var adjustedInlineWall2 = AdjustInlineWallToGapCenter(inlineLine2, trueConnectionPoint);

        adjustmentData[crossWall] = adjustedCrossWall;
        adjustmentData[inlineWall1] = adjustedInlineWall1;
        adjustmentData[inlineWall2] = adjustedInlineWall2;

        return adjustmentData;

        // Helper method to find the true connection point where cross wall intersects inline walls
        XYZ? FindTrueConnectionPoint(Line crossWallLine, Line inline1, Line inline2)
        {
            // Try intersection with first inline wall
            var intersection1 = crossWallLine.Intersection(inline1);
            if (intersection1 != null) return intersection1;

            // Try intersection with second inline wall
            var intersection2 = crossWallLine.Intersection(inline2);
            if (intersection2 != null) return intersection2;

            return null;
        }

        // Helper method to adjust cross wall by moving it back by gap distance
        Line AdjustCrossWallWithGap(Line crossWallLine, XYZ connectionPt, double gap)
        {
            var p0 = crossWallLine.GetEndPoint(0);
            var p1 = crossWallLine.GetEndPoint(1);

            // Find which endpoint is closer to the connection point
            var near = p0.DistanceTo(connectionPt) <= p1.DistanceTo(connectionPt) ? p0 : p1;
            var far = near.IsAlmostEqualTo(p0) ? p1 : p0;

            // Calculate direction from connection point to the near endpoint (away from inline walls)
            var moveDirection = (near - connectionPt).Normalize();

            // Move the near endpoint away from connection point by the gap distance
            var newNear = near + moveDirection * gap;

            return Line.CreateBound(far, newNear);
        }

        // Helper method to adjust inline wall to meet at the gap center
        Line AdjustInlineWallToGapCenter(Line inlineWallLine, XYZ gapCenter)
        {
            var p0 = inlineWallLine.GetEndPoint(0);
            var p1 = inlineWallLine.GetEndPoint(1);

            // Find which endpoint is closer to the gap center
            var near = p0.DistanceTo(gapCenter) <= p1.DistanceTo(gapCenter) ? p0 : p1;
            var far = near.IsAlmostEqualTo(p0) ? p1 : p0;

            // Extend the wall to meet at the gap center
            return Line.CreateBound(far, gapCenter);
        }
    }
}
