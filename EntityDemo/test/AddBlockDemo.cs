using AutoCAD_2022_Plugin_Demo.EntityDemo.service;
using AutoCAD_2022_Plugin_Demo.EntityDemo.test;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;

[assembly: CommandClass(typeof(AddBlockDemo))]


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.test
{

    public static  class AddBlockDemo
    {

        private static Database db = Application.DocumentManager.MdiActiveDocument.Database;

        [CommandMethod("AddRectPlateDemo1")]
        public static void AddRectPlateDemo1()
        {
            db.AddRectPlateToModelSpace(new Point3d(100, 100, 0), 100, 200, 8, 22, string.Empty);
            db.AddRectPlateToModelSpace(new Point3d(300, 300, 0), 100, 200, 8, 22, "不锈钢");
        }


        [CommandMethod("AddRectPlateDemo2")]
        public static void AddRectPlateDemo2()
        {
            db.AddRectPlateToModelSpaceByTxt();
        }

    }

}
