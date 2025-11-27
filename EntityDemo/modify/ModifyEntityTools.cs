using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Diagnostics;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.modify
{

    public static class ModifyEntityTools
    {

        /// <summary>
        /// （推荐）在指定的数据库中，通过实体ID改变图形颜色。
        /// </summary>
        /// <param name="db">要操作的图形数据库。</param>
        /// <param name="entityId">要修改颜色的实体的 ObjectId。</param>
        /// <param name="colorIndex">颜色索引 (ACI)。</param>
        /// <exception cref="ArgumentNullException">当 db 为 null 时抛出。</exception>
        /// <exception cref="ArgumentException">当 entityId 无效时抛出。</exception>
        /// <exception cref="InvalidCastException">当 ObjectId 指向的对象不是 Entity 类型时抛出。</exception>
        /// <exception cref="Exception">其他可能发生的异常。</exception>
        public static void ChangeEntityColor(this Database db, ObjectId entityId, short colorIndex)
        {
            // 1. 输入参数有效性检查
            if(db == null)
            {
                throw new ArgumentNullException(nameof(db), "数据库对象不能为空。");
            }

            if(entityId.IsNull)
            {
                throw new ArgumentException("实体ID无效（IsNull）。", nameof(entityId));
            }

            if(!entityId.IsValid)
            {
                throw new ArgumentException("实体ID无效（IsValid为false）。", nameof(entityId));
            }

            // 使用 using 语句确保事务被正确释放
            using(Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 2. 检查实体是否已被删除
                    if(entityId.IsErased)
                    {
                        // 可以选择抛出异常或记录日志后返回
                        trans.Abort();
                        throw new ArgumentException($"ID为 {entityId} 的实体已被删除，无法修改颜色。", nameof(entityId));
                    }

                    // 3. 以写模式打开实体，并检查类型
                    DBObject dbObj = trans.GetObject(entityId, OpenMode.ForWrite);
                    if(dbObj is not Entity entity)
                    {
                        trans.Abort();
                        throw new InvalidCastException($"ID为 {entityId} 的对象不是一个实体 (Entity)，无法修改颜色。");
                    }

                    // 4. 检查实体是否可写（例如，是否被锁定）
                    if(!entity.IsWriteEnabled)
                    {
                        trans.Abort();
                        throw new Exception($"ID为 {entityId} 的实体 '{entity.GetType().Name}' 不可写（可能被锁定或在外部参照中）。");
                    }

                    // 5. 执行核心操作：修改颜色
                    // 建议使用 Color 属性而非已过时的 ColorIndex
                    entity.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);

                    // 6. 提交事务
                    trans.Commit();
                    Debug.WriteLine($"成功将实体 {entityId} 的颜色修改为索引 {colorIndex}。");
                }
                catch(Exception ex)
                {
                    // 发生异常时回滚事务，保证数据库状态一致性
                    trans.Abort();
                    Debug.WriteLine($"修改实体颜色失败: {ex.Message}");

                    // 将异常向上抛出，让调用者决定如何处理
                    throw;
                }
            }
        }

        /// <summary>
        /// （重载）改变一个已打开的图形对象的颜色。
        /// </summary>
        /// <param name="db">要操作的图形数据库。</param>
        /// <param name="entity">要修改颜色的实体对象。</param>
        /// <param name="colorIndex">颜色索引 (ACI)。</param>
        /// <exception cref="ArgumentNullException">当 db 或 entity 为 null 时抛出。</exception>
        /// <exception cref="Exception">当实体不可写或操作失败时抛出。</exception>
        public static void ChangeEntityColor(this Database db, Entity entity, short colorIndex)
        {
            // 1. 输入参数有效性检查
            if(db == null)
            {
                throw new ArgumentNullException(nameof(db), "数据库对象不能为空。");
            }

            if(entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "实体对象不能为空。");
            }

            // 2. 检查实体是否是新创建但尚未添加到数据库的对象
            if(entity.IsNewObject)
            {
                // 对于新对象，直接修改其属性即可
                entity.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
                Debug.WriteLine($"成功将新实体 '{entity.GetType().Name}' 的颜色预设为索引 {colorIndex}。");
            }
            else
            {
                // 3. 对于已存在于数据库中的实体，调用第一个重载方法来完成
                // 这可以确保事务处理和所有安全检查都被执行
                db.ChangeEntityColor(entity.Id, colorIndex);
            }
        }

    }

}
