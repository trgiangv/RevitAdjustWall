using Autodesk.Revit.DB;

namespace RevitAdjustWall.Extensions;

public static class CurveExtensions
{
    /// <summary>
    ///     Returns the intersection point of two lines. The lines are considered to be endless
    /// </summary>
    /// <param name="c1">first line</param>
    /// <param name="c2">second line</param>
    /// <returns>Intersection point. Returns null if the lines are parallel</returns>
    public static XYZ? Intersection(this Curve c1, Curve c2)
    {
        var p1 = c1.GetEndPoint(0);
        var q1 = c1.GetEndPoint(1);
        var p2 = c2.GetEndPoint(0);
        var q2 = c2.GetEndPoint(1);
        var v1 = q1 - p1;
        var v2 = q2 - p2;
        var w = p2 - p1;
        XYZ? p5 = null;
        var c = (v2.X * w.Y - v2.Y * w.X)
                / (v2.X * v1.Y - v2.Y * v1.X);
        if (double.IsInfinity(c)) return p5;
        var x = p1.X + c * v1.X;
        var y = p1.Y + c * v1.Y;
        p5 = new XYZ(x, y, 0);
        return p5;
    }
}