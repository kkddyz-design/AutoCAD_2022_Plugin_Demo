using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Diagnostics;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.modify
{

    public static class ModifyEntityTools
    {

        // update委托,通过参数将orginEntity修改为newEntity 
        public delegate Entity EntityUpdater(Entity orginEntity);


        /*
         * 
         * 模型层(Domain Model) originEntity --> Entity[] result
         * 1. return Entity[0] 编辑不产生新对象
         * 2. return Entity[1] 编辑产生新对象,保存
         * 3. return null      方法
         * 
         * 数据访问层
         * UpdateEntityToModelSpace对数据库进行更新(添加)操作,
         * 由业务层传入设置的updater实现决定更新的策略
         * 
         * 业务层
         * 1.  决定由自己还是调用UpdateEntity在数据库中更新
         * 2.1 决定策略updater对象
         * 2.2 决定是否保留原对象(在业务层中决定这个逻辑,Model层只负责执行编辑,编辑以外的事不考虑)
         * 3. 处理updater的返回值
         *  3.1 return null         返回void
         *  3.2 return Entity[1]    返回Entity
         *  3.3 return Entity[2]    返回Entity[]
         *  
         *  数据访问层和模型层解耦,他们之间的调用通过委托实现，而委托变量由业务层决定。
         *  
         *  
         */
        public static Entity[] UpdateEntityToModelSpace(this Database db, ObjectId entityId, bool keepOriginal, Func<Entity, Entity[]> updater)
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

            // 2. 使用事务来复制实体
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

                    // 2.4.3 删除原实体 -- 可选由业务层传入keepOriginal决定
                    if(!keepOriginal) {
                        originEntity.Erase();
                    }

                    // 2.6 提交事务
                    trans.Commit();

                    // 2.7 返回updater调用结果
                    return entityArray;
                }
                catch(Exception ex) {
                    trans.Abort();
                    Debug.WriteLine($"编辑实体失败: {ex.Message}");
                    throw ex;
                }
            }
        }

        /*
         * (常调用) 更新后保留原对象
         */
        public static Entity[] UpdateEntityToModelSpace(this Database db, ObjectId entityId, Func<Entity, Entity[]> updater)
        {
            bool keepOriginal = true;
            return UpdateEntityToModelSpace(db, entityId, keepOriginal, updater);
        }

        /// <summary>
        /// 智能地修改实体颜色。 让Entity的扩展方法作为 “主入口”，内部根据实体状态， 调用Database的扩展方法来处理“已存在于数据库中” 的情况。
        /// </summary>
        /// <param name="entity">要修改颜色的实体对象</param>
        /// <param name="colorIndex">ACI颜色索引（1-255）</param>
        public static void ChangeEntityColor(this Entity entity, short colorIndex)
        {
            // 判断实体是否已被添加到数据库
            if(entity.IsNewObject) {
                // 情况A：实体是新创建的
                EntityModifiers.ChangeColor(entity, colorIndex);
            }
            else {
                // 情况B：实体已存在于数据库中 调用db扩展方法访问数据库 
                entity.Database.UpdateEntityToModelSpace(entity.Id, (originalEntity) =>
                                                                    EntityModifiers.ChangeColor(originalEntity, colorIndex));
            }
        }


        /// <summary>
        /// 复制实体调用入口。如果实体是新创建的（未添加到数据库），则直接克隆并移动。 如果实体已存在于数据库中，则通过事务安全地克隆并移动。
        /// </summary>
        /// <param name="entity">要复制的源实体对象。</param>
        /// <param name="basePoint">复制的基点（源实体上的参考点）。</param>
        /// <param name="targetPoint">新实体的目标位置点。</param>
        public static Entity CopyEntity(this Entity entity, Point3d basePoint, Point3d targetPoint)
        {
            Entity copyedEntity = null;
            if(entity.IsNewObject) {
                // 情况A：实体是新创建的
                copyedEntity = EntityModifiers.CopyEntity(entity, basePoint, targetPoint)[0];
            }
            else {
                // 情况B：实体已存在于数据库中 调用Database的扩展方法来执行复制
                copyedEntity = entity.Database.
                    UpdateEntityToModelSpace(entity.Id, (oriainglEntity) =>
                                                        EntityModifiers.CopyEntity(oriainglEntity, basePoint, targetPoint))[0];
            }
            return copyedEntity;
        }

        public static Entity MirrorEntity(this Entity entity, Point3d pointA, Point3d pointB, bool keepOriginal)
        {
            Entity mirroredEntity = null;
            if(entity.IsNewObject) {
                // 情况A：实体是新创建的 
                mirroredEntity = EntityModifiers.MirrorEntity(entity, pointA, pointB)[0];
                if(!keepOriginal) {
                    // 删除原对象
                    entity = null; // 通知GC回收
                }
            }
            else {
                // 情况B：实体已存在于数据库中 根据keepOriginal判断是否删除原对象
                if(!keepOriginal) {
                    // 删除
                    mirroredEntity = entity.Database.
                            UpdateEntityToModelSpace(entity.Id, false, (oriainglEntity) =>
                                                                       EntityModifiers.MirrorEntity(oriainglEntity, pointA, pointB))[0];
                }
                else {
                    // 保留
                    mirroredEntity = entity.Database.
                           UpdateEntityToModelSpace(entity.Id, true, (oriainglEntity) =>
                                                                     EntityModifiers.MirrorEntity(oriainglEntity, pointA, pointB))[0];
                }
            }
            return mirroredEntity;
        }

        /// <summary>
        /// 智能地移动实体。 如果实体是新创建的（未添加到数据库），则直接修改其几何属性。 如果实体已存在于数据库中，则通过事务安全地进行修改。
        /// </summary>
        /// <param name="entity">要移动的实体对象。</param>
        /// <param name="basePoint">实体当前的基点（移动的起始点）。</param>
        /// <param name="targetPoint">实体要移动到的目标位置点。</param>
        public static void MoveEntity(this Entity entity, Point3d basePoint, Point3d targetPoint)
        {
            // 判断实体是否已被添加到数据库
            if(entity.IsNewObject) {
                // 情况A：实体是新创建的
                EntityModifiers.MoveEntity(entity, basePoint, targetPoint);
            }
            else {
                // 情况B：实体已存在于数据库中 调用db扩展方法访问数据库 
                entity.Database.UpdateEntityToModelSpace(entity.Id, (originalEntity) =>
                                                                    EntityModifiers.MoveEntity(originalEntity, basePoint, targetPoint));
            }
        }


        /// <summary>
        /// 智能地旋转实体。 如果实体是新创建的（未添加到数据库），则直接修改其几何属性。 如果实体已存在于数据库中，则通过事务安全地进行修改。
        /// </summary>
        /// <param name="entity">要旋转的实体对象。</param>
        /// <param name="basePoint">旋转的基点。</param>
        /// <param name="degree">旋转角度（以度为单位）。</param>
        public static void RotateEntity(this Entity entity, Point3d basePoint, double degree)
        {
            if(entity.IsNewObject) {
                // 情况A：实体是新创建的，尚未添加到数据库 调用EntityModifiers编辑原对象，这个方法返回Entity[0]没必要返回
                EntityModifiers.RotateEntity(entity, basePoint, degree);
            }
            else {
                // 情况B：实体已存在于数据库中 调用db扩展方法访问数据库 
                entity.Database.UpdateEntityToModelSpace(entity.Id, (originalEntity) =>
                                                                    EntityModifiers.RotateEntity(originalEntity, basePoint, degree));
            }
        }

    }

}

