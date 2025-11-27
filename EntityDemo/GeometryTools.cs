/*
 * 提供几何相关工具方法
 */
using System;
using Autodesk.AutoCAD.Geometry;

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
        public static bool AreCollinear(
            this Point3d firstPoint,
            Point3d secondPoint,
            Point3d thirdPoint
        )
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

        /*
         * 获取向量角度
         */
        public static double GetAngleToXAxis(this Point3d startPoint, Point3d endPoint)
        {
            //声明一个与X轴平行的向量
            Vector3d temp = new Vector3d(1, 0, 0);
            //获取起点到终点的向量
            Vector3d VsToe = startPoint.GetVectorTo(endPoint);
            return VsToe.Y > 0 ? temp.GetAngleTo(VsToe) : -temp.GetAngleTo(VsToe);
        }

        /*
         * 获取两点之间的距离
         */
        public static double GetDistanceBetweenTwoPoint(this Point3d point1, Point3d point2)
        {
            return Math.Sqrt(
                (point1.X - point2.X) * (point1.X - point2.X)
                    + (point1.Y - point2.Y) * (point1.Y - point2.Y)
                    + (point1.Z - point2.Z) * (point1.Z - point2.Z)
            );
        }

        /*
         * 获取两点的中心点
         */

        public static Point3d GetCenterPointBetweenTwoPoint(this Point3d Point1, Point3d point2)
        {
            return new Point3d(
                (Point1.X + point2.X) / 2,
                (Point1.Y + point2.Y) / 2,
                (Point1.Z + point2.Z) / 2
            );
        }
    }
}
