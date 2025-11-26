/*
 * 学习图形绘制 即Entity对象的操作
 */
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;

/*
 * 一般在AssemblyInfo.cs中配置特性
 * 这里为了方便阅读,设置在namespace上面
 */
[assembly: CommandClass(typeof(AutoCAD_2022_Plugin_Demo.DrawDemo.EntityDemo))]
namespace AutoCAD_2022_Plugin_Demo.DrawDemo
{

    public class EntityDemo
    {

        // =================================直线部分==============================================
        /*
         * 插入一条直线/图形
         */
        [CommandMethod("LineDemo1")]
        public static void LineDemo1()
        {
            // 创建直线对象 -- 只存在于内存
            Line line1 = new Line();

            // 设置直线属性
            Point3d startPoint = new Point3d(100, 100, 0);
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
             * 不封装获取 Database 是为了遵循 职责单一原则，保持方法的灵活性、可测试性和清晰性。
             * 这会导致方法的职责不清晰，可读性和可维护性下降。
             * 如果以后需要向非当前文档的数据库添加实体（例如，处理一个后台打开的 DWG 文件），这个方法就无法复用了。
             * 
             * 如果 AddEnityToModelSpace 内部直接获取当前 Database，它就引入了一个 隐藏的依赖：Application.DocumentManager.MdiActiveDocument
             * 这会带来一些问题：
             * 1. 如果方法在非 UI 线程调用，MdiActiveDocument 可能为 null，导致程序崩溃
             * 2. 如果当前没有激活的文档，方法也会失败。
             * 将Database 作为参数传入，可以让这些依赖关系 显式化，调用者在调用前必须确保 Database 是有效的，从而减少潜在的错误。
             */
            ObjectId objectId = db.AddEnityToModelSpace(line1);

            // objectId是 结构体类型（Value Type），它的默认值是其所有成员的零值（例如，Id 为 0），而不是 null
            // 因此，if (objectId != null) 这个判断 永远为 true
            if (objectId.IsValid)
            {
                // 注意字符串前面的 $ 符号，它告诉编译器这是一个插值字符串，{objectId} 会被变量 objectId 的值替换。
                doc.Editor.WriteMessage($"成功创建直线，ID为：{objectId}");
            }
            else
            {
                doc.Editor.WriteMessage($"创建直线{objectId}失败！");
            }
        }

        /*
         * 插入多条直线/图形
         */
        [CommandMethod("LineDemo2")]
        public static void LineDemo2()
        {
            Point3d p1 = new Point3d(0, 0, 0);
            Point3d p2 = new Point3d(100, 0, 0);
            Point3d p3 = new Point3d(100, 100, 0);
            Point3d p4 = new Point3d(0, 100, 0);
            Line line1 = new Line(p1, p2);
            Line line2 = new Line(p2, p3);
            Line line3 = new Line(p3, p4);
            Line line4 = new Line(p4, p1);

            // 获取db
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            ObjectId[] objectIds =   db.AddEnityToModelSpace(line1, line2,line3,line4);
            for (int i = 0; i < objectIds.Length; i++)
            {
                ObjectId objectId = objectIds[i];

                if (objectId.IsValid)
                {
                    doc.Editor.WriteMessage($"成功创建直线，ID为：{objectId}\n");
                }
                else
                {
                    doc.Editor.WriteMessage($"创建直线{objectId}失败！\n");
                }
            }
        }

        /*
         * 使用AddLine添加直线
         */
        [CommandMethod("LineDemo3")]
        public static void LineDemo3()
        {
            Point3d p1 = new Point3d(0, 0, 0);
            Point3d p2 = new Point3d(100, 0, 0);

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // 两点绘制直线
            ObjectId objectId = db.AddLineToModelSpace(p1,p2);

            if (objectId.IsValid)
            {
                doc.Editor.WriteMessage($"成功创建直线，ID为：{objectId}\n");
            }
            else
            {
                doc.Editor.WriteMessage($"创建直线{objectId}失败！\n");
            }

            // 角度60°长度100,绘制直线
            objectId = db.AddLineToModelSpace(p1,100,240 );

            if (objectId.IsValid)
            {
                doc.Editor.WriteMessage($"成功创建直线，ID为：{objectId}\n");
            }
            else
            {
                doc.Editor.WriteMessage($"创建直线{objectId}失败！\n");
            }
        }


        // =================================================================================

    }
}
