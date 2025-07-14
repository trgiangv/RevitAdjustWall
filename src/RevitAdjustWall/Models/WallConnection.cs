using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using RevitAdjustWall.Extensions;
using RevitAdjustWall.Services.ConnectionHandlers;

namespace RevitAdjustWall.Models;

/// <summary>
/// Represents a wall connection point with its type and related walls
/// </summary>
public class WallConnection
{
    public const int MinWallsForConnection = 2;
    public const int MaxWallsForConnection = 3;

    /// <summary>
    /// Gets or sets the type of wall connection
    /// </summary>
    public WallConnectionType ConnectionType { get; set; }
    
    /// <summary>
    /// Gets or sets the connection handler for this connection
    /// </summary>
    public IConnectionHandler? ConnectionHandler { get; set; }

    /// <summary>
    /// Gets or sets the walls involved in this connection
    /// </summary>
    public List<Wall> ConnectedWalls { get; set; } = [];

    /// <summary>
    /// Gets or sets the connection point in 3D space
    /// </summary>
    public XYZ? ConnectionPoint { get; set; }
    
    /// <summary>
    /// Checks if the wall connection is valid for adjustment
    /// </summary>
    /// <returns>True if the connection is valid</returns>
    public bool IsValid()
    {
        var number = ConnectedWalls.Count is >= MinWallsForConnection and <= MaxWallsForConnection;
        var areLines = ConnectedWalls.All(w => w.Location is LocationCurve { Curve: Line });
        var hasConnectionType = ConnectionType != WallConnectionType.None;
        Debug.WriteLine($"WallConnection.IsValid: {number} && {areLines} && {hasConnectionType}");

        return number && areLines && hasConnectionType;
    }

    public void ApplyAdjustments(double gapDistance)
    {
        var wallExtendData =
            ConnectionHandler!.CalculateAdjustment(ConnectedWalls, ConnectionPoint!, ConnectionType, gapDistance);
        
        foreach (var wallExtend in wallExtendData)
        {
            Trace.TraceInformation(wallExtend.Key.Name);
            wallExtend.Key.SetLocation(wallExtend.Value);
        }
    }
}