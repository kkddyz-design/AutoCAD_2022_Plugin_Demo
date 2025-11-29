/*
 * 提供几何相关工具方法
 */
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Reflection;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain
{

    public static class GeometryTools
    {

        // 扩展double实现弧度转换角度
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
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
            if(v21.GetAngleTo(v23) == 0 || v21.GetAngleTo(v23) == Math.PI) {
                return true;
            }
            else {
                return false;
            }
        }

        // 扩展double实现角度转换弧度
        public static double DegreeToAngle(this double degree)
        {
            return degree * Math.PI / 180;
        }

        /*
         * 获取向量角度
         */
        public static double GetAngleToXAxis(this Point3d startPoint, Point3d endPoint)
        {
            // 声明一个与X轴平行的向量
            Vector3d temp = new Vector3d(1, 0, 0);

            // 获取起点到终点的向量
            Vector3d VsToe = startPoint.GetVectorTo(endPoint);
            return VsToe.Y > 0 ? temp.GetAngleTo(VsToe) : -temp.GetAngleTo(VsToe);
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
         * return entity switch 使用的是 C# 8.0 引入的 switch 表达式 语法糖，核心作用是简化多条件分支的返回逻辑，让代码更简洁、可读性更高。
         * 
         * witch (entity){
         *      case Line line:
         *          return line.StartPoint;
         *      case Circle circle:
         *          return circle.Center;
         *      default:
         *          return GetFallbackPosition(entity);
         * }
         * 
         * 
         */
        public static Point3d GetPosition(this Entity entity)
        {
            // 1. 优先处理已知有明确 Position 或基准点的实体类型（性能最优）
            return entity switch
            {
                Line line => line.StartPoint,                // 直线：取起点作为基准点
                Circle circle => circle.Center,              // 圆：取圆心作为基准点
                DBText dbText => dbText.Position,            // 单行文字：取插入点（Position 属性）
                MText mText => mText.Location,               // 多行文字：取插入点（Location 属性，无 Position）
                Polyline polyline => polyline.StartPoint,    // 多段线：取起点
                BlockReference blockRef => blockRef.Position,// 块参照：取插入点
                Arc arc => arc.Center,                       // 圆弧：取圆心
                Ellipse ellipse => ellipse.Center,           // 椭圆：取圆心
                DBPoint point => point.Position,             // 点实体：取自身位置
                _ => GetFallbackPosition(entity)             // 其他类型：使用 fallback 逻辑
            };
        }

        // Fallback 逻辑：处理未知类型，尝试反射获取 Position/Location，最后用边界框中心
        private static Point3d GetFallbackPosition(Entity entity)
        {
            Type entityType = entity.GetType();
            string entityTypeName = entityType.Name;

            // 2. 尝试通过反射获取常见的位置属性（Position 或 Location）
            PropertyInfo positionProp = entityType.GetProperty("Position", typeof(Point3d));
            if(positionProp != null && positionProp.CanRead) {
                return (Point3d)positionProp.GetValue(entity);
            }

            PropertyInfo locationProp = entityType.GetProperty("Location", typeof(Point3d));
            if(locationProp != null && locationProp.CanRead) {
                return (Point3d)locationProp.GetValue(entity);
            }

            // 3. 反射获取失败，使用实体的边界框中心作为最终 fallback（通用性最强）
            try {
                Extents3d extents = entity.GeometricExtents;
                return new Point3d(
                    (extents.MinPoint.X + extents.MaxPoint.X) / 2,
                    (extents.MinPoint.Y + extents.MaxPoint.Y) / 2,
                    extents.MinPoint.Z // 保留 Z 坐标（2D 实体 Z 通常为 0）
                );
            }
            catch(Exception ex) {
                // 极端情况：边界框获取失败（理论极少出现），抛出明确异常
                throw new NotSupportedException(
                    $"实体类型 {entityTypeName} 不支持 Position/Location 属性，且无法获取边界框。",
                    ex
                );
            }
        }
        /*
         * 计算原始实体到中心点的距离（以实体的基点为例，可根据需求调整）
         * 环形阵列调用
         */

        public static double GetEntityDistanceToCenter(this Entity entity, Point3d center)
        {
            Point3d entityBasePoint; // 实体的基准点（根据类型动态获取）

            entityBasePoint = entity.GetPosition();

            // 2. 计算基准点到环形中心点的距离 根号下x^2+y^2
            return Math.Sqrt(
                Math.Pow(entityBasePoint.X - center.X, 2) +
                Math.Pow(entityBasePoint.Y - center.Y, 2)
            );
        }

    }

}
