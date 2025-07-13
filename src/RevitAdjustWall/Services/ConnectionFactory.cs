using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitAdjustWall.Models;
using RevitAdjustWall.Services.ConnectionHandlers;

namespace RevitAdjustWall.Services;

/// <summary>
/// Factory class for creating and managing connection handlers
/// Implements the Factory pattern to provide appropriate handlers for different connection types
/// </summary>
public class ConnectionFactory
{
    private readonly List<IConnectionHandler> _handlers;
    
    /// <summary>
    /// Initializes a new instance of the ConnectionHandlerFactory
    /// Creates all available connection handlers in priority order
    /// </summary>
    public ConnectionFactory()
    {
        _handlers =
        [
            new CornerConnectionHandler(),
            new TriShapeConnectionHandler(),
            new InlineConnectionHandler(),
            new TShapeConnectionHandler()
        ];
    }
    
    public WallConnection AnalyzeConnection(List<Wall> walls)
    {
        var connection = new WallConnection();
        XYZ? foundConnectionPoint = null;

        // Find the first handler that can handle the set of walls
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(walls, out foundConnectionPoint));
        connection.ConnectionHandler = handler;
        connection.ConnectionType = handler?.ConnectionType ?? WallConnectionType.None;
        connection.ConnectedWalls = walls;
        connection.ConnectionPoint = foundConnectionPoint;
        
        return connection;
    }
}
