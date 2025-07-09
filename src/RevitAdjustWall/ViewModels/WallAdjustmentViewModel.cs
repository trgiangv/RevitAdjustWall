using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAdjustWall.Models;
using RevitAdjustWall.Services;
using RevitAdjustWall.Validation;
using RevitAdjustWall.Exceptions;

namespace RevitAdjustWall.ViewModels
{
    /// <summary>
    /// ViewModel for the Wall Adjustment functionality
    /// Implements MVVM pattern and follows SOLID principles
    /// </summary>
    public class WallAdjustmentViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly IWallSelectionService _wallSelectionService;
        private readonly IWallAdjustmentService _wallAdjustmentService;
        private readonly UIDocument _uiDocument;
        private readonly WallAdjustmentModel _model;

        private string _gapDistanceText;
        private string _statusMessage;
        private string _validationMessage;
        private bool _isProcessing;
        private ObservableCollection<string> _selectedWallsInfo;

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
        public bool CanAdjustWalls => 
            IsValidGapDistance && 
            _model.SelectedWalls.Count > 0 && 
            CanExecuteCommands;

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
        public WallAdjustmentViewModel(
            UIDocument uiDocument,
            IWallSelectionService wallSelectionService = null,
            IWallAdjustmentService wallAdjustmentService = null)
        {
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _wallSelectionService = wallSelectionService ?? new WallSelectionService();
            _wallAdjustmentService = wallAdjustmentService ?? new WallAdjustmentService();

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

        #region Command Implementations

        private void ExecutePickWall()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Please select walls...";

                var selectedWalls = _wallSelectionService.PickWalls(_uiDocument);
                
                if (selectedWalls.Count > 0)
                {
                    _model.SelectedWalls.Clear();
                    _model.SelectedWalls.AddRange(selectedWalls);
                    
                    UpdateSelectedWallsInfo();
                    StatusMessage = $"Selected {selectedWalls.Count} wall(s)";
                }
                else
                {
                    StatusMessage = "No walls selected";
                }

                OnPropertyChanged(nameof(CanAdjustWalls));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error selecting walls: {ex.Message}";
                MessageBox.Show($"Error selecting walls: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteActiveView()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Getting walls from active view...";

                var wallsInView = _wallSelectionService.GetWallsInView(_model.Document, _model.ActiveView);
                
                if (wallsInView.Count > 0)
                {
                    _model.SelectedWalls.Clear();
                    _model.SelectedWalls.AddRange(wallsInView);
                    
                    UpdateSelectedWallsInfo();
                    StatusMessage = $"Found {wallsInView.Count} wall(s) in active view";
                }
                else
                {
                    StatusMessage = "No walls found in active view";
                }

                OnPropertyChanged(nameof(CanAdjustWalls));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error getting walls from view: {ex.Message}";
                MessageBox.Show($"Error getting walls from view: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteAdjustWalls()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Adjusting walls...";

                if (_wallAdjustmentService.AdjustWalls(_model))
                {
                    StatusMessage = "Walls adjusted successfully";
                    MessageBox.Show("Walls have been adjusted successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "Failed to adjust walls";
                    MessageBox.Show("Failed to adjust walls. Please check your selection and try again.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adjusting walls: {ex.Message}";
                MessageBox.Show($"Error adjusting walls: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
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
        }

        #endregion
    }
}
