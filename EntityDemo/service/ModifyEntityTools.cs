using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
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


        public static void ScaleEntity(this Entity entity, Point3d basePoint, double scaleFactor)
        {
            if(entity.IsNewObject) {
                // 情况A：实体是新创建的，尚未添加到数据库 调用EntityModifiers编辑原对象，这个方法返回Entity[0]没必要返回
                EntityModifiers.ScaleEntity(entity, basePoint, scaleFactor);
            }
            else {
                // 情况B：实体已存在于数据库中 调用db扩展方法访问数据库 
                entity.Database.UpdateEntityToModelSpace(entity.Id, (originalEntity) =>
                                                                    EntityModifiers.ScaleEntity(entity, basePoint, scaleFactor));
            }
        }

    }

}

