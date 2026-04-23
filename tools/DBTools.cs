using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/*
 * 专门用于访问数据库
 * 
 * addEntity需要重构
 */

namespace AutoCAD_2022_Plugin_Demo.tools
{

    public static class DBTools
    {

        #region 根据实体获取块参考

        public static ObjectId AddEntityToModelSpaceWithJig(this Database db, Entity entity)
        {
            /*
           * 开启事务处理
           * 在 AutoCAD 中，对数据库的所有修改操作都应该在事务中进行
           * 这是一种安全机制，可以确保一系列操作要么全部成功，要么在出错时全部回滚，保证数据一致性
           * 使用 'using' 语句可以确保事务在使用完毕后被正确释放，即使发生异常
           */
            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                ObjectId entityId = ObjectId.Null;

                try {
                    // 打开块表 BlockTable 是一个数据库表，它存储了所有块定义的记录
                    BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);

                    // 打开块表记录
                    BlockTableRecord btr = (BlockTableRecord)
                        trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 这一步是将我们在内存中创建的实体，逻辑上“放入”模型空间
                    entityId = btr.AppendEntity(entity);

                    // 更新数据
                    trans.AddNewlyCreatedDBObject(entity, true);

                    // 事务提交
                    trans.Commit();

                    Console.WriteLine($"Entity:{entityId}已成功创建！");
                }
                catch(Exception ex) {
                    // 如果在事务过程中发生任何异常，捕获并显示错误信息
                    Console.WriteLine($"创建Entity:{entityId}时发生错误:{ex.Message}");

                    // 由于我们使用了 'using' 语句，即使不手动调用 Abort()，当代码块结束时事务也会自动回滚
                }
                return entityId;
            }
        }
        #endregion

        #region 添加Entity
        public static ObjectId AddEntityToModelSpace(this Database db, Entity entity)
        {
            /*
             * 开启事务处理
             * 在 AutoCAD 中，对数据库的所有修改操作都应该在事务中进行
             * 这是一种安全机制，可以确保一系列操作要么全部成功，要么在出错时全部回滚，保证数据一致性
             * 使用 'using' 语句可以确保事务在使用完毕后被正确释放，即使发生异常
             */
            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                // 返回添加图元的ObjectId
                ObjectId entityId = ObjectId.Null;

                try {
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
                catch(Exception ex) {
                    // 如果在事务过程中发生任何异常，捕获并显示错误信息
                    Console.WriteLine($"创建Entity:{entityId}时发生错误:{ex.Message}");

                    // 由于我们使用了 'using' 语句，即使不手动调用 Abort()，当代码块结束时事务也会自动回滚
                }
                return entityId;
            }
        }

        public static ObjectId[] AddEntityToModelSpace(this Database db, params Entity[] entitys)
        {
            // 非空检查
            if(entitys == null || entitys.Length == 0) {
                return Array.Empty<ObjectId>(); // 返回空数组，避免空引用异常
            }

            // 创建一个与输入实体数组长度相同的 ObjectId 数组，用于存储结果
            ObjectId[] objectIds = new ObjectId[entitys.Length];
            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                ObjectId entityId = ObjectId.Null;
                try {
                    // 打开块表
                    BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);

                    // 打开模型空间块表记录
                    BlockTableRecord btr = (BlockTableRecord)
                        trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // 遍历所有传入的实体
                    for(int i = 0; i < entitys.Length; i++) {
                        Entity entity = entitys[i];
                        if(entity != null) {
                            // 将实体添加到模型空间
                            entityId = btr.AppendEntity(entity);

                            // 将新创建的实体通知事务，并获取其 ObjectId
                            trans.AddNewlyCreatedDBObject(entity, true);

                            // 记录当前实体的 ObjectId
                            objectIds[i] = entity.ObjectId;
                            Console.WriteLine($"Entity:{objectIds[i]}已成功创建！");
                        }
                        else {
                            // 如果传入的实体为 null，存储一个无效的 ObjectId
                            objectIds[i] = ObjectId.Null;
                        }
                    }
                    trans.Commit();
                }
                catch(Exception ex) {
                    Console.WriteLine($"创建Entity:{entityId}时发生错误:{ex.Message}");

                    // 返回空数组或 null，表示操作失败
                    return Array.Empty<ObjectId>();
                }
                return objectIds;
            }
        }

        #endregion

        #region 编辑Entity
        public static Entity[] UpdateEntityToModelSpace(this Database db, ObjectId entityId, Func<Entity, Entity[]> updater)
        {
            // 1. 输入参数有效性检查
            if(db == null) {
                throw new ArgumentNullException(nameof(db), "数据库对象不能为空。");
            }
            if(entityId.IsNull || !entityId.IsValid) {
                throw new ArgumentException($"实体ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。", nameof(entityId));
            }

            if(updater == null) {
                throw new ArgumentException("策略方法update为空");
            }

            // 2. 通过事务更新实体
            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                try {
                    /*
                     * 这里的as是C#中的类型转换运算符，主要用于安全地将一个对象转换为目标类型。
                     * 这里尝试将 trans.GetObject(...) 返回的 DBObject 对象，转换为 BlockTable 类型.
                     *  1. 如果转换成功，blockTable 变量将引用该 BlockTable 对象。
                     *  2. 如果转换失败（例如，返回的对象不是 BlockTable 类型或为 null），blockTable 变量将被赋值为 null，不会抛出 InvalidCastException 异常。
                     * 
                     * 如果直接强制转换（(BlockTable)trans.GetObject(...)）在转换失败时会抛出异常
                     * as 则返回 null，这样可以避免异常处理，让代码更简洁、更安全。
                     * 
                     */

                    // 2.1 以Write模式打开原实体 并转换为Entity类型
                    Entity originEntity = trans.GetObject(entityId, OpenMode.ForWrite) as Entity;

                    // 如果类型转换失败,entity会被赋值为null
                    if(originEntity == null || originEntity.IsErased) {
                        trans.Abort();
                        throw new ArgumentException($"ID为 {entityId} 的实体不存在或已被删除。");
                    }

                    // 2.2 获取块表
                    ObjectId blockTableId = db.BlockTableId;
                    BlockTable blockTable = trans.GetObject(blockTableId, OpenMode.ForRead) as BlockTable;
                    if(blockTable == null) {
                        trans.Abort();
                        throw new Exception("无法获取块表（BlockTable）。");
                    }

                    // 2.3 获取模型空间
                    ObjectId modelSpaceId = blockTable[BlockTableRecord.ModelSpace]; // 关键：获取模型空间ID
                    BlockTableRecord modelSpace = trans.GetObject(modelSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    if(modelSpace == null) {
                        trans.Abort();
                        throw new Exception("无法获取模型空间（ModelSpace）。");
                    }

                    // 2.4 调用updater获取操作结果
                    Entity[] entityArray = updater.Invoke(originEntity);

                    if(entityArray == null) {
                        throw new ArgumentException("策略方法返回null");
                    }
                    else {
                        // 2.4.1 修改实体 updater返回entity[0] 会进入else但不会执行下面循环
                        // 2.4.2 添加实体 -- 由updater决定
                        foreach(Entity newEntity in entityArray) {
                            if(newEntity != null) {
                                modelSpace.AppendEntity(newEntity);
                                trans.AddNewlyCreatedDBObject(newEntity, true);
                                Debug.WriteLine($"成功添加实体 {entityId}，新实体ID为 {newEntity.Id}。");
                            }
                            else {
                                Debug.WriteLine("警告：更新策略返回的实体数组中包含null元素，已跳过。");
                            }
                        }
                    }

                    // 2.5 提交事务
                    trans.Commit();

                    // 2.6 返回updater调用结果
                    return entityArray;
                }
                catch(Exception ex) {
                    trans.Abort();
                    Debug.WriteLine($"编辑实体失败: {ex.Message}");
                    throw ex;
                }
            }
        }

        #endregion

        #region 查询/删除Entity
        public static Entity GetEntityFromModelSpace(this Database db, ObjectId entityId)
        {
            // 1. 输入参数有效性检查
            if(db == null) {
                throw new ArgumentNullException(nameof(db), "数据库对象不能为空。");
            }
            if(entityId.IsNull || !entityId.IsValid) {
                throw new ArgumentException($"实体ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。", nameof(entityId));
            }

            Entity entity;
            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                try {
                    // 2.1 以Write模式打开原实体 并转换为Entity类型
                    entity = trans.GetObject(entityId, OpenMode.ForWrite) as Entity;
                }
                catch(Exception ex) {
                    trans.Abort();
                    Debug.WriteLine($"获取实体失败: {ex.Message}");
                    throw ex;
                }
            }

            return entity;
        }


        public static bool DeleteEntityToModelSpace(this Database db, ObjectId entityId)
        {
            // 1. 输入参数有效性检查
            if(db == null) {
                throw new ArgumentNullException(nameof(db), "数据库对象不能为空。");
            }
            if(entityId.IsNull || !entityId.IsValid) {
                throw new ArgumentException($"实体ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。", nameof(entityId));
            }

            // 2. 通过事务删除实体
            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                try {
                    // 2.1 以Write模式打开原实体 并转换为Entity类型
                    Entity entity = trans.GetObject(entityId, OpenMode.ForWrite) as Entity;

                    // 如果类型转换失败,entity会被赋值为null
                    if(entity == null || entity.IsErased) {
                        trans.Abort();

                        // 这种情况视为一种 “预期内的失败”
                        // throw new ArgumentException($"ID为 {entityId} 的实体不存在或已被删除。"); 
                        return false;
                    }

                    // 2.2 获取块表
                    ObjectId blockTableId = db.BlockTableId;
                    BlockTable blockTable = trans.GetObject(blockTableId, OpenMode.ForRead) as BlockTable;
                    if(blockTable == null) {
                        trans.Abort();
                        throw new Exception("无法获取块表（BlockTable）。");
                    }

                    // 2.3 获取模型空间
                    ObjectId modelSpaceId = blockTable[BlockTableRecord.ModelSpace]; // 关键：获取模型空间ID
                    BlockTableRecord modelSpace = trans.GetObject(modelSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    if(modelSpace == null) {
                        trans.Abort();
                        throw new Exception("无法获取模型空间（ModelSpace）。");
                    }

                    // 2.4 Erase删除对象
                    entity.Erase();

                    // 2.5 提交事务
                    trans.Commit();

                    // 2.6 返回true
                    return true;
                }
                catch(Exception ex) {
                    trans.Abort();
                    Debug.WriteLine($"编辑实体失败: {ex.Message}");
                    throw ex;
                }
            }
        }
        #endregion

        #region 块表
        /// <summary>
        /// 添加块表记录
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="btrName">块表名</param>
        /// <param name="entityList">块中的实体对象</param>
        /// <returns>ObjectId</returns>
        public static ObjectId AddBlockTableRecord(this Database db, string btrName, List<Entity> entityList)
        {
            ObjectId btrId = ObjectId.Null;

            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = new BlockTableRecord();

                if(!bt.Has(btrName)) {
                    // 1. 创建新的块表记录（块定义）
                    btr.Name = btrName;         // 必须给块命名（否则块表无法识别）
                    btr.Origin = Point3d.Origin;// 块原点（默认设为(0,0,0)，方便插入定位）
                    // btr中的Entity位置都是相对于btr.Origin
                    // 插入块参照时,btr.Origin = position(入参)

                    // 2. 遍历实体列表，添加到块中
                    for(int i = 0; i < entityList.Count; i++) {
                        // 将单个实体（如矩形、文字、属性定义）“添加到块表记录的实体集合中”，建立 “块→实体” 的归属关系
                        btr.AppendEntity(entityList[i]);
                    }

                    // 3. 将新块表记录添加到块表
                    btrId = bt.Add(btr); // 把块添加到块表，返回块的ObjectId
                    trans.AddNewlyCreatedDBObject(btr, true); // 注册块表记录到数据库
                }
                trans.Commit();
            }

            return btrId;
        }

        /// <summary>
        /// 根据块定义创建块参照
        /// </summary>
        /// <param name="db"></param>
        /// <param name="blockRefId"></param>
        /// <returns></returns>
        public static BlockReference GetBlockReferenceById(this Database db, ObjectId blockRefId)
        {
            BlockReference blockRef = null;

            // 1. 校验 ID 是否有效
            if(blockRefId == ObjectId.Null)
    {
                return null;
            }

            using(Transaction trans = db.TransactionManager.StartTransaction())
    {
                try
        {
                    // 2. 通过 ID 获取对象
                    // 注意：这里使用 OpenMode.ForRead，因为我们只是读取数据，不修改它
                    // 如果你后续需要修改这个块参照的属性（如位置、旋转角），请改为 OpenMode.ForWrite
                    DBObject obj = trans.GetObject(blockRefId, OpenMode.ForRead);

                    // 3. 类型检查与转换
                    if(obj is BlockReference)
            {
                        blockRef = obj as BlockReference;
                    }
                    else
            {
                        // 如果传入的 ID 对应的不是块参照（比如是线条或文字），则返回 null
                        // 也可以根据需要抛出异常
                        // throw new Exception("传入的 ObjectId 不是一个块参照 (BlockReference)");
                    }
                }
                catch(Exception)
        {
                    // 处理可能的异常（例如 ID 指向的对象已被删除）
                    trans.Abort();
                    return null;
                }

                // 注意：这里不 Commit 也不 Abort
                // 因为只是读取操作，事务结束时对象会自动关闭。
                // 如果你需要保持对象打开以便后续操作，通常不在此处使用 using (Transaction) 包裹，
                 // 或者需要在外部管理事务。但在工具函数中，通常读取完即释放。
    }

            return blockRef;
        }

        /// <summary>
        /// 向模型空间插入块参照
        /// </summary>
        /// <param name="db">数据库</param>
        /// <param name="btrId">块引用</param>
        /// <param name="position">插入位置</param>
        /// <returns>失败返回 ObjectId.Null</returns>
        public static ObjectId AddBlockReferenceToModelSpace(this Database db, ObjectId btrId, Point3d position)
        {
            ObjectId refId = ObjectId.Null;

            if(btrId == ObjectId.Null) {
                return refId;

                // throw new Exception("当前块未定义");
            }
            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                // 打开块表
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                if(bt.Has(btrId)) {
                    // 创建块参照: 里面包含插入块的位置和图形信息
                    BlockReference blockRef = new BlockReference(position, btrId);

                    // 打开模型空间
                    BlockTableRecord modelSpace = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // 添加块参照(块参照对应就是cad中的块)
                    refId = modelSpace.AppendEntity(blockRef);          // AppendEntity = 把实体放到模型空间里 || 把家具搬进房间 
                    trans.AddNewlyCreatedDBObject(blockRef, true);      // AddNewlyCreatedDBObject = 把实体交给事务管理 || 给家具登记备案
                }

                trans.Commit();
            }

            return refId;
        }

        /// <summary>
        /// 通过块名获取ObjectId
        /// </summary>
        /// <param name="db"></param>
        /// <param name="btrName"></param>
        /// <returns></returns>
        public static ObjectId GetBlockIdByName(this Database db, string btrName)
        {
            ObjectId btrId = ObjectId.Null;

            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                if(bt.Has(btrName)) {
                    btrId = bt[btrName];
                }
            }

            return btrId;
            #endregion
        }


        /// <summary>
        /// 根据块名称获取块定义
        /// </summary>
        /// <param name="db"></param>
        /// <param name="blockName"></param>
        /// <returns></returns>
        public static BlockTableRecord GetBlockTableRecordByName(this Database db, string blockName)
        {
            if(string.IsNullOrEmpty(blockName)) {
                return null;
            }

            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if(bt.Has(blockName)) {
                    BlockTableRecord blockDef = trans.GetObject(bt[blockName], OpenMode.ForRead) as BlockTableRecord;
                    trans.Commit();
                    return blockDef;
                }

                trans.Commit();
                return null;
            }
        }

    }

}