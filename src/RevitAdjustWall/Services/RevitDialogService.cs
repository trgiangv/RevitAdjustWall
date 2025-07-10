using Autodesk.Revit.UI;

namespace RevitAdjustWall.Services;

/// <summary>
/// Implementation of dialog service using Revit TaskDialog
/// Provides consistent user notifications throughout the application
/// </summary>
public class RevitDialogService : IDialogService
{
    private const string DefaultTitle = "Revit Adjust Wall";

    /// <summary>
    /// Shows an error dialog with the specified message
    /// </summary>
    /// <param name="title">The dialog title</param>
    /// <param name="message">The error message</param>
    /// <param name="detailedMessage">Optional detailed error information</param>
    /// <returns>The task dialog result</returns>
    public TaskDialogResult ShowError(string title, string message, string detailedMessage = null)
    {
        var dialog = new TaskDialog(title ?? DefaultTitle)
        {
            MainIcon = TaskDialogIcon.TaskDialogIconError,
            MainInstruction = "Error",
            MainContent = message,
            CommonButtons = TaskDialogCommonButtons.Ok
        };

        if (!string.IsNullOrEmpty(detailedMessage))
        {
            dialog.ExpandedContent = detailedMessage;
            dialog.FooterText = "Click 'Show details' for more information.";
        }

        return dialog.Show();
    }

    /// <summary>
    /// Shows a warning dialog with the specified message
    /// </summary>
    /// <param name="title">The dialog title</param>
    /// <param name="message">The warning message</param>
    /// <param name="detailedMessage">Optional detailed warning information</param>
    /// <returns>The task dialog result</returns>
    public TaskDialogResult ShowWarning(string title, string message, string detailedMessage = null)
    {
        var dialog = new TaskDialog(title ?? DefaultTitle)
        {
            MainIcon = TaskDialogIcon.TaskDialogIconWarning,
            MainInstruction = "Warning",
            MainContent = message,
            CommonButtons = TaskDialogCommonButtons.Ok
        };

        if (!string.IsNullOrEmpty(detailedMessage))
        {
            dialog.ExpandedContent = detailedMessage;
            dialog.FooterText = "Click 'Show details' for more information.";
        }

        return dialog.Show();
    }

    /// <summary>
    /// Shows an information dialog with the specified message
    /// </summary>
    /// <param name="title">The dialog title</param>
    /// <param name="message">The information message</param>
    /// <param name="detailedMessage">Optional detailed information</param>
    /// <returns>The task dialog result</returns>
    public TaskDialogResult ShowInformation(string title, string message, string detailedMessage = null)
    {
        var dialog = new TaskDialog(title ?? DefaultTitle)
        {
            MainIcon = TaskDialogIcon.TaskDialogIconInformation,
            MainInstruction = "Information",
            MainContent = message,
            CommonButtons = TaskDialogCommonButtons.Ok
        };

        if (!string.IsNullOrEmpty(detailedMessage))
        {
            dialog.ExpandedContent = detailedMessage;
            dialog.FooterText = "Click 'Show details' for more information.";
        }

        return dialog.Show();
    }

    /// <summary>
    /// Shows a confirmation dialog with Yes/No options
    /// </summary>
    /// <param name="title">The dialog title</param>
    /// <param name="message">The confirmation message</param>
    /// <param name="detailedMessage">Optional detailed information</param>
    /// <returns>The task dialog result</returns>
    public TaskDialogResult ShowConfirmation(string title, string message, string detailedMessage = null)
    {
        var dialog = new TaskDialog(title ?? DefaultTitle)
        {
            MainIcon = TaskDialogIcon.TaskDialogIconNone,
            MainInstruction = "Confirmation",
            MainContent = message,
            CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
            DefaultButton = TaskDialogResult.No
        };

        if (!string.IsNullOrEmpty(detailedMessage))
        {
            dialog.ExpandedContent = detailedMessage;
            dialog.FooterText = "Click 'Show details' for more information.";
        }

        return dialog.Show();
    }
}