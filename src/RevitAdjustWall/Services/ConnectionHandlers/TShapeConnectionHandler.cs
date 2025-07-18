using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitAdjustWall.Extensions;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services.ConnectionHandlers;

/// <summary>
/// Handler for T-Shape wall connections
/// Manages perpendicular wall junctions where one wall connects to the middle or end of another
/// Includes refined logic to distinguish from Corner connections based on wall thickness and length
/// </summary>
public class TShapeConnectionHandler : BaseConnectionHandler
{
    public override WallConnectionType ConnectionType => WallConnectionType.TShape;
    
    /// <summary>
    /// Determines if this handler can process the given wall configuration
    /// T-Shape connections require exactly 2 perpendicular walls where one wall connects to the middle or end of another
    /// </summary>
    public override bool CanHandle(List<WallInfo> walls, out XYZ? foundConnectionPoint)
    {
        if (walls.Count != WallConnection.MinWallsForConnection)
        {
            foundConnectionPoint = null;
            return false;
        }

        var wall1 = walls[0];
        var wall2 = walls[1];
        
        var line1 = wall1.Line;
        var line2 = wall2.Line;

        if (!AreWallsPerpendicular(wall1, wall2))
        {
            foundConnectionPoint = null;
            return false;
        }
        
        var connectionPoint = line1.Intersection(line2);
        
        var isConnectionPointInsideLine1 = IsPointOnLine(connectionPoint!, line1);
        var isConnectionPointInsideLine2 = IsPointOnLine(connectionPoint!, line2);
        
        // if connection point is not on either line, then it's not a T-Shape -> L-Shape
        if (!isConnectionPointInsideLine1 && !isConnectionPointInsideLine2)
        {
            foundConnectionPoint = null;
            return false;
        }
        
        // continue check if one of the walls is longer than the other than haft thickness of the other wall -> Cross
        var crossWall = isConnectionPointInsideLine1 ? wall2 : wall1;
        var mainWall = isConnectionPointInsideLine1 ? wall1 : wall2;
        
        var crossEndPoint = GetClosestEndpoint(crossWall.Line, connectionPoint!);
        var crossWallLength = crossEndPoint!.DistanceTo(connectionPoint!);
        var halfMainWallThickness = mainWall.HalfThickness;
        
        if (wall1.Equals(crossWall) && isConnectionPointInsideLine1 && crossWallLength > halfMainWallThickness 
            || wall2.Equals(crossWall) && isConnectionPointInsideLine2 && crossWallLength > halfMainWallThickness)
        {
            foundConnectionPoint = null;
            return false;
        }

        foundConnectionPoint = connectionPoint;
        return true;
    }

    public override Dictionary<WallInfo, Line> CalculateAdjustment(
        List<WallInfo> walls, XYZ connectionPoint, double gapDistance)
    {
        var adjustmentData = new Dictionary<WallInfo, Line>();

        var wall1 = walls[0];
        var wall2 = walls[1];

        var line1 = wall1.Line;
        var line2 = wall2.Line;
        
        // Determine which wall is the "main" wall (has connection point on its line)
        // and which is the "connecting" wall (connects to the main wall)
        var isConnectionOnLine1 = IsPointOnLine(connectionPoint, line1);
        var isConnectionOnLine2 = IsPointOnLine(connectionPoint, line2);

        var crossWall = isConnectionOnLine1 ? wall2 : wall1;
        var mainWall = isConnectionOnLine2 ? wall1 : wall2;
        var mainLine = isConnectionOnLine1 ? line1 : line2;
        var crossLine = isConnectionOnLine1 ? line2 : line1;


        // For T-Shape connections:
        // 1. Main wall: keep unchanged (no modification needed)
        // 2. Cross wall (connecting wall): adjust to maintain gap distance from main wall

        // Main wall: keep original line unchanged
        adjustmentData[mainWall] = mainLine;

        // Cross wall adjustment: maintain gap from main wall
        var connectingWallAdjustment = PushBack(crossLine, mainWall.HalfThickness + gapDistance);
        adjustmentData[crossWall] = connectingWallAdjustment;

        return adjustmentData;
        
        Line PushBack(Line wallLine, double adjustmentDistance)
        {
            var p0 = wallLine.GetEndPoint(0);
            var p1 = wallLine.GetEndPoint(1);
            var near = p0.DistanceTo(connectionPoint) <= p1.DistanceTo(connectionPoint) ? p0 : p1;
            var far = near.IsAlmostEqualTo(p0) ? p1 : p0;

            var dir = (far - near).Normalize();
            var newNear = connectionPoint + dir * adjustmentDistance;

            return Line.CreateBound(far, newNear);
        }
    }
}
