using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
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
        
        var wall1Thickness = GetWallThickness(wall1);
        var wall2Thickness = GetWallThickness(wall2);
        
        var connectionPoint = FindConnectionPoint(walls);
        var nearestEndpoint1 = GetClosestEndpoint(wall1, connectionPoint);
        var nearestEndpoint2 = GetClosestEndpoint(wall2, connectionPoint);

        if (nearestEndpoint1 == null || nearestEndpoint2 == null)
        {
            foundConnectionPoint = null;
            return false;
            
        }
        
        // Check if the nearest endpoints are within the thickness of the other wall
        var distance1 = nearestEndpoint1.DistanceTo(connectionPoint);
        var distance2 = nearestEndpoint2.DistanceTo(connectionPoint);
        
        foundConnectionPoint = connectionPoint;
        return distance1 < wall2Thickness || distance2 < wall1Thickness;
    }
    
    public override Dictionary<Wall, WallExtend> CalculateAdjustment(
        List<Wall> walls, XYZ connectionPoint, WallConnectionType connectionType, double gapDistance)
    {
        var adjustmentData = new Dictionary<Wall, WallExtend>();
        
        var wall1 = walls[0];
        var wall2 = walls[1];

        var line1 = GetWallLine(wall1);
        var line2 = GetWallLine(wall2);

        var extend1 = new WallExtend();
        var extend2 = new WallExtend();
        
        
        var isConnectionPointInsideLine1 = IsPointOnLine(connectionPoint, line1!, gapDistance);
        var isConnectionPointInsideLine2 = IsPointOnLine(connectionPoint, line2!, gapDistance);
        
        extend1.ExtendPoint = GetClosestEndpoint(wall1, connectionPoint);
        extend1.Value = gapDistance + GetWallThickness(wall2) / 2 + extend1.ExtendPoint!.DistanceTo(connectionPoint);
        extend1.Direction = isConnectionPointInsideLine1 
            ? (connectionPoint - extend1.ExtendPoint).Normalize() 
            : (extend1.ExtendPoint - connectionPoint).Normalize();
        extend1.NewLocation = Line.CreateBound(GetFarthestEndpoint(wall1, connectionPoint), extend1.ExtendPoint + extend1.Direction * extend1.Value);
        
        
        extend2.ExtendPoint = GetClosestEndpoint(wall2, connectionPoint);
        extend2.Value = gapDistance + GetWallThickness(wall1) / 2 + extend2.ExtendPoint!.DistanceTo(connectionPoint);
        
        extend2.Direction = isConnectionPointInsideLine2
            ? (connectionPoint - extend2.ExtendPoint).Normalize() 
            : (extend2.ExtendPoint - connectionPoint).Normalize();
        extend2.NewLocation = Line.CreateBound(GetFarthestEndpoint(wall2, connectionPoint), extend2.ExtendPoint + extend2.Direction * extend2.Value);
        
        adjustmentData.Add(wall1, extend1);
        adjustmentData.Add(wall2, extend2);
        return adjustmentData;
    }
}
