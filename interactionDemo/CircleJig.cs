using AutoCAD_2022_Plugin_Demo.EntityDemo.domain;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.interactionDemo
{

    public class CircleJig : EntityJig
    {

        // 初始化CircleJig,会触发update,若此时jRadius还是初始值0,会报错
        public double jRadius { get; set; } = 1e-6;


        public CircleJig(Point3d center)
        : base(new Circle()) // 调用基类 EntityJig 的构造函数,传入一个要 “动态交互” 的实体（Entity）
        {
            // Entity 属性是 基类 EntityJig 提供的内置属性，核心作用是「持有并暴露你在构造时传给 EntityJig 的那个实体对象」
            ((Circle)Entity).Center = center;
        }


        /*
         * 在构造函数中设置this.Entity.center;在update()中设置this.Entity.radisu
         * constructor() {Entity.Center = center(入参)}
         * sampler(){jr = NewR } ==> update(){ Entity.r = jr}
         */
        public Circle GetCircle()
        {
            return Entity as Circle;
        }


        /// <summary>
        /// 采集用户输入，比如拖动半径
        /// </summary>
        /// <param name="prompts"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            // 设置拖拽选项
            JigPromptPointOptions jigOptions = new JigPromptPointOptions();
            jigOptions.Message = "\n请指定圆上一点[取消(U)]";

            /*
             * RubberBand（结合 BasePoint 使用时，会显示从基准点到当前鼠标位置的 “动态虚线”）
             * Crosshair  标准十字光标（CAD 最常用的默认光标，X/Y 轴交叉线）
             * RectangularCursor 矩形光标（类似文本编辑器的 “I” 型光标，但为矩形框）
             */
            jigOptions.Cursor = CursorType.RubberBand; // 设置基点光标提示
            jigOptions.BasePoint = ((Circle)Entity).Center;
            jigOptions.UseBasePoint = true;

            jigOptions.AppendKeywordsToMessage = false;
            jigOptions.Keywords.Add("U"); // 设置取消快捷键

            // 获取用户输入
            PromptPointResult jigResult = prompts.AcquirePoint(jigOptions);

            // 根据输入类型更新或终止循环
            if(jigResult.Status == PromptStatus.OK) {
                double newRadius = jigResult.Value.GetDistanceBetweenTwoPoint(((Circle)Entity).Center);
                if(Math.Abs(newRadius - jRadius) < 1e-6) {
                    return SamplerStatus.NoChange;
                }
                else {
                    jRadius = newRadius;     // 更新到属性jRadius
                    return SamplerStatus.OK; // 仅在Sampler()返回OK时,调用Update
                }
            }
            else if(jigResult.Status == PromptStatus.Keyword) {
                switch(jigResult.StringResult) {
                    // 设置keyword终止循环
                    case "U":
                        return SamplerStatus.Cancel;
                    default:
                        return SamplerStatus.NoChange;
                }
            }
            else {
                return SamplerStatus.NoChange;
            }
        }

        /// <summary>
        /// 根据用户输入更新实体状态
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override bool Update()
        {
            // 初始化时会调用update，若jRadius初值为0则需要判断
            // 也可以将初值设置为1e-6
            if(jRadius > 0) {
                ((Circle)Entity).Radius = jRadius;
            }

            return true; // 系统调用update更新实体状态,重新绘制图形
        }

    }

}
