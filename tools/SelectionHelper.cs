using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;


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
        /// 【按类型过滤】框选获取指定类型的实体（如 DBText, Circle, Line 等）
        /// </summary>
        /// <typeparam name="T">目标类型：DBText / Line / Circle / MText 等</typeparam>
        public static List<T> GetSelectedEntities<T>(Editor ed) where T : DBObject
        {
            var result = new List<T>();

            // 过滤条件：只选择指定类型
            var filter = new SelectionFilter(
                new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, GetRXClass<T>().DxfName)
            });

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = $"\n请框选【{typeof(T).Name}】类型实体：";

            PromptSelectionResult psr = ed.GetSelection(pso, filter);
            if(psr.Status != PromptStatus.OK) {
                return result;
            }

            using(Transaction tr = ed.Document.Database.TransactionManager.StartTransaction()) {
                foreach(SelectedObject selObj in psr.Value) {
                    if(selObj == null || !selObj.ObjectId.IsValid) {
                        continue;
                    }

                    T obj = selObj.ObjectId.GetObject(OpenMode.ForRead) as T;
                    if(obj != null) {
                        result.Add(obj);
                    }
                }
                tr.Commit();
            }

            return result;
        }

        /// <summary>
        /// 【你最需要的】直接框选获取所有 DBText（单行文字）
        /// </summary>
        public static List<DBText> GetSelectedTexts(Editor ed)
        {
            return GetSelectedEntities<DBText>(ed);
        }

        #region 内部辅助方法（获取类型RXClass）
        private static RXClass GetRXClass<T>() where T : DBObject
        {
            return RXClass.GetClass(typeof(T));
        }
        #endregion

    }

}