using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity
{

    /// <summary>
    /// 一个继承自 Polyline 的多边形实体类。 它本质上是一个闭合的多段线。
    /// </summary>
    public class Polygon : Polyline
    {

        public Point3d polygonCenter
        {
            get;
        }

        #region 构造函数

        /// <summary>
        /// 构造正多边形（继承自 Polyline）
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">外接圆半径（必须大于0）</param>
        /// <param name="sideNum">边数（必须≥3）</param>
        /// <param name="startDegree">起始角度</param>
        public Polygon(Point3d center, double radius, int sideNum)
        {
            double startDegree;
            if(sideNum % 2 == 0) {
                startDegree = 180 / sideNum;
            }
            else {
                startDegree = 90;
            }

            // 1. 完善参数验证
            if(sideNum < 3) {
                throw new ArgumentOutOfRangeException(nameof(sideNum), "正多边形边数必须不小于3");
            }

            if(radius <= 0) {
                throw new ArgumentOutOfRangeException(nameof(radius), "正多边形外接圆半径必须大于0");
            }

            // 设置多边形中心
            polygonCenter = center;

            // 2. 处理起始角度（确保在0°~360°范围内，避免负数或超大值）
            startDegree = startDegree % 360; // 取模运算，将角度归一化
            if(startDegree < 0) {
                startDegree += 360; // 负数角度转为正数（如-90° → 270°）
            }

            // 3. 计算每个顶点的角度间隔（弧度）
            double angleStep = 2 * Math.PI / sideNum; // 360°/边数，即每个顶点的角度差

            // 4. 循环计算顶点并添加到当前 Polygon 实例（自身）
            for(int i = 0; i < sideNum; i++) {
                // 当前顶点的角度（起始角度 + i*间隔角度，转为弧度）
                double currentAngle = startDegree.DegreeToRadian() + i * angleStep;

                // 极坐标转笛卡尔坐标（基于中心点和半径）
                double x = center.X + radius * Math.Cos(currentAngle);
                double y = center.Y + radius * Math.Sin(currentAngle);

                // 直接添加顶点到当前 Polygon（继承自 Polyline，可直接调用 AddVertexAt）
                // 最后两个参数：bulge（凸度，0表示直线）、startWidth（起始宽度，0默认）、endWidth（结束宽度，0默认）
                AddVertexAt(i, new Point2d(x, y), 0, 0, 0);
            }

            // 5. 设置多边形为闭合（继承自 Polyline 的 Closed 属性）
            Closed = true;
        }


        #endregion

        #region 公共属性 (封装一些常用的几何计算)

        /// <summary>
        /// 获取多边形的面积。
        /// </summary>
        public new double Area
        {
            get
            {
                // Polyline 基类已经有 Area 属性，对于闭合多段线，它会返回正确的面积
                // 这里我们可以直接使用它，或者如果需要更复杂的计算，可以重写
                return base.Area;
            }
        }

        /// <summary>
        /// 获取多边形的周长。
        /// </summary>
        public double Perimeter
        {
            get
            {
                double perimeter = 0.0;
                Point3d previous = GetPointAtParameter(NumberOfVertices - 1);

                for(int i = 0; i < NumberOfVertices; i++) {
                    Point3d current = GetPointAtParameter(i);
                    perimeter += previous.DistanceTo(current);
                    previous = current;
                }
                return perimeter;
            }
        }

        /// <summary>
        /// 获取多边形的顶点集合。
        /// </summary>
        public Point3dCollection Vertices
        {
            get
            {
                Point3dCollection pnts = new Point3dCollection();
                for(int i = 0; i < NumberOfVertices; i++) {
                    pnts.Add(GetPointAtParameter(i));
                }
                return pnts;
            }
        }
        #endregion

        #region 公共方法

        /// <summary>
        /// 判断一个点是否在多边形内部或边界上。 使用射线法（Ray Casting Method）。
        /// </summary>
        /// <param name="point">要测试的点。</param>
        /// <returns>如果点在内部或边界上，返回 true；否则返回 false。</returns>
        /// 不知道有啥用,先留着
        public bool Contains(Point3d point)
        {
            // 首先检查点是否在实体的轴对齐边界框外，快速排除
            // GeometricExtents 是一个属性，不是方法，所以不需要加 ()
            Extents3d bbox = GeometricExtents;
            if(point.X < bbox.MinPoint.X || point.X > bbox.MaxPoint.X ||
                point.Y < bbox.MinPoint.Y || point.Y > bbox.MaxPoint.Y ||
                point.Z < bbox.MinPoint.Z || point.Z > bbox.MaxPoint.Z) {
                return false;
            }

            // 如果点在边界框内，再进行更精确的射线法判断
            bool inside = false;
            int n = NumberOfVertices;
            for(int i = 0, j = n - 1; i < n; j = i++) {
                Point3d pi = GetPointAtParameter(i);
                Point3d pj = GetPointAtParameter(j);

                // 检查点是否在边的端点上
                if(point.IsEqualTo(pi) || point.IsEqualTo(pj)) {
                    return true;
                }

                // 射线法核心逻辑（假设多边形在XY平面上）
                if(((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                    (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X)) {
                    inside = !inside;
                }
            }
            return inside;
        }
        #endregion

    }

}
