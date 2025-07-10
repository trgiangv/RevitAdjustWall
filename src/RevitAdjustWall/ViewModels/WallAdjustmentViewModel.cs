using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.UI;
using RevitAdjustWall.Models;
using RevitAdjustWall.Services;
using RevitAdjustWall.Validation;

namespace RevitAdjustWall.ViewModels;

/// <summary>
/// ViewModel for the Wall Adjustment functionality
/// Implements MVVM pattern and follows SOLID principles
/// </summary>
public class WallAdjustmentViewModel : BaseViewModel, IDisposable
{
    #region Private Fields

    private readonly IWallSelectionService _wallSelectionService;
    private readonly IWallAdjustmentService _wallAdjustmentService;
    private readonly IExternalEventService _externalEventService;
    private readonly IDialogService _dialogService;
    private readonly UIDocument _uiDocument;
    private readonly WallAdjustmentModel _model;

    private string _gapDistanceText;
    private string _statusMessage;
    private string _validationMessage;
    private bool _isProcessing;
    private ObservableCollection<string> _selectedWallsInfo;
    private Window _parentWindow;
    private bool _disposed = false;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the gap distance text input
    /// </summary>
    public string GapDistanceText
    {
        get => _gapDistanceText;
        set
        {
            if (SetProperty(ref _gapDistanceText, value))
            {
                ValidateAndUpdateGapDistance();
                OnPropertyChanged(nameof(IsValidGapDistance));
            }
        }
    }

    /// <summary>
    /// Gets or sets the status message for user feedback
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets the validation message for input errors
    /// </summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>
    /// Gets or sets whether the application is currently processing
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                OnPropertyChanged(nameof(CanExecuteCommands));
            }
        }
    }

    /// <summary>
    /// Gets information about selected walls
    /// </summary>
    public ObservableCollection<string> SelectedWallsInfo
    {
        get => _selectedWallsInfo;
        set => SetProperty(ref _selectedWallsInfo, value);
    }

    /// <summary>
    /// Gets whether the gap distance is valid
    /// </summary>
    public bool IsValidGapDistance =>
        InputValidator.TryParseGapDistance(_gapDistanceText, out _);

    /// <summary>
    /// Gets whether commands can be executed
    /// </summary>
    public bool CanExecuteCommands => !IsProcessing;

    /// <summary>
    /// Gets whether the adjust operation can be performed
    /// </summary>
    public bool CanAdjustWalls
    {
        get
        {
            var isValidGap = IsValidGapDistance;
            var hasWalls = _model.SelectedWalls.Count > 0;
            var canExecute = CanExecuteCommands;
            var modelValid = _model.IsValid();

            // Debug logging to help identify issues
            System.Diagnostics.Debug.WriteLine($"CanAdjustWalls: IsValidGap={isValidGap}, HasWalls={hasWalls}, CanExecute={canExecute}, ModelValid={modelValid}, GapMm={_model.GapDistanceMm}");

            return isValidGap && hasWalls && canExecute;
        }
    }

    #endregion

    #region Commands

    public ICommand PickWallCommand { get; }
    public ICommand ActiveViewCommand { get; }
    public ICommand AdjustWallsCommand { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of WallAdjustmentViewModel
    /// </summary>
    /// <param name="uiDocument">The Revit UI document</param>
    /// <param name="wallSelectionService">Service for wall selection operations</param>
    /// <param name="wallAdjustmentService">Service for wall adjustment operations</param>
    /// <param name="externalEventService">Service for managing external events</param>
    /// <param name="dialogService">Service for user dialogs</param>
    public WallAdjustmentViewModel(
        UIDocument uiDocument,
        IWallSelectionService wallSelectionService = null,
        IWallAdjustmentService wallAdjustmentService = null,
        IExternalEventService externalEventService = null,
        IDialogService dialogService = null)
    {
        _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
        _wallSelectionService = wallSelectionService ?? new WallSelectionService();
        _wallAdjustmentService = wallAdjustmentService ?? new WallAdjustmentService();
        _externalEventService = externalEventService ?? throw new ArgumentNullException(nameof(externalEventService));
        _dialogService = dialogService ?? new RevitDialogService();

        _model = new WallAdjustmentModel
        {
            Document = _uiDocument.Document,
            ActiveView = _uiDocument.ActiveView
        };

        _selectedWallsInfo = new ObservableCollection<string>();
        _gapDistanceText = "10"; // Default 10mm
        _statusMessage = "Ready";

        // Initialize commands
        PickWallCommand = new RelayCommand(ExecutePickWall, () => CanExecuteCommands);
        ActiveViewCommand = new RelayCommand(ExecuteActiveView, () => CanExecuteCommands);
        AdjustWallsCommand = new RelayCommand(ExecuteAdjustWalls, () => CanAdjustWalls);

        ValidateAndUpdateGapDistance();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the parent window for window management during operations
    /// </summary>
    /// <param name="window">The parent window</param>
    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }

    /// <summary>
    /// Refreshes the command states by notifying property changes
    /// </summary>
    public void RefreshCommandStates()
    {
        OnPropertyChanged(nameof(CanAdjustWalls));
        OnPropertyChanged(nameof(CanExecuteCommands));
        OnPropertyChanged(nameof(IsValidGapDistance));
    }

    #endregion

    #region Command Implementations

    private void ExecutePickWall()
    {
        try
        {
            // Provide immediate UI feedback
            IsProcessing = true;
            StatusMessage = "Please select walls in Revit...";
            OnPropertyChanged(nameof(IsProcessing));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(CanExecuteCommands));

            // Hide the parent window to allow direct interaction with Revit model
            var wasVisible = false;
            if (_parentWindow != null)
            {
                wasVisible = _parentWindow.IsVisible;
                if (wasVisible)
                {
                    _parentWindow.Hide();
                }
            }

            try
            {
                // Call wall selection service directly (synchronous, no threading needed)
                var selectedWalls = _wallSelectionService.PickWalls(_uiDocument);
                var message = selectedWalls.Count > 0
                    ? $"Selected {selectedWalls.Count} wall(s)"
                    : "No walls selected";

                // Process results directly
                if (selectedWalls.Count > 0)
                {
                    _model.SelectedWalls.Clear();
                    _model.SelectedWalls.AddRange(selectedWalls);
                    UpdateSelectedWallsInfo();
                }

                StatusMessage = message;
                OnPropertyChanged(nameof(CanAdjustWalls));
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                StatusMessage = "Wall selection was cancelled by user.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during wall selection: {ex.Message}";
                _dialogService.ShowError("Wall Selection Error",
                    "An error occurred during wall selection.",
                    ex.Message);
            }
            finally
            {
                // Restore the parent window and UI state
                if (_parentWindow != null && wasVisible)
                {
                    _parentWindow.Show();
                    _parentWindow.Activate();
                }
                IsProcessing = false;
                OnPropertyChanged(nameof(IsProcessing));
                OnPropertyChanged(nameof(CanExecuteCommands));
            }

        }
        catch (Exception ex)
        {
            StatusMessage = $"Error initiating wall selection: {ex.Message}";
            _dialogService.ShowError("Wall Selection Error",
                "An error occurred while initiating wall selection.",
                ex.Message);
            IsProcessing = false;
            OnPropertyChanged(nameof(IsProcessing));
            OnPropertyChanged(nameof(CanExecuteCommands));
        }
    }

    private void ExecuteActiveView()
    {
        try
        {
            // Provide immediate UI feedback
            IsProcessing = true;
            StatusMessage = "Searching for walls in active view...";
            OnPropertyChanged(nameof(IsProcessing));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(CanExecuteCommands));

            try
            {
                // Call wall selection service directly (synchronous, no threading needed)
                var selectedWalls = _wallSelectionService.GetWallsInView(_uiDocument.Document, _uiDocument.ActiveView);
                var message = selectedWalls.Count > 0
                    ? $"Found {selectedWalls.Count} wall(s) in active view"
                    : "No walls found in active view";

                // Process results directly
                if (selectedWalls.Count > 0)
                {
                    _model.SelectedWalls.Clear();
                    _model.SelectedWalls.AddRange(selectedWalls);
                    UpdateSelectedWallsInfo();
                }

                StatusMessage = message;
                OnPropertyChanged(nameof(CanAdjustWalls));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error getting walls from view: {ex.Message}";
                _dialogService.ShowError("Active View Selection Error",
                    "An error occurred while getting walls from the active view.",
                    ex.Message);
            }
            finally
            {
                IsProcessing = false;
                OnPropertyChanged(nameof(IsProcessing));
                OnPropertyChanged(nameof(CanExecuteCommands));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error initiating active view analysis: {ex.Message}";
            _dialogService.ShowError("Active View Selection Error",
                "An error occurred while initiating active view analysis.",
                ex.Message);
            IsProcessing = false;
            OnPropertyChanged(nameof(IsProcessing));
            OnPropertyChanged(nameof(CanExecuteCommands));
        }
    }

    private void ExecuteAdjustWalls()
    {
        try
        {
            // Provide immediate UI feedback
            IsProcessing = true;
            StatusMessage = "Adjusting walls...";
            OnPropertyChanged(nameof(IsProcessing));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(CanExecuteCommands));

            // Use external event for wall adjustment to ensure proper transaction context
            _externalEventService.ExecuteWallAdjustment(_model, (success, message) =>
            {
                // This callback runs on the UI thread after the external event completes
                try
                {
                    StatusMessage = message;

                    if (success)
                    {
                        _dialogService.ShowInformation("Wall Adjustment Complete",
                            "Walls have been adjusted successfully!",
                            $"Gap distance: {_model.GapDistanceMm} mm\nWalls adjusted: {_model.SelectedWalls.Count}");
                    }
                    else
                    {
                        _dialogService.ShowWarning("Wall Adjustment Failed",
                            "Failed to adjust walls. Please check your selection and try again.",
                            message);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error processing adjustment result: {ex.Message}";
                    _dialogService.ShowError("Wall Adjustment Error",
                        "An error occurred while processing the adjustment result.",
                        ex.Message);
                }
                finally
                {
                    IsProcessing = false;
                    OnPropertyChanged(nameof(IsProcessing));
                    OnPropertyChanged(nameof(CanExecuteCommands));
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error initiating wall adjustment: {ex.Message}";
            _dialogService.ShowError("Wall Adjustment Error",
                "An error occurred while initiating wall adjustment.",
                ex.Message);
            IsProcessing = false;
        }
    }

    #endregion

    #region Private Methods

    private void ValidateAndUpdateGapDistance()
    {
        if (InputValidator.TryParseGapDistance(_gapDistanceText, out double value))
        {
            _model.GapDistanceMm = value;
            ValidationMessage = string.Empty;
        }
        else
        {
            ValidationMessage = InputValidator.GetGapDistanceValidationError(_gapDistanceText);
        }

        // Refresh command states after gap distance validation
        RefreshCommandStates();
    }

    private void UpdateSelectedWallsInfo()
    {
        SelectedWallsInfo.Clear();

        for (int i = 0; i < _model.SelectedWalls.Count; i++)
        {
            var wall = _model.SelectedWalls[i];
            var wallInfo = $"Wall {i + 1}: ID {wall.Id.Value} - {wall.WallType.Name}";
            SelectedWallsInfo.Add(wallInfo);
        }

        // Refresh command states after updating wall selection
        RefreshCommandStates();
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes of resources used by the ViewModel
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
            // Dispose of external event service
            _externalEventService?.Dispose();

            // Clear collections
            _selectedWallsInfo?.Clear();
            _model?.SelectedWalls?.Clear();

            _disposed = true;
        }
    }

    #endregion
}