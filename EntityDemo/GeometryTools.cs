/*
 * 提供几何相关工具方法
 */
using System;

namespace AutoCAD_2022_Plugin_Demo.DrawDemo
{
    public static class GeometryTools
    {
        // 扩展Double 实现角度转换弧度 
        public static double DegreeToAngle(this Double degree)
        {
            return degree * Math.PI / 180;
        }
        // 扩展Double 实现弧度转换角度
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
        }
    }
}
