using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo
{

    public class BlockTools

    {

        /// <summary>
        /// 批量将 DWG 文件中所有动态块转为静态块（保留图元、属性，移除动态特性）
        /// </summary>
        /// <param name="doc">当前 AutoCAD 文档（可通过 Application.DocumentManager.MdiActiveDocument 获取）</param>
        /// <returns>转换成功的动态块名称列表</returns>
        public List<string> ConvertAllDynamicBlocksToStatic()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            // 创建容器存储转换的动态块名称
            List<string> convertedBlockNames = new List<string>();

            if(doc == null) {
                throw new ArgumentNullException("文档不能为空");
            }

            Database db = doc.Database;
            Editor ed = doc.Editor;
            ed.WriteMessage("\n开始批量转换动态块为静态块...\n");

            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                // 1. 打开块表（所有块定义存储在块表中）
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if(bt == null) {
                    throw new Exception("无法获取块表");
                }

                // 2. 收集所有动态块定义的 ObjectId
                List<ObjectId> dynamicBlockIds = new List<ObjectId>();

                // 2.1 遍历访问块定义，将动态块添加到dynamicBlockIds
                foreach(ObjectId blockId in bt) {
                    BlockTableRecord btr = trans.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;
                    if(btr == null) {
                        continue;
                    }

                    // 判断是否为动态块：动态块的块表记录包含 "AcDbDynamicBlockReference" 相关字典
                    if(btr.IsDynamicBlock) {
                        dynamicBlockIds.Add(blockId);
                        ed.WriteMessage($"发现动态块：{btr.Name}\n");
                    }
                }

                // 3. 逐个转换动态块为静态块
                foreach(ObjectId dynamicBlockId in dynamicBlockIds) {
                    BlockTableRecord dynamicBtr = trans.GetObject(dynamicBlockId, OpenMode.ForRead) as BlockTableRecord;
                    string blockName = dynamicBtr.Name;

                    try {
                        // 3.1 创建新的静态块定义（复制原动态块的图元、属性，移除动态参数）
                        BlockTableRecord staticBtr = CreateStaticBlockFromDynamic(dynamicBtr, trans);
                        if(staticBtr == null) {
                            continue;
                        }

                        // 3.2 替换原动态块定义

                        dynamicBtr.Erase(); // 删除动态块定义

                        bt.Add(staticBtr); // 添加新静态块定义
                        trans.AddNewlyCreatedDBObject(staticBtr, true); // 注册新对象到事务

                        // 3.3 更新所有已插入的块引用（确保图纸中现有块实例同步为静态块）
                        UpdateBlockReferences(db, trans, blockName);

                        convertedBlockNames.Add(blockName);
                        ed.WriteMessage($"成功转换动态块：{blockName}\n");
                    }
                    catch(Exception ex) {
                        ed.WriteMessage($"转换失败 {blockName}：{ex.Message}\n");
                        continue;
                    }
                }

                trans.Commit();
                ed.WriteMessage($"\n转换完成！共成功转换 {convertedBlockNames.Count} 个动态块\n");
            }

            return convertedBlockNames;
        }

        #region 辅助方法（核心逻辑支撑）
        /// <summary>
        /// 判断块表记录是否为动态块
        /// </summary>


        /// <summary>
        /// 从动态块创建静态块（复制图元、属性，移除动态参数）
        /// </summary>
        private BlockTableRecord CreateStaticBlockFromDynamic(BlockTableRecord dynamicBtr, Transaction trans)
        {
            // 创建新的静态块表记录
            BlockTableRecord staticBtr = new BlockTableRecord();
            staticBtr.Name = dynamicBtr.Name;       // 保持块名称不变
            staticBtr.Origin = dynamicBtr.Origin;   // 保持块原点一致
            staticBtr.Units = dynamicBtr.Units;     // 保持单位一致

            // 复制原动态块中的所有图元（线条、文字、属性定义等）
            foreach(ObjectId entId in dynamicBtr) {
                Entity ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;
                if(ent == null) {
                    continue;
                }

                // 跳过动态块特有的参数图元（如拉伸参数、旋转参数的夹点图元）
                if(ent is AttributeDefinition || ent is Line || ent is Circle || ent is DBText || ent is MText) {
                    Entity entClone = ent.Clone() as Entity; // 克隆图元
                    staticBtr.AppendEntity(entClone); // 添加到新静态块
                    trans.AddNewlyCreatedDBObject(entClone, true);
                }
            }

            return staticBtr;
        }

        /// <summary>
        /// 更新图纸中所有该名称的块引用（确保同步为静态块）
        /// </summary>
        private void UpdateBlockReferences(Database db, Transaction trans, string blockName)
        {
            // 打开模型空间和布局空间，遍历所有块引用
            BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            foreach(ObjectId btrId in bt) {
                // 获取块表记录
                BlockTableRecord btr = trans.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                if(!btr.IsLayout) {
                    continue; // 仅处理布局（模型空间+图纸空间）
                }

                // 遍历布局中的所有实体，找到该块的引用
                foreach(ObjectId entId in btr) {
                    BlockReference br = trans.GetObject(entId, OpenMode.ForWrite) as BlockReference;
                    if(br == null) {
                        continue;
                    }

                    // 若块引用指向原动态块，更新为新静态块
                    if(br.Name.Equals(blockName, StringComparison.OrdinalIgnoreCase)) {
                        // 重新设置块引用的块定义 ID（指向新静态块）
                        ObjectId staticBlockId = bt[blockName];
                        br.BlockTableRecord = staticBlockId;
                    }
                }
            }
        }
        #endregion

    }

}
