using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.tools
{

    /// <summary>
    /// 调用Autocad原生命令
    /// </summary>
    public static class AutoCadTools
    {

        /// <summary>
        /// 调用圆角命令，仅通过传入的两个对象ID实现精准圆角（无手动选择）
        /// </summary>
        /// <param name="id1">第一个对象ID（直线/圆弧/圆，必须有效）</param>
        /// <param name="id2">第二个对象ID（直线/圆弧/圆，必须有效）</param>
        /// <param name="filletRadius">圆角半径（默认10）</param>
        public static void ExecuteFilletByObjectId(ObjectId id1, ObjectId id2, double filletRadius)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 1. 严格校验对象ID有效性（核心：避免无效ID导致命令失败）
            if(!id1.IsValid || !id2.IsValid) {
                ed.WriteMessage("\n错误：传入的对象ID无效！");
                return;
            }

            // 2. 校验圆角半径合法性
            if(filletRadius <= 0) {
                ed.WriteMessage("\n错误：圆角半径必须大于0！");
                return;
            }

            try {
                // 3. 调用FILLET命令，直接传入对象ID（无手动选择）
                ed.Command(
                    "_.FILLET",       // 圆角命令（兼容多语言版本）
                    "_R",             // 选择“半径”选项
                    filletRadius,     // 设置圆角半径
                    string.Empty,     // 按Enter确认半径设置
                    id1,              // 第一个对象ID（关键参数）
                    id2,              // 第二个对象ID（关键参数）
                    string.Empty      // 结束命令
                );

                ed.WriteMessage($"\n圆角操作完成！\n半径：{filletRadius}\n对象1 ID：{id1}\n对象2 ID：{id2}");
            }
            catch(Exception ex) {
                ed.WriteMessage($"\n圆角操作失败：{ex.Message}");
            }
        }

    }

}
