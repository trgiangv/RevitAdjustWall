using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services.ConnectionHandlers;

/// <summary>
/// Interface for wall connection handlers implementing the Strategy pattern
/// Each connection type (Corner, Inline, T-Shape, Tri-Shape) has its own handler
/// </summary>
public interface IConnectionHandler
{
    /// <summary>
    /// Gets the connection type this handler manages
    /// </summary>
    WallConnectionType ConnectionType { get; }
    
    /// <summary>
    /// Determines if this handler can process the given wall configuration
    /// </summary>
    /// <param name="walls">The walls at the connection point</param>
    /// <param name="foundConnectionPoint">The connection point</param>
    /// <returns>True if the handler can process the configuration</returns>
    bool CanHandle(List<Wall> walls, out XYZ? foundConnectionPoint);

    /// <summary>
    /// Calculates new wall endpoints based on gap distance for this connection type
    /// </summary>
    /// <param name="walls">The walls at the connection point</param>
    /// <param name="connectionPoint">The connection point</param>
    /// <param name="connectionType">The connection type</param>
    /// <param name="gapDistance">The gap distance in feet</param>
    /// <returns>Dictionary of walls and their new endpoints</returns>
    Dictionary<Wall, Line> CalculateAdjustment(
        List<Wall> walls, XYZ connectionPoint, WallConnectionType connectionType, double gapDistance);
}
