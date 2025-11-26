/*
 * 提供几何相关工具方法
 */
using Autodesk.AutoCAD.Geometry;
using System;

namespace AutoCAD_2022_Plugin_Demo.DrawDemo
{
    public static class GeometryTools
    {

        // 扩展Double实现角度转换弧度 
        public static Double DegreeToAngle(this Double degree)
        {
            return degree * Math.PI / 180;
        }
        // 扩展Double实现弧度转换角度
        public static Double AngleToDegree(this Double angle)
        {
            return angle * 180 / Math.PI;
        }

        /*
         * 扩展Point3D 通过startPoint,length,degree计算endPoint
         */
        public static Point3d GetEndPoint(this Point3d startPoint, Double length, Double degree)
        {

            Double X = startPoint.X + (length * Math.Cos(degree.DegreeToAngle()));
            Double Y = startPoint.Y + (length * Math.Sin(degree.DegreeToAngle()));
            Point3d endPoint = new Point3d(X, Y, 0);
            return endPoint;
        }

    }
}
