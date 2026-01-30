using Autodesk.AutoCAD.DatabaseServices;
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

    }

}
