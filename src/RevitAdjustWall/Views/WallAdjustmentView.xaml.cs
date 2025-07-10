using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using RevitAdjustWall.ViewModels;

namespace RevitAdjustWall.Views;

/// <summary>
/// Interaction logic for WallAdjustmentView.xaml
/// Code-behind follows MVVM pattern with minimal logic
/// </summary>
public partial class WallAdjustmentView : Window
{
    private static readonly Regex NumericRegex = new Regex(@"^[0-9]*\.?[0-9]*$");

    /// <summary>
    /// Initializes a new instance of WallAdjustmentView
    /// </summary>
    public WallAdjustmentView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of WallAdjustmentView with a specific ViewModel
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind to this view</param>
    public WallAdjustmentView(WallAdjustmentViewModel viewModel) : this()
    {
        DataContext = viewModel;

        // Set this window as the parent window for the ViewModel
        if (viewModel != null)
        {
            viewModel.SetParentWindow(this);
        }

        // Ensure proper cleanup when window closes
        Closed += WallAdjustmentView_Closed;
    }

    /// <summary>
    /// Handles the window closed event to ensure proper resource cleanup
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event arguments</param>
    private void WallAdjustmentView_Closed(object sender, EventArgs e)
    {
        // Dispose of the ViewModel if it implements IDisposable
        if (DataContext is IDisposable disposableViewModel)
        {
            disposableViewModel.Dispose();
        }

        // Clear the DataContext to help with garbage collection
        DataContext = null;
    }

    /// <summary>
    /// Handles text input validation for numeric-only input
    /// Ensures only numeric values can be entered in the gap distance textbox
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Text composition event arguments</param>
    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Allow only numeric input (including decimal point)
        if (!NumericRegex.IsMatch(e.Text))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles key down events for the numeric textbox
    /// Prevents pasting of non-numeric content
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Key event arguments</param>
    private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Block paste operation (Ctrl+V)
        if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles the window closing event
    /// Provides opportunity for cleanup if needed
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Cancel event arguments</param>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Any cleanup logic can be added here if needed
        // Currently, no special cleanup is required
    }
}