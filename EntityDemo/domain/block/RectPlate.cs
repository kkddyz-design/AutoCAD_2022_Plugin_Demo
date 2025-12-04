using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block
{

    public class RectPlate
    {

        public string blockName { get; set; }

        /// <summary>
        /// 板厚
        /// </summary>
        public int thick { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int count { get; set; }

        /// <summary>
        /// 块定义
        /// </summary>
        public List<Entity> entityList = new List<Entity>();


        /// <summary>
        /// 后续传入一个TextStyle指定边距,颜色,高度
        /// </summary>
        /// <param name="positon"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="thick"></param>
        /// <param name="count"></param>
        /// <param name="remark">标注</param>
        /// 整体分布:左下规格 居中 数量 右上 remark 
        public RectPlate(double width, double height, int thick, int count, string remarkStr)
        {
            double textHeight = 15;
            double textMargin = 10;

            // 当height<100,按比例缩放字体大小
            FormatTools.ScaleTextHeightAndMargin(height, width, ref textHeight, ref textMargin);

            // 确保width >= height
            if(width < height) {
                exchange(ref width, ref height);
            }

            if(remarkStr.Equals(string.Empty)) {
                // 生成blockName 用RectPlate_width_height_thick_count命名块
                blockName = $"Rectplate_{height}_{width}_{thick}_{count}";
            }
            else {
                // 生成blockName 用RectPlate_width_height_thick_count命名块
                blockName = $"Rectplate_{height}_{width}_{thick}_{count}_{remarkStr}";
            }

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
            DBText remarkText = new DBText();
            remarkText.TextString = remarkStr;
            remarkText.Height = textHeight;

            remarkText.HorizontalMode = TextHorizontalMode.TextRight;    // 水平右对齐
            remarkText.VerticalMode = TextVerticalMode.TextTop;          // 垂直上对齐
            Point3d args = new Point3d(width - textMargin, height - textMargin, 0);
            remarkText.AlignmentPoint = new Point3d(width - textMargin, height - textMargin, 0);

            // 设置颜色为洋红（索引6）
            remarkText.Color = Color.FromColorIndex(ColorMethod.ByAci, 6);

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
