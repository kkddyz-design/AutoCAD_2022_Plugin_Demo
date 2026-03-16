/*
 * 提供几何相关工具方法
 */
using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.tools
{

    public static class GeometryTools
    {

        /// <summary>
        /// 扩展point3d的offset方法
        /// </summary>
        /// <param name="originalPoint"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="offsetZ"></param>
        /// <returns>偏移后的point3d对象</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Point3d offset(this Point3d originalPoint, double offsetX, double offsetY, double offsetZ)
        {
            // 健壮性校验：防止传入 null 导致空指针异常
            if(originalPoint == null) {
                throw new ArgumentNullException(nameof(originalPoint), "原始坐标点不能为 null");
            }

            // 计算平移后的新坐标
            double newX = originalPoint.X + offsetX;
            double newY = originalPoint.Y + offsetY;
            double newZ = originalPoint.Z + offsetZ;

            // 返回新的 Point3D 对象（原始对象保持不变，符合不可变设计原则）
            return new Point3d(newX, newY, newZ);
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
        public static double DegreeToRadian(this double degree)
        {
            return degree * Math.PI / 180;
        }


        // 扩展double实现弧度转换角度
        public static double RadianToDegree(this double radian)
        {
            return radian * 180 / Math.PI;
        }


        /// <summary>
        /// 计算从起点到终点的向量与X轴正方向的夹角。
        /// </summary>
        /// <param name="startPoint">向量的起点。</param>
        /// <param name="endPoint">向量的终点。</param>
        /// <returns>标准化后的角度，范围在 [0°, 360°) 之间。</returns>
        public static double GetAngleToXAxis(this Point3d startPoint, Point3d endPoint)
        {
            // 1. 声明一个与X轴正方向平行的向量
            Vector3d xAxis = new Vector3d(1, 0, 0);

            // 2. 获取从起点到终点的向量
            Vector3d direction = startPoint.GetVectorTo(endPoint);

            // 3. 处理零向量的特殊情况
            if(direction.IsZeroLength()) {
                return 0.0;
            }

            // 4. 计算与X轴正方向的夹角（弧度），范围在 [0, π]
            double angleInRadians = xAxis.GetAngleTo(direction);

            // 5. 将弧度转换为角度
            double angleInDegrees = angleInRadians * 180.0 / Math.PI;

            // 6. 判断方向向量是否在X轴下方（第三、四象限）
            // 如果在下方，则角度应该是 360° 减去计算出的锐角
            if(direction.Y < 0) {
                angleInDegrees = 360.0 - angleInDegrees;
            }

            return angleInDegrees;
        }

        /// <summary>
        /// 计算从起点到终点的向量与X轴正方向的夹角（返回弧度值，范围 [0, 2π]）
        /// </summary>
        /// <param name="startPoint">起始点</param>
        /// <param name="endPoint">终点</param>
        /// <returns>夹角（弧度），零向量返回0.0</returns>
        public static double GetRadiansToXAxis(this Point3d startPoint, Point3d endPoint)
        {
            // 1. 声明与X轴正方向平行的向量
            Vector3d xAxis = new Vector3d(1, 0, 0);

            // 2. 获取从起点到终点的方向向量
            Vector3d direction = startPoint.GetVectorTo(endPoint);

            // 3. 处理零向量（起点=终点）的特殊情况
            if(direction.IsZeroLength()) {
                return 0.0;
            }

            // 4. 计算与X轴正方向的夹角（弧度），原生返回范围 [0, π]
            double angleInRadians = xAxis.GetAngleTo(direction);

            // 5. 判断方向向量是否在X轴下方（Y<0，第三/四象限）
            // 若在下方，将角度修正为 [π, 2π] 范围（保持总范围 [0, 2π]）
            if(direction.Y < 0) {
                angleInRadians = 2 * Math.PI - angleInRadians;
            }

            // 直接返回弧度值
            return angleInRadians;
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
            double X = startPoint.X + (length * Math.Cos(degree.DegreeToRadian()));
            double Y = startPoint.Y + (length * Math.Sin(degree.DegreeToRadian()));
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
        public static Point3d GetEntityPosition(this Entity entity)
        {
            // 1. 优先处理已知有明确 Position 或基准点的实体类型（性能最优）
            // return entity switch
            // {
            // Line line => line.StartPoint,                // 直线：取起点作为基准点
            // Circle circle => circle.Center,              // 圆：取圆心作为基准点
            // Rectangle rectangle => rectangle.Center,     // 矩形,取中心
            // Polygon polygon => polygon.polygonCenter,    // 多边形,取中心
            // DBText dbText => dbText.Position,            // 单行文字：取插入点（Position 属性）
            // MText mText => mText.Location,               // 多行文字：取插入点（Location 属性，无 Position）
            // Polyline polyline => polyline.StartPoint,    // 多段线：取起点
            // BlockReference blockRef => blockRef.Position,// 块参照：取插入点
            // Arc arc => arc.Center,                       // 圆弧：取圆心
            // Ellipse ellipse => ellipse.Center,           // 椭圆：取圆心
            // DBPoint point => point.Position,             // 点实体：取自身位置
            // _ => GetFallbackPosition(entity)             // 其他类型：使用 fallback 逻辑
            // };
            // 1. 直线：取起点
            if(entity is Line line) {
                return line.StartPoint;
            }

            // 2. 圆：取圆心
            else if(entity is Circle circle) {
                return circle.Center;
            }

            // 3. 矩形（子类，优先于 Polyline）：取中心
            else if(entity is Rectangle rectangle) {
                return rectangle.Center;
            }

            // 4. 多边形（子类，优先于 Polyline）：取中心
            else if(entity is Polygon polygon) {
                return polygon.polygonCenter;
            }

            // 5. 单行文字：取插入点
            else if(entity is DBText dbText) {
                return dbText.Position;
            }

            // 6. 多行文字：取插入点
            else if(entity is MText mText) {
                return mText.Location;
            }

            // 7. 多段线（父类，放在子类后面）：取起点
            else if(entity is Polyline polyline) {
                return polyline.StartPoint;
            }

            // 8. 块参照：取插入点
            else if(entity is BlockReference blockRef) {
                return blockRef.Position;
            }

            // 9. 圆弧：取圆心
            else if(entity is Arc arc) {
                return arc.Center;
            }

            // 10. 椭圆：取圆心
            else if(entity is Ellipse ellipse) {
                return ellipse.Center;
            }

            // 11. 点实体：取自身位置
            else if(entity is DBPoint point) {
                return point.Position;
            }

            // 12. 其他类型：使用 fallback 逻辑
            else {
                return GetFallbackPosition(entity);
            }
        }

        // Fallback 逻辑：处理未知类型，尝试反射获取 Position/Location，最后用边界框中心
        private static Point3d GetFallbackPosition(Entity entity)
        {
            return new Point3d(0, 0, 0);

            // Type entityType = entity.GetType();
            // string entityTypeName = entityType.Name;

            //// 2. 尝试通过反射获取常见的位置属性（Position 或 Location）
            // PropertyInfo positionProp = entityType.GetProperty("Position", typeof(Point3d));
            // if(positionProp != null && positionProp.CanRead) {
            // return (Point3d)positionProp.GetValue(entity);
            // }

            // PropertyInfo locationProp = entityType.GetProperty("Location", typeof(Point3d));
            // if(locationProp != null && locationProp.CanRead) {
            // return (Point3d)locationProp.GetValue(entity);
            // }

            //// 3. 反射获取失败，使用实体的边界框中心作为最终 fallback（通用性最强）
            // try {
            // Extents3d extents = entity.GeometricExtents;
            // return new Point3d(
            // (extents.MinPoint.X + extents.MaxPoint.X) / 2,
            // (extents.MinPoint.Y + extents.MaxPoint.Y) / 2,
            // extents.MinPoint.Z // 保留 Z 坐标（2D 实体 Z 通常为 0）
            // );
            // }
            // catch(Exception ex) {
            // // 极端情况：边界框获取失败（理论极少出现），抛出明确异常
            // throw new NotSupportedException(
            // $"实体类型 {entityTypeName} 不支持 Position/Location 属性，且无法获取边界框。",
            // ex
            // );
            // }
        }
        /*
         * 计算原始实体到中心点的距离（以实体的基点为例，可根据需求调整）
         * 环形阵列调用
         */

        public static double GetEntityDistanceToCenter(this Entity entity, Point3d center)
        {
            Point3d entityBasePoint; // 实体的基准点（根据类型动态获取）

            entityBasePoint = entity.GetEntityPosition();

            // 2. 计算基准点到环形中心点的距离 根号下x^2+y^2
            return Math.Sqrt(
                Math.Pow(entityBasePoint.X - center.X, 2) +
                Math.Pow(entityBasePoint.Y - center.Y, 2)
            );
        }


        /*
         * 自定义方法：获取实体自身的朝向角度（如直线的角度、文字的旋转角）
         * 用于环形阵列
         */
        public static double GetOrientationAngle(this Entity entity)
        {
            switch(entity) {
                case DBText text:
                    return text.Rotation * 180 / Math.PI; // 文字旋转角（弧度转角度）
                case Line line:
                    Vector3d lineVec = line.EndPoint - line.StartPoint;
                    return Math.Atan2(lineVec.Y, lineVec.X) * 180 / Math.PI; // 直线方向角
                case Circle circle:
                    return 0; // 圆形无朝向，返回0
                case BlockReference block:
                    return block.Rotation * 180 / Math.PI; // 块旋转角
                // 其他实体类型可根据需求扩展
                default:
                    return 0; // 默认朝向0°
            }
        }

        /*
         * 将point3d转换为point2d
         */
        public static Point2d ToPoint2d(this Point3d point)
        {
            return new Point2d(point.X, point.Y);
        }


        /// <summary>
        /// 使用弃用的IntersectWith多参数重载计算射线与圆的交点（多个交点取Y最小）;  注意计算时，会将射线反向无限延长
        /// </summary>
        /// <param name="ray">无限射线对象</param>
        /// <param name="circle">圆对象</param>
        /// <returns>符合要求的交点</returns>
        /// <exception cref="ArgumentNullException">参数为空时抛出</exception>
        /// <exception cref="Exception">无交点时抛出</exception>
        public static Point3d GetIntersectionBetween_Ray_Circle(Ray ray, Circle circle)
        {
            // 1. 参数校验
            if(ray == null) {
                throw new ArgumentNullException(nameof(ray), "射线对象不能为空！");
            }

            if(circle == null) {
                throw new ArgumentNullException(nameof(circle), "圆对象不能为空！");
            }

            // 2. 初始化交点集合
            Point3dCollection intersectPoints = new Point3dCollection();

            // 3. 调用IntersectWith多参数重载（核心）
            // 参数说明：
            // 参数1：待求交的对象（圆）
            // 参数2：求交延伸模式（ExtendBoth=延伸两者至相交）
            // 参数3：输出交点集合
            // 参数4：交点排序方式（0=无排序）
            // 参数5：求交公差（0=精确求交）
            ray.IntersectWith(
            circle,
            Intersect.ExtendBoth,
            intersectPoints,
            0,
            0
            );

            // 4. 处理交点结果
            if(intersectPoints.Count == 0) {
                throw new System.Exception($"射线（起点：{ray.BasePoint}）与圆（圆心：{circle.Center}，半径：{circle.Radius}）无交点！");
            }
            else if(intersectPoints.Count == 1) {
                // 只有1个交点（相切），直接返回
                return intersectPoints[0];
            }
            else {
                // 多个交点：转换为可枚举集合，按Y坐标升序排序，取第一个（Y最小）
                return intersectPoints.Cast<Point3d>().OrderBy(p => p.Y).First();
            }
        }


        /// <summary>
        /// 求射线与圆的交点，并支持自定义排序策略
        /// </summary>
        /// <param name="ray">射线对象</param>
        /// <param name="circle">圆对象</param>
        /// <param name="comparer">排序比较器（可选，默认按Y坐标升序）</param>
        /// <returns>排序后的交点数组</returns>
        /// <exception cref="ArgumentNullException">射线/圆为空时抛出</exception>
        /// <exception cref="Exception">无交点时抛出</exception>
        public static Point3d[] GetIntersectionsBetween_Ray_Circle(
            Ray ray,
            Circle circle,
            IComparer<Point3d>? comparer = null // 排序策略接口（可选参数）
        )
        {
            // 1. 参数校验
            if(ray == null) {
                throw new ArgumentNullException(nameof(ray), "射线对象不能为空！");
            }

            if(circle == null) {
                throw new ArgumentNullException(nameof(circle), "圆对象不能为空！");
            }

            // 2. 初始化交点集合
            Point3dCollection intersectPoints = new Point3dCollection();

            // 3. 调用求交方法
            ray.IntersectWith(
                circle,
                Intersect.ExtendBoth,
                intersectPoints,
                0,
                0
            );

            // 4. 处理无交点情况
            if(intersectPoints.Count == 0) {
                throw new System.Exception($"射线（起点：{ray.BasePoint}）与圆（圆心：{circle.Center}，半径：{circle.Radius}）无交点！");
            }

            // 5. 转换为可枚举集合
            var points = intersectPoints.Cast<Point3d>();

            // 6. 应用排序策略（默认按Y升序，否则用传入的比较器）
            var sortedPoints = comparer == null
                ? points.OrderBy(p => p.Y) // 默认策略：按Y坐标升序
                : points.OrderBy(p => p, comparer); // 自定义排序策略

            // 7. 转换为数组并返回
            return sortedPoints.ToArray();
        }

        // ===================== 可选：预设常用的排序比较器（方便调用） =====================
        /// <summary>
        /// 按X坐标升序的比较器
        /// </summary>
        public static readonly IComparer<Point3d> OrderByX = Comparer<Point3d>.Create((a, b) => a.X.CompareTo(b.X));


        /// <summary>
        /// 按X坐标降序的比较器
        /// </summary>
        public static readonly IComparer<Point3d> OrderByXDesc = Comparer<Point3d>.Create((a, b) => b.X.CompareTo(a.X));

        /// <summary>
        /// 按Y坐标降序的比较器
        /// </summary>
        public static readonly IComparer<Point3d> OrderByYDesc = Comparer<Point3d>.Create((a, b) => b.Y.CompareTo(a.Y));

        /// <summary>
        /// 按到射线基点的距离升序的比较器
        /// </summary>
        public static IComparer<Point3d> OrderByDistanceToRayBase(Ray ray)
        {
            return Comparer<Point3d>.Create((a, b) =>
                {
                    double distA = a.DistanceTo(ray.BasePoint);
                    double distB = b.DistanceTo(ray.BasePoint);
                    return distA.CompareTo(distB);
                });
        }

        #region 基于参考圆弧和直线，通过相切相切半径的方式计算圆弧，并直接返回圆弧和直线上的切点


        private const double Tolerance = 1e-6;

        /// <summary>
        /// 计算圆角弧与参考圆弧和参考直线的切点
        /// </summary>
        /// <param name="refArc">参考圆弧（圆心必须在原点0,0）</param>
        /// <param name="refLine">参考直线</param>
        /// <param name="filletRadius">圆角半径</param>
        /// <returns>Point3d[2] - [0]圆弧上的切点, [1]直线上的切点; 若无解返回null</returns>
        public static Point3d[] GetFilletTangentPoints(Arc refArc, Line refLine, double filletRadius)
        {
            // 1. 基础验证
            if(refArc == null || refLine == null || filletRadius <= Tolerance) {
                return null;
            }

            // 验证圆心是否在原点（允许小误差）
            if(Math.Abs(refArc.Center.X) > Tolerance || Math.Abs(refArc.Center.Y) > Tolerance || Math.Abs(refArc.Center.Z) > Tolerance) {
                return null;
            }

            try {
                double arcRadius = refArc.Radius;

                // 2. 转换为2D坐标（直接取X,Y，Z=0）
                Point2d lineStart2d = new Point2d(refLine.StartPoint.X, refLine.StartPoint.Y);
                Point2d lineEnd2d = new Point2d(refLine.EndPoint.X, refLine.EndPoint.Y);
                Line2d refLine2d = new Line2d(lineStart2d, lineEnd2d);

                // 3. 创建圆心轨迹圆：半径为 arcRadius + filletRadius，圆心在原点
                double centerDistance = arcRadius + filletRadius;

                // 创建完整圆：使用三点构造函数
                Point2d pointOnCircle = new Point2d(centerDistance, 0); // 圆上的点（正X轴方向）
                Point2d origin2d = new Point2d(0, 0);
                CircularArc2d centerLocusCircle = new CircularArc2d(origin2d, centerDistance, 0, 2 * Math.PI, new Vector2d(1, 0), true);

                // 4. 创建直线的等距线（向上偏移）
                Point2d offsetStartPoint = new Point2d(refLine.StartPoint.X, refLine.StartPoint.Y + filletRadius);
                Point2d offsetEndPoint = new Point2d(refLine.EndPoint.X, refLine.EndPoint.Y + filletRadius);

                Line2d offsetLine2d = new Line2d(offsetStartPoint, offsetEndPoint);

                // 5. 求交点 -- 圆角圆心
                List<Point2d> intersectionPoints = new List<Point2d>();
                GetIntersectionPoints(offsetLine2d, centerLocusCircle, intersectionPoints);

                // 6. 筛选第一象限内的点（X>0, Y>0）
                var firstQuadrantPoints = intersectionPoints
                    .Where(p => p.X > Tolerance && p.Y > Tolerance)
                    .ToList();

                if(firstQuadrantPoints.Count == 0) {
                    return null;
                }

                // 7. 取第一个第一象限的点（通常是最合适的）
                Point2d bestCenter2d = firstQuadrantPoints.First();
                Point3d filletCenter3d = new Point3d(bestCenter2d.X, bestCenter2d.Y, 0);

                // 8. 计算切点
                Point3d arcTangentPoint = GetArcTangentPoint(arcRadius, filletCenter3d);
                Point3d lineTangentPoint = GetLineTangentPoint(refLine, filletCenter3d);

                // 9. 验证切点是否有效
                if(IsPointOnArc(refArc, arcTangentPoint) && IsPointOnLine(refLine, lineTangentPoint)) {
                    return new Point3d[] { filletCenter3d, arcTangentPoint, lineTangentPoint };
                }

                return null;
            }
            catch(System.Exception ex) {
                Debug.WriteLine($"计算错误: {ex.Message}");
                return null;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 直线向上偏移（针对第一象限）
        /// </summary>
        private static Line2d OffsetLineUpward(Line2d line, double distance)
        {
            // 获取直线的方向向量
            Vector2d dir = (line.EndPoint - line.StartPoint).GetNormal();

            // 在Z=0平面中，向上法向量 = (-Y, X)
            Vector2d up = new Vector2d(-dir.Y, dir.X).GetNormal();

            // 偏移
            return new Line2d(
                line.StartPoint + up * distance,
                line.EndPoint + up * distance
            );
        }

        /// <summary>
        /// 获取两条曲线的交点
        /// </summary>
        private static void GetIntersectionPoints(Curve2d curve1, Curve2d curve2, List<Point2d> points)
        {
            if(curve1 == null || curve2 == null) {
                return;
            }

            try {
                CurveCurveIntersector2d intersector = new CurveCurveIntersector2d(
                    curve1, curve2, new Tolerance(Tolerance, Tolerance));

                int pointCount = intersector.NumberOfIntersectionPoints;

                for(int i = 0; i < pointCount; i++) {
                    Point2d intersectionPoint = intersector.GetIntersectionPoint(i);

                    // 去重
                    if(!points.Any(p => p.GetDistanceTo(intersectionPoint) < Tolerance)) {
                        points.Add(intersectionPoint);
                    }
                }
            }
            catch {
                // 忽略异常
            }
        }

        /// <summary>
        /// 获取圆弧上的切点（圆心在原点）
        /// </summary>
        private static Point3d GetArcTangentPoint(double arcRadius, Point3d filletCenter)
        {
            // 从原点指向圆角圆心的方向
            Vector3d direction = new Vector3d(filletCenter.X, filletCenter.Y, 0).GetNormal();

            // 切点 = 方向 * 圆弧半径
            return new Point3d(
                direction.X * arcRadius,
                direction.Y * arcRadius,
                0
            );
        }

        /// <summary>
        /// 获取直线上的切点
        /// </summary>
        private static Point3d GetLineTangentPoint(Line line, Point3d filletCenter)
        {
            return line.GetClosestPointTo(filletCenter, false);
        }

        /// <summary>
        /// 验证点是否在圆弧上
        /// </summary>
        private static bool IsPointOnArc(Arc arc, Point3d point)
        {
            // 检查距离
            double distToCenter = point.DistanceTo(arc.Center);
            if(Math.Abs(distToCenter - arc.Radius) > Tolerance) {
                return false;
            }

            // 检查角度范围
            Vector3d dir = point - arc.Center;
            Plane plane = new Plane(Point3d.Origin, arc.Normal);
            double angle = dir.AngleOnPlane(plane);

            double startAngle = arc.StartAngle;
            double endAngle = arc.EndAngle;

            if(startAngle <= endAngle) {
                return angle >= startAngle - Tolerance && angle <= endAngle + Tolerance;
            }
            else {
                return angle >= startAngle - Tolerance || angle <= endAngle + Tolerance;
            }
        }

        /// <summary>
        /// 验证点是否在直线上
        /// </summary>
        private static bool IsPointOnLine(Line line, Point3d point)
        {
            Point3d closest = line.GetClosestPointTo(point, false);
            return closest.DistanceTo(point) < Tolerance;
        }
        #endregion

        #endregion
    }

}


