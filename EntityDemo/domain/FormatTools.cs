using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain
{

    public class FormatTools
    {

        /// <summary>
        /// 设置区域中文字的属性:边距,,高度
        /// </summary>
        public struct TextStyle
        {

            public double margin;
            public double textHeight;

        }

        /// <param name="textHeight"></param>
        /// <param name="textMargin"></param>
        /// 整体高度3*textHeight + 6*textMargin ,最低 100 .小于100,等比缩放
        /// 80~100,factor=0.8 ;  60~80 actor=0.6 ; 40~60 actor=0.4; 20~40 actor=0.2;
        public static void ScaleTextHeightAndMargin(double height, double width, ref double textHeight, ref double textMargin)
        {
            if(height < 40 && height >= 20) {
                textHeight *= 0.2;
                textMargin *= 0.2;
            }
            else if(height < 60 && height >= 40) {
                textHeight *= 0.4;
                textMargin *= 0.4;
            }
            else if(height < 80 && height >= 60) {
                textHeight *= 0.6;
                textMargin *= 0.6;
            }
            else if(height < 100 && height >= 80) {
                textHeight *= 0.8;
                textMargin *= 0.8;
            }
            else {
                // 暂不考虑放大
            }

            //// 缩放结束后当height=100时,显示100-100的长度大概是100
            //// 需要按照长度二次缩放 统一缩放倍数,否则height0.2(极端情况)
            // if(width <= 150) {
            // double factor = width / 150;
            // textHeight *= factor;
            // textMargin *= factor;
            // }
        }

    }

}
