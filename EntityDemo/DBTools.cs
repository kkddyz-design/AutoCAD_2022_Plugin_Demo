using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Diagnostics;
using System.Linq;

/*
 * 专门用于访问数据库
 * 
 * addEntity需要重构
 */

namespace AutoCAD_2022_Plugin_Demo.EntityDemo
{

    public static class DBTools
    {

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

        /*
         * params 是 C# 中的一个关键字，它允许方法接收可变数量的参数，这些参数会被自动封装成一个数组。
         * 没有 params 的情况下，如果你想让方法支持添加 1 个、2 个或多个实体，你可能需要编写多个重载方法：
         * 这显然非常繁琐且不灵活。
         *
         * params 的使用方式：
         *  1.传入单个实体
         *  Line line = new Line(...);
         *  db.AddEntityToModelSpace(line);
         *
         *  2.传入多个实体，用逗号分隔
         *  Line line1 = new Line(...);
         *  Circle circle = new Circle(...);
         *  Text text = new Text(...);
         *  db.AddEntityToModelSpace(line1, circle, text);
         *
         *  3.传入一个实体数组
         *  Entity[] entities = new Entity[]
         *  { new Line(...),new Circle(...),new Text(...)};
         *  db.AddEntityToModelSpace(entities);
         *
         *  编译器会自动将前两种方式（传入单个或多个实体）转换为第三种方式
         *
         *  使用 params 的注意事项：
         *  1.params 参数必须是方法的最后一个参数。
         *  2.一个方法只能有一个 params 参数。
         *
         *   方法内部如何处理entitys：
         *   在方法内部，entitys 参数的类型是 Entity[]（一个 Entity 数组）。你可以像处理普通数组一样遍历它
         */
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

    }

}