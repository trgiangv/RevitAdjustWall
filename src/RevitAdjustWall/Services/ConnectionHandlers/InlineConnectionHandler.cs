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

        if (!AreWallsParallel(wall1, wall2))
        {
            foundConnectionPoint = null;
            return false;
        }
        
        foundConnectionPoint = FindConnectionPoint(walls);
        return true;
    }
    
    public override Dictionary<Wall, Line> CalculateAdjustment(
        List<Wall> walls, XYZ connectionPoint, WallConnectionType connectionType, double gapDistance)
    {
        return new Dictionary<Wall, Line>();
    }
}
