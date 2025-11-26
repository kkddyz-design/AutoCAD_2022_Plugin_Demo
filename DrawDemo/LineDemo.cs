/*
 * 学习直线绘制
 */
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

/*
 * 一般在AssemblyInfo.cs中配置特性
 * 这里为了方便阅读,设置在namespace上面
 */
[assembly: CommandClass(typeof(AutoCAD_2022_Plugin_Demo.DrawDemo.LineDemo))]
namespace AutoCAD_2022_Plugin_Demo.DrawDemo
{
    
    public class LineDemo
    {
        [CommandMethod("LineDemo1")]
        public static void LineDemo1()
        {
            // 创建直线对象 -- 只存在于内存
            Line line1 = new Line();

            // 设置直线属性
            Point3d startPoint = new Point3d(100,100,0);
            Point3d endPoint = new Point3d(200, 200, 0);

            line1.StartPoint = startPoint;
            line1.EndPoint = endPoint;

            /*
             * 声明图形数据库对象
             * Application.DocumentManager.MdiActiveDocument获取当前激活的绘图窗口（文档）
             */
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            /*
             * 开启事务处理
             * 在 AutoCAD 中，对数据库的所有修改操作都应该在事务中进行
             * 这是一种安全机制，可以确保一系列操作要么全部成功，要么在出错时全部回滚，保证数据一致性
             * 使用 'using' 语句可以确保事务在使用完毕后被正确释放，即使发生异常
             */
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 打开块表 BlockTable 是一个数据库表，它存储了所有块定义的记录
                    // trans.GetObject返回DBObject类,BlockTable,BlockTableRecord都继承自该类
                    BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);

                    // 打开块表记录
                    //  bt[BlockTableRecord.ModelSpace] 通过名称 "ModelSpace" 从块表中获取模型空间的记录
                    // 模型空间是我们通常绘图的区域，它本身也是一个特殊的块
                    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 将直线加入到块表记录
                    // 这一步是将我们在内存中创建的直线，逻辑上“放入”模型空间
                    btr.AppendEntity(line1);

                    // 更新数据
                    // 它告诉事务管理器这个新创建的 DBObject (line1) 需要被添加到数据库中
                    // 第二个参数 'true' 表示让事务来管理这个对象的生命周期
                    trans.AddNewlyCreatedDBObject(line1, true);

                    // 事务提交
                    trans.Commit();

                    doc.Editor.WriteMessage("\n直线已成功创建！\n");
                }
                catch (Exception ex)
                {
                    // 如果在事务过程中发生任何异常，捕获并显示错误信息
                    doc.Editor.WriteMessage($"\n创建直线时发生错误: {ex.Message}\n");

                    // 由于我们使用了 'using' 语句，即使不手动调用 Abort()，当代码块结束时事务也会自动回滚
                }
            }
        }
    }
}
