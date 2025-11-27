/*
 * 封装Entity绘制函数，并通过this扩展AutoCAD原有类
 */
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo
{

    // 扩展方法必须在非泛型静态类中定义
    public static class EntityTools
    {
        /*
         * 绘制圆弧 参数： center, startPoint, degree
         */

        public static ObjectId AddArcToModelSpace(
            this Database db,
            Point3d center,
            Point3d startPoint,
            double degree
        )
        {
            // 获取半径
            double radius = center.GetDistanceBetweenTwoPoint(startPoint);

            // 获取起点角度
            double startAngle = center.GetAngleToXAxis(startPoint);

            // 声明圆弧对象
            Arc arc = new Arc(center, radius, startAngle, startAngle + degree.DegreeToAngle());
            return AddEntityToModelSpace(db, arc);
        }

        /*
         * 绘制圆弧 参数： startPoint,midPoint,endPoint
         */
        public static ObjectId AddArcToModelSpace(
            this Database db,
            Point3d startPoint,
            Point3d midPoint,
            Point3d endPoint
        )
        {
            ObjectId objectId = ObjectId.Null;

            if(startPoint.AreCollinear(midPoint, endPoint))
            {
                return objectId;
            }
            else
            {
                // CircularArc3d通过三点计算圆弧的圆心,半径,起始终止弧度
                CircularArc3d cArc = new CircularArc3d(startPoint, midPoint, endPoint);

                // 如何通过点计算StartAngle,EndAngle?? 借助Vector3D(向量)
                Point3d center = cArc.Center;

                // 获取center到圆弧端点的向量
                Vector3d startVector = center.GetVectorTo(startPoint);
                Vector3d endVector = center.GetVectorTo(endPoint);

                // xVector X轴上的单位向量
                Vector3d xVector = new Vector3d(1, 0, 0);

                // 通过GetAngleTo得到两个向量之间的夹角
                // double startAngle = xVector.GetAngleTo(startVector);
                // double endAngle = xVector.GetAngleTo(endVector);
                // startAngle == cArc.StartAngle ; EndAngle ==  cArc.EndAngle

                // 将数据层和工具层分开,Arc内部只需要和db交互的数据,和基于这些数据的函数
                Arc arc = new Arc(cArc.Center, cArc.Radius, cArc.StartAngle, cArc.EndAngle);

                // 将圆弧写入db
                db.AddEntityToModelSpace(arc);
            }
            return objectId;
        }

        /*
         * 绘制圆弧 参数： center, radius, startDegree, endDegree
         */
        public static ObjectId AddArcToModelSpace(
            this Database db,
            Point3d center,
            double radius,
            double startDegree,
            double endDegree
        )
        {
            return db.AddEntityToModelSpace(
                new Arc(center, radius, startDegree.DegreeToAngle(), endDegree.DegreeToAngle())
            );
        }

        /*
         * 绘制圆
         */
        public static ObjectId AddCircleToModelSpace(
            this Database db,
            Point3d center,
            double radius
        )
        {
            Circle c = new Circle(center, new Vector3d(0, 0, 1), radius);
            return AddEntityToModelSpace(db, c);
        }

        /*
         * 两点绘制圆
         */
        public static ObjectId AddCircleToModelSpace(
            this Database db,
            Point3d point1,
            Point3d point2
        )
        {
            // 获取中心点
            Point3d center = point1.GetCenterPointBetweenTwoPoint(point2);

            // 获取半径
            double radius = point1.GetDistanceBetweenTwoPoint(center);
            return AddCircleToModelSpace(db, center, radius);
        }

        /*
         * 三点绘制圆
         */
        public static ObjectId AddCircleToModelSpace(
            this Database db,
            Point3d point1,
            Point3d point2,
            Point3d point3
        )
        {
            // 先判断三点是否在同一条直线上
            if(point1.AreCollinear(point2, point3))
            {
                return ObjectId.Null;
            }

            // 声明几何类的CircularArc3d对象
            CircularArc3d cArc = new CircularArc3d(point1, point2, point3);
            return db.AddCircleToModelSpace(cArc.Center, cArc.Radius);
        }

        /*
         * 在静态方法的第一个参数前加上 this 关键字，表明该方法是对这个参数类型的扩展。
         * 核心作用： 让你能够为一个已有的类（即使这个类是密封的 sealed，或者你没有它的源代码）添加新的方法，
         * 而无需创建新的派生类、修改原类的代码，或者使用任何设计模式（如装饰器模式）。
         * 这在以下场景中特别有用：
         * 1. 扩展第三方库的类：比如 AutoCAD 的 Database、Entity 等类，你无法修改它们的源代码，但可以通过扩展方法为它们添加自定义的功能。
         * 2. 为密封类添加方法：对于标记为 sealed 的类，无法继承，扩展方法是唯一的 “添加” 方法的途径。
         *
         * 这是一个语法糖：
         * 对于扩展方法，你可以像调用Database类的实例方法一样来调用它：
         * 编译器在编译时会自动将其转换为静态方法的调用，但从代码书写和阅读的角度来看，它更像是db对象自身的方法。
         *
         * 总结：this 的作用是：将ddEnityToModelSpace 这个静态方法，伪装成 Database 类的一个实例方法，从而让代码的调用方式更加自然
         *
         */

        public static ObjectId AddEntityToModelSpace(this Database db, Entity entity)
        {
            /*
             * 开启事务处理
             * 在 AutoCAD 中，对数据库的所有修改操作都应该在事务中进行
             * 这是一种安全机制，可以确保一系列操作要么全部成功，要么在出错时全部回滚，保证数据一致性
             * 使用 'using' 语句可以确保事务在使用完毕后被正确释放，即使发生异常
             */
            using(Transaction trans = db.TransactionManager.StartTransaction())
            {
                // 返回添加图元的ObjectId
                ObjectId entityId = ObjectId.Null;

                try
                {
                    // 打开块表 BlockTable 是一个数据库表，它存储了所有块定义的记录
                    // trans.GetObject返回DBObject类,BlockTable,BlockTableRecord都继承自该类
                    BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);

                    // 打开块表记录
                    // bt[BlockTableRecord.ModelSpace] 通过名称 "ModelSpace" 从块表中获取模型空间的记录
                    // 模型空间是我们通常绘图的区域，它本身也是一个特殊的块
                    BlockTableRecord btr = (BlockTableRecord)
                        trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 将直线加入到块表记录
                    // 这一步是将我们在内存中创建的直线，逻辑上“放入”模型空间
                    entityId = btr.AppendEntity(entity);

                    // 更新数据
                    // 它告诉事务管理器这个新创建的 DBObject (line1) 需要被添加到数据库中
                    // 第二个参数 'true' 表示让事务来管理这个对象的生命周期
                    trans.AddNewlyCreatedDBObject(entity, true);

                    // 事务提交
                    trans.Commit();

                    Console.WriteLine($"Entity:{entityId}已成功创建！");
                }
                catch(Exception ex)
                {
                    // 如果在事务过程中发生任何异常，捕获并显示错误信息
                    Console.WriteLine($"创建Entity:{entityId}时发生错误:{ex.Message}");

                    // 由于我们使用了 'using' 语句，即使不手动调用 Abort()，当代码块结束时事务也会自动回滚
                }
                return entityId;
            }
        }

        /*
         * params 是 C# 中的一个关键字，它允许方法接收可变数量的参数，这些参数会被自动封装成一个数组。
         * 没有 params 的情况下，如果你想让方法支持添加 1 个、2 个或多个实体，你可能需要编写多个重载方法：
         * 这显然非常繁琐且不灵活。
         *
         * params 的使用方式：
         *  1.传入单个实体
         *  Line line = new Line(...);
         *  db.AddEntityToModelSpace(line);
         *
         *  2.传入多个实体，用逗号分隔
         *  Line line1 = new Line(...);
         *  Circle circle = new Circle(...);
         *  Text text = new Text(...);
         *  db.AddEntityToModelSpace(line1, circle, text);
         *
         *  3.传入一个实体数组
         *  Entity[] entities = new Entity[]
         *  { new Line(...),new Circle(...),new Text(...)};
         *  db.AddEntityToModelSpace(entities);
         *
         *  编译器会自动将前两种方式（传入单个或多个实体）转换为第三种方式
         *
         *  使用 params 的注意事项：
         *  1.params 参数必须是方法的最后一个参数。
         *  2.一个方法只能有一个 params 参数。
         *
         *   方法内部如何处理entitys：
         *   在方法内部，entitys 参数的类型是 Entity[]（一个 Entity 数组）。你可以像处理普通数组一样遍历它
         */
        public static ObjectId[] AddEntityToModelSpace(this Database db, params Entity[] entitys)
        {
            // 非空检查
            if(entitys == null || entitys.Length == 0)
            {
                return Array.Empty<ObjectId>(); // 返回空数组，避免空引用异常
            }

            // 创建一个与输入实体数组长度相同的 ObjectId 数组，用于存储结果
            ObjectId[] objectIds = new ObjectId[entitys.Length];
            using(Transaction trans = db.TransactionManager.StartTransaction())
            {
                ObjectId entityId = ObjectId.Null;
                try
                {
                    // 打开块表
                    BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);

                    // 打开模型空间块表记录
                    BlockTableRecord btr = (BlockTableRecord)
                        trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 遍历所有传入的实体
                    for(int i = 0; i < entitys.Length; i++)
                    {
                        Entity entity = entitys[i];
                        if(entity != null)
                        {
                            // 将实体添加到模型空间
                            entityId = btr.AppendEntity(entity);

                            // 将新创建的实体通知事务，并获取其 ObjectId
                            trans.AddNewlyCreatedDBObject(entity, true);

                            // 记录当前实体的 ObjectId
                            objectIds[i] = entity.ObjectId;
                            Console.WriteLine($"Entity:{objectIds[i]}已成功创建！");
                        }
                        else
                        {
                            // 如果传入的实体为 null，存储一个无效的 ObjectId
                            objectIds[i] = ObjectId.Null;
                        }
                    }
                    trans.Commit();
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"创建Entity:{entityId}时发生错误:{ex.Message}");

                    // 返回空数组或 null，表示操作失败
                    return Array.Empty<ObjectId>();
                }
                return objectIds;
            }
        }


        /*
         * DB扩展添加直线命令 参数：startPoint，endPoint
         */
        public static ObjectId AddLineToModelSpace(
            this Database db,
            Point3d startPoint,
            Point3d endPoint
        )
        {
            return AddEntityToModelSpace(db, new Line(startPoint, endPoint));
        }

        /*
         * DB扩展添加直线命令 参数：startPoint，length，degree
         */
        public static ObjectId AddLineToModelSpace(
            this Database db,
            Point3d startPoint,
            double length,
            double degree
        )
        {
            // 通过startPoint,length,degree计算endPoint
            Point3d endPoint = startPoint.GetEndPoint(length, degree);
            return AddLineToModelSpace(db, startPoint, endPoint);
        }

    }

}
