using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.tools.layout
{

    /// <summary>
    /// 给方板使用布局类
    /// </summary>
    public static class RectPlateLayout
    {

        /// <summary>
        /// 垂直布局：从上往下排列，自动设置 BlockPosition
        /// </summary>
        /// <param name="plates">板件列表</param>
        /// <param name="startPoint">起始点（默认 0,0,0）</param>
        /// <param name="spacing">间距，默认30</param>
        public static void VerticalLayoutTopToBottom(
            List<Rect_Plate> plates,
            Point3d startPoint = default,
            double spacing = 30)
        {
            if(plates == null || plates.Count == 0) {
                return;
            }

            // 默认起点 (0,0,0)
            if(startPoint == default) {
                startPoint = new Point3d(0, 0, 0);
            }

            double currentY = startPoint.Y;

            foreach(var plate in plates) {
                // 设置当前板的位置（X不变，Y从上往下）
                plate.BlockPosition = new Point3d(
                    startPoint.X,
                    currentY,
                    0
                );

                // 向下移动：当前高度 + 间距
                currentY -= (plate.Rect_Plate_H + spacing);
            }
        }

    }

}
