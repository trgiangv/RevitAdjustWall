using System;
using Autodesk.Revit.UI;
using RevitAdjustWall.Events;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services;

/// <summary>
/// Service for managing external events for document modification operations
/// Only operations that modify the Revit document should use external events
/// </summary>
public class ExternalEventService : IExternalEventService, IDisposable
{
    private readonly ExternalEvent _wallAdjustmentEvent;
    private readonly WallAdjustmentEventHandler _wallAdjustmentHandler;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the ExternalEventService
    /// </summary>
    /// <param name="wallAdjustmentService">The wall adjustment service</param>
    public ExternalEventService(IWallAdjustmentService wallAdjustmentService)
    {
        if (wallAdjustmentService == null)
            throw new ArgumentNullException(nameof(wallAdjustmentService));

        // Create event handler for document modification operations
        _wallAdjustmentHandler = new WallAdjustmentEventHandler(wallAdjustmentService);

        // Create external event
        _wallAdjustmentEvent = ExternalEvent.Create(_wallAdjustmentHandler);
    }

    /// <summary>
    /// Executes wall adjustment operation using external event
    /// </summary>
    /// <param name="model">The wall adjustment model</param>
    /// <param name="callback">Callback to invoke with the result</param>
    public void ExecuteWallAdjustment(WallAdjustmentModel model, Action<bool, string> callback)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ExternalEventService));

        if (model == null)
            throw new ArgumentNullException(nameof(model));

        _wallAdjustmentHandler.SetData(model, callback);
        _wallAdjustmentEvent.Raise();
    }



    /// <summary>
    /// Disposes of external events and resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _wallAdjustmentEvent?.Dispose();
            _disposed = true;
        }
    }
}