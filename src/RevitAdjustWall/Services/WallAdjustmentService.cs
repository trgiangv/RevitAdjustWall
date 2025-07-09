using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitAdjustWall.Models;

namespace RevitAdjustWall.Services
{
    /// <summary>
    /// Implementation of wall adjustment service
    /// Follows Single Responsibility Principle (SRP) and Open/Closed Principle (OCP)
    /// </summary>
    public class WallAdjustmentService : IWallAdjustmentService
    {
        private const double TOLERANCE = 0.01; // 1cm tolerance for point comparison

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
        /// </summary>
        /// <param name="model">The wall adjustment model containing all necessary data</param>
        /// <returns>True if adjustment was successful</returns>
        public bool AdjustWalls(WallAdjustmentModel model)
        {
            if (!CanAdjustWalls(model))
                return false;

            try
            {
                using (var transaction = new Transaction(model.Document, "Adjust Wall Gaps"))
                {
                    transaction.Start();

                    var connections = AnalyzeWallConnections(model.SelectedWalls);
                    var gapDistanceInFeet = model.GetGapDistanceInFeet();

                    foreach (var connection in connections)
                    {
                        var newEndpoints = CalculateNewWallEndpoints(connection, gapDistanceInFeet);
                        ApplyWallAdjustments(newEndpoints);
                    }

                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
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
            return model != null && 
                   model.IsValid() && 
                   model.SelectedWalls.All(wall => wall.IsValidObject);
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

        private void ApplyWallAdjustments(Dictionary<Wall, (XYZ StartPoint, XYZ EndPoint)> newEndpoints)
        {
            foreach (var kvp in newEndpoints)
            {
                var wall = kvp.Key;
                var (startPoint, endPoint) = kvp.Value;

                var locationCurve = wall.Location as LocationCurve;
                if (locationCurve != null)
                {
                    var newLine = Line.CreateBound(startPoint, endPoint);
                    locationCurve.Curve = newLine;
                }
            }
        }

        #endregion
    }
}
