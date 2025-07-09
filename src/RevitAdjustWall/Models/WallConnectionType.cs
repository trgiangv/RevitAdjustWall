using System.Collections.Generic;

namespace RevitAdjustWall.Models
{
    /// <summary>
    /// Enumeration representing different types of wall connections
    /// Based on the business logic requirements
    /// </summary>
    public enum WallConnectionType
    {
        /// <summary>
        /// Two walls connected end-to-end in a straight line, forming a continuous wall
        /// </summary>
        Inline,

        /// <summary>
        /// Two walls meet at a corner, forming a 90-degree angle
        /// </summary>
        Corner,

        /// <summary>
        /// Two walls where one wall connects perpendicularly to the middle of another,
        /// forming a "T" or cross junction
        /// </summary>
        TShape,

        /// <summary>
        /// Three walls connected at a single point: two walls are aligned inline,
        /// and the third wall connects perpendicularly at the joint between the other two
        /// </summary>
        TriShape
    }

    /// <summary>
    /// Represents a wall connection point with its type and related walls
    /// </summary>
    public class WallConnection
    {
        /// <summary>
        /// Gets or sets the type of wall connection
        /// </summary>
        public WallConnectionType ConnectionType { get; set; }

        /// <summary>
        /// Gets or sets the walls involved in this connection
        /// </summary>
        public List<Autodesk.Revit.DB.Wall> ConnectedWalls { get; set; }

        /// <summary>
        /// Gets or sets the connection point in 3D space
        /// </summary>
        public Autodesk.Revit.DB.XYZ ConnectionPoint { get; set; }

        /// <summary>
        /// Initializes a new instance of the WallConnection class
        /// </summary>
        public WallConnection()
        {
            ConnectedWalls = new List<Autodesk.Revit.DB.Wall>();
        }
    }
}
