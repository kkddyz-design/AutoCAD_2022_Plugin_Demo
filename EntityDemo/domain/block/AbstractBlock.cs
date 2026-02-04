using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block
{

    /// <summary>
    /// 作所有块对象的父类。
    /// </summary>
    public class AbstractBlock

    {

        /// <summary>
        /// 块名
        /// </summary>
        public string BlockName { get; set; }

        /// <summary>
        /// 板厚
        /// </summary>
        public int BlockThick { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int BlockCount { get; set; }

        public Point3d BlockPosition { get; set; }

        /// <summary>
        /// 块定义
        /// </summary>
        public List<Entity> entityList = new List<Entity>();

    }

}
