using System.Collections.Generic;
using Autodesk.Revit.DB;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services;

/// <summary>
/// Interface for wall adjustment operations
/// Follows the Interface Segregation Principle (ISP)
/// </summary>
public interface IWallAdjustmentService
{
    /// <summary>
    /// Analyzes wall connections to determine connection types
    /// </summary>
    /// <param name="walls">The walls to analyze</param>
    /// <returns>List of wall connections with their types</returns>
    List<WallConnection> AnalyzeWallConnections(List<Wall> walls);

    /// <summary>
    /// Adjusts walls based on the specified gap distance and connection rules
    /// </summary>
    /// <param name="model">The wall adjustment model containing all necessary data</param>
    /// <returns>True if adjustment was successful</returns>
    bool AdjustWalls(WallAdjustmentModel model);

    /// <summary>
    /// Calculates the new wall endpoints based on gap distance and connection type
    /// </summary>
    /// <param name="connection">The wall connection to adjust</param>
    /// <param name="gapDistance">The gap distance in feet</param>
    /// <returns>Dictionary of walls and their new endpoints</returns>
    Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)> CalculateNewWallEndpoints(
        WallConnection connection, double gapDistance);

    /// <summary>
    /// Validates if the adjustment operation can be performed
    /// </summary>
    /// <param name="model">The wall adjustment model to validate</param>
    /// <returns>True if adjustment is possible</returns>
    bool CanAdjustWalls(WallAdjustmentModel model);
}