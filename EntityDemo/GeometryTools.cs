/*
 * 提供几何相关工具方法
 */
using Autodesk.AutoCAD.Geometry;
using System;

namespace AutoCAD_2022_Plugin_Demo.DrawDemo
{
    public static class GeometryTools
    {

        // 扩展double实现角度转换弧度 
        public static double DegreeToAngle(this double degree)
        {
            return degree * Math.PI / 180;
        }
        // 扩展double实现弧度转换角度
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
        }

        /*
         * 扩展Point3D 通过startPoint,length,degree计算endPoint
         */
        public static Point3d GetEndPoint(this Point3d startPoint, double length, double degree)
        {

            double X = startPoint.X + (length * Math.Cos(degree.DegreeToAngle()));
            double Y = startPoint.Y + (length * Math.Sin(degree.DegreeToAngle()));
            Point3d endPoint = new Point3d(X, Y, 0);
            return endPoint;
        }

        /*
         * 判断三点是否共线
         */
        public static bool AreCollinear(this Point3d firstPoint, Point3d secondPoint, Point3d thirdPoint)
        {
            Vector3d v21 = secondPoint.GetVectorTo(firstPoint);
            Vector3d v23 = secondPoint.GetVectorTo(thirdPoint);
            if (v21.GetAngleTo(v23) == 0 || v21.GetAngleTo(v23) == Math.PI)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
