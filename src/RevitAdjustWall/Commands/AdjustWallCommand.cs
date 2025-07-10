using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAdjustWall.Services;
using RevitAdjustWall.ViewModels;
using RevitAdjustWall.Views;

namespace RevitAdjustWall.Commands;

/// <summary>
/// Main command class that implements IExternalCommand
/// Entry point for the Revit add-in
/// Follows the Command pattern and integrates all components
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class AdjustWallCommand : IExternalCommand
{
    /// <summary>
    /// Executes the wall adjustment command
    /// </summary>
    /// <param name="commandData">Command data from Revit</param>
    /// <param name="message">Message to return to Revit</param>
    /// <param name="elements">Elements to highlight in case of error</param>
    /// <returns>Result of the command execution</returns>
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            // Create dialog service for consistent error handling
            var dialogService = new RevitDialogService();

            // Validate input parameters
            if (commandData?.Application?.ActiveUIDocument == null)
            {
                dialogService.ShowError("Initialization Error", "No active document found.",
                    "Please ensure you have an active Revit document open before running this command.");
                message = "No active document found.";
                return Result.Failed;
            }

            var uiDocument = commandData.Application.ActiveUIDocument;
            var document = uiDocument.Document;

            // Validate that we have a valid document
            if (document == null)
            {
                dialogService.ShowError("Document Error", "No active Revit document found.",
                    "The active UI document does not contain a valid Revit document.");
                message = "No active Revit document found.";
                return Result.Failed;
            }

            // Check if the document is read-only
            if (document.IsReadOnly)
            {
                dialogService.ShowError("Document Access Error", "Cannot modify a read-only document.",
                    "The current document is read-only. Please open a document that allows modifications.");
                message = "Cannot modify a read-only document.";
                return Result.Failed;
            }

            // Validate that we have an active view
            if (uiDocument.ActiveView == null)
            {
                dialogService.ShowError("View Error", "No active view found.",
                    "Please ensure you have an active view in Revit before running this command.");
                message = "No active view found.";
                return Result.Failed;
            }

            // Create services using Dependency Injection pattern
            var wallSelectionService = new WallSelectionService();
            var wallAdjustmentService = new WallAdjustmentService();
            var externalEventService = new ExternalEventService(wallAdjustmentService);

            // Create ViewModel with dependencies
            var viewModel = new WallAdjustmentViewModel(
                uiDocument,
                wallSelectionService,
                wallAdjustmentService,
                externalEventService,
                dialogService);

            // Create and show the WPF dialog
            var view = new WallAdjustmentView(viewModel);
                
            // Set the owner to the Revit main window for proper modal behavior
            var revitWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (revitWindow != IntPtr.Zero)
            {
                _ = new System.Windows.Interop.WindowInteropHelper(view)
                {
                    Owner = revitWindow
                };
            }

            // Show the dialog modally
            view.ShowDialog();

            // Return success regardless of dialog result
            // The actual operation results are handled within the ViewModel
            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            // User cancelled the operation - no need to show error dialog
            message = "Operation was cancelled by user.";
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            // Show error dialog for unexpected exceptions
            var dialogService = new RevitDialogService();
            dialogService.ShowError("Unexpected Error",
                "An unexpected error occurred while initializing the Wall Adjustment Tool.",
                $"Error details: {ex.Message}\n\nStack trace: {ex.StackTrace}");

            message = $"An error occurred: {ex.Message}";

            // In a production environment, you might want to log this to a file
            System.Diagnostics.Debug.WriteLine($"AdjustWallCommand Error: {ex}");

            return Result.Failed;
        }
    }
}