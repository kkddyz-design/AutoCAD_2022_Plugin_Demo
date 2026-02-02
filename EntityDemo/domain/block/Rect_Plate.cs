using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block
{

    public class Rect_Plate : AbstractBlock
    {

        /// <summary>
        /// 矩形下料
        /// </summary>
        /// <param name="width">矩形宽度</param>
        /// <param name="height">矩形高度</param>
        /// <param name="thick">矩形厚度</param>
        /// <param name="count">数量</param>
        /// <param name="material">材质</param>
        public Rect_Plate(double width, double height, int thick, int count, string material)
        {
            double textHeight = 15;
            double textMargin = 10;

            // 当height<100,按比例缩放字体大小
            FormatTools.ScaleTextHeightAndMargin(height, width, ref textHeight, ref textMargin);

            // 确保width >= height
            if(width < height) {
                exchange(ref width, ref height);
            }

            blockName = $"Rectplate_{height}_{width}_{thick}_{count}_{material}";

            // 创建矩形 
            Rectangle rectangle = new Rectangle(Point3d.Origin, width, height);

            // 创建文本"高-宽" 左对齐 
            DBText height_width = new DBText();
            height_width.TextString = $"{height}-{width}";
            height_width.Position = new Point3d(textMargin, textMargin, 0);
            height_width.Height = textHeight;

            // 创建文本-count 居中对齐
            DBText countText = new DBText();
            countText.TextString = $"+={count}";

            countText.HorizontalMode = TextHorizontalMode.TextLeft;     // 水平左对齐
            countText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            countText.Height = textHeight / 2;
            countText.AlignmentPoint = new Point3d(textMargin * 2, height / 2, 0);

            // 创建文本-thick
            DBText thickText = new DBText();
            thickText.TextString = $"*={thick}";

            thickText.HorizontalMode = TextHorizontalMode.TextLeft;     // 水平左对齐
            thickText.VerticalMode = TextVerticalMode.TextVerticalMid;  // 垂直居中
            thickText.Height = textHeight / 2;
            thickText.AlignmentPoint = new Point3d(textMargin * 2, height / 2 + textHeight / 2 * 1.6, 0); // 行间距为文字高度1.6倍

            // 创建文本 remark 
            // DBText remarkText = new DBText();
            // remarkText.TextString = remarkStr;
            // remarkText.Height = textHeight;

            // remarkText.HorizontalMode = TextHorizontalMode.TextRight;    // 水平右对齐
            // remarkText.VerticalMode = TextVerticalMode.TextTop;          // 垂直上对齐
            // Point3d args = new Point3d(width - textMargin, height - textMargin, 0);
            // remarkText.AlignmentPoint = new Point3d(width - textMargin, height - textMargin, 0);

            // 添加实体
            entityList.Add(rectangle);
            entityList.Add(height_width);
            entityList.Add(thickText);
            entityList.Add(countText);
            entityList.Add(remarkText);
        }

        private static void exchange(ref double a, ref double b)
        {
            double temp;
            temp = a;
            a = b;
            b = temp;
        }

    }

}
