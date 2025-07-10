using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAdjustWall.Services;

/// <summary>
/// Interface for wall selection operations
/// Follows the Interface Segregation Principle (ISP)
/// </summary>
public interface IWallSelectionService
{
    /// <summary>
    /// Allows user to pick walls from the active view
    /// </summary>
    /// <param name="uiDocument">The UI document for user interaction</param>
    /// <returns>List of selected walls</returns>
    List<Wall> PickWalls(UIDocument uiDocument);

    /// <summary>
    /// Gets all walls in the active view
    /// </summary>
    /// <param name="document">The Revit document</param>
    /// <param name="view">The active view</param>
    /// <returns>List of walls in the view</returns>
    List<Wall> GetWallsInView(Document document, View view);

    /// <summary>
    /// Validates if the selected walls are suitable for adjustment
    /// </summary>
    /// <param name="walls">The walls to validate</param>
    /// <returns>True if walls are valid for adjustment</returns>
    bool ValidateWallSelection(List<Wall> walls);
}