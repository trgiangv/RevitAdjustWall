using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitAdjustWall.Models
{
    /// <summary>
    /// Represents the data model for wall adjustment operations
    /// </summary>
    public class WallAdjustmentModel
    {
        /// <summary>
        /// Gets or sets the gap distance in millimeters
        /// </summary>
        public double GapDistanceMm { get; set; }

        /// <summary>
        /// Gets or sets the selected walls for adjustment
        /// </summary>
        public List<Wall> SelectedWalls { get; set; }

        /// <summary>
        /// Gets or sets the active view where operations will be performed
        /// </summary>
        public View ActiveView { get; set; }

        /// <summary>
        /// Gets or sets the Revit document
        /// </summary>
        public Document Document { get; set; }

        /// <summary>
        /// Initializes a new instance of the WallAdjustmentModel class
        /// </summary>
        public WallAdjustmentModel()
        {
            SelectedWalls = new List<Wall>();
            GapDistanceMm = 0.0;
        }

        /// <summary>
        /// Validates the model data
        /// </summary>
        /// <returns>True if the model is valid, false otherwise</returns>
        public bool IsValid()
        {
            return GapDistanceMm > 0 && 
                   SelectedWalls != null && 
                   SelectedWalls.Count > 0 && 
                   Document != null;
        }

        /// <summary>
        /// Converts millimeters to Revit internal units (feet)
        /// </summary>
        /// <returns>Gap distance in feet</returns>
        public double GetGapDistanceInFeet()
        {
            return UnitUtils.ConvertToInternalUnits(GapDistanceMm, UnitTypeId.Millimeters);
        }
    }
}
