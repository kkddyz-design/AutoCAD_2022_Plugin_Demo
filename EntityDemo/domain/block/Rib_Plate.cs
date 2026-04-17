using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity;
using AutoCAD_2022_Plugin_Demo.tools;
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

        public string Tag { get; set; }

        public double ButtomMagrin { get; set; }


        public string Material { get; set; }


        public DBText CountText { get; set; }

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
            int ribPlateThick,
            double buttomMagrin,
            double upperDistance,
            int cnt,
            double textHeight,
            double textMargin
        )
        {
            Tag = tag;
            BlockCount = cnt;
            ButtomMagrin = buttomMagrin;
            Material = material;
            Rib_Plate_OD = OD;
            Rib_Plate_H = H;
            BlockThick = ribPlateThick;
            ButtomMagrin = buttomMagrin;

            // 定义块名
            // 避免空指针异常
            DBText countText = new DBText();
            CountText = countText;
            SetBlockNameAndCount(cnt);

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
            // 从底板规格提取底板厚度和底板宽度

            // 定义分割符：δ、*、=（三个分隔符一起分割）
            char[] separators = { 'δ', '*', '=' };

            // 分割字符串，同时去除空字符串
            string[] rectParas = rectSpce.Split(separators, StringSplitOptions.RemoveEmptyEntries);

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
            Point3d sideLineEnd = GeometryTools.GetIntersectionBetween_Ray_Circle(sideRay, offset_circle);

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

            countText.TextString = $"+={cnt}";

            countText.HorizontalMode = TextHorizontalMode.TextLeft;     // 左对齐
            countText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            countText.Height = textHeight / 2;
            countText.AlignmentPoint = new Point3d(h_line_end.X - textMargin * 2, BlockPosition.Y + Rib_Plate_H / 2, 0);

            // 创建文本-BlockThick
            DBText thickText = new DBText();
            thickText.TextString = $"*={ribPlateThick}";

            thickText.HorizontalMode = TextHorizontalMode.TextLeft;     // 左对齐
            thickText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            thickText.Height = textHeight / 2;
            thickText.AlignmentPoint = new Point3d(h_line_end.X - textMargin * 2, BlockPosition.Y + Rib_Plate_H / 2 + textHeight / 2 * 1.6, 0); // 行间距为文字高度1.6倍

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


        public void SetBlockNameAndCount(int count)
        {
            BlockCount = count;

            string ODPreText = "管径";
            if(Rib_Plate_OD < 100) {
                ODPreText = "管径0";
            }

            string HPreText = "H";
            if(Rib_Plate_H < 100) {
                HPreText = "H0";
            }

            BlockName = $"肋板_{Material}_{ODPreText}{Rib_Plate_OD}_{HPreText}{Rib_Plate_H}_厚{BlockThick}_边距{ButtomMagrin}_{BlockCount}个";

            // 修改内容
            CountText.TextString = $"+={BlockCount}";
        }

    }

}
