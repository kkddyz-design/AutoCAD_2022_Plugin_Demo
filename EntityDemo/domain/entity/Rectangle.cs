using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity
{

    /// <summary>
    /// 矩形实体类（继承自 Polyline，明确自身类型标识）
    /// </summary>
    public class Rectangle : Polyline
    {

        #region 独有字段（核心：区分 Rectangle 与 Polyline）
        // 用私有字段存储关键属性，避免每次通过父类顶点计算（提升性能+强化类型标识）
        private Point3d _lowerLeftCorner;
        private double _width;
        private double _height;
        #endregion

        #region 构造函数（优化：前置校验+清空父类+显式初始化）
        /// <summary>
        /// 基础构造函数（私有，禁止外部直接调用，确保必须通过带参构造初始化）
        /// </summary>
        private Rectangle() : base()
        {
        }

        /// <summary>
        /// 使用左下角点、宽度和高度创建矩形
        /// </summary>
        /// <param name="lowerLeftCorner">左下角点（3D坐标）</param>
        /// <param name="width">宽度（必须>0）</param>
        /// <param name="height">高度（必须>0）</param>
        public Rectangle(Point3d lowerLeftCorner, double width, double height) : this()
        {
            // 关键1：前置参数校验（无效值直接抛异常，避免后续逻辑浪费）
            ValidateParameters(lowerLeftCorner, width, height);

            // 关键2：初始化独有字段（强化类型标识，区分于 Polyline）
            _lowerLeftCorner = lowerLeftCorner;
            _width = width;
            _height = height;

            // 构建矩形顶点（确保顺序正确：左下→右下→右上→左上）
            BuildRectangleVertices();
        }

        /// <summary>
        /// 使用两个对角点创建矩形
        /// </summary>
        /// <param name="corner1">第一个对角点</param>
        /// <param name="corner2">第二个对角点</param>
        public Rectangle(Point3d corner1, Point3d corner2) : this()
        {
            // 校验对角点有效性
            if(double.IsNaN(corner1.X) || double.IsNaN(corner1.Y) || double.IsNaN(corner2.X) || double.IsNaN(corner2.Y)) {
                throw new ArgumentException("对角点坐标必须是有效数值！");
            }

            // 计算标准化的左下/右上点（确保顺序正确）
            double minX = Math.Min(corner1.X, corner2.X);
            double minY = Math.Min(corner1.Y, corner2.Y);
            double maxX = Math.Max(corner1.X, corner2.X);
            double maxY = Math.Max(corner1.Y, corner2.Y);
            double z = corner1.Z; // 统一Z坐标（取第一个点的Z）

            // 计算宽度和高度
            double width = maxX - minX;
            double height = maxY - minY;

            // 校验宽高有效性
            ValidateParameters(new Point3d(minX, minY, z), width, height);

            // 初始化独有字段
            _lowerLeftCorner = new Point3d(minX, minY, z);
            _width = width;
            _height = height;

            // 构建矩形
            BuildRectangleVertices();
        }
        #endregion

        #region 公共属性（优化：基于独有字段实现，性能更高+逻辑更稳）
        /// <summary>
        /// 获取矩形的左下角点（只读，通过 Width/Height 间接修改）
        /// </summary>
        public Point3d LowerLeftCorner
        {
            get => _lowerLeftCorner;
            private set => _lowerLeftCorner = value;
        }

        /// <summary>
        /// 获取矩形的右上角点（基于独有字段计算，避免依赖父类顶点）
        /// </summary>
        public Point3d UpperRightCorner
        {
            get => new Point3d(
                _lowerLeftCorner.X + _width,
                _lowerLeftCorner.Y + _height,
                _lowerLeftCorner.Z
            );
        }

        /// <summary>
        /// 获取或设置矩形的宽度（修改时同步更新父类顶点）
        /// </summary>
        public double Width
        {
            get => _width;
            set
            {
                if(value <= 0) {
                    throw new ArgumentOutOfRangeException(nameof(value), "宽度必须为正数！");
                }

                _width = value;
                UpdatePolylineVertices(); // 同步更新父类 Polyline 的顶点
            }
        }

        /// <summary>
        /// 获取或设置矩形的高度（修改时同步更新父类顶点）
        /// </summary>
        public double Height
        {
            get => _height;
            set
            {
                if(value <= 0) {
                    throw new ArgumentOutOfRangeException(nameof(value), "高度必须为正数！");
                }

                _height = value;
                UpdatePolylineVertices(); // 同步更新父类 Polyline 的顶点
            }
        }

        /// <summary>
        /// 获取矩形中心点（基于独有字段计算，高效且稳定）
        /// </summary>
        public Point3d Center
        {
            get => new Point3d(
                _lowerLeftCorner.X + _width / 2,
                _lowerLeftCorner.Y + _height / 2,
                _lowerLeftCorner.Z
            );
        }

        /// <summary>
        /// 矩形专有标识属性（彻底区分于 Polyline，调试/类型判断用）
        /// </summary>
        public bool IsRectangle => true;
        #endregion

        #region 公共方法
        /// <summary>
        /// 获取矩形中心点作为位置（重写/明确自身方法，避免和父类混淆）
        /// </summary>
        public Point3d GetPosition()
        {
            return Center;
        }

        /// <summary>
        /// 重写 ToString（直观识别类型，调试必备）
        /// </summary>
        public override string ToString()
        {
            return $"Rectangle [左下角=({_lowerLeftCorner.X:F2},{_lowerLeftCorner.Y:F2}), 宽={_width:F2}, 高={_height:F2}, 中心=({Center.X:F2},{Center.Y:F2})]";
        }

        /// <summary>
        /// 重写 Equals（可选，强化类型判断）
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Rectangle rectangle &&
                   _lowerLeftCorner.Equals(rectangle._lowerLeftCorner) &&
                   _width.Equals(rectangle._width) &&
                   _height.Equals(rectangle._height);
        }

        /// <summary>
        /// 重写 GetHashCode（配合 Equals 使用）
        /// </summary>
        public override int GetHashCode()
        {
            // 传统哈希值计算逻辑：初始值取一个质数（如 17），每个字段的哈希值乘以 31（质数）后累加
            int hash = 17; // 初始质数（减少哈希冲突）

            // 组合 _lowerLeftCorner 的哈希值（Point3d 自带 GetHashCode()）
            hash = hash * 31 + _lowerLeftCorner.GetHashCode();

            // 组合 _width 的哈希值（double 需用 BitConverter 转为 int，避免精度问题）
            hash = hash * 31 + BitConverter.DoubleToInt64Bits(_width).GetHashCode();

            // 组合 _height 的哈希值（同上）
            hash = hash * 31 + BitConverter.DoubleToInt64Bits(_height).GetHashCode();

            return hash;
        }
        #endregion

        #region 私有辅助方法（优化：代码复用+逻辑封装）
        /// <summary>
        /// 校验矩形参数有效性
        /// </summary>
        private void ValidateParameters(Point3d lowerLeft, double width, double height)
        {
            if(double.IsNaN(lowerLeft.X) || double.IsNaN(lowerLeft.Y) || double.IsInfinity(lowerLeft.X)) {
                throw new ArgumentException("左下角点坐标必须是有效数值！", nameof(lowerLeft));
            }

            if(width <= 0) {
                throw new ArgumentOutOfRangeException(nameof(width), "宽度必须为正数！");
            }

            if(height <= 0) {
                throw new ArgumentOutOfRangeException(nameof(height), "高度必须为正数！");
            }
        }

        /// <summary>
        /// 构建矩形顶点（初始化时调用）
        /// </summary>
        private void BuildRectangleVertices()
        {
            // 按顺序添加4个顶点（左下→右下→右上→左上）
            AddVertexAt(0, _lowerLeftCorner.ToPoint2d(), 0, 0, 0); // 左下
            AddVertexAt(1, new Point2d(_lowerLeftCorner.X + _width, _lowerLeftCorner.Y), 0, 0, 0); // 右下
            AddVertexAt(2, UpperRightCorner.ToPoint2d(), 0, 0, 0); // 右上
            AddVertexAt(3, new Point2d(_lowerLeftCorner.X, _lowerLeftCorner.Y + _height), 0, 0, 0); // 左上

            // 强制闭合+验证（确保是有效矩形）
            Closed = true;
            if(!Closed) {
                throw new InvalidOperationException("矩形创建失败：无法闭合！");
            }
        }

        /// <summary>
        /// 更新父类 Polyline 的顶点（Width/Height 修改时调用）
        /// </summary>
        private void UpdatePolylineVertices()
        {
            // 同步更新4个顶点
            SetPointAt(0, _lowerLeftCorner.ToPoint2d()); // 左下（不变）
            SetPointAt(1, new Point2d(_lowerLeftCorner.X + _width, _lowerLeftCorner.Y)); // 右下（X随宽度变）
            SetPointAt(2, UpperRightCorner.ToPoint2d()); // 右上（X/Y随宽高变）
            SetPointAt(3, new Point2d(_lowerLeftCorner.X, _lowerLeftCorner.Y + _height)); // 左上（Y随高度变）
        }
        #endregion

    }

}
