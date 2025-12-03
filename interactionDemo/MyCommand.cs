using AutoCAD_2022_Plugin_Demo.EntityDemo;
using AutoCAD_2022_Plugin_Demo.EntityDemo.service;
using AutoCAD_2022_Plugin_Demo.interactionDemo;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;


[assembly: CommandClass(typeof(MyCommand))]


namespace AutoCAD_2022_Plugin_Demo.interactionDemo
{

    /// <summary>
    /// 学习使用命令行进行用户交互
    /// </summary>
    public static class MyCommand
    {

        #region 学习PromptPointOptions PromptPointResult
        /// <summary>
        /// 学习PromptPointOptions对象，PromptPointResult
        /// </summary>
        [CommandMethod("InteractionDemo1")]
        public static void InteractionDemo1()
        {
            /*
             * 1. 获取Editor对象
             * Editor对象是用户与CAD绘图区交互的中介
             *  1. 向命令行输出文本 ed.writeMessage()
             *  2. 获取用户输入 GetPoint()
             *  3. 选择对象GetSelection
             *  4. 执行cad内置命令 ed.Command()
             *  5. 交互确认,弹窗提示
             */
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            /*
             * 2. 创建PromptPointResult对象
             * 封装用户通过命令行 / 绘图区输入点的最终结果（包括输入的点坐标、操作状态、快捷键触发信息等），
             * 是开发者判断用户操作并执行后续逻辑的关键依据。
             */

            // 3. 配置 PromptPointOptions
            PromptPointOptions options = new PromptPointOptions("\n请选择点:[确认(A) /  取消(B)]")
            {
                // 常用 - 允许输入空值
                AllowNone = true,

                // 常用 - 初始化options设置msg，keywrods.add设置快捷键；这种方式添加快捷键需要关闭
                AppendKeywordsToMessage = false,

                // 常用 - UseBasePoint : 是否启用「基准点模式」
                UseBasePoint = false,

                BasePoint = new Point3d(0, 0, 0),

                // 常用 - UseDashedLine : 是否在「基准点→光标」之间显示虚线预览（视觉辅助，提升交互体验）
                // 该属性仅在基准点模式下生效,若未启用，即使设为 true 也不会显示虚线。
                UseDashedLine = true,

                // 不常用 - LimitsChecked : 是否检查输入点是否在「图形界限（LIMITS）」内（超出则视为无效输入）
                // 需要配合Limit命令使用：设置一个矩形边界（左下角 + 右上角坐标），作为 2D 绘图的 “有效区域”
                // 启用界限检查后，超出该区域的点会被拒绝，常用于图纸模板（如 A4、A3 纸的尺寸），避免绘图超出纸张范围。

                // 慎用 - AllowArbitraryInput : 是否允许「任意格式输入」（包括非标准坐标字符串，AutoCAD 会尝试解析）
                AllowArbitraryInput = false,
                LimitsChecked = false
            };

            // 2. 添加快捷键
            // ppOptions.SetMessageAndKeywords(
            // "\n请选择点:[确认(A)/取消(B)]",  // 提示文本（含快捷键占位符）
            // "A,B"                                     // 快捷键列表
            // );
            options.Keywords.Add("A");
            options.Keywords.Add("B");

            // 3. 执行点输入 给GetPoint配置不同的option，就可以实现不同的快捷键提示
            PromptPointResult result = ed.GetPoint(options);

            // 先判断操作状态，只有Status=OK时，ppr.Value才有效
            switch(result.Status) {
                case PromptStatus.Keyword:

                    // 处理快捷键逻辑
                    string keyword = result.StringResult.ToUpper(); // 统一转为大写
                    switch(keyword) {
                        case "B":
                            ed.WriteMessage("\n触发快捷键B");
                            break;
                        case "A":
                            ed.WriteMessage("\n触发快捷键A");
                            break;
                    }
                    break;

                case PromptStatus.OK:

                    // 正常选择点的逻辑
                    Point3d pt = result.Value;
                    ed.WriteMessage($"\n选择的点：({pt.X}, {pt.Y})");
                    break;

                case PromptStatus.Cancel:

                    // 用户按ESC取消
                    ed.WriteMessage("\n用户取消操作");
                    break;

                case PromptStatus.None:

                    // 在这里做默认处理,给点设置一个默认值
                    ed.WriteMessage("\n当前没有选择点");
                    break;
            }
        }

        #endregion


        #region 仿直线命令

        [CommandMethod("LineDemo")]
        /// <summary>
        /// 仿直线命令
        /// </summary>
        public static void LineDemo()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 直线列表 让内存和db保持一致
            List<Line> lineList = new List<Line>();

            // 当用户输入C，isColsed = true，直线未闭合,一直循环写入
            bool isColsed = false;

            #region 获取第一个点

            Point3d pointStart = new Point3d(100, 100, 0);

            // 第一个点
            PromptPointResult ppr = ed.GetPoint("\n指定第一个点: ", new string[] { });

            // 默认起点为上一次输入的点
            Point3d pointPre;

            // 输入ESC，直接退出
            if(ppr.Status == PromptStatus.Cancel) {
                return;
            }
            else if(ppr.Status == PromptStatus.None) {
                // 如果第一个点为空值,就做默认赋值
                pointPre = pointStart;
            }
            else if(ppr.Status == PromptStatus.OK) {
                pointPre = ppr.Value;
            }
            else {
                return;
            }
            #endregion

            // U操作会把当前的pre设置为直线列表中最后一条直线的起点 
            while(!isColsed) {
                if(lineList.Count >= 2) {
                    ppr = ed.GetPoint("\n指定下一点或[闭合[C]/放弃(U)]", pointPre, new string[] { "C", "U" });
                }
                else {
                    ppr = ed.GetPoint("\n指定下一点或[放弃(U)]", pointPre, new string[] { "U" });
                }

                // 获取pointNext 将pointPre点作为基点
                Point3d pointNext;

                switch(ppr.Status) {
                    case PromptStatus.Cancel:
                        return; // 退出
                    case PromptStatus.None:
                        return; // 退出
                    case PromptStatus.Keyword:
                        string keyword = ppr.StringResult.ToUpper(); // 统一转为大写
                        switch(keyword) {
                            case "U":
                                if(lineList.Count == 0) {
                                    ed.WriteMessage($"用户输入快捷键{keyword},取消当前操作");

                                    // 还原到最初状态 
                                    ppr = ed.GetPoint("\n指定第一个点: ", new string[] { });

                                    if(ppr.Status == PromptStatus.Cancel) {
                                        return;
                                    }
                                    else if(ppr.Status == PromptStatus.None) {
                                        // 如果第一个点为空值,就做默认赋值
                                        pointPre = pointStart;
                                    }
                                    else if(ppr.Status == PromptStatus.OK) {
                                        pointPre = ppr.Value;
                                    }
                                    else {
                                        return;
                                    }
                                }
                                else // count > 0 移除线
                                {
                                    // RemoveLastLine在内存和数据中同时删除
                                    Line lastLine = RemoveLastLine(db, lineList);
                                    pointPre = lastLine.StartPoint;
                                }
                                break;
                            case "C":
                                isColsed = true;

                                // 将直线收尾闭合 
                                Point3d startPoint = lineList.First().StartPoint;
                                Point3d endPoint = lineList.Last().EndPoint;
                                lineList.Add(new Line(startPoint, endPoint)); // 好像没啥用这个
                                db.AddLineToModelSpace(startPoint, endPoint);
                                return; // 闭合直接退出命令
                        }
                        break;
                    case PromptStatus.OK:

                        pointNext = ppr.Value;

                        // 添加到list
                        Line newLine = new Line(pointPre, pointNext);
                        lineList.Add(newLine);
                        db.AddEntityToModelSpace(newLine);

                        // 更新基点
                        pointPre = pointNext;
                        break;
                    default:
                        break;
                }
            }
        }


        // 删除绘制的最后一条直线
        private static Line RemoveLastLine(Database db, List<Line> lineList)
        {
            Line lastLine = lineList.Last();

            lineList.Remove(lastLine);
            db.DeleteEntityToModelSpace(lastLine.ObjectId);

            return lastLine;
        }
        #endregion

        #region 仿圆命令
        [CommandMethod("MyCircle1")]
        public static void MyCircle1()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            #region 获取圆心
            Point3d center = new Point3d();

            PromptPointOptions ppOptions = new PromptPointOptions("\n 请指定圆心");
            PromptPointResult pointResult = ed.GetPoint(ppOptions);
            if(pointResult.Status == PromptStatus.OK) {
                center = pointResult.Value;
            }
            if(pointResult.Status == PromptStatus.Cancel) {
                return;
            }

            #endregion

            CircleJig circleJig = new CircleJig(center);

            // PromptResult.value是Point3d
            // EntityJig子类对象中sampler方法获得的距离只是临时的，不会返回。
            // 因而这里PromptResult只会返回基类，而不是任何子类。
            // 提供GetCircle,直接返回circleJig.Entity
            PromptResult promptResult = ed.Drag(circleJig);

            if(promptResult.Status == PromptStatus.OK) {
                db.AddEntityToModelSpace(circleJig.GetCircle());
            }
            else {
                // do nothing
            }
        }
        #endregion

    }

}