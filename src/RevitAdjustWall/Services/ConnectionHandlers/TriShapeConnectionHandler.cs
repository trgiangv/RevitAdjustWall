using System;
using System.Collections.Generic;
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
    public override bool CanHandle(List<WallInfo> walls, out XYZ? foundConnectionPoint)
    {
        foundConnectionPoint = null;
        
        if (walls.Count != WallConnection.MaxWallsForConnection)
        {
            return false;
        }

        var wall1 = walls[0];
        var wall2 = walls[1];
        var wall3 = walls[2];
        
        var line1 = wall1.Line;
        var line2 = wall2.Line;
        var line3 = wall3.Line;
        
        var walls12Parallel = AreLinesInline(line1, line2);
        var walls13Parallel = AreLinesInline(line1, line3);
        var walls23Parallel = AreLinesInline(line2, line3);

        // Count parallel pairs - should be exactly 1 for valid Tri-Shape
        var parallelPairCount = (walls12Parallel ? 1 : 0) +
                               (walls13Parallel ? 1 : 0) +
                               (walls23Parallel ? 1 : 0);

        if (parallelPairCount != 1)
        {
            return false;
        }

        WallInfo crossWall;
        WallInfo inlineWall1;

        if (walls12Parallel)
        {
            inlineWall1 = wall1;
            crossWall = wall3;
        }
        else if (walls13Parallel)
        {
            inlineWall1 = wall1;
            crossWall = wall2;
        }
        else
        {
            inlineWall1 = wall2;
            crossWall = wall1;
        }
        
        if (!AreWallsPerpendicular(inlineWall1, crossWall))
        {
            return false;
        }
        
        foundConnectionPoint = crossWall.Line.Intersection(inlineWall1.Line);
        return true;
    }

    public override Dictionary<WallInfo, Line> CalculateAdjustment(
        List<WallInfo> walls, XYZ connectionPoint, double gapDistance)
    {
        var adjustmentData = new Dictionary<WallInfo, Line>();

        var wall1 = walls[0];
        var wall2 = walls[1];
        var wall3 = walls[2];

        var line1 = wall1.Line;
        var line2 = wall2.Line;
        var line3 = wall3.Line;
        
        var walls12Inline = AreWallsParallel(wall1, wall2);
        var walls13Inline = AreWallsParallel(wall1, wall3);
        var walls23Inline = AreWallsParallel(wall2, wall3);

        WallInfo crossWall;
        WallInfo inlineWall1, inlineWall2;
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
            return adjustmentData;
        }
        var largestHalfInlineWallThickness = Math.Max(inlineWall1.Thickness, inlineWall2.Thickness) / 2.0;
        var adjustedCrossWall = AdjustCrossWallWithGap(crossLine, connectionPoint, gapDistance, largestHalfInlineWallThickness);
        var adjustedInlineWall1 = AdjustInlineWallToGapCenter(inlineLine1, connectionPoint, gapDistance);
        var adjustedInlineWall2 = AdjustInlineWallToGapCenter(inlineLine2, connectionPoint, gapDistance);

        adjustmentData[crossWall] = adjustedCrossWall;
        adjustmentData[inlineWall1] = adjustedInlineWall1;
        adjustmentData[inlineWall2] = adjustedInlineWall2;

        return adjustmentData;
        
        Line AdjustCrossWallWithGap(Line crossWallLine, XYZ connectionPt, double gap, double largestHalfInlineThickness)
        {
            var p0 = crossWallLine.GetEndPoint(0);
            var p1 = crossWallLine.GetEndPoint(1);
            
            var near = p0.DistanceTo(connectionPt) <= p1.DistanceTo(connectionPt) ? p0 : p1;
            var far = near.IsAlmostEqualTo(p0) ? p1 : p0;
            
            var directionFromConnection = (far - near).Normalize();
            var newNear = connectionPt + directionFromConnection * (gap + largestHalfInlineThickness);
            return Line.CreateBound(far, newNear);
        }
        
        Line AdjustInlineWallToGapCenter(Line inlineWallLine, XYZ connectionPt, double gapDist)
        {
            var p0 = inlineWallLine.GetEndPoint(0);
            var p1 = inlineWallLine.GetEndPoint(1);
            
            var near = p0.DistanceTo(connectionPt) <= p1.DistanceTo(connectionPt) ? p0 : p1;
            var far = near.IsAlmostEqualTo(p0) ? p1 : p0;

            var newEnd = connectionPt + (far - connectionPt).Normalize() * gapDist / 2;
            return Line.CreateBound(far, newEnd);
        }
    }
}
