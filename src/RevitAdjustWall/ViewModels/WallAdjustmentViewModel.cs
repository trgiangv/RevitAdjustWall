using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAdjustWall.Commands;
using RevitAdjustWall.Extensions;
using RevitAdjustWall.Models;
using RevitAdjustWall.Services;
using RevitAdjustWall.Validation;

namespace RevitAdjustWall.ViewModels;

/// <summary>
/// ViewModel for the Wall Adjustment functionality
/// </summary>
public class WallAdjustmentViewModel : BaseViewModel
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly WallSelectionFilter _selectionFilter = new();

    private string _gapDistanceText;
    private double _gapDistance;
    private string _validationErrorMessage;
    
    public string GapDistanceText
    {
        get => _gapDistanceText;
        set
        {
            if (!SetField(ref _gapDistanceText, value)) return;
            ValidateAndUpdateGapDistance();
            OnPropertyChanged(nameof(IsValidGapDistance));
        }
    }
    
    public double GapDistance
    {
        get => _gapDistance;
        set => SetField(ref _gapDistance, value);
    }
    
    public bool IsValidGapDistance =>
        InputValidator.TryParseGapDistance(_gapDistanceText, out _);

    /// <summary>
    /// Gets the validation error message for the gap distance input
    /// </summary>
    public string ValidationErrorMessage
    {
        get => _validationErrorMessage;
        private set => SetField(ref _validationErrorMessage, value);
    }

    public ICommand PickWallsCommand { get; }
    

    public WallAdjustmentViewModel()
    {
        _connectionFactory = new ConnectionFactory();

        _gapDistanceText = "20";
        _gapDistance = 20.0.FromMillimeters();
        _validationErrorMessage = string.Empty;

        PickWallsCommand = new RelayCommand(ExecutePickWall, CanExecutePickWall);
        // ValidateAndUpdateGapDistance();
    }

    /// <summary>
    /// Determines if the PickWalls command can be executed
    /// </summary>
    /// <returns>True if the gap distance is valid</returns>
    private bool CanExecutePickWall()
    {
        return IsValidGapDistance;
    }

    /// <summary>
    /// Refreshes the command states by notifying property changes
    /// </summary>
    private void RefreshCommandStates()
    {
        OnPropertyChanged(nameof(IsValidGapDistance));
        if (PickWallsCommand is RelayCommand relayCommand)
        {
            relayCommand.RaiseCanExecuteChanged();
        }
    }
    
    /// <summary>
    /// Executes the wall picking command with improved selection workflow
    /// Supports rectangle selection and continuous prompting until completion
    /// </summary>
    private void ExecutePickWall()
    {
        try
        {
            var elements = AdjustWallCommand.Uidoc!.Selection.PickElementsByRectangle(
                _selectionFilter, "Select walls (or press Esc to finish)");
            
            AdjustWallCommand.ExternalEventHandler?.Raise(uiapp =>
            {
                var walls = elements.Cast<Wall>().ToList();
                var connection = _connectionFactory.AnalyzeConnection(walls);
                Trace.TraceInformation(connection.ConnectionType.ToString());

                if (!connection.IsValid())
                {
                    TaskDialog.Show("Error", "Invalid wall connection. Cannot proceed with adjustment.");
                    return;
                }

                using var transaction = new Transaction(uiapp.ActiveUIDocument.Document, "Adjust Wall Gaps");
                transaction.Start();
                connection.ApplyAdjustments(GapDistance);
                transaction.Commit();
            });
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", "An error occurred: " + ex.Message);
        }
    }

    private void ValidateAndUpdateGapDistance()
    {
        var isValid = InputValidator.TryParseGapDistance(_gapDistanceText, out var valueInFeet);

        if (isValid)
        {
            GapDistance = valueInFeet;
            ValidationErrorMessage = string.Empty;
            OnPropertyChanged(nameof(GapDistance));
        }
        else
        {
            ValidationErrorMessage = InputValidator.GetGapDistanceValidationError(_gapDistanceText);
        }

        RefreshCommandStates();
    }
}