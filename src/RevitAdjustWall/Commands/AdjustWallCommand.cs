using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAdjustWall.Services;
using RevitAdjustWall.ViewModels;
using RevitAdjustWall.Views;

namespace RevitAdjustWall.Commands
{
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
                // Validate input parameters
                if (commandData?.Application?.ActiveUIDocument == null)
                {
                    message = "No active document found.";
                    return Result.Failed;
                }

                var uiDocument = commandData.Application.ActiveUIDocument;
                var document = uiDocument.Document;

                // Validate that we have a valid document
                if (document == null)
                {
                    message = "No active Revit document found.";
                    return Result.Failed;
                }

                // Check if the document is read-only
                if (document.IsReadOnly)
                {
                    message = "Cannot modify a read-only document.";
                    return Result.Failed;
                }

                // Validate that we have an active view
                if (uiDocument.ActiveView == null)
                {
                    message = "No active view found.";
                    return Result.Failed;
                }

                // Create services using Dependency Injection pattern
                var wallSelectionService = new WallSelectionService();
                var wallAdjustmentService = new WallAdjustmentService();

                // Create ViewModel with dependencies
                var viewModel = new WallAdjustmentViewModel(
                    uiDocument, 
                    wallSelectionService, 
                    wallAdjustmentService);

                // Create and show the WPF dialog
                var view = new WallAdjustmentView(viewModel);
                
                // Set the owner to the Revit main window for proper modal behavior
                var revitWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (revitWindow != IntPtr.Zero)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(view);
                    helper.Owner = revitWindow;
                }

                // Show the dialog modally
                var dialogResult = view.ShowDialog();

                // Return success regardless of dialog result
                // The actual operation results are handled within the ViewModel
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // User cancelled the operation
                message = "Operation was cancelled by user.";
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                // Log the error and return failure
                message = $"An error occurred: {ex.Message}";
                
                // In a production environment, you might want to log this to a file
                System.Diagnostics.Debug.WriteLine($"AdjustWallCommand Error: {ex}");
                
                return Result.Failed;
            }
        }

        /// <summary>
        /// Gets the command name for display purposes
        /// </summary>
        public static string GetCommandName()
        {
            return "Adjust Wall";
        }

        /// <summary>
        /// Gets the command description
        /// </summary>
        public static string GetCommandDescription()
        {
            return "Adjusts wall gaps based on specified distance and connection rules";
        }

        /// <summary>
        /// Gets the command tooltip
        /// </summary>
        public static string GetCommandTooltip()
        {
            return "Click to open the Wall Adjustment tool for modifying wall gaps";
        }
    }
}
