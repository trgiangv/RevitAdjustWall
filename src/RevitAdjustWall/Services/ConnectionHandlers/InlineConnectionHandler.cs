using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services.ConnectionHandlers;

/// <summary>
/// Handler for Inline wall connections
/// Manages straight-line end-to-end wall connections where walls are collinear
/// </summary>
public class InlineConnectionHandler : BaseConnectionHandler
{
    public override WallConnectionType ConnectionType => WallConnectionType.Inline;
    
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
        
        var line1 = GetWallLine(wall1);
        var line2 = GetWallLine(wall2);
        

        if (!AreLinesInline(line1!, line2!))
        {
            foundConnectionPoint = null;
            return false;
        }
        
        foundConnectionPoint = new List<XYZ> { line1!.GetEndPoint(0), line1!.GetEndPoint(1), line2!.GetEndPoint(0), line2!.GetEndPoint(1) }
            .OrderBy(p=>p.X).ThenBy(p=>p.Y).ThenBy(p=>p.Z)
            .ElementAt(1);
        return true;
    }
    
    public override Dictionary<Wall, Line> CalculateAdjustment(
        List<Wall> walls, XYZ connectionPoint, WallConnectionType connectionType, double gapDistance)
    {
        var adjustmentData = new Dictionary<Wall, Line>();

        var wall1 = walls[0];
        var wall2 = walls[1];

        var line1 = GetWallLine(wall1)!;
        var line2 = GetWallLine(wall2)!;

        var isConnectionOnLine1 = IsPointOnLine(connectionPoint, line1!);
        var isConnectionOnLine2 = IsPointOnLine(connectionPoint, line2!);
        
        Wall wallToAdjust, referenceWall;
        Line lineToAdjust, referenceLine;
        switch (isConnectionOnLine1)
        {
            case false when !isConnectionOnLine2:
                return adjustmentData;
            case true:
                wallToAdjust = wall2;
                lineToAdjust = line2;
                referenceWall = wall1;
                referenceLine = line1;
                break;
            default:
                wallToAdjust = wall1;
                lineToAdjust = line1;
                referenceWall = wall2;
                referenceLine = line2;
                break;
        }

        adjustmentData[wallToAdjust] = PushBack(lineToAdjust, gapDistance);
        adjustmentData[referenceWall] = referenceLine;
        
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
