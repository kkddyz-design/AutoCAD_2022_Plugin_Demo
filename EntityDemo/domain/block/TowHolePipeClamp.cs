using AutoCAD_2022_Plugin_Demo.tools;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block
{

    /// <summary>
    /// 双孔管夹类 - 包括A2,A3,A22
    /// </summary>
    public  class TowHolePipeClamp : AbstractBlock
    {

        /// <summary>
        /// 管夹内径
        /// </summary>
        public double ClampInnerDiameter { get; set; }

        /// <summary>
        /// R角半径
        /// </summary>
        public double ClampFilletRadius { get; set; }

        /// <summary>
        /// 开档,偏移中心线时/2
        /// </summary>
        public double ClampSpan { get; set; }

        /// <summary>
        /// 管夹材质
        /// </summary>
        public string ClampMaterial { get; set; }


        /// <summary>
        /// 展开长
        /// </summary>
        public double ClampLength { get; set; }


        public double ClampWidth { get; set; }

        /// <summary>
        /// 管夹耳朵长度
        /// </summary>
        public double ClampEar { get; set; }

        /// <summary>
        /// 管夹开孔孔径
        /// </summary>
        public double ClampHole { get; set; }

        /// <summary>
        /// L方向 孔边距
        /// </summary>
        public double ClampHoleMargin { get; set; }


        // 上面是属性，下面是管夹对应的图元对象


        public Arc RightInnerArc { get; set; }

        public Arc RightOutterArc { get; set; }

        public Circle InnerCircle { get; set; }

        public Circle OutterCircle { get; set; }

        public Ray SpanRay { get; set; }

        public Ray ThickRay { get; set; }

        public Line InnerLine { get; set; }

        public Line OutterLine { get; set; }

        public TowHolePipeClamp(
            double innerDiameter,
            int thick,
            int count,
            double filletRadius,
            double span,
            string material,
            double holeMargin,
            double clampEar,
            double clampHole

        )
        {
            // 0. 初始化变量
            ClampInnerDiameter = innerDiameter;
            BlockThick = thick;
            BlockCount = count;
            ClampFilletRadius = filletRadius;
            ClampSpan = span;
            ClampMaterial = material;
            ClampHoleMargin = holeMargin;
            ClampEar = clampEar;
            ClampHole = clampHole;

            // 1. 定义块名
            BlockName = $"{ClampMaterial}-内径{ClampInnerDiameter}-宽{ClampWidth}-耳长{ClampEar}-开孔{clampHole}-厚度{thick}-数量{count}";

            // 2. 创建内径圆
            Circle ID_Circle = new Circle(Point3d.Origin, new Vector3d(0, 0, 1), ClampInnerDiameter / 2);

            // 3. 创建外径圆
            Circle OD_Circle = new Circle(Point3d.Origin, new Vector3d(0, 0, 1), ClampInnerDiameter / 2 + BlockThick);

            // 4. 创建射线组

            Ray xRay = new Ray();
            xRay.BasePoint = Point3d.Origin;
            xRay.UnitDir = new Vector3d(1, 0, 0); // 创建X方向的射线

            Ray spanRay = new Ray();
            spanRay.BasePoint = new Point3d(0, ClampSpan / 2, 0); // 开档所在射线
            spanRay.UnitDir = new Vector3d(1, 0, 0);

            Ray thickRay = new Ray();
            thickRay.BasePoint = new Point3d(0, ClampSpan / 2 + BlockThick, 0);
            thickRay.UnitDir = new Vector3d(1, 0, 0);    // 管夹上边所在射线

            // 5. 计算得到射线与内外圆的交点 --取值
            // 将交点降序排列，取第一个(即取X值较大的交点)
            Point3d SpanRayInterPoint = GeometryTools.GetIntersectionsBetween_Ray_Circle(spanRay, ID_Circle, GeometryTools.OrderByXDesc)[0];

            Point3d ThickRayInterPoint = GeometryTools.GetIntersectionsBetween_Ray_Circle(thickRay, OD_Circle, GeometryTools.OrderByXDesc)[0];

            // 6. 创建圆弧对象 -- 创建一侧的圆弧，最终通过镜像生成整个管夹

            double innerArcStartRedian = GeometryTools.GetRadiansToXAxis(Point3d.Origin, SpanRayInterPoint);
            Arc innerArc = new Arc(Point3d.Origin, ClampInnerDiameter / 2, innerArcStartRedian, Math.PI / 2);
            innerArc.ChangeColor(6);

            double outterArcStartRedian = GeometryTools.GetRadiansToXAxis(Point3d.Origin, ThickRayInterPoint);
            Arc outterArc = new Arc(Point3d.Origin, ClampInnerDiameter / 2 + BlockThick, outterArcStartRedian, Math.PI / 2);

            // 7. 创建直线 
            Line innerEarLine = new Line(SpanRayInterPoint, new Point3d(SpanRayInterPoint.X + 100, SpanRayInterPoint.Y, SpanRayInterPoint.Z));
            Line outterEarLine = new Line(ThickRayInterPoint, new Point3d(ThickRayInterPoint.X + 100, ThickRayInterPoint.Y, ThickRayInterPoint.Z));

            // 8. 绘制圆弧圆角 
            Arc innerFillet = new Arc();

            InnerLine = innerEarLine;
            OutterLine = outterEarLine;

            RightInnerArc = innerArc;
            RightOutterArc = outterArc;

            InnerCircle = ID_Circle;
            OutterCircle = OD_Circle;
            SpanRay = spanRay;
            ThickRay = thickRay;
        }

    }

}
