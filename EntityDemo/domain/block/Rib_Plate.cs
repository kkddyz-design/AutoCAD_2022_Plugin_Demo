using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block
{

    public class Rib_Plate : AbstractBlock
    {

        /// <summary>
        /// Rib_Plate_H,Rib_Plate_L 用于计算排序
        /// </summary>
        public double Rib_Plate_H { get; set; }

        public double Rib_Plate_L { get; set; }

        public double Rib_Plate_OD { get; set; }


        /// <summary>
        /// 创建Rib_Plate(肋板)的块定义
        /// </summary>
        /// <param name="tag">89-100-碳钢</param>
        /// <param name="OD">89</param>
        /// <param name="H">100</param>
        /// <param name="material">碳钢</param>
        /// <param name="offset">肋板/管夹厚度</param>
        /// <param name="rectSpce">底板矩形</param>
        /// <param name="ribPlateThick">肋板厚</param>
        /// <param name="buttomMagrin">肋板侧边距</param>
        /// <param name="upperDistance">上中心距; "-"表示侧边垂直,该参数省略</param>
        /// <param name="cnt">肋板分类汇总数量</param>
        public Rib_Plate(
            string tag,
            double OD,
            double H,
            string material,
            double offset,
            string rectSpce,
            double ribPlateThick,
            double buttomMagrin,
            double upperDistance,
            double cnt,
            double textHeight,
            double textMargin
        )
        {
            // 定义块名
            BlockName = $"Rib_Plate_{OD}_{H}_{material}_{ribPlateThick}_{cnt}_{buttomMagrin}_{upperDistance}";

            // 创建OD圆
            Circle od_circle = new Circle(Point3d.Origin, new Vector3d(0, 0, 1), OD / 2);

            // 创建偏移圆
            Circle offset_circle = new Circle(Point3d.Origin, new Vector3d(0, 0, 1), OD / 2 + offset);

            // 创建H直线
            Point3d h_line_start = new Point3d(0, -OD / 2, 0);
            Point3d h_line_end = new Point3d(0, -OD / 2 - H, 0);
            Line h_line = new Line(h_line_start, h_line_end);

            // 创建底板矩形
            // 获取底板矩形长宽 -- 数据源确保是H*L的顺序
            string[] rectParas = rectSpce.Split(new char[] { '*' });
            double rectH = int.Parse(rectParas[0]);
            double rectL = int.Parse(rectParas[1]);
            Rectangle bRect = new Rectangle(new Point3d(-rectL / 2, h_line_end.Y, 0), rectL, rectH);

            // 创建肋板侧边线

            // 侧边线起点
            double sideLineStart_x = rectL / 2 - buttomMagrin;
            double sideLineStart_y = bRect.LowerLeftCorner.Y + rectH;
            Point3d sideLineStart = new Point3d(sideLineStart_x, sideLineStart_y, 0);

            // 通过几何求交得到侧边线终点

            Point3d sideRayStart = new Point3d(h_line_end.X + upperDistance, h_line_end.Y, 0);

            // 构造垂直向上的射线 -- 用中心距
            Ray sideRay = new Ray();
            sideRay.BasePoint = sideRayStart;
            sideRay.UnitDir = new Vector3d(0, 1, 0).GetNormal(); // 垂直向上方向

            // 计算交点作为sideLineEnd
            Point3d sideLineEnd = GetIntersectionBetween_Ray_Circle(sideRay, offset_circle);

            // 创建左侧肋板边线
            Line sideLine = new Line(sideLineStart, sideLineEnd);

            // 镜像获得右侧肋板边线
            Line mirrored_sideLine = (Line)sideLine.MirrorEntity(h_line_start, h_line_end)[0];

            // 以两个侧边线的起点作为肋板底线
            Line ribPlateButtomLine = new Line(sideLine.StartPoint, mirrored_sideLine.StartPoint);

            // mirrored_sideLine.StartPoint即肋板左下角作为基准点
            Rib_Plate_L = rectL;
            Rib_Plate_H = H;
            Rib_Plate_OD = OD;
            BlockPosition = mirrored_sideLine.StartPoint;

            // 创建文本-BlockCount 居中对齐

            DBText countText = new DBText();
            countText.TextString = $"+={cnt}";

            countText.HorizontalMode = TextHorizontalMode.TextMid;     // 水平居中
            countText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            countText.Height = textHeight / 2;
            countText.AlignmentPoint = new Point3d(h_line_end.X, BlockPosition.Y + Rib_Plate_H / 2, 0);

            // 创建文本-BlockThick
            DBText thickText = new DBText();
            thickText.TextString = $"*={ribPlateThick}";

            thickText.HorizontalMode = TextHorizontalMode.TextMid;     // 水平居中
            thickText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            thickText.Height = textHeight / 2;
            thickText.AlignmentPoint = new Point3d(h_line_end.X, BlockPosition.Y + Rib_Plate_H / 2 + textHeight / 2 * 1.6, 0); // 行间距为文字高度1.6倍

            // 创建文本-H
            DBText HText = new DBText();
            HText.TextString = $"H{H}";
            HText.HorizontalMode = TextHorizontalMode.TextMid;     // 水平居中
            HText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            HText.Height = textHeight;

            HText.AlignmentPoint = new Point3d(h_line_end.X, h_line_end.Y + rectH + textHeight * 1.6, 0);

            // 创建文本-OD
            DBText ODText = new DBText();
            ODText.TextString = $"{OD}";
            ODText.HorizontalMode = TextHorizontalMode.TextMid;     // 水平居中
            ODText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            ODText.Height = textHeight;

            // 行间距为文字高度1.6倍
            ODText.AlignmentPoint = new Point3d(HText.AlignmentPoint.X, HText.AlignmentPoint.Y + textHeight * 1.6, 0);

            // 绘制肋板圆弧

            // CircularArc3d通过三点计算圆弧的圆心,半径,起始终止弧度
            Point3d midPoint = new Point3d(0, -OD / 2 - offset, 0);

            CircularArc3d cArc = new CircularArc3d(mirrored_sideLine.EndPoint, midPoint, sideLine.EndPoint);

            // 重新计算角度
            double startAngel = GeometryTools.DegreeToRadian(GeometryTools.GetAngleToXAxis(Point3d.Origin, mirrored_sideLine.EndPoint));
            double endAngel = GeometryTools.DegreeToRadian(GeometryTools.GetAngleToXAxis(Point3d.Origin, sideLine.EndPoint));

            Arc ribPlate_arc = new Arc(cArc.Center, cArc.Radius, startAngel, endAngel);

            // 加入entitiess数组
            entityList.Add(sideLine);
            entityList.Add(mirrored_sideLine);
            entityList.Add(ribPlateButtomLine);
            entityList.Add(ribPlate_arc);

            entityList.Add(countText);
            entityList.Add(thickText);
            entityList.Add(ODText);
            entityList.Add(HText);

            // 坐标平移 以mirrored_sideLine.startPoint作为原点
            entityList.MoveEntity(mirrored_sideLine.StartPoint, Point3d.Origin);

            // 特殊材质-设置颜色
            if(!material.Equals("碳钢")) {
                entityList.SetEntityListColor(6);
            }
        }


        /// <summary>
        /// 使用弃用的IntersectWith多参数重载计算射线与圆的交点（多个交点取Y最小）
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
                throw new Exception($"射线（起点：{ray.BasePoint}）与圆（圆心：{circle.Center}，半径：{circle.Radius}）无交点！");
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

    }

}
