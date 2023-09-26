//BSD, 2014-present, WinterDev

using PixelFarm.VectorMath;

namespace PixelFarm.CpuBlit.VertexProcessing
{
    /// <summary>
    /// bezire curve generator
    /// </summary>
    public static class BezierCurve
    {
        public static void Curve3GetControlPoints(Vector2d start, Vector2d controlPoint, Vector2d endPoint, out Vector2d control1, out Vector2d control2)
        {
            double x1 = start.X + (controlPoint.X - start.X) * 2 / 3;
            double y1 = start.Y + (controlPoint.Y - start.Y) * 2 / 3;
            double x2 = controlPoint.X + (endPoint.X - controlPoint.X) / 3;
            double y2 = controlPoint.Y + (endPoint.Y - controlPoint.Y) / 3;
            control1 = new Vector2d(x1, y1);
            control2 = new Vector2d(x2, y2);
        }
    }
}