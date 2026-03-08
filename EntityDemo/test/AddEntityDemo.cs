/*
 * 学习图形绘制 即Entity对象的操作
 */
using AutoCAD_2022_Plugin_Demo.EntityDemo.domain;
using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block;
using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.entity;
using AutoCAD_2022_Plugin_Demo.EntityDemo.service;
using AutoCAD_2022_Plugin_Demo.EntityDemo.test;
using AutoCAD_2022_Plugin_Demo.tools;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
/*
 * 一般在AssemblyInfo.cs中配置特性
 * 这里为了方便阅读,设置在namespace上面
 */
[assembly: CommandClass(typeof(AddEntityDemo))]


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.test
{

    public class AddEntityDemo
    {

        private static Document doc = Application.DocumentManager.MdiActiveDocument; //获取当前激活的绘图窗口（文档）
        private static Database db = doc.Database; // 图形数据库对象


        // 创建圆弧对象 方式1：圆心+半径+起始弧度+终止弧度
        [CommandMethod("ArcDemo1")]
        public static void ArcDemo1()
        {
            // StartAngle使用弧度制 圆弧绘制沿逆时针,因此StartAngle(负数)必须小于EndAngle
            Arc arc1 = new Arc();
            arc1.Center = new Point3d(0, 0, 0); // 中心
            arc1.Radius = 100; // 半径
            arc1.StartAngle = -Math.PI / 4; // 起始弧度
            arc1.EndAngle = Math.PI / 4; //  终止弧度

            double startDegree = -45;
            double endDegree = 45;
            Arc arc2 = new Arc(
                new Point3d(200, 200, 0),
                100,
                startDegree.DegreeToRadian(),
                endDegree.DegreeToRadian()
            );

            // 将圆弧写入db
            db.AddEntityToModelSpace(arc1);
            db.AddEntityToModelSpace(arc2);
        }

        // 三点画圆
        [CommandMethod("ArcDemo2")]
        public static void ArcDemo2()
        {
            // 三点必须不共线
            Point3d startPoint = new Point3d(0, -50, 0);
            Point3d midPoint = new Point3d(50, 0, 0);
            Point3d endPoint = new Point3d(0, 50, 0);

            db.AddArcToModelSpace(startPoint, midPoint, endPoint);
        }

        // 起点+角度画圆弧
        [CommandMethod("ArcDemo3")]
        public static void ArcDemo3()
        {
            // 三点必须不共线
            Point3d center = new Point3d(0, 0, 0);
            Point3d startPoint = new Point3d(0, 90, 0);
            double degree = 90;

            db.AddArcToModelSpace(center, startPoint, degree);
        }

        [CommandMethod("ArcDemo4")]
        public static void ArcDemo4()
        {
            Point3d startPoint = new Point3d(40, -34, 0);
            Point3d midPoint = new Point3d(0, -50.5, 0);
            Point3d endPoint = new Point3d(-40, -34, 0);

            db.AddArcToModelSpace(startPoint, midPoint, endPoint);
        }


        // 两点 + 圆心 EntityTools实现了,这里不写了

        [CommandMethod("CircleDemo")]
        public static void CircleDemo()
        {
            db.AddCircleToModelSpace(new Point3d(50, 50, 0), 100);

            db.AddCircleToModelSpace(new Point3d(100, 100, 0), new Point3d(200, 100, 0));

            db.AddCircleToModelSpace(
                new Point3d(300, 300, 0),
                new Point3d(270, 180, 0),
                new Point3d(160, 240, 0)
            );
        }

        [CommandMethod("EllipseDemo")]
        public void EllipseDemo()
        {
            // 声明图形数据库

            Ellipse e2 = new Ellipse(new Point3d(100, 100, 0), Vector3d.ZAxis, new Vector3d(100, 0, 0), 5, 0, 2 * Math.PI);
            db.AddEntityToModelSpace(e2);
        }


        [CommandMethod("HatchDemo")]
        public void HatchDemo()
        {
            // 渐变填充
            ObjectIdCollection objIds = new ObjectIdCollection();
            objIds.Add(db.AddCircleToModelSpace(new Point3d(100, 100, 0), 100));
            string hatchGradientName = HatchTools.HatchGradientName.gr_invcylinder;
            db.HatchGradient(2, 6, HatchTools.HatchGradientName.gr_hemisperical,
                db.AddEntityToModelSpace(new Rectangle(new Point3d(100, 100, 0), new Point3d(500, 300, 0))));
        }

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
             * 不封装获取 Database 是为了遵循 职责单一原则，保持方法的灵活性、可测试性和清晰性。
             * 这会导致方法的职责不清晰，可读性和可维护性下降。
             * 如果以后需要向非当前文档的数据库添加实体（例如，处理一个后台打开的 DWG 文件），这个方法就无法复用了。
             *
             * 如果 AddEntityToModelSpace 内部直接获取当前 Database，它就引入了一个 隐藏的依赖：Application.DocumentManager.MdiActiveDocument
             * 这会带来一些问题：
             * 1. 如果方法在非 UI 线程调用，MdiActiveDocument 可能为 null，导致程序崩溃
             * 2. 如果当前没有激活的文档，方法也会失败。
             * 将Database 作为参数传入，可以让这些依赖关系 显式化，调用者在调用前必须确保 Database 是有效的，从而减少潜在的错误。
             */
            ObjectId objectId = db.AddEntityToModelSpace(line1);

            // objectId是 结构体类型（Value Type），它的默认值是其所有成员的零值（例如，Id 为 0），而不是 null
            // 因此，if (objectId != null) 这个判断 永远为 true
            if(objectId.IsValid)
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

            ObjectId[] objectIds = db.AddEntityToModelSpace(line1, line2, line3, line4);
            for(int i = 0; i < objectIds.Length; i++)
            {
                ObjectId objectId = objectIds[i];

                if(objectId.IsValid)
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
            ObjectId objectId = db.AddLineToModelSpace(p1, p2);

            if(objectId.IsValid)
            {
                doc.Editor.WriteMessage($"成功创建直线，ID为：{objectId}\n");
            }
            else
            {
                doc.Editor.WriteMessage($"创建直线{objectId}失败！\n");
            }

            // 角度60°长度100,绘制直线
            objectId = db.AddLineToModelSpace(p1, 100, 240);

            if(objectId.IsValid)
            {
                doc.Editor.WriteMessage($"成功创建直线，ID为：{objectId}\n");
            }
            else
            {
                doc.Editor.WriteMessage($"创建直线{objectId}失败！\n");
            }
        }

        /*
         * 插入多边形
         */
        [CommandMethod("PolygonDemo1")]
        public static void PolygonDemo1()
        {
            Point3d center1 = new Point3d(100, 100, 0);
            Point3d center2 = new Point3d(200, 100, 0);
            Point3d center3 = new Point3d(300, 100, 0);
            Point3d center4 = new Point3d(400, 100, 0);
            Point3d center5 = new Point3d(500, 100, 0);
            Point3d center6 = new Point3d(600, 100, 0);

            Polygon polygon1 = new Polygon(center1, 50, 3);
            Polygon polygon2 = new Polygon(center2, 50, 4);
            Polygon polygon3 = new Polygon(center3, 50, 5);
            Polygon polygon4 = new Polygon(center4, 50, 6);
            Polygon polygon5 = new Polygon(center5, 50, 7);
            Polygon polygon6 = new Polygon(center6, 50, 8);

            db.AddEntityToModelSpace(polygon1);
            db.AddEntityToModelSpace(polygon2);
            db.AddEntityToModelSpace(polygon3);
            db.AddEntityToModelSpace(polygon4);
            db.AddEntityToModelSpace(polygon5);
            db.AddEntityToModelSpace(polygon6);
        }


        /*
         * 插入多段线 - 三角
         */
        [CommandMethod("PolyLineDemo1")]
        public static void PolyLineDemo1()
        {
            // Polyline是二维,只能使用Point2D作为顶点

            Point2d p1 = new Point2d(100, 100);
            Point2d p2 = new Point2d(200, 100);
            Point2d p3 = new Point2d(200, 200);

            db.AddPolylineToModelSpace(true, 10, p1, p2, p3);
        }

        /*
         * 插入圆弧多段线 - 槽口
         */
        [CommandMethod("PolyLineDemo2")]
        public static void PolyLineDemo2()
        {
            // 1. 定义顶点（顺序：下直线右端点→右半圆→上直线右端点→左半圆→下直线左端点）
            // 圆角矩形的尺寸：宽度300（400-100），高度100（200-100），半圆直径=高度（100），半径=50
            Point2d pRightBottom = new Point2d(400, 100);  // 下直线右端点
            Point2d pRightTop = new Point2d(400, 200);     // 上直线右端点
            Point2d pLeftTop = new Point2d(100, 200);      // 上直线左端点
            Point2d pLeftBottom = new Point2d(100, 100);   // 下直线左端点

            // 2. 创建多段线
            Polyline pl = new Polyline();
            pl.SetDatabaseDefaults();

            // 3. 按顺序添加顶点（关键：凸度值配合顶点方向） 一定先添加凸度为1的端点,按照下左->下右->上右->上左 最后没法通过闭合设置曲线
            // 顶点1：下直线右端点 → 凸度=1（右半圆，向上凸）
            pl.AddVertexAt(0, pRightBottom, 1, 0, 0);

            // 顶点2：上直线右端点 → 凸度=0（上直线，水平向左）
            pl.AddVertexAt(1, pRightTop, 0, 0, 0);

            // 顶点3：上直线左端点 → 凸度=1（左半圆，向下凸）
            pl.AddVertexAt(2, pLeftTop, 1, 0, 0);

            // 顶点4：下直线左端点 → 凸度=0（下直线，水平向右）
            pl.AddVertexAt(3, pLeftBottom, 0, 0, 0);

            // 4. 闭合多段线（自动连接最后一个顶点和第一个顶点，形成下直线）
            pl.Closed = true;

            db.AddEntityToModelSpace(pl);
        }


        /*
         * 加入矩形
         */
        [CommandMethod("RecDemo1")]
        public static void RecDemo1()
        {
            Point3d leftDown = new Point3d(100, 100, 0);
            Point3d rightUp = new Point3d(400, 200, 0);

            Point3d targetPoint = new Point3d(1000, 1000, 0);
            Rectangle rect = new Rectangle(leftDown, rightUp);
            db.AddEntityToModelSpace(rect);

            db.MoveEntityToModelSpace(rect.Id, rect.GetPosition(), targetPoint);
        }

        [CommandMethod("Demo11")]
        public static void Demo11()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            Circle ID_circle = new Circle(Point3d.Origin, new Vector3d(0, 0, 1), 100);
            Circle OD_circle = new Circle(Point3d.Origin, new Vector3d(0, 0, 1), 150);

            Line line1 = new Line(Point3d.Origin, new Point3d(200, 0, 0));
            Line line2 = (Line)line1.CopyEntity(line1.StartPoint, new Point3d(0, 50, 0))[0];

            Line line3 = (Line)line1.MirrorEntity(Point3d.Origin, new Point3d(0, 50, 0))[0];
            Line line4 = (Line)line2.MirrorEntity(Point3d.Origin, new Point3d(0, 50, 0))[0];

            ObjectId id1 = db.AddEntityToModelSpace(ID_circle);
            db.AddEntityToModelSpace(OD_circle);
            db.AddEntityToModelSpace(line1);

            ObjectId id2 = db.AddEntityToModelSpace(line2);
            db.AddEntityToModelSpace(line3);
            db.AddEntityToModelSpace(line4);

            double filletRadius = 10;

            try {
                // ========== 核心：名词-动词选择模式 ==========

                // 临时存储原始PICKFIRST值，用于后续恢复
                object oldPickFirst = null;

                // 1. 保存并开启PICKFIRST（先选择后执行）
                oldPickFirst = Application.GetSystemVariable("PICKFIRST");
                Application.SetSystemVariable("PICKFIRST", 1);

                // 2. 创建包含两个对象的选择集

                ObjectId[] targetIds = new ObjectId[] { id1, id2 };

                // SelectionSet selSet = SelectionSet.Create(targetIds);
                SelectionSet selSet = SelectionSet.FromObjectIds(targetIds);

                // 3. 设置为“隐含选择”（模拟用户手动选中对象）
                ed.SetImpliedSelection(selSet);

                //// 4. 调用FILLET命令（依赖隐含选择，无需手动选对象）
                ed.Command(
                "_.FILLET",       // 圆角命令
                "_R",             // 选择半径选项
                filletRadius,     // 设置圆角半径
                string.Empty,     // 确认半径（Enter）
                string.Empty,     // 选择第一个对象（隐含选择，直接Enter）
                string.Empty      // 选择第二个对象（隐含选择，直接Enter）
                );

                // 5. 强制刷新视图（确保图形立即显示变化）
                ed.Regen();

                // ========== 清理操作 ==========
                // 清除隐含选择，避免影响后续命令
                SelectionSet emptySelSet = SelectionSet.FromObjectIds(new ObjectId[0]);
                ed.SetImpliedSelection(emptySelSet);

                ed.WriteMessage($"\n圆角操作完成！\n半径：{filletRadius}\n对象1 ID：{id1}\n对象2 ID：{id2}");
            }
            catch(System.Exception ex) {
                ed.WriteMessage($"\n圆角操作失败：{ex.Message}");
            }

            // 通过Editor调用trim快速修剪
            // Point3d startP = new Point3d(-200, 30, 0);
            // Point3d endP = new Point3d(200, 29, 0);
            // using(Transaction tr = db.TransactionManager.StartTransaction()) {
            // // TR 命令 → 空格（快速模式）→ F（栏选）→ 起点 → 终点 → 空格

            // doc.Editor.Command(
            // "_.TRIM",       // 修剪命令
            // string.Empty,   // 空格 = 直接进入快速模式（所有对象当边界）
            // "_F",           // 栏选 Fence
            // startP,         // 你指定的栏选起点
            // endP,           // 你指定的栏选终点
            // string.Empty,
            // string.Empty);  // 结束命令
            // tr.Commit();
            // doc.Editor.WriteMessage("\n快速栏选修剪完成！");
            // }
        }

        [CommandMethod("TestClamp")]
        public static void TestClamp()
        {
            TowHolePipeClamp towHolePipeClamp = new TowHolePipeClamp(156, 6, 4, 10, 25, "碳钢", 35, 50, 14);

            ObjectId RightInnerArcId = db.AddEntityToModelSpace(towHolePipeClamp.RightInnerArc);
            ObjectId RightOutterArc = db.AddEntityToModelSpace(towHolePipeClamp.RightOutterArc);
            ObjectId InnerLineId = db.AddEntityToModelSpace(towHolePipeClamp.InnerLine);
            ObjectId OutterLineId = db.AddEntityToModelSpace(towHolePipeClamp.OutterLine);

            AutoCadTools.ExecuteFilletByObjectId(RightInnerArcId, InnerLineId, 10);
            AutoCadTools.ExecuteFilletByObjectId(RightOutterArc, OutterLineId, 10);

            // db.AddEntityToModelSpace(towHolePipeClamp.OutterCircle);
            // db.AddEntityToModelSpace(towHolePipeClamp.InnerCircle);
            // db.AddEntityToModelSpace(towHolePipeClamp.SpanRay);
            // db.AddEntityToModelSpace(towHolePipeClamp.ThickRay);
        }

    }

}
