using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitAdjustWall.Extensions;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services.ConnectionHandlers;

/// <summary>
/// Handler for Corner (L-Shape) wall connections
/// Manages perpendicular wall connections forming 90-degree angles
/// </summary>
public class CornerConnectionHandler : BaseConnectionHandler
{
    public override WallConnectionType ConnectionType => WallConnectionType.Corner;
    
    /// <summary>
    /// Determines if this handler can process the given wall configuration
    /// Corner connections require exactly 2 walls that are perpendicular
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

        if (!AreWallsPerpendicular(wall1, wall2))
        {
            foundConnectionPoint = null;
            return false;
        }
        
        var line1 = wall1.Line;
        var line2 = wall2.Line;

        var connectionPoint = line1.Intersection(line2)!;
        var nearestEndpoint1 = GetClosestEndpoint(line1, connectionPoint)!;
        var nearestEndpoint2 = GetClosestEndpoint(line2, connectionPoint)!;
        
        var isConnectionPointInsideLine1 = IsPointOnLine(connectionPoint, line1);
        var isConnectionPointInsideLine2 = IsPointOnLine(connectionPoint, line2);
        
        // Check if the nearest endpoints are within the thickness of the other wall
        var distance1 = nearestEndpoint1.DistanceTo(connectionPoint);
        var distance2 = nearestEndpoint2.DistanceTo(connectionPoint);
        
        // if connection point is on line1, then wall1 is valid if distance is within half the thickness of wall2
        bool isWall1Valid;
        if (isConnectionPointInsideLine1)
        {
            isWall1Valid = Math.Round(wall2.HalfThickness - distance1) > 1e-6;
        }
        else
        {
            isWall1Valid = true;
        }
        
        bool isWall2Valid;
        if (isConnectionPointInsideLine2)
        {
            isWall2Valid = Math.Round(wall1.HalfThickness - distance2) > 1e-6;
        }
        else
        {
            isWall2Valid = true;
        }
        
        foundConnectionPoint = connectionPoint;
        return isWall1Valid && isWall2Valid;
    }
    
    public override Dictionary<WallInfo, Line> CalculateAdjustment(
        List<WallInfo> walls,
        XYZ connectionPoint,
        double gapDistance
    )
    {
        var output = new Dictionary<WallInfo, Line>();

        var w1 = walls[0];
        var w2 = walls[1];

        var c1 = w1.Line;
        var c2 = w2.Line;
        
        var t1Half = w1.HalfThickness;
        var t2Half = w2.HalfThickness;

        output[w1] = PushBack(c1, - t2Half - gapDistance);
        output[w2] = PushBack(c2, t1Half);

        return output;
        
        Line PushBack(Line centre, double otherHalfThk)
        {
            var p0 = centre.GetEndPoint(0);
            var p1 = centre.GetEndPoint(1);
            var near = p0.DistanceTo(connectionPoint) <= p1.DistanceTo(connectionPoint) ? p0 : p1;
            var far  = near.IsAlmostEqualTo(p0) ? p1 : p0;

            var dir = (far - near).Normalize();
            var newNear = connectionPoint + dir * (gapDistance + otherHalfThk);
            
            return Line.CreateBound(far, newNear);
        }
    }
}
