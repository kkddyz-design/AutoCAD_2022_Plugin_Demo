using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.tools
{

    /// <summary>
    /// AutoCAD 选择集工具类 功能：框选/点选获取实体，支持按类型过滤
    /// </summary>
    public static class SelectionHelper
    {

        /// <summary>
        /// 【通用】用户框选，返回所有选中的 DBObject
        /// </summary>
        public static List<DBObject> GetSelectedEntities(Editor ed)
        {
            var result = new List<DBObject>();

            // 框选设置
            var pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\n请框选需要处理的实体：";

            // 获取选择集
            PromptSelectionResult psr = ed.GetSelection(pso);
            if(psr.Status != PromptStatus.OK) {
                return result;
            }

            // 遍历选择集
            using(Transaction tr = ed.Document.Database.TransactionManager.StartTransaction()) {
                foreach(SelectedObject selObj in psr.Value) {
                    if(selObj == null || !selObj.ObjectId.IsValid) {
                        continue;
                    }

                    // 打开实体（只读模式）
                    DBObject obj = selObj.ObjectId.GetObject(OpenMode.ForRead);
                    if(obj != null) {
                        result.Add(obj);
                    }
                }
                tr.Commit();
            }

            return result;
        }

        /// <summary>
        /// 【固定为 DBText】框选获取所有单行文字
        /// </summary>
        public static List<DBText> GetSelectedTexts(Editor ed)
        {
            List<DBText> textList = new List<DBText>();

            // 过滤：只选文字
            SelectionFilter filter = new SelectionFilter(
                new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "TEXT")
            });

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\n请框选【文字】实体：";

            PromptSelectionResult psr = ed.GetSelection(pso, filter);
            if(psr.Status != PromptStatus.OK) {
                return textList;
            }

            // ==========================================
            // 第一步：先把所有文字 全部读出来（事务内）
            // ==========================================
            using(Transaction tr = ed.Document.Database.TransactionManager.StartTransaction()) {
                foreach(SelectedObject selObj in psr.Value) {
                    if(!selObj.ObjectId.IsValid) {
                        continue;
                    }

                    DBText text = selObj.ObjectId.GetObject(OpenMode.ForRead) as DBText;
                    if(text != null) {
                        textList.Add(text);
                    }
                }
                tr.Commit();
            }

            return textList;

            // ==========================================
            // 第二步：全部读完后 → 统一排序（正确位置！）
            // ==========================================
            // List<DBText> sortedList = textList
            // .OrderByDescending(t => t.Position.Y)  // 先按 Y 降序（上→下）
            // .ThenBy(t => t.Position.X)             // 再按 X 升序（左→右）
            // .ToList();

            // return sortedList;
        }


        /// <summary>
        /// 对DBText按表格行列排序，返回 二维列表（行→列） 第一行 = CAD最上方行 每行内部 = 从左到右
        /// </summary>
        public static List<List<DBText>> SortTextsByPosition(List<DBText> textList)
        {
            // 空数据返回空二维列表
            if(textList == null || textList.Count == 0) {
                return new List<List<DBText>>();
            }

            // 同一行容差（Y坐标差距 <100 视为同一行，可根据你的图纸调整）
            double rowTolerance = textList.First().Height;

            // 核心逻辑：分组 → 排序行 → 排序列内 → 转二维列表
            var twoDimensionalList = textList
                // 按Y坐标分组（识别每一行）
                .GroupBy(t => Math.Round(t.Position.Y / rowTolerance) * rowTolerance)

                // 行从上到下（Y大的排前面）
                .OrderByDescending(g => g.Key)

                // 每行内部：从左到右排序
                .Select(rowGroup => rowGroup.OrderBy(t => t.Position.X).ToList())

                // 转二维列表
                .ToList();

            return twoDimensionalList;
        }

    }

}