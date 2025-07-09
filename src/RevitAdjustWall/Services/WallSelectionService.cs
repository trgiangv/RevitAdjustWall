using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitAdjustWall.Services
{
    /// <summary>
    /// Implementation of wall selection service
    /// Follows Single Responsibility Principle (SRP)
    /// </summary>
    public class WallSelectionService : IWallSelectionService
    {
        /// <summary>
        /// Allows user to pick walls from the active view
        /// </summary>
        /// <param name="uiDocument">The UI document for user interaction</param>
        /// <returns>List of selected walls</returns>
        public List<Wall> PickWalls(UIDocument uiDocument)
        {
            try
            {
                var selectedWalls = new List<Wall>();
                var selection = uiDocument.Selection;

                // Create a wall filter for selection
                var wallFilter = new WallSelectionFilter();

                // Allow user to pick multiple walls
                var pickedReferences = selection.PickObjects(ObjectType.Element, wallFilter, "Select walls to adjust");

                foreach (var reference in pickedReferences)
                {
                    var element = uiDocument.Document.GetElement(reference);
                    if (element is Wall wall)
                    {
                        selectedWalls.Add(wall);
                    }
                }

                return selectedWalls;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // User cancelled the selection
                return new List<Wall>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error during wall selection: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets all walls in the active view
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <param name="view">The active view</param>
        /// <returns>List of walls in the view</returns>
        public List<Wall> GetWallsInView(Document document, View view)
        {
            try
            {
                var collector = new FilteredElementCollector(document, view.Id)
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType();

                return collector.Cast<Wall>().ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving walls from view: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates if the selected walls are suitable for adjustment
        /// </summary>
        /// <param name="walls">The walls to validate</param>
        /// <returns>True if walls are valid for adjustment</returns>
        public bool ValidateWallSelection(List<Wall> walls)
        {
            if (walls == null || walls.Count == 0)
                return false;

            // Check if all walls are valid and not null
            return walls.All(wall => wall != null && wall.IsValidObject);
        }
    }

    /// <summary>
    /// Selection filter to allow only wall elements
    /// </summary>
    public class WallSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Wall;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
