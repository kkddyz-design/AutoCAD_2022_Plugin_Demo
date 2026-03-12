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


        public Arc InnerRefArc { get; set; }

        public Arc OuterRefArc { get; set; }

        public Circle InnerRefCircle { get; set; }

        public Circle OuterRefCircle { get; set; }

        public Ray SpanRay { get; set; }

        public Ray ThickRay { get; set; }

        public Line InnerRefLine { get; set; }

        public Line OuterRefLine { get; set; }

        public Arc OuterFilletArc { get; set; }

        public Arc InnerFilletArc { get; set; }

        public Line OutEar { get; set; }

        public Line InnerEar { get; set; }

        public Arc InnerArc { get; set; }

        public Arc OuterArc { get; set; }


        public Line EdgeLine { get; set; }

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

            double innerRefArcStartRedian = Point3d.Origin.GetRadiansToXAxis(SpanRayInterPoint);
            Arc innerRefArc = new Arc(Point3d.Origin, ClampInnerDiameter / 2, innerRefArcStartRedian, Math.PI / 2);

            double outerRefArcStartRedian = GeometryTools.GetRadiansToXAxis(Point3d.Origin, ThickRayInterPoint);
            Arc outerRefArc = new Arc(Point3d.Origin, ClampInnerDiameter / 2 + BlockThick, outerRefArcStartRedian, Math.PI / 2);

            // 7. 创建直线 
            Line innerRefLine = new Line(SpanRayInterPoint, new Point3d(SpanRayInterPoint.X + 100, SpanRayInterPoint.Y, SpanRayInterPoint.Z));
            Line outerRefLine = new Line(ThickRayInterPoint, new Point3d(ThickRayInterPoint.X + 100, ThickRayInterPoint.Y, ThickRayInterPoint.Z));

            // 8. 计算圆角的切点绘制圆弧

            // 创建管夹顶面的圆角
            Point3d[] outerTangentPoints = GeometryTools.GetFilletTangentPoints(outerRefArc, outerRefLine, ClampFilletRadius);

            // 根据切点计算圆弧起始弧度
            Point3d outerFilletCenter = outerTangentPoints[0];
            Point3d outerArcTangentPoint = outerTangentPoints[1];
            Point3d outerLineTangentPoint = outerTangentPoints[2];

            double outerFilletstartRedian = outerFilletCenter.GetRadiansToXAxis(outerArcTangentPoint);
            double outerFilletEndRedian = outerFilletCenter.GetRadiansToXAxis(outerLineTangentPoint);

            Arc outerFilletArc = new Arc(outerFilletCenter, ClampFilletRadius, outerFilletstartRedian, outerFilletEndRedian);
            OuterFilletArc = outerFilletArc;

            // 9. 绘制ear部分
            Line outerEar = new Line(outerLineTangentPoint, new Point3d(outerLineTangentPoint.X + ClampEar, outerLineTangentPoint.Y, outerLineTangentPoint.Z));

            // 10. 同理 绘制下部分的圆弧和ear

            // 创建管夹底面的圆角
            Point3d[] innerTangentPoint = GeometryTools.GetFilletTangentPoints(innerRefArc, innerRefLine, ClampFilletRadius);

            // 根据切点计算圆弧起始弧度
            Point3d innerFilletCenter = innerTangentPoint[0];
            Point3d innerArcTangentPoint = innerTangentPoint[1];
            Point3d innerLineTangentPoint = innerTangentPoint[2];

            double innerFilletstartRedian = innerFilletCenter.GetRadiansToXAxis(innerArcTangentPoint);
            double innerFilletEndRedian = innerFilletCenter.GetRadiansToXAxis(innerLineTangentPoint);

            Arc innerFilletArc = new Arc(innerFilletCenter, ClampFilletRadius, innerFilletstartRedian, innerFilletEndRedian);
            InnerFilletArc = innerFilletArc;

            // 绘制ear部分 -- 注意长度
            Line innerEar = new Line(innerLineTangentPoint, new Point3d(outerEar.EndPoint.X, innerLineTangentPoint.Y, innerLineTangentPoint.Z));

            // 11 计算管夹圆弧
            double innerArcStartRedian = Point3d.Origin.GetRadiansToXAxis(innerArcTangentPoint);
            double outerArcStartRedian = Point3d.Origin.GetRadiansToXAxis(outerArcTangentPoint);
            Arc innerArc = new Arc(Point3d.Origin, ClampInnerDiameter / 2, innerArcStartRedian, Math.PI / 2);
            Arc outerArc = new Arc(Point3d.Origin, ClampInnerDiameter / 2 + BlockThick, outerArcStartRedian, Math.PI / 2);

            // 12.绘制边线，完成闭合 
            Line edgeLine = new Line(innerEar.EndPoint, outerEar.EndPoint);

            // 13. 镜像

            EdgeLine = edgeLine;
            InnerArc = innerArc;
            OuterArc = outerArc;
            InnerEar = innerEar;
            OutEar = outerEar;
            InnerRefLine = innerRefLine;
            OuterRefLine = outerRefLine;

            InnerRefArc = innerRefArc;
            OuterRefArc = outerRefArc;

            InnerRefCircle = ID_Circle;
            OuterRefCircle = OD_Circle;
            SpanRay = spanRay;
            ThickRay = thickRay;
        }

    }

}
