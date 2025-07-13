using System.Text.RegularExpressions;
using System.Windows.Input;
using RevitAdjustWall.ViewModels;

namespace RevitAdjustWall.Views;

/// <summary>
/// Interaction logic for WallAdjustmentView.xaml
/// Code-behind follows MVVM pattern with minimal logic
/// </summary>
public partial class WallAdjustmentView
{
    private static readonly Regex NumericRegex = new Regex(@"^[0-9]*\.?[0-9]*$");

    /// <summary>
    /// Initializes a new instance of WallAdjustmentView with a specific ViewModel
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind to this view</param>
    public WallAdjustmentView(WallAdjustmentViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
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
        if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            e.Handled = true;
        }
    }
}