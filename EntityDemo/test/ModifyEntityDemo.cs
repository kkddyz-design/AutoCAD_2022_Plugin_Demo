using AutoCAD_2022_Plugin_Demo.EntityDemo.add;
using AutoCAD_2022_Plugin_Demo.EntityDemo.modify;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
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


        [CommandMethod("ChangeColorDemo1")]
        public static void ChangeColorDemo1()
        {
            // db.AddCircleModelSpace(new Point3d(100, 100, 0), 50);
            Circle c1 = new Circle(new Point3d(100, 100, 0), new Vector3d(0, 0, 1), 50);
            Circle c2 = new Circle(new Point3d(200, 100, 0), new Vector3d(0, 0, 1), 50);

            // 先写入,再修改
            db.AddEntityToModelSpace(c1);
            c1.ChangeEntityColor(6);

            // 先修改,再写入
            c2.ChangeEntityColor(10);
            db.AddEntityToModelSpace(c2);
        }


        [CommandMethod("CopyDemo1")]
        public static void CopyDemo1()
        {
            Point3d center = new Point3d(100, 100, 0);
            Point3d targetCenter = new Point3d(200, 200, 0);

            // 先修改,再写入db
            Circle c1 = new Circle(center, new Vector3d(0, 0, 1), 50);
            Entity c2 = c1.CopyEntity(center, targetCenter);
            db.AddEntityToModelSpace(c1);
            db.AddEntityToModelSpace(c2);
        }

        [CommandMethod("CopyDemo2")]
        public static void CopyDemo2()
        {
            Point3d center = new Point3d(100, 100, 0);
            Point3d targetCenter = new Point3d(200, 200, 0);

            // 先写入db,再修改
            Circle c1 = new Circle(center, new Vector3d(0, 0, 1), 50);
            db.AddEntityToModelSpace(c1);

            Entity c2 = c1.CopyEntity(center, targetCenter);
            db.AddEntityToModelSpace(c2);
        }


        [CommandMethod("MoveDemo1")]
        public static void MoveDemo1()
        {
            Point3d center = new Point3d(100, 100, 0);
            Point3d targetCenter = new Point3d(0, 0, 0);

            // 先修改,再写入db
            Circle c1 = new Circle(center, new Vector3d(0, 0, 1), 50);
            c1.MoveEntity(center, targetCenter);
            db.AddEntityToModelSpace(c1);

            // 先写入db，再修改
            Circle c2 = new Circle(center, new Vector3d(0, 0, 1), 50);
            db.AddEntityToModelSpace(c2);
            c2.MoveEntity(center, targetCenter);
        }

        [CommandMethod("RotateDemo1")]
        public static void RotateDemo1()
        {
            // 创建三角形
            Point3d center = new Point3d(100, 100, 0);

            // 直接在数据库创建对象,在demo层面不知道内存创建的对象
            ObjectId objId = db.AddPolygonToModelSpace(center, 100, 3, Math.PI / 2);

            db.UpdateEntityToModelSpace(objId, originalEntity => EntityModifiers.RotateEntity(originalEntity, center, 90));
        }


        [CommandMethod("ScaleDemo1")]
        public static void ScaleDemo1()
        {
            Point3d center = new Point3d(100, 100, 0);

            // 先缩放,再写入db
            Circle c1 = new Circle(center, new Vector3d(0, 0, 1), 50);
            c1.ScaleEntity(center, 2);
            db.AddEntityToModelSpace(c1);
        }

        [CommandMethod("ScaleDemo2")]
        public static void ScaleDemo2()
        {
            Point3d center = new Point3d(100, 100, 0);

            // 先写入再缩放
            Circle c1 = new Circle(center, new Vector3d(0, 0, 1), 50);
            db.AddEntityToModelSpace(c1);
            c1.ScaleEntity(center, 2);
        }

    }

}
