using System.Collections.Generic;
using System.Diagnostics;
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
    public override bool CanHandle(List<Wall> walls, out XYZ? foundConnectionPoint)
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
        
        var line1 = GetWallLine(wall1);
        var line2 = GetWallLine(wall2);
        
        var gapDistance = 10.0.FromMillimeters();
        var wall1Thickness = GetWallThickness(wall1);
        var wall2Thickness = GetWallThickness(wall2);
        
        var connectionPoint = FindConnectionPoint(walls);
        var nearestEndpoint1 = GetClosestEndpoint(wall1, connectionPoint);
        var nearestEndpoint2 = GetClosestEndpoint(wall2, connectionPoint);
        
        var isConnectionPointInsideLine1 = IsPointOnLine(connectionPoint!, line1!, gapDistance);
        var isConnectionPointInsideLine2 = IsPointOnLine(connectionPoint!, line2!, gapDistance);

        if (nearestEndpoint1 == null || nearestEndpoint2 == null)
        {
            foundConnectionPoint = null;
            return false;
        }
        
        // Check if the nearest endpoints are within the thickness of the other wall
        var distance1 = nearestEndpoint1.DistanceTo(connectionPoint);
        var distance2 = nearestEndpoint2.DistanceTo(connectionPoint);
        
        foundConnectionPoint = connectionPoint;

        bool isWall1Valid;
        if (isConnectionPointInsideLine1)
        {
            isWall1Valid = distance1 < wall2Thickness;
        }
        else
        {
            isWall1Valid = true;
        }
        
        bool isWall2Valid;
        if (isConnectionPointInsideLine2)
        {
            isWall2Valid = distance2 < wall1Thickness;
        }
        else
        {
            isWall2Valid = true;
        }
        
        return isWall1Valid && isWall2Valid;
    }
    
    public override Dictionary<Wall, Line> CalculateAdjustment(
        List<Wall> walls,
        XYZ connectionPoint,
        WallConnectionType connectionType,
        double gapDistance
    )
    {
        var output = new Dictionary<Wall, Line>();

        var w1 = walls[0];
        var w2 = walls[1];

        var c1 = GetWallLine(w1);
        var c2 = GetWallLine(w2);
        if (c1 == null || c2 == null) return output;
        
        var t1Half = GetWallThickness(w1) / 2.0;
        var t2Half = GetWallThickness(w2) / 2.0;

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
