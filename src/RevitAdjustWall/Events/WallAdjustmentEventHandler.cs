using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAdjustWall.Models;
using RevitAdjustWall.Services;

namespace RevitAdjustWall.Events;

/// <summary>
/// External event handler for wall adjustment operations
/// Implements IExternalEventHandler to handle UI-initiated transactions safely
/// </summary>
public class WallAdjustmentEventHandler : IExternalEventHandler
{
    private readonly IWallAdjustmentService _wallAdjustmentService;
    private WallAdjustmentModel _model;
    private Action<bool, string> _callback;

    /// <summary>
    /// Initializes a new instance of the WallAdjustmentEventHandler
    /// </summary>
    /// <param name="wallAdjustmentService">The wall adjustment service</param>
    public WallAdjustmentEventHandler(IWallAdjustmentService wallAdjustmentService)
    {
        _wallAdjustmentService = wallAdjustmentService ?? throw new ArgumentNullException(nameof(wallAdjustmentService));
    }

    /// <summary>
    /// Sets the data for the next execution
    /// </summary>
    /// <param name="model">The wall adjustment model</param>
    /// <param name="callback">Callback to invoke with the result</param>
    public void SetData(WallAdjustmentModel model, Action<bool, string> callback)
    {
        _model = model;
        _callback = callback;
    }

    /// <summary>
    /// Executes the wall adjustment operation with proper transaction management
    /// </summary>
    /// <param name="app">The Revit application</param>
    public void Execute(UIApplication app)
    {
        try
        {
            if (_model == null)
            {
                _callback?.Invoke(false, "No model data provided for wall adjustment.");
                return;
            }

            if (!_wallAdjustmentService.CanAdjustWalls(_model))
            {
                var debugInfo = $"CanAdjustWalls failed: Model={_model != null}, IsValid={_model?.IsValid()}, " +
                                $"GapMm={_model?.GapDistanceMm}, WallCount={_model?.SelectedWalls?.Count}, " +
                                $"Document={_model?.Document != null}";
                System.Diagnostics.Debug.WriteLine(debugInfo);
                _callback?.Invoke(false, $"Cannot adjust walls. Please check your selection and gap distance. Debug: {debugInfo}");
                return;
            }

            // Create transaction for wall adjustment operations
            using (var transaction = new Transaction(_model.Document, "Adjust Wall Gaps"))
            {
                try
                {
                    transaction.Start();

                    System.Diagnostics.Debug.WriteLine("Starting wall adjustment transaction");

                    bool success = _wallAdjustmentService.AdjustWalls(_model);

                    if (success)
                    {
                        transaction.Commit();
                        System.Diagnostics.Debug.WriteLine("Wall adjustment transaction committed successfully");
                        _callback?.Invoke(true, "Walls adjusted successfully.");
                    }
                    else
                    {
                        transaction.RollBack();
                        System.Diagnostics.Debug.WriteLine("Wall adjustment failed, transaction rolled back");
                        _callback?.Invoke(false, "Failed to adjust walls.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception during wall adjustment: {ex.Message}");

                    // Ensure transaction is rolled back on error
                    if (transaction.GetStatus() == TransactionStatus.Started)
                    {
                        transaction.RollBack();
                        System.Diagnostics.Debug.WriteLine("Transaction rolled back due to exception");
                    }

                    _callback?.Invoke(false, $"Error adjusting walls: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Outer exception in wall adjustment handler: {ex.Message}");
            _callback?.Invoke(false, $"Unexpected error during wall adjustment: {ex.Message}");
        }
        finally
        {
            // Clear the data after execution
            _model = null;
            _callback = null;
        }
    }

    /// <summary>
    /// Gets the name of the external event
    /// </summary>
    public string GetName()
    {
        return "Wall Adjustment Event Handler";
    }
}