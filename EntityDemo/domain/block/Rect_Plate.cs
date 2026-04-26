using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity;
using AutoCAD_2022_Plugin_Demo.tools;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block
{

    public class Rect_Plate : AbstractBlock
    {

        public double Rect_Plate_H { get; set; }

        public double Rect_Plate_L { get; set; }

        public string Rect_Plate_Material { get; set; }

        public string Tag { get; set; }


        public double OD { get; set; }

        public DBText CountText { get; set; }


        /// <summary>
        /// 用于cmd创建带有info的rect对象,仅 输入double rectH, double rectL, int thick, int count
        /// </summary>
        public Rect_Plate(double rectH, double rectL, int thick, int count) : this(11451, rectH, rectL, thick, count, "碳钢")
        {
        }

        /// <summary>
        /// 矩形下料
        /// </summary>
        /// <param name="rectL">矩形宽度</param>
        /// <param name="rectH">矩形高度</param>
        /// <param name="thick">矩形厚度</param>
        /// <param name="count">数量</param>
        /// <param name="material">材质</param>
        public Rect_Plate(double od, double rectH, double rectL, int thick, int count, string material)
        {
            // 初始化私有变量
            OD = od;
            Rect_Plate_H = rectH;
            Rect_Plate_L = rectL;
            BlockThick = thick;
            BlockCount = count;
            Rect_Plate_Material = material;

            double textHeight = 20;
            double textMargin = 10;

            // 当height<100,按比例缩放字体大小
            FormatTools.ScaleTextHeightAndMargin(rectH, rectL, ref textHeight, ref textMargin);

            // 设置Tag
            Tag = $"方板_{material}_管径{od}_H{rectH}_L{rectL}_厚{thick}";

            // 设置count
            DBText countText = new DBText(); // 确保初始化设置时,countText不为空
            CountText = countText;
            SetBlockNameAndCount(BlockCount);

            // 创建矩形 
            Rectangle rectangle = new Rectangle(Point3d.Origin, rectL, rectH);

            // 创建文本"高-宽" 居中对齐 
            DBText height_width = new DBText();

            height_width.HorizontalMode = TextHorizontalMode.TextMid;     // 居中对齐
            height_width.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            height_width.TextString = $"{rectH}-{rectL}";
            height_width.Height = textHeight;
            height_width.AlignmentPoint = new Point3d(rectL / 2, textMargin * 2 + 10, 0);

            // 创建文本-BlockCount 居中对齐

            countText.TextString = $"+={count}";

            countText.HorizontalMode = TextHorizontalMode.TextLeft;     // 板厚数量左对齐
            countText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            countText.Height = textHeight / 2;
            countText.AlignmentPoint = new Point3d(rectL / 2 - textMargin, rectH / 2 + 10, 0);

            // 创建文本-BlockThick
            DBText thickText = new DBText();
            thickText.TextString = $"*={thick}";

            thickText.HorizontalMode = TextHorizontalMode.TextLeft;     // 居中对齐
            thickText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            thickText.Height = textHeight / 2;
            thickText.AlignmentPoint = new Point3d(rectL / 2 - textMargin, rectH / 2 + textHeight / 2 * 1.6 + 10, 0); // 行间距为文字高度1.6倍

            // 添加实体
            entityList.Add(rectangle);
            entityList.Add(height_width);
            entityList.Add(thickText);
            entityList.Add(countText);

            // 设置特殊材质颜色
            // 特殊材质-设置颜色
            if(!material.Equals("碳钢")) {
                entityList.SetEntityListColor(6);
            }

            // 插入点以左上角，但是定义的Rect对象是以左下角为基点创建对象的
            entityList.MoveEntity(new Point3d(0, rectH, 0), Point3d.Origin);
        }


        public void SetBlockNameAndCount(int count)
        {
            BlockCount = count;

            string ODPreText = "管径";
            if(OD < 100) {
                ODPreText = "管径0";
            }

            string HPreText = "H";
            if(Rect_Plate_H < 100) {
                HPreText = "H0";
            }

            BlockName = $"方板_{Rect_Plate_Material}_{ODPreText}{OD}_{HPreText}{Rect_Plate_H}_L{Rect_Plate_L}_厚{BlockThick}_{BlockCount}个";

            // 修改DBText
            CountText.TextString = $"+={BlockCount}";
        }

    }

}
