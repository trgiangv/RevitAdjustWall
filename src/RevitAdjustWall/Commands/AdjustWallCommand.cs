using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAdjustWall.ViewModels;
using RevitAdjustWall.Views;

namespace RevitAdjustWall.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class AdjustWallCommand : IExternalCommand
{
    public static UIApplication Uiapp = null!;

    /// <summary>
    /// Static reference to active UIDocument
    /// </summary>
    public static UIDocument? Uidoc => Uiapp.ActiveUIDocument;

    /// <summary>
    /// Static reference to active Document
    /// </summary>
    public static Document? Doc => Uidoc?.Document;
    
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            Uiapp = commandData.Application;

            var viewModel = new WallAdjustmentViewModel();
            var view = new WallAdjustmentView(viewModel)
            {
                Owner = UIFramework.MainWindow.getMainWnd()
            };
            
            view.Show();

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AdjustWallCommand Error: {ex}");
            message = $"An error occurred: {ex.Message}";
            return Result.Failed;
        }
    }
}