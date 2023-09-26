//MIT, 2017-present, WinterDev
using System;
using PointF = System.Numerics.Vector2;

namespace PixelFarm.VectorMath
{

    public struct Point
    {
        public int x;
        public int y;
        public Point(int x, int y)
        {

            this.x = x;
            this.y = y;
        }
        public void Offset(int dx, int dy)
        {
            this.x += dx;
            this.y += dy;
        }
#if DEBUG
        public override string ToString()
        {
            return "(" + x + "," + y + ")";
        }
#endif
    }

    public static class MyVectorHelper
    {
        public static Vector2d NewFromPoint(PointF p)
        {
            return new Vector2d(p.X, p.Y);
        }

        /// <summary>
        /// create vector from start to end
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static Vector2d NewFromTwoPoints(PointF start, PointF end)
        {
            return new Vector2d(end.X - start.X, end.Y - start.Y);
        }

        public static bool IsClockwise(PointF pt1, PointF pt2, PointF pt3)
        {
            Vector2d V21 = NewFromTwoPoints(pt2, pt1);
            Vector2d v23 = NewFromTwoPoints(pt2, pt3);
            return Vector2d.Cross(V21, v23) < 0;// sin(angle pt1 pt2 pt3) > 0, 0<angle pt1 pt2 pt3 <180
            //return V21.Cr(v23) < 0; // sin(angle pt1 pt2 pt3) > 0, 0<angle pt1 pt2 pt3 <180
        }

        public static bool IsCCW(PointF pt1, PointF pt2, PointF pt3)
        {
            Vector2d V21 = NewFromTwoPoints(pt2, pt1);
            Vector2d v23 = NewFromTwoPoints(pt2, pt3);
            return Vector2d.Cross(V21, v23) > 0;// sin(angle pt2 pt1 pt3) < 0, 180<angle pt2 pt1 pt3 <360

            //return V21.CrossProduct(v23) > 0;  // sin(angle pt2 pt1 pt3) < 0, 180<angle pt2 pt1 pt3 <360
        }

    }

     

}