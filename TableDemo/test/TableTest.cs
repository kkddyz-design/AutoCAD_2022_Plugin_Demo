using AutoCAD_2022_Plugin_Demo.EntityDemo;
using AutoCAD_2022_Plugin_Demo.TableDemo.test;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;

[assembly: CommandClass(typeof(TableTest))]


namespace AutoCAD_2022_Plugin_Demo.TableDemo.test


{

    public class TableTest
    {

        Database db = HostApplicationServices.WorkingDatabase;

        [CommandMethod("addTable1")]
        public void addTable()
        {
            Table t = new Table();
            t.SetSize(10, 5);
            t.SetRowHeight(10);
            t.SetColumnWidth(50);
            t.Position = new Point3d(100, 100, 0);
            t.Cells[0, 0].TextString = "材料清单表";
            t.Cells[0, 0].TextHeight = 6;
            db.AddEntityToModelSpace(t);
        }

    }

}
