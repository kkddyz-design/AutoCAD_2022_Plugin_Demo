using AutoCAD_2022_Plugin_Demo.EntityDemo.modify;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;

[assembly: CommandClass(typeof(ModifyEntityDemo))]


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.modify
{

    public static class ModifyEntityDemo
    {

        private static Document doc = Application.DocumentManager.MdiActiveDocument; //获取当前激活的绘图窗口（文档）
        private static Database db = doc.Database; // 图形数据库对象

    }

}
