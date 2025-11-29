/*
 * 封装Entity绘制函数，并通过this扩展AutoCAD原有类
 * 
 * Line Circle Arc(圆弧) Ellipse(椭圆)  Polyline(多段线)
 * 矩形，多边形都是Polyline 没有对应的实体类,考虑封装成ntity
 */
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.add
{

    // 扩展方法必须在非泛型静态类中定义
    public static class AddEntityTools
    {
        /*
         * 绘制圆弧 参数： center, leftDown, degree
         */

        public static ObjectId AddArcToModelSpace(this Database db, Point3d center, Point3d startPoint, double degree)
        {
            // 获取半径
            double radius = center.GetDistanceBetweenTwoPoint(startPoint);

            // 获取起点角度
            double startAngle = center.GetAngleToXAxis(startPoint);

            // 声明圆弧对象
            Arc arc = new Arc(center, radius, startAngle, startAngle + degree.DegreeToAngle());
            return db.AddEntityToModelSpace(arc);
        }

        /*
         * 三点绘制圆弧 参数： startPoint,midPoint,endPoint
         */
        public static ObjectId AddArcToModelSpace(this Database db, Point3d startPoint, Point3d midPoint, Point3d endPoint)
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

                #region
                // 如何通过点计算StartAngle,EndAngle?? 借助Vector3D(向量)
                // Point3d center = cArc.Center;
                // 获取center到圆弧端点的向量
                // Vector3d startVector = center.GetVectorTo(startPoint);
                // Vector3d endVector = center.GetVectorTo(endPoint);

                // xVector X轴上的单位向量
                // Vector3d xVector = new Vector3d(1, 0, 0);

                // 通过GetAngleTo得到两个向量之间的夹角
                // double startAngle = xVector.GetAngleTo(startVector);
                // double endAngle = xVector.GetAngleTo(endVector);
                // startAngle == cArc.StartAngle ; EndAngle ==  cArc.EndAngle
                #endregion

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
        public static ObjectId AddCircleToModelSpace(this Database db, Point3d center, double radius)
        {
            Circle c = new Circle(center, new Vector3d(0, 0, 1), radius);
            return db.AddEntityToModelSpace(c);
        }

        /*
         * 两点绘制圆
         */
        public static ObjectId AddCircleToModelSpace(this Database db, Point3d point1, Point3d point2)
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
        public static ObjectId AddCircleToModelSpace(this Database db, Point3d point1, Point3d point2, Point3d point3)
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
         * 绘制椭圆 - 两点
         * 以这两个点的连线为 “轴”，自动计算长轴、短轴比例（ratio = 纵差/横差）
         * 快速通过两个点 “大致” 画椭圆，但短轴由两点的纵横比决定（不够灵活）
         */

        public static ObjectId AddEllipseToModelSpace(this Database db, Point3d point1, Point3d point2)
        {
            // 椭圆的圆心
            Point3d center = point1.GetCenterPointBetweenTwoPoint(point2);
            double ratio = Math.Abs((point1.Y - point2.Y) / (point1.X - point2.X));
            Vector3d majorVector = new Vector3d(Math.Abs((point1.X - point2.X)) / 2, 0, 0);

            // 声明椭圆对象
            Ellipse elli = new Ellipse(center, Vector3d.ZAxis, majorVector, ratio, 0, 2 * Math.PI);
            return db.AddEntityToModelSpace(elli);
        }


        /*
         * 绘制椭圆 - 	长轴两端点 + 短轴长度
         * 
         * 明确指定长轴的两个端点，再手动输入短轴长度，计算长短轴比例
         * 需要精确控制长轴范围和短轴长度的场景（最常用）
         */
        public static ObjectId AddEllipseToModelSpace(
            this Database db, 
            Point3d majorPoint1,    // 长轴端点1
            Point3d majorPoint2,    // 长轴端点2
            double shortRadius      // 短轴长度
        )
        {
            // 椭圆的圆心
            Point3d center = majorPoint1.GetCenterPointBetweenTwoPoint(majorPoint2);

            // 短轴与长轴的比
            double ratio = 2 * shortRadius / majorPoint1.GetDistanceBetweenTwoPoint(majorPoint2);

            // 长轴的向量
            Vector3d majorAxis = majorPoint2.GetVectorTo(center);
            Ellipse elli = new Ellipse(center, Vector3d.ZAxis, majorAxis, ratio, 0, 2 * Math.PI);

            return db.AddEntityToModelSpace(elli);
        }

        /*
         * 绘制椭圆 - 圆心 + 长轴半径 + 短轴半径 + 角度
         * 直接指定圆心、长轴 / 短轴半径、长轴旋转角、椭圆弧的起止角
         * 需要精确控制椭圆的 “位置、方向、弧度范围” 的场景（如画椭圆弧）
         */
        public static ObjectId AddEllipseToModelSpace(
            this Database db,
            Point3d center,
            double majorRadius, // 长轴长度
            double shortRadius, // 短轴长度
            double degere,      // 长轴与X轴夹角
            double startDegree, // 起始角度
            double endDegree    // 终止角度
        )
        {
            // 计算相关参数
            double ratio = shortRadius / majorRadius;
            Vector3d majorAxis = new Vector3d(majorRadius * Math.Cos(degere.DegreeToAngle()), majorRadius * Math.Sin(degere.DegreeToAngle()), 0);

            // 声明椭圆对象
            Ellipse elli = new Ellipse(center, Vector3d.ZAxis, majorAxis, ratio, startDegree.DegreeToAngle(), endDegree.DegreeToAngle());
            return db.AddEntityToModelSpace(elli);
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


        /*
         * DB扩展添加直线命令 参数：leftDown，rightUp
         */
        public static ObjectId AddLineToModelSpace(this Database db, Point3d startPoint, Point3d endPoint)
        {
            return db.AddEntityToModelSpace(new Line(startPoint, endPoint));
        }

        /*
         * DB扩展添加直线命令 参数：leftDown，length，degree
         */
        public static ObjectId AddLineToModelSpace(this Database db, Point3d startPoint, double length, double degree)
        {
            // 通过startPoint,length,degree计算endPoint
            Point3d endPoint = startPoint.GetEndPoint(length, degree);
            return AddLineToModelSpace(db, startPoint, endPoint);
        }

        /*
         * 绘制多边形
         */
        public static ObjectId AddPolygonToModelSpace(this Database db, Point3d center, double radius, int sideNum, double startAngle)
        {
            if(sideNum < 3)
            {
                return ObjectId.Null;
            }

            Polyline pl = new Polyline();

            // 循环计算顶点坐标
            for(int i = 0; i < sideNum; i++)
            {
                // 计算当前顶点对应的角度（弧度）
                // - 2 * Math.PI 是一个完整的圆周角
                // - 除以 sideNum 得到每个顶点之间的角度间隔
                // - 乘以 i 得到当前顶点的角度偏移量
                // - i = 0, angle = startAngle = 90°
                double angle = startAngle + (i * 2 * Math.PI / sideNum);

                // 使用三角函数计算顶点在笛卡尔坐标系中的坐标
                double x = center.X + radius * Math.Cos(angle);
                double y = center.Y + radius * Math.Sin(angle);

                // 创建Point2d对象
                Point2d vertex = new Point2d(x, y);
                pl.AddVertexAt(i, vertex, 0, 0, 0);
            }

            pl.Closed = true;
            return db.AddEntityToModelSpace(pl);
        }


        /*
         * 绘制折线多段线 ： bulge = 0，宽度固定
         */
        public static ObjectId AddPolylineToModelSpace(this Database db, bool isClosed, double width, params Point2d[] vertices)
        {
            if(vertices == null || vertices.Length < 0)
            {
                return ObjectId.Null;
            }

            Polyline pl = new Polyline();
            for(int i = 0; i < vertices.Length; i++)
            {
                pl.AddVertexAt(i, vertices[i], 0, 0, 0);
            }

            pl.Closed = isClosed;    // 是否闭合
            pl.ConstantWidth = width;// 多段线宽度

            return db.AddEntityToModelSpace(pl);
        }


        /*
         * 2点绘制矩形
         * 本质：通过多段线绘制直线
         * X1,Y2 ----------------- X2,Y2
         * |                         |
         * |                         |           
         * X1,Y1 ----------------- X2,Y1
         */
        public static ObjectId AddRectangleToModelSpace(this Database db, Point2d leftDown, Point2d rightUp)
        {
            double X1 = leftDown.X;
            double Y1 = leftDown.Y;
            double X2 = rightUp.X;
            double Y2 = rightUp.Y;

            Point2d leftUp = new Point2d(X1, Y2);
            Point2d rightDown = new Point2d(X2, Y1);

            Polyline pl = new Polyline();
            pl.AddVertexAt(0, leftDown, 0, 0, 0);
            pl.AddVertexAt(1, rightDown, 0, 0, 0);
            pl.AddVertexAt(2, rightUp, 0, 0, 0);
            pl.AddVertexAt(3, leftUp, 0, 0, 0);

            // 一定要设置闭合,不然虽然顶点收尾重合了,但是不会产生闭合曲线
            pl.Closed = true;

            return db.AddEntityToModelSpace(pl);
        }

    }

}
