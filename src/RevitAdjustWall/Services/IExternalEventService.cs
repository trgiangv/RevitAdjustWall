using System;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services;

/// <summary>
/// Interface for managing external events for document modification operations
/// Only operations that modify the Revit document should use external events
/// </summary>
public interface IExternalEventService
{
    /// <summary>
    /// Executes wall adjustment operation using external event
    /// This requires external event because it modifies the document
    /// </summary>
    /// <param name="model">The wall adjustment model</param>
    /// <param name="callback">Callback to invoke with the result</param>
    void ExecuteWallAdjustment(WallAdjustmentModel model, Action<bool, string> callback);

    /// <summary>
    /// Disposes of external events and resources
    /// </summary>
    void Dispose();
}