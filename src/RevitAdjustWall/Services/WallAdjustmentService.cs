using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services;

/// <summary>
/// Implementation of wall adjustment service
/// Follows Single Responsibility Principle (SRP) and Open/Closed Principle (OCP)
/// </summary>
public class WallAdjustmentService : IWallAdjustmentService
{
    private static double TOLERANCE = UnitUtils.ConvertToInternalUnits(0.1, UnitTypeId.Millimeters); // 1cm tolerance for point comparison

    /// <summary>
    /// Analyzes wall connections to determine connection types
    /// </summary>
    /// <param name="walls">The walls to analyze</param>
    /// <returns>List of wall connections with their types</returns>
    public List<WallConnection> AnalyzeWallConnections(List<Wall> walls)
    {
        var connections = new List<WallConnection>();

        try
        {
            // Group walls by their endpoints to find connections
            var wallEndpoints = GetWallEndpoints(walls);
            var connectionPoints = FindConnectionPoints(wallEndpoints);

            foreach (var connectionPoint in connectionPoints)
            {
                var connectedWalls = GetWallsAtPoint(walls, connectionPoint);
                if (connectedWalls.Count >= 2)
                {
                    var connection = new WallConnection
                    {
                        ConnectionPoint = connectionPoint,
                        ConnectedWalls = connectedWalls,
                        ConnectionType = DetermineConnectionType(connectedWalls, connectionPoint)
                    };
                    connections.Add(connection);
                }
            }

            return connections;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error analyzing wall connections: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Adjusts walls based on the specified gap distance and connection rules
    /// Note: This method should only be called from within an IExternalEventHandler
    /// where proper transaction context is already established
    /// </summary>
    /// <param name="model">The wall adjustment model containing all necessary data</param>
    /// <returns>True if adjustment was successful</returns>
    public bool AdjustWalls(WallAdjustmentModel model)
    {
        if (!CanAdjustWalls(model))
            return false;

        try
        {
            var connections = AnalyzeWallConnections(model.SelectedWalls);
            var gapDistanceInFeet = model.GetGapDistanceInFeet();

            System.Diagnostics.Debug.WriteLine($"Found {connections.Count} wall connections to adjust");

            // Process each connection - transaction context is managed by the calling IExternalEventHandler
            foreach (var connection in connections)
            {
                try
                {
                    var newEndpoints = CalculateNewWallEndpoints(connection, gapDistanceInFeet);

                    if (newEndpoints.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Adjusting {newEndpoints.Count} walls for {connection.ConnectionType} connection");
                        ApplyWallAdjustments(newEndpoints);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adjusting {connection.ConnectionType} connection: {ex.Message}");
                    throw new InvalidOperationException($"Failed to adjust {connection.ConnectionType} connection: {ex.Message}", ex);
                }
            }

            // Regenerate the document to ensure all changes are properly applied
            model.Document.Regenerate();

            System.Diagnostics.Debug.WriteLine("Wall adjustment completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in wall adjustment: {ex.Message}");
            throw new InvalidOperationException($"Error adjusting walls: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Calculates the new wall endpoints based on gap distance and connection type
    /// </summary>
    /// <param name="connection">The wall connection to adjust</param>
    /// <param name="gapDistance">The gap distance in feet</param>
    /// <returns>Dictionary of walls and their new endpoints</returns>
    public Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)> CalculateNewWallEndpoints(
        WallConnection connection, double gapDistance)
    {
        var newEndpoints = new Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)>();

        switch (connection.ConnectionType)
        {
            case WallConnectionType.Inline:
                CalculateInlineAdjustment(connection, gapDistance, newEndpoints);
                break;
            case WallConnectionType.Corner:
                CalculateCornerAdjustment(connection, gapDistance, newEndpoints);
                break;
            case WallConnectionType.TShape:
                CalculateTShapeAdjustment(connection, gapDistance, newEndpoints);
                break;
            case WallConnectionType.TriShape:
                CalculateTriShapeAdjustment(connection, gapDistance, newEndpoints);
                break;
        }

        return newEndpoints;
    }

    /// <summary>
    /// Validates if the adjustment operation can be performed
    /// </summary>
    /// <param name="model">The wall adjustment model to validate</param>
    /// <returns>True if adjustment is possible</returns>
    public bool CanAdjustWalls(WallAdjustmentModel model)
    {
        if (model == null)
        {
            System.Diagnostics.Debug.WriteLine("CanAdjustWalls: model is null");
            return false;
        }

        var isValid = model.IsValid();
        if (!isValid)
        {
            System.Diagnostics.Debug.WriteLine($"CanAdjustWalls: model.IsValid() = false. GapMm={model.GapDistanceMm}, WallCount={model.SelectedWalls?.Count}, Document={model.Document != null}");
            return false;
        }

        var allWallsValid = model.SelectedWalls.All(wall => wall.IsValidObject);
        if (!allWallsValid)
        {
            System.Diagnostics.Debug.WriteLine("CanAdjustWalls: Some walls are not valid objects");
            return false;
        }

        System.Diagnostics.Debug.WriteLine($"CanAdjustWalls: All checks passed. GapMm={model.GapDistanceMm}, WallCount={model.SelectedWalls.Count}");
        return true;
    }

    #region Private Helper Methods

    private Dictionary<Wall, List<XYZ>> GetWallEndpoints(List<Wall> walls)
    {
        var wallEndpoints = new Dictionary<Wall, List<XYZ>>();

        foreach (var wall in walls)
        {
            var locationCurve = wall.Location as LocationCurve;
            if (locationCurve?.Curve is Line line)
            {
                wallEndpoints[wall] = new List<XYZ> { line.GetEndPoint(0), line.GetEndPoint(1) };
            }
        }

        return wallEndpoints;
    }

    private List<XYZ> FindConnectionPoints(Dictionary<Wall, List<XYZ>> wallEndpoints)
    {
        var connectionPoints = new List<XYZ>();
        var allPoints = wallEndpoints.Values.SelectMany(points => points).ToList();

        for (int i = 0; i < allPoints.Count; i++)
        {
            for (int j = i + 1; j < allPoints.Count; j++)
            {
                if (allPoints[i].DistanceTo(allPoints[j]) < TOLERANCE)
                {
                    if (!connectionPoints.Any(p => p.DistanceTo(allPoints[i]) < TOLERANCE))
                    {
                        connectionPoints.Add(allPoints[i]);
                    }
                }
            }
        }

        return connectionPoints;
    }

    private List<Wall> GetWallsAtPoint(List<Wall> walls, XYZ point)
    {
        var wallsAtPoint = new List<Wall>();

        foreach (var wall in walls)
        {
            var locationCurve = wall.Location as LocationCurve;
            if (locationCurve?.Curve is Line line)
            {
                if (line.GetEndPoint(0).DistanceTo(point) < TOLERANCE ||
                    line.GetEndPoint(1).DistanceTo(point) < TOLERANCE)
                {
                    wallsAtPoint.Add(wall);
                }
            }
        }

        return wallsAtPoint;
    }

    private WallConnectionType DetermineConnectionType(List<Wall> connectedWalls, XYZ connectionPoint)
    {
        if (connectedWalls.Count == 2)
        {
            // Check if walls are inline or corner
            var wall1Direction = GetWallDirection(connectedWalls[0]);
            var wall2Direction = GetWallDirection(connectedWalls[1]);
                
            var angle = wall1Direction.AngleTo(wall2Direction);
                
            if (Math.Abs(angle) < 0.1 || Math.Abs(angle - Math.PI) < 0.1)
                return WallConnectionType.Inline;
            else
                return WallConnectionType.Corner;
        }
        else if (connectedWalls.Count == 3)
        {
            return WallConnectionType.TriShape;
        }
        else if (connectedWalls.Count > 3)
        {
            return WallConnectionType.TShape;
        }

        return WallConnectionType.Corner; // Default
    }

    private XYZ GetWallDirection(Wall wall)
    {
        var locationCurve = wall.Location as LocationCurve;
        if (locationCurve?.Curve is Line line)
        {
            return (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
        }
        return XYZ.BasisX;
    }

    private void CalculateInlineAdjustment(WallConnection connection, double gapDistance,
        Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)> newEndpoints)
    {
        // For inline connections: Two walls connected end-to-end in a straight line
        // Create a gap by moving the endpoints away from the connection point

        if (connection.ConnectedWalls.Count != 2) return;

        var wall1 = connection.ConnectedWalls[0];
        var wall2 = connection.ConnectedWalls[1];
        var connectionPoint = connection.ConnectionPoint;

        // Get wall directions
        var wall1Direction = GetWallDirection(wall1);
        var wall2Direction = GetWallDirection(wall2);

        // Calculate new endpoints by moving away from connection point
        var wall1Line = (wall1.Location as LocationCurve)?.Curve as Line;
        var wall2Line = (wall2.Location as LocationCurve)?.Curve as Line;

        if (wall1Line != null && wall2Line != null)
        {
            // Determine which endpoint of each wall is at the connection
            var wall1Start = wall1Line.GetEndPoint(0);
            var wall1End = wall1Line.GetEndPoint(1);
            var wall2Start = wall2Line.GetEndPoint(0);
            var wall2End = wall2Line.GetEndPoint(1);

            // Find the endpoints that are at the connection point
            bool wall1StartAtConnection = wall1Start.DistanceTo(connectionPoint) < TOLERANCE;
            bool wall2StartAtConnection = wall2Start.DistanceTo(connectionPoint) < TOLERANCE;

            // Calculate new endpoints with gap
            XYZ wall1NewStart, wall1NewEnd, wall2NewStart, wall2NewEnd;

            if (wall1StartAtConnection)
            {
                wall1NewStart = connectionPoint + wall1Direction * gapDistance / 2;
                wall1NewEnd = wall1End;
            }
            else
            {
                wall1NewStart = wall1Start;
                wall1NewEnd = connectionPoint - wall1Direction * gapDistance / 2;
            }

            if (wall2StartAtConnection)
            {
                wall2NewStart = connectionPoint - wall2Direction * gapDistance / 2;
                wall2NewEnd = wall2End;
            }
            else
            {
                wall2NewStart = wall2Start;
                wall2NewEnd = connectionPoint + wall2Direction * gapDistance / 2;
            }

            newEndpoints[wall1] = (wall1NewStart, wall1NewEnd);
            newEndpoints[wall2] = (wall2NewStart, wall2NewEnd);
        }
    }

    private void CalculateCornerAdjustment(WallConnection connection, double gapDistance,
        Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)> newEndpoints)
    {
        // For corner connections: Two walls meet at a 90-degree angle
        // Create a gap by moving both walls away from the corner point

        if (connection.ConnectedWalls.Count != 2) return;

        var wall1 = connection.ConnectedWalls[0];
        var wall2 = connection.ConnectedWalls[1];
        var connectionPoint = connection.ConnectionPoint;

        var wall1Line = (wall1.Location as LocationCurve)?.Curve as Line;
        var wall2Line = (wall2.Location as LocationCurve)?.Curve as Line;

        if (wall1Line != null && wall2Line != null)
        {
            // Get wall directions
            var wall1Direction = GetWallDirection(wall1);
            var wall2Direction = GetWallDirection(wall2);

            // Determine which endpoints are at the connection
            var wall1Start = wall1Line.GetEndPoint(0);
            var wall1End = wall1Line.GetEndPoint(1);
            var wall2Start = wall2Line.GetEndPoint(0);
            var wall2End = wall2Line.GetEndPoint(1);

            bool wall1StartAtConnection = wall1Start.DistanceTo(connectionPoint) < TOLERANCE;
            bool wall2StartAtConnection = wall2Start.DistanceTo(connectionPoint) < TOLERANCE;

            // Calculate new endpoints for corner gap
            XYZ wall1NewStart, wall1NewEnd, wall2NewStart, wall2NewEnd;

            if (wall1StartAtConnection)
            {
                wall1NewStart = connectionPoint + wall1Direction * gapDistance;
                wall1NewEnd = wall1End;
            }
            else
            {
                wall1NewStart = wall1Start;
                wall1NewEnd = connectionPoint - wall1Direction * gapDistance;
            }

            if (wall2StartAtConnection)
            {
                wall2NewStart = connectionPoint + wall2Direction * gapDistance;
                wall2NewEnd = wall2End;
            }
            else
            {
                wall2NewStart = wall2Start;
                wall2NewEnd = connectionPoint - wall2Direction * gapDistance;
            }

            newEndpoints[wall1] = (wall1NewStart, wall1NewEnd);
            newEndpoints[wall2] = (wall2NewStart, wall2NewEnd);
        }
    }

    private void CalculateTShapeAdjustment(WallConnection connection, double gapDistance,
        Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)> newEndpoints)
    {
        // For T-shape connections: One wall connects perpendicularly to the middle of another
        // The perpendicular wall gets shortened, the main wall gets a gap

        if (connection.ConnectedWalls.Count < 2) return;

        var connectionPoint = connection.ConnectionPoint;

        // Identify the main wall (the one that the T connects to in the middle)
        // and the perpendicular wall(s)
        Wall mainWall = null;
        var perpendicularWalls = new List<Wall>();

        foreach (var wall in connection.ConnectedWalls)
        {
            var wallLine = (wall.Location as LocationCurve)?.Curve as Line;
            if (wallLine != null)
            {
                var start = wallLine.GetEndPoint(0);
                var end = wallLine.GetEndPoint(1);

                // Check if connection point is at the middle of the wall (not at endpoints)
                var distToStart = start.DistanceTo(connectionPoint);
                var distToEnd = end.DistanceTo(connectionPoint);
                var wallLength = start.DistanceTo(end);

                if (distToStart > TOLERANCE && distToEnd > TOLERANCE &&
                    Math.Abs(distToStart + distToEnd - wallLength) < TOLERANCE)
                {
                    mainWall = wall;
                }
                else
                {
                    perpendicularWalls.Add(wall);
                }
            }
        }

        if (mainWall != null)
        {
            // Split the main wall and create gap
            var mainWallLine = (mainWall.Location as LocationCurve)?.Curve as Line;
            if (mainWallLine != null)
            {
                var mainDirection = GetWallDirection(mainWall);
                var start = mainWallLine.GetEndPoint(0);
                var end = mainWallLine.GetEndPoint(1);

                // Create gap in main wall
                var gapStart = connectionPoint - mainDirection * gapDistance / 2;
                var gapEnd = connectionPoint + mainDirection * gapDistance / 2;

                // For now, adjust the main wall to end before the gap
                // In a full implementation, you might need to create two separate walls
                newEndpoints[mainWall] = (start, gapStart);
            }
        }

        // Adjust perpendicular walls
        foreach (var perpWall in perpendicularWalls)
        {
            var perpLine = (perpWall.Location as LocationCurve)?.Curve as Line;
            if (perpLine != null)
            {
                var perpDirection = GetWallDirection(perpWall);
                var start = perpLine.GetEndPoint(0);
                var end = perpLine.GetEndPoint(1);

                bool startAtConnection = start.DistanceTo(connectionPoint) < TOLERANCE;

                if (startAtConnection)
                {
                    var newStart = connectionPoint + perpDirection * gapDistance;
                    newEndpoints[perpWall] = (newStart, end);
                }
                else
                {
                    var newEnd = connectionPoint - perpDirection * gapDistance;
                    newEndpoints[perpWall] = (start, newEnd);
                }
            }
        }
    }

    private void CalculateTriShapeAdjustment(WallConnection connection, double gapDistance,
        Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)> newEndpoints)
    {
        // For Tri-shape connections: Three walls connected at a single point
        // Two walls are aligned inline, and the third wall connects perpendicularly

        if (connection.ConnectedWalls.Count != 3) return;

        var connectionPoint = connection.ConnectionPoint;
        var walls = connection.ConnectedWalls;

        // Identify the inline walls and the perpendicular wall
        Wall perpendicularWall = null;
        var inlineWalls = new List<Wall>();

        for (int i = 0; i < walls.Count; i++)
        {
            for (int j = i + 1; j < walls.Count; j++)
            {
                var dir1 = GetWallDirection(walls[i]);
                var dir2 = GetWallDirection(walls[j]);
                var angle = dir1.AngleTo(dir2);

                // Check if walls are inline (angle close to 0 or π)
                if (Math.Abs(angle) < 0.1 || Math.Abs(angle - Math.PI) < 0.1)
                {
                    inlineWalls.Add(walls[i]);
                    inlineWalls.Add(walls[j]);

                    // The remaining wall is perpendicular
                    for (int k = 0; k < walls.Count; k++)
                    {
                        if (k != i && k != j)
                        {
                            perpendicularWall = walls[k];
                            break;
                        }
                    }
                    break;
                }
            }
            if (inlineWalls.Count > 0) break;
        }

        if (inlineWalls.Count == 2 && perpendicularWall != null)
        {
            // Treat inline walls similar to inline connection
            var inlineConnection = new WallConnection
            {
                ConnectedWalls = inlineWalls,
                ConnectionPoint = connectionPoint,
                ConnectionType = WallConnectionType.Inline
            };
            CalculateInlineAdjustment(inlineConnection, gapDistance, newEndpoints);

            // Adjust perpendicular wall
            var perpLine = (perpendicularWall.Location as LocationCurve)?.Curve as Line;
            if (perpLine != null)
            {
                var perpDirection = GetWallDirection(perpendicularWall);
                var start = perpLine.GetEndPoint(0);
                var end = perpLine.GetEndPoint(1);

                bool startAtConnection = start.DistanceTo(connectionPoint) < TOLERANCE;

                if (startAtConnection)
                {
                    var newStart = connectionPoint + perpDirection * gapDistance;
                    newEndpoints[perpendicularWall] = (newStart, end);
                }
                else
                {
                    var newEnd = connectionPoint - perpDirection * gapDistance;
                    newEndpoints[perpendicularWall] = (start, newEnd);
                }
            }
        }
    }

    /// <summary>
    /// Applies wall adjustments with proper join management
    /// Preserves existing joins where appropriate and re-establishes joins after geometry changes
    /// </summary>
    /// <param name="newEndpoints">Dictionary of walls and their new endpoints</param>
    private void ApplyWallAdjustments(Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)> newEndpoints)
    {
        if (newEndpoints == null || newEndpoints.Count == 0)
            return;

        // Step 1: Analyze existing wall joins before making changes
        var existingJoins = AnalyzeExistingWallJoins(newEndpoints.Keys.ToList());

        // Step 2: Temporarily disallow joins to prevent automatic joining during geometry changes
        DisallowWallJoins(newEndpoints.Keys.ToList());

        try
        {
            // Step 3: Apply geometry changes
            ApplyGeometryChanges(newEndpoints);

            // Step 4: Re-establish appropriate joins based on connection types and gap requirements
            ReestablishWallJoins(newEndpoints.Keys.ToList(), existingJoins);
        }
        catch (Exception ex)
        {
            // If something goes wrong, try to restore original join states
            RestoreWallJoins(existingJoins);
            throw new InvalidOperationException($"Error applying wall adjustments: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Analyzes existing wall joins before making geometry changes
    /// </summary>
    /// <param name="walls">Walls to analyze</param>
    /// <returns>Dictionary containing join information for each wall</returns>
    private Dictionary<Wall, WallJoinInfo> AnalyzeExistingWallJoins(List<Wall> walls)
    {
        var joinInfo = new Dictionary<Wall, WallJoinInfo>();

        foreach (var wall in walls)
        {
            var locationCurve = wall.Location as LocationCurve;
            if (locationCurve != null)
            {
                var info = new WallJoinInfo
                {
                    Wall = wall,
                    StartJoinAllowed = WallUtils.IsWallJoinAllowedAtEnd(wall, 0),
                    EndJoinAllowed = WallUtils.IsWallJoinAllowedAtEnd(wall, 1),
                    StartJoinType = locationCurve.get_JoinType(0),
                    EndJoinType = locationCurve.get_JoinType(1),
                    StartJoinedElements = GetJoinedElementsAtEnd(wall, 0),
                    EndJoinedElements = GetJoinedElementsAtEnd(wall, 1)
                };
                joinInfo[wall] = info;
            }
        }

        return joinInfo;
    }

    /// <summary>
    /// Gets elements joined to a wall at a specific end
    /// </summary>
    /// <param name="wall">The wall to check</param>
    /// <param name="end">The end to check (0 or 1)</param>
    /// <returns>List of joined elements</returns>
    private List<Element> GetJoinedElementsAtEnd(Wall wall, int end)
    {
        var joinedElements = new List<Element>();
        var locationCurve = wall.Location as LocationCurve;

        if (locationCurve != null)
        {
            try
            {
                var elementsAtJoin = locationCurve.get_ElementsAtJoin(end);
                if (elementsAtJoin != null)
                {
                    foreach (ElementId elementId in elementsAtJoin)
                    {
                        var element = wall.Document.GetElement(elementId);
                        if (element != null)
                        {
                            joinedElements.Add(element);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Some walls may not have joins, which is normal
            }
        }

        return joinedElements;
    }

    /// <summary>
    /// Temporarily disallows wall joins to prevent automatic joining during geometry changes
    /// </summary>
    /// <param name="walls">Walls to disallow joins for</param>
    private void DisallowWallJoins(List<Wall> walls)
    {
        foreach (var wall in walls)
        {
            try
            {
                WallUtils.DisallowWallJoinAtEnd(wall, 0);
                WallUtils.DisallowWallJoinAtEnd(wall, 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Could not disallow joins for wall {wall.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Applies the actual geometry changes to walls
    /// </summary>
    /// <param name="newEndpoints">Dictionary of walls and their new endpoints</param>
    private void ApplyGeometryChanges(Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)> newEndpoints)
    {
        foreach (var kvp in newEndpoints)
        {
            var wall = kvp.Key;
            var (startPoint, endPoint) = kvp.Value;

            var locationCurve = wall.Location as LocationCurve;
            if (locationCurve != null)
            {
                try
                {
                    // Validate the new line
                    if (startPoint.DistanceTo(endPoint) < TOLERANCE)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Skipping wall {wall.Id} - new line too short");
                        continue;
                    }

                    var newLine = Line.CreateBound(startPoint, endPoint);
                    locationCurve.Curve = newLine;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to update geometry for wall {wall.Id}: {ex.Message}", ex);
                }
            }
        }
    }

    /// <summary>
    /// Re-establishes appropriate wall joins after geometry changes with connection type-specific logic
    /// </summary>
    /// <param name="walls">Walls to re-establish joins for</param>
    /// <param name="existingJoins">Information about existing joins before changes</param>
    private void ReestablishWallJoins(List<Wall> walls, Dictionary<Wall, WallJoinInfo> existingJoins)
    {
        // Group walls by their connection types for specialized handling
        var wallConnections = AnalyzeWallConnections(walls);

        foreach (var connection in wallConnections)
        {
            ApplyConnectionSpecificJoinLogic(connection, existingJoins);
        }

        // Handle any remaining walls that weren't part of connections
        foreach (var wall in walls)
        {
            if (existingJoins.TryGetValue(wall, out var joinInfo))
            {
                try
                {
                    // Re-allow joins where they were previously allowed
                    if (joinInfo.StartJoinAllowed)
                    {
                        WallUtils.AllowWallJoinAtEnd(wall, 0);

                        // Try to re-establish joins with previously joined elements
                        ReestablishJoinsWithElements(wall, 0, joinInfo.StartJoinedElements, joinInfo.StartJoinType);
                    }

                    if (joinInfo.EndJoinAllowed)
                    {
                        WallUtils.AllowWallJoinAtEnd(wall, 1);

                        // Try to re-establish joins with previously joined elements
                        ReestablishJoinsWithElements(wall, 1, joinInfo.EndJoinedElements, joinInfo.EndJoinType);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not re-establish joins for wall {wall.Id}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Applies connection type-specific join logic
    /// </summary>
    /// <param name="connection">The wall connection to handle</param>
    /// <param name="existingJoins">Information about existing joins</param>
    private void ApplyConnectionSpecificJoinLogic(WallConnection connection, Dictionary<Wall, WallJoinInfo> existingJoins)
    {
        switch (connection.ConnectionType)
        {
            case WallConnectionType.Inline:
                HandleInlineConnectionJoins(connection, existingJoins);
                break;
            case WallConnectionType.Corner:
                HandleCornerConnectionJoins(connection, existingJoins);
                break;
            case WallConnectionType.TShape:
                HandleTShapeConnectionJoins(connection, existingJoins);
                break;
            case WallConnectionType.TriShape:
                HandleTriShapeConnectionJoins(connection, existingJoins);
                break;
        }
    }

    /// <summary>
    /// Handles join logic for inline connections (two walls in a straight line with a gap)
    /// </summary>
    private void HandleInlineConnectionJoins(WallConnection connection, Dictionary<Wall, WallJoinInfo> existingJoins)
    {
        // For inline connections with gaps, we typically don't want to join the walls
        // at the gap location, but we may want to preserve other joins
        foreach (var wall in connection.ConnectedWalls)
        {
            if (existingJoins.TryGetValue(wall, out var joinInfo))
            {
                var locationCurve = wall.Location as LocationCurve;
                if (locationCurve?.Curve is Line line)
                {
                    // Determine which end is at the connection point
                    var start = line.GetEndPoint(0);
                    var end = line.GetEndPoint(1);
                    bool startAtConnection = start.DistanceTo(connection.ConnectionPoint) < TOLERANCE;
                    bool endAtConnection = end.DistanceTo(connection.ConnectionPoint) < TOLERANCE;

                    // Allow joins at ends that are NOT at the gap
                    if (!startAtConnection && joinInfo.StartJoinAllowed)
                    {
                        WallUtils.AllowWallJoinAtEnd(wall, 0);
                        ReestablishJoinsWithElements(wall, 0, joinInfo.StartJoinedElements, joinInfo.StartJoinType);
                    }

                    if (!endAtConnection && joinInfo.EndJoinAllowed)
                    {
                        WallUtils.AllowWallJoinAtEnd(wall, 1);
                        ReestablishJoinsWithElements(wall, 1, joinInfo.EndJoinedElements, joinInfo.EndJoinType);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handles join logic for corner connections (two walls meeting at an angle with a gap)
    /// </summary>
    private void HandleCornerConnectionJoins(WallConnection connection, Dictionary<Wall, WallJoinInfo> existingJoins)
    {
        // For corner connections with gaps, we don't join at the corner but preserve other joins
        foreach (var wall in connection.ConnectedWalls)
        {
            if (existingJoins.TryGetValue(wall, out var joinInfo))
            {
                var locationCurve = wall.Location as LocationCurve;
                if (locationCurve?.Curve is Line line)
                {
                    var start = line.GetEndPoint(0);
                    var end = line.GetEndPoint(1);
                    bool startAtConnection = start.DistanceTo(connection.ConnectionPoint) < TOLERANCE;
                    bool endAtConnection = end.DistanceTo(connection.ConnectionPoint) < TOLERANCE;

                    // Allow joins at ends that are NOT at the gap, but use appropriate join types
                    if (!startAtConnection && joinInfo.StartJoinAllowed)
                    {
                        WallUtils.AllowWallJoinAtEnd(wall, 0);
                        ReestablishJoinsWithElements(wall, 0, joinInfo.StartJoinedElements, JoinType.Miter);
                    }

                    if (!endAtConnection && joinInfo.EndJoinAllowed)
                    {
                        WallUtils.AllowWallJoinAtEnd(wall, 1);
                        ReestablishJoinsWithElements(wall, 1, joinInfo.EndJoinedElements, JoinType.Miter);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handles join logic for T-shape connections
    /// </summary>
    private void HandleTShapeConnectionJoins(WallConnection connection, Dictionary<Wall, WallJoinInfo> existingJoins)
    {
        // For T-shape connections, we need to handle the main wall and perpendicular walls differently
        foreach (var wall in connection.ConnectedWalls)
        {
            if (existingJoins.TryGetValue(wall, out var joinInfo))
            {
                // Allow joins at ends that don't interfere with the gap
                if (joinInfo.StartJoinAllowed)
                {
                    WallUtils.AllowWallJoinAtEnd(wall, 0);
                    ReestablishJoinsWithElements(wall, 0, joinInfo.StartJoinedElements, joinInfo.StartJoinType);
                }

                if (joinInfo.EndJoinAllowed)
                {
                    WallUtils.AllowWallJoinAtEnd(wall, 1);
                    ReestablishJoinsWithElements(wall, 1, joinInfo.EndJoinedElements, joinInfo.EndJoinType);
                }
            }
        }
    }

    /// <summary>
    /// Handles join logic for tri-shape connections (three walls meeting at a point)
    /// </summary>
    private void HandleTriShapeConnectionJoins(WallConnection connection, Dictionary<Wall, WallJoinInfo> existingJoins)
    {
        // For tri-shape connections, preserve joins that don't interfere with the gaps
        foreach (var wall in connection.ConnectedWalls)
        {
            if (existingJoins.TryGetValue(wall, out var joinInfo))
            {
                var locationCurve = wall.Location as LocationCurve;
                if (locationCurve?.Curve is Line line)
                {
                    var start = line.GetEndPoint(0);
                    var end = line.GetEndPoint(1);
                    bool startAtConnection = start.DistanceTo(connection.ConnectionPoint) < TOLERANCE;
                    bool endAtConnection = end.DistanceTo(connection.ConnectionPoint) < TOLERANCE;

                    // Allow joins at ends that are NOT at the tri-shape connection
                    if (!startAtConnection && joinInfo.StartJoinAllowed)
                    {
                        WallUtils.AllowWallJoinAtEnd(wall, 0);
                        ReestablishJoinsWithElements(wall, 0, joinInfo.StartJoinedElements, joinInfo.StartJoinType);
                    }

                    if (!endAtConnection && joinInfo.EndJoinAllowed)
                    {
                        WallUtils.AllowWallJoinAtEnd(wall, 1);
                        ReestablishJoinsWithElements(wall, 1, joinInfo.EndJoinedElements, joinInfo.EndJoinType);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attempts to re-establish joins between a wall and previously joined elements
    /// </summary>
    /// <param name="wall">The wall to join</param>
    /// <param name="end">The end of the wall (0 or 1)</param>
    /// <param name="previouslyJoinedElements">Elements that were previously joined</param>
    /// <param name="joinType">The type of join to establish</param>
    private void ReestablishJoinsWithElements(Wall wall, int end, List<Element> previouslyJoinedElements, JoinType joinType)
    {
        var locationCurve = wall.Location as LocationCurve;
        if (locationCurve == null) return;

        foreach (var element in previouslyJoinedElements)
        {
            if (element is Wall otherWall && otherWall.IsValidObject)
            {
                try
                {
                    // Check if walls are still close enough to join
                    if (AreWallsCloseEnoughToJoin(wall, otherWall))
                    {
                        // Try to join the geometries
                        if (!JoinGeometryUtils.AreElementsJoined(wall.Document, wall, otherWall))
                        {
                            JoinGeometryUtils.JoinGeometry(wall.Document, wall, otherWall);
                        }

                        // Set the join type if it's a specific type (Miter is commonly used)
                        if (joinType == JoinType.Miter)
                        {
                            locationCurve.set_JoinType(end, joinType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not join wall {wall.Id} with wall {otherWall.Id}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Checks if two walls are close enough to be joined
    /// </summary>
    /// <param name="wall1">First wall</param>
    /// <param name="wall2">Second wall</param>
    /// <returns>True if walls can be joined</returns>
    private bool AreWallsCloseEnoughToJoin(Wall wall1, Wall wall2)
    {
        var location1 = wall1.Location as LocationCurve;
        var location2 = wall2.Location as LocationCurve;

        if (location1?.Curve is Line line1 && location2?.Curve is Line line2)
        {
            // Check if any endpoint of wall1 is close to any endpoint of wall2
            var wall1Start = line1.GetEndPoint(0);
            var wall1End = line1.GetEndPoint(1);
            var wall2Start = line2.GetEndPoint(0);
            var wall2End = line2.GetEndPoint(1);

            return wall1Start.DistanceTo(wall2Start) < TOLERANCE ||
                   wall1Start.DistanceTo(wall2End) < TOLERANCE ||
                   wall1End.DistanceTo(wall2Start) < TOLERANCE ||
                   wall1End.DistanceTo(wall2End) < TOLERANCE;
        }

        return false;
    }

    /// <summary>
    /// Restores wall joins to their original state in case of errors
    /// </summary>
    /// <param name="existingJoins">Original join information</param>
    private void RestoreWallJoins(Dictionary<Wall, WallJoinInfo> existingJoins)
    {
        foreach (var kvp in existingJoins)
        {
            var wall = kvp.Key;
            var joinInfo = kvp.Value;

            try
            {
                if (wall.IsValidObject)
                {
                    var locationCurve = wall.Location as LocationCurve;
                    if (locationCurve != null)
                    {
                        // Restore join allowance
                        if (joinInfo.StartJoinAllowed)
                            WallUtils.AllowWallJoinAtEnd(wall, 0);
                        else
                            WallUtils.DisallowWallJoinAtEnd(wall, 0);

                        if (joinInfo.EndJoinAllowed)
                            WallUtils.AllowWallJoinAtEnd(wall, 1);
                        else
                            WallUtils.DisallowWallJoinAtEnd(wall, 1);

                        // Restore join types
                        locationCurve.set_JoinType(0, joinInfo.StartJoinType);
                        locationCurve.set_JoinType(1, joinInfo.EndJoinType);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Could not restore joins for wall {wall.Id}: {ex.Message}");
            }
        }
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Contains information about wall joins for preservation during adjustments
    /// </summary>
    private class WallJoinInfo
    {
        public Wall Wall { get; set; }
        public bool StartJoinAllowed { get; set; }
        public bool EndJoinAllowed { get; set; }
        public JoinType StartJoinType { get; set; }
        public JoinType EndJoinType { get; set; }
        public List<Element> StartJoinedElements { get; set; } = new List<Element>();
        public List<Element> EndJoinedElements { get; set; } = new List<Element>();
    }

    #endregion
}