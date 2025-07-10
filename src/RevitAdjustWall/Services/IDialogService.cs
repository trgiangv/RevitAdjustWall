using Autodesk.Revit.UI;

namespace RevitAdjustWall.Services;

/// <summary>
/// Interface for dialog services to handle user notifications
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an error dialog with the specified message
    /// </summary>
    /// <param name="title">The dialog title</param>
    /// <param name="message">The error message</param>
    /// <param name="detailedMessage">Optional detailed error information</param>
    /// <returns>The task dialog result</returns>
    TaskDialogResult ShowError(string title, string message, string detailedMessage = null);

    /// <summary>
    /// Shows a warning dialog with the specified message
    /// </summary>
    /// <param name="title">The dialog title</param>
    /// <param name="message">The warning message</param>
    /// <param name="detailedMessage">Optional detailed warning information</param>
    /// <returns>The task dialog result</returns>
    TaskDialogResult ShowWarning(string title, string message, string detailedMessage = null);

    /// <summary>
    /// Shows an information dialog with the specified message
    /// </summary>
    /// <param name="title">The dialog title</param>
    /// <param name="message">The information message</param>
    /// <param name="detailedMessage">Optional detailed information</param>
    /// <returns>The task dialog result</returns>
    TaskDialogResult ShowInformation(string title, string message, string detailedMessage = null);

    /// <summary>
    /// Shows a confirmation dialog with Yes/No options
    /// </summary>
    /// <param name="title">The dialog title</param>
    /// <param name="message">The confirmation message</param>
    /// <param name="detailedMessage">Optional detailed information</param>
    /// <returns>The task dialog result</returns>
    TaskDialogResult ShowConfirmation(string title, string message, string detailedMessage = null);
}