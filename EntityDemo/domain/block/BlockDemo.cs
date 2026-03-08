using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoCAD_2022_Plugin_Demo.tools;

[assembly: CommandClass(typeof(BlockDemo))]


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block
{

    public class BlockDemo
    {

        static Database db = HostApplicationServices.WorkingDatabase;
        static Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

        /// <summary>
        /// 测试方法
        /// </summary>
        [CommandMethod("TestGetBlockTableRecord")]
        public static void GetBlockTableRecord()
        {
            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // AutoCAD 数据库的 “间接引用” 设计
                // CAD 中所有实体 / 表记录都不会直接暴露，而是通过 ObjectId（对象 ID）间接引用
                foreach(ObjectId objId in bt) {
                    BlockTableRecord btr = objId.GetObject(OpenMode.ForRead) as BlockTableRecord;

                    // 依次打开 模型空间,布局1,布局2，自定义块
                }
            }
        }

        /// <summary>
        /// 测试方法
        /// </summary>
        [CommandMethod("TestAddBlockTableRecord")]
        public static void AddBlockTableRecord()
        {
            string btrName = "一堆圆";
            Point3d p1 = new Point3d(100, 100, 0);
            Point3d p2 = new Point3d(100, 200, 0);
            Point3d p3 = new Point3d(100, 300, 0);
            Point3d p4 = new Point3d(100, 400, 0);
            Point3d p5 = new Point3d(100, 500, 0);

            Circle circle1 = new Circle(p1, new Vector3d(0, 0, 1), 30);
            Circle circle2 = new Circle(p2, new Vector3d(0, 0, 1), 30);
            Circle circle3 = new Circle(p3, new Vector3d(0, 0, 1), 30);
            Circle circle4 = new Circle(p4, new Vector3d(0, 0, 1), 30);
            Circle circle5 = new Circle(p5, new Vector3d(0, 0, 1), 30);

            db.AddBlockTableRecord(btrName, new List<Entity> { circle1, circle2, circle3, circle4, circle5 });
        }

        /// <summary>
        /// 测试方法
        /// </summary>
        [CommandMethod("TestAddBlockRef")]
        public static void AddBlockRef()
        {
            Point3d basePoint = new Point3d(100, 100, 0);

            string btrName = "一堆圆";
            ObjectId blockId = db.GetBlockIdByName(btrName);
            if(blockId != ObjectId.Null) {
                db.AddBlockReferenceToModelSpace(blockId, basePoint);
            }
            else {
                ed.WriteMessage($"请先插入块定义：{btrName}");
            }
        }

    }

}
