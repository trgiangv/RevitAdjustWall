namespace RevitAdjustWall.Models;

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
    TriShape,
    
    /// <summary>
    /// No connection type detected
    /// </summary>
    None
}