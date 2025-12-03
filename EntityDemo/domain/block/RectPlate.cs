using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block
{

    public class RectPlate : Rectangle
    {

        /// <summary>
        /// 板厚
        /// </summary>
        public int thick { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int count { get; set; }

    }

}
