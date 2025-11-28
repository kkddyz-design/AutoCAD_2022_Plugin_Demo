using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Diagnostics;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.modify
{

    public static class ModifyEntityTools
    {

        /// <summary>
        /// 智能地修改实体颜色。 让Entity的扩展方法作为 “主入口”，内部根据实体状态， 调用Database的扩展方法来处理“已存在于数据库中” 的情况。  用户代码 ->
        /// Entity.ChangeEntityColor -> 判断实体状态 -> 如果已存在 -> Database.ChangeEntityColor -> 执行事务修改
        /// </summary>
        /// <param name="entity">要修改颜色的实体对象</param>
        /// <param name="colorIndex">ACI颜色索引（1-255）</param>
        /// <exception cref="ArgumentNullException">当实体为null时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当颜色索引超出有效范围时抛出</exception>
        /// <exception cref="InvalidOperationException">当实体状态不一致（如已删除或数据库引用丢失）时抛出</exception>
        /// <exception cref="Exception">当在事务处理中发生其他错误时抛出</exception>
        public static void ChangeEntityColor(this Entity entity, short colorIndex)
        {
            // 1. 输入参数有效性检查
            if(entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "实体对象不能为空。");
            }
            if(colorIndex < 1 || colorIndex > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(colorIndex), "颜色索引必须在1到255之间。");
            }

            // 2. 核心逻辑：判断实体是否已被添加到数据库
            if(entity.IsNewObject)
            {
                // 情况A：实体是新创建的，尚未添加到数据库 (IsNewObject 为 true)
                // 此时可以直接、安全地修改其属性
                entity.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
                Debug.WriteLine($"成功为新实体 '{entity.GetType().Name}' (尚未添加到数据库) 设置颜色为索引 {colorIndex}。");
            }
            else
            {
                // 情况B：实体已存在于数据库中 (IsNewObject为false)
                // 必须通过事务来修改，以保证数据库状态的一致性

                // 2.1 检查实体是否已被删除(并发场景)
                if(entity.IsErased)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' (ID: {entity.Id}) 已被删除，无法修改颜色。");
                }

                // 2.2 检查实体的Database引用是否有效
                Database db = entity.Database;
                if(db == null)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' (ID: {entity.Id}) 的数据库引用为空，无法执行操作。");
                }

                // 2.3 检查实体的ObjectId是否有效
                ObjectId objectId = entity.Id;
                if(objectId.IsNull || !objectId.IsValid)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' 的ID无效 (IsNull: {objectId.IsNull}, IsValid: {objectId.IsValid})。");
                }

                // 调用Database扩展方法修改颜色
                db.ChangeEntityColor(objectId, colorIndex);
            }
        }


        /// <summary>
        /// （推荐）在指定的数据库中，通过实体ID改变图形颜色。 此方法是一个专注于事务处理的辅助方法，通常由Entity.ChangeEntityColor调用。
        /// </summary>
        /// <param name="db">要操作的图形数据库。</param>
        /// <param name="entityId">要修改颜色的实体的 ObjectId。</param>
        /// <param name="colorIndex">颜色索引 (ACI)。</param>
        /// <exception cref="ArgumentNullException">当 db 为 null 时抛出。</exception>
        /// <exception cref="ArgumentException">当 entityId 无效时抛出。</exception>
        /// <exception cref="ArgumentOutOfRangeException">当颜色索引超出有效范围时抛出。</exception>
        /// <exception cref="InvalidCastException">当指定ID的对象不是实体时抛出。</exception>
        /// <exception cref="Exception">当在事务处理中发生其他错误时抛出。</exception>
        public static void ChangeEntityColor(this Database db, ObjectId entityId, short colorIndex)
        {
            // 1. 输入参数有效性检查
            if(db == null)
            {
                throw new ArgumentNullException(nameof(db), "数据库对象不能为空。");
            }
            if(entityId.IsNull || !entityId.IsValid)
            {
                throw new ArgumentException($"实体ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。", nameof(entityId));
            }
            if(colorIndex < 1 || colorIndex > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(colorIndex), "颜色索引必须在1到255之间。");
            }

            // 使用 using 语句确保事务被正确释放
            using(Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 2. 以写模式打开实体
                    DBObject dbObj = trans.GetObject(entityId, OpenMode.ForWrite);

                    // 3. 检查获取到的对象是否为null或已被删除
                    if(dbObj == null || dbObj.IsErased)
                    {
                        trans.Abort();
                        throw new ArgumentException($"ID为 {entityId} 的实体不存在或已被删除。");
                    }

                    // 4. 检查对象类型是否为Entity
                    Entity entityToModify = dbObj as Entity;
                    if(entityToModify == null)
                    {
                        trans.Abort();
                        throw new InvalidCastException($"ID为 {entityId} 的对象类型为 '{dbObj.GetType().Name}'，不是一个实体 (Entity)，无法修改颜色。");
                    }

                    // 5. 检查实体是否可写
                    if(!entityToModify.IsWriteEnabled)
                    {
                        trans.Abort();
                        throw new Exception($"ID为 {entityId} 的实体 '{entityToModify.GetType().Name}' 不可写（可能被锁定或在外部参照中）。");
                    }

                    // 6. 执行核心操作：修改颜色
                    entityToModify.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);

                    // 7. 提交事务
                    trans.Commit();
                    Debug.WriteLine($"成功将实体 {entityId} 的颜色修改为索引 {colorIndex}。");
                }
                catch(Exception ex)
                {
                    // 发生异常时回滚事务
                    trans.Abort();
                    Debug.WriteLine($"修改实体颜色失败: {ex.Message}");

                    // 将异常向上抛出，让调用者决定如何处理
                    throw;
                }
            }
        }


        /// <summary>
        /// 复制实体调用入口。如果实体是新创建的（未添加到数据库），则直接克隆并移动。 如果实体已存在于数据库中，则通过事务安全地克隆并移动。
        /// </summary>
        /// <param name="entity">要复制的源实体对象。</param>
        /// <param name="basePoint">复制的基点（源实体上的参考点）。</param>
        /// <param name="targetPoint">新实体的目标位置点。</param>
        /// <returns>新创建的、已移动到目标位置的实体对象。</returns>
        /// <exception cref="ArgumentNullException">当输入参数为 null 时抛出。</exception>
        /// <exception cref="InvalidOperationException">当实体状态不一致时抛出。</exception>
        public static Entity CopyEntity(this Entity entity, Point3d basePoint, Point3d targetPoint)
        {
            // 1. 输入参数有效性检查
            if(entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "实体对象不能为空。");
            }

            if(basePoint == null)
            {
                throw new ArgumentNullException(nameof(basePoint), "基点不能为空。");
            }

            if(targetPoint == null)
            {
                throw new ArgumentNullException(nameof(targetPoint), "目标点不能为空。");
            }

            // 2. 计算平移向量
            Vector3d translationVector = targetPoint - basePoint;
            if(translationVector.IsZeroLength())
            {
                Debug.WriteLine("平移向量为零，复制后的实体将与原实体重叠。");
            }

            // 3. 核心逻辑：判断实体状态
            if(entity.IsNewObject)
            {
                // 情况A：实体是新创建的，尚未添加到数据库
                // 直接克隆实体并应用变换
                Entity newEntity = entity.Clone() as Entity;
                if(newEntity != null)
                {
                    Matrix3d translationMatrix = Matrix3d.Displacement(translationVector);
                    newEntity.TransformBy(translationMatrix);
                    Debug.WriteLine($"成功复制新实体 '{entity.GetType().Name}'。");
                    return newEntity;
                }
                else
                {
                    throw new InvalidOperationException($"无法克隆实体 '{entity.GetType().Name}'。");
                }
            }
            else
            {
                // 情况B：实体已存在于数据库中
                // 必须通过事务来克隆

                // 检查实体是否已被删除
                if(entity.IsErased)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' (ID: {entity.Id}) 已被删除，无法复制。");
                }

                // 获取数据库引用并检查有效性
                Database db = entity.Database;
                if(db == null)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' (ID: {entity.Id}) 的数据库引用为空，无法执行操作。");
                }

                // 获取实体ID并检查有效性
                ObjectId entityId = entity.Id;
                if(entityId.IsNull || !entityId.IsValid)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' 的ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。");
                }

                // 调用 Database 的扩展方法来执行复制
                return db.CopyEntity(entityId, basePoint, targetPoint);
            }
        }

        /// <summary>
        /// 在指定的数据库中，通过实体ID复制一个已存在的实体。 此方法是一个专注于事务处理的辅助方法，通常由 Entity.CopyEntity 调用。
        /// </summary>
        /// <param name="db">要操作的图形数据库。</param>
        /// <param name="entityId">要复制的源实体的 ObjectId。</param>
        /// <param name="basePoint">复制的基点（源实体上的参考点）。</param>
        /// <param name="targetPoint">新实体的目标位置点。</param>
        /// <returns>新创建实体的 ObjectId。</returns>
        /// <exception cref="ArgumentNullException">当输入参数为 null 时抛出。</exception>
        /// <exception cref="ArgumentException">当实体ID无效或对象不是实体时抛出。</exception>
        public static Entity CopyEntity(this Database db, ObjectId entityId, Point3d basePoint, Point3d targetPoint)
        {
            // 1. 输入参数有效性检查
            if(db == null)
            {
                throw new ArgumentNullException(nameof(db), "数据库对象不能为空。");
            }

            if(entityId.IsNull || !entityId.IsValid)
            {
                throw new ArgumentException($"实体ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。", nameof(entityId));
            }

            if(basePoint == null)
            {
                throw new ArgumentNullException(nameof(basePoint), "基点不能为空。");
            }

            if(targetPoint == null)
            {
                throw new ArgumentNullException(nameof(targetPoint), "目标点不能为空。");
            }

            // 2. 计算平移向量
            Vector3d translationVector = targetPoint - basePoint;
            if(translationVector.IsZeroLength())
            {
                Debug.WriteLine("平移向量为零，复制后的实体将与原实体重叠。");
            }

            // 3. 使用事务来复制实体
            using(Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 以读模式打开源实体
                    DBObject dbObj = trans.GetObject(entityId, OpenMode.ForRead);
                    if(dbObj == null || dbObj.IsErased)
                    {
                        trans.Abort();
                        throw new ArgumentException($"ID为 {entityId} 的实体不存在或已被删除。");
                    }
                    if(!(dbObj is Entity sourceEntity))
                    {
                        trans.Abort();
                        throw new ArgumentException($"ID为 {entityId} 的对象不是实体，无法复制。");
                    }

                    // 克隆实体
                    Entity newEntity = sourceEntity.Clone() as Entity;
                    if(newEntity == null)
                    {
                        trans.Abort();
                        throw new InvalidOperationException($"无法克隆实体 '{sourceEntity.GetType().Name}'。");
                    }

                    // 对新实体应用平移变换
                    Matrix3d translationMatrix = Matrix3d.Displacement(translationVector);
                    newEntity.TransformBy(translationMatrix);

                    // 正确获取模型空间并添加新实体
                    ObjectId blockTableId = db.BlockTableId;

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
                    BlockTable blockTable = trans.GetObject(blockTableId, OpenMode.ForRead) as BlockTable;
                    if(blockTable == null)
                    {
                        trans.Abort();
                        throw new Exception("无法获取块表（BlockTable）。");
                    }
                    ObjectId modelSpaceId = blockTable[BlockTableRecord.ModelSpace]; // 关键：获取模型空间ID
                    BlockTableRecord modelSpace = trans.GetObject(modelSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    if(modelSpace == null)
                    {
                        trans.Abort();
                        throw new Exception("无法获取模型空间（ModelSpace）。");
                    }

                    modelSpace.AppendEntity(newEntity);
                    trans.AddNewlyCreatedDBObject(newEntity, true);

                    // 提交事务
                    trans.Commit();
                    Debug.WriteLine($"成功复制实体 {entityId}，新实体ID为 {newEntity.Id}。");
                    return newEntity;
                }
                catch(Exception ex)
                {
                    trans.Abort();
                    Debug.WriteLine($"复制实体失败: {ex.Message}");
                    throw;
                }
            }
        }


        public static void MirrorEntity(this  Entity entity, Point3d point1, Point3d point2, bool keepOriginal)
        {
        }

        public static void MirrorEntity(this Database db, ObjectId objectId, Point3d point1, Point3d point2, bool keepOriginal)
        {
        }

        /// <summary>
        /// 智能地移动实体。 如果实体是新创建的（未添加到数据库），则直接修改其几何属性。 如果实体已存在于数据库中，则通过事务安全地进行修改。
        /// </summary>
        /// <param name="entity">要移动的实体对象。</param>
        /// <param name="basePoint">实体当前的基点（移动的起始点）。</param>
        /// <param name="targetPoint">实体要移动到的目标位置点。</param>
        /// <exception cref="ArgumentNullException">当输入参数为 null 时抛出。</exception>
        /// <exception cref="InvalidOperationException">当实体状态不一致时抛出。</exception>
        public static void MoveEntity(this Entity entity, Point3d basePoint, Point3d targetPoint)
        {
            // 1. 输入参数有效性检查
            if(entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "实体对象不能为空。");
            }

            if(basePoint == null)
            {
                throw new ArgumentNullException(nameof(basePoint), "基点不能为空。");
            }

            if(targetPoint == null)
            {
                throw new ArgumentNullException(nameof(targetPoint), "目标点不能为空。");
            }

            // 2. 计算平移向量
            Vector3d translationVector = targetPoint - basePoint;
            if(translationVector.IsZeroLength())
            {
                Debug.WriteLine("平移向量为零，无需执行移动操作。");
                return;
            }

            // 3. 核心逻辑：判断实体状态
            if(entity.IsNewObject)
            {
                // 情况A：实体是新创建的，尚未添加到数据库
                // 直接修改其几何属性
                Matrix3d translationMatrix = Matrix3d.Displacement(translationVector);

                // 执行移动（AutoCAD 实体的 TransformBy 方法，参数必须是 Matrix3d）
                entity.TransformBy(translationMatrix);
                Debug.WriteLine($"成功移动新实体 '{entity.GetType().Name}'。");
            }
            else
            {
                // 情况B：实体已存在于数据库中
                // 必须通过事务来修改

                // 检查实体是否已被删除
                if(entity.IsErased)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' (ID: {entity.Id}) 已被删除，无法移动。");
                }

                // 获取数据库引用并检查有效性
                Database db = entity.Database;
                if(db == null)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' (ID: {entity.Id}) 的数据库引用为空，无法执行操作。");
                }

                // 获取实体ID并检查有效性
                ObjectId entityId = entity.Id;
                if(entityId.IsNull || !entityId.IsValid)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' 的ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。");
                }

                // 调用 Database 的扩展方法来执行移动
                db.MoveEntity(entityId, basePoint, targetPoint);
            }
        }

        /// <summary>
        /// 在指定的数据库中，通过实体ID移动一个已存在的实体。 此方法是一个专注于事务处理的辅助方法，通常由 Entity.MoveEntity 调用。
        /// </summary>
        /// <param name="db">要操作的图形数据库。</param>
        /// <param name="entityId">要移动的实体的 ObjectId。</param>
        /// <param name="basePoint">实体当前的基点（移动的起始点）。</param>
        /// <param name="targetPoint">实体要移动到的目标位置点。</param>
        /// <exception cref="ArgumentNullException">当输入参数为 null 时抛出。</exception>
        /// <exception cref="ArgumentException">当实体ID无效或对象不是实体时抛出。</exception>
        public static void MoveEntity(this Database db, ObjectId entityId, Point3d basePoint, Point3d targetPoint)
        {
            // 1. 输入参数有效性检查
            if(db == null)
            {
                throw new ArgumentNullException(nameof(db), "数据库对象不能为空。");
            }

            if(entityId.IsNull || !entityId.IsValid)
            {
                throw new ArgumentException($"实体ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。", nameof(entityId));
            }

            if(basePoint == null)
            {
                throw new ArgumentNullException(nameof(basePoint), "基点不能为空。");
            }

            if(targetPoint == null)
            {
                throw new ArgumentNullException(nameof(targetPoint), "目标点不能为空。");
            }

            // 2. 计算平移向量
            Vector3d translationVector = targetPoint - basePoint;
            if(translationVector.IsZeroLength())
            {
                Debug.WriteLine("平移向量为零，无需执行移动操作。");
                return;
            }

            // 3. 使用事务来移动实体
            using(Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 以写模式打开实体
                    DBObject dbObj = trans.GetObject(entityId, OpenMode.ForWrite);

                    // 检查对象是否为 null 或已被删除
                    if(dbObj == null || dbObj.IsErased)
                    {
                        trans.Abort();
                        throw new ArgumentException($"ID为 {entityId} 的实体不存在或已被删除。");
                    }

                    // 检查对象类型是否为 Entity
                    if(!(dbObj is Entity entityToMove))
                    {
                        trans.Abort();
                        throw new ArgumentException($"ID为 {entityId} 的对象类型为 '{dbObj.GetType().Name}'，不是一个实体 (Entity)，无法移动。");
                    }

                    // 检查实体是否可写
                    if(!entityToMove.IsWriteEnabled)
                    {
                        trans.Abort();
                        throw new Exception($"ID为 {entityId} 的实体 '{entityToMove.GetType().Name}' 不可写（可能被锁定或在外部参照中）。");
                    }

                    // Matrix3d.Displacement 是一个静态方法，
                    // 其作用是通过平移向量translationVector,创建一个表示 “平移变换”矩阵(translationMatrix)
                    Matrix3d translationMatrix = Matrix3d.Displacement(translationVector);

                    // 执行移动（AutoCAD 实体的 TransformBy 方法，参数必须是 Matrix3d）
                    entityToMove.TransformBy(translationMatrix);

                    // 提交事务
                    trans.Commit();
                    Debug.WriteLine($"成功移动实体 {entityId}。");
                }
                catch(Exception ex)
                {
                    // 发生异常时回滚事务
                    trans.Abort();
                    Debug.WriteLine($"移动实体失败: {ex.Message}");

                    // 将异常向上抛出，让调用者决定如何处理
                    throw;
                }
            }
        }


        /// <summary>
        /// 智能地旋转实体。 如果实体是新创建的（未添加到数据库），则直接修改其几何属性。 如果实体已存在于数据库中，则通过事务安全地进行修改。
        /// </summary>
        /// 当前实现默认绕 Z 轴 旋转。如果你需要绕 X 轴或 Y 轴旋转，只需将 Vector3d.ZAxis 替换为 Vector3d.XAxis 或 Vector3d.YAxis
        /// <param name="entity">要旋转的实体对象。</param>
        /// <param name="basePoint">旋转的基点。</param>
        /// <param name="degree">旋转角度（以度为单位）。</param>
        /// <exception cref="ArgumentNullException">当输入参数为 null 时抛出。</exception>
        /// <exception cref="InvalidOperationException">当实体状态不一致时抛出。</exception>
        public static void RotateEntity(this Entity entity, Point3d basePoint, double degree)
        {
            // 1. 输入参数有效性检查
            if(entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "实体对象不能为空。");
            }

            if(basePoint == null)
            {
                throw new ArgumentNullException(nameof(basePoint), "基点不能为空。");
            }

            // 2. 将角度从度转换为弧度
            double radian = degree * Math.PI / 180.0;

            // 如果角度为0，则无需进行任何操作
            if(radian == 0)
            {
                Debug.WriteLine("旋转角度为零，无需执行旋转操作。");
                return;
            }

            // 3. 核心逻辑：判断实体状态
            if(entity.IsNewObject)
            {
                // 情况A：实体是新创建的，尚未添加到数据库
                // 直接创建旋转矩阵并应用
                Matrix3d rotationMatrix = Matrix3d.Rotation(radian, Vector3d.ZAxis, basePoint);
                entity.TransformBy(rotationMatrix);
                Debug.WriteLine($"成功旋转新实体 '{entity.GetType().Name}' {degree} 度。");
            }
            else
            {
                // 情况B：实体已存在于数据库中
                // 必须通过事务来修改

                // 检查实体是否已被删除
                if(entity.IsErased)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' (ID: {entity.Id}) 已被删除，无法旋转。");
                }

                // 获取数据库引用并检查有效性
                Database db = entity.Database;
                if(db == null)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' (ID: {entity.Id}) 的数据库引用为空，无法执行操作。");
                }

                // 获取实体ID并检查有效性
                ObjectId entityId = entity.Id;
                if(entityId.IsNull || !entityId.IsValid)
                {
                    throw new InvalidOperationException($"实体 '{entity.GetType().Name}' 的ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。");
                }

                // 调用 Database 的扩展方法来执行旋转
                db.RotateEntity(entityId, basePoint, degree);
            }
        }

        /// <summary>
        /// 在指定的数据库中，通过实体ID旋转一个已存在的实体。 此方法是一个专注于事务处理的辅助方法，通常由 Entity.RotateEntity 调用。
        /// </summary>
        /// <param name="db">要操作的图形数据库。</param>
        /// <param name="entityId">要旋转的实体的 ObjectId。</param>
        /// <param name="basePoint">旋转的基点。</param>
        /// <param name="degree">旋转角度（以度为单位）。</param>
        /// <exception cref="ArgumentNullException">当输入参数为 null 时抛出。</exception>
        /// <exception cref="ArgumentException">当实体ID无效或对象不是实体时抛出。</exception>
        public static void RotateEntity(this Database db, ObjectId entityId, Point3d basePoint, double degree)
        {
            // 1. 输入参数有效性检查
            if(db == null)
            {
                throw new ArgumentNullException(nameof(db), "数据库对象不能为空。");
            }

            if(entityId.IsNull || !entityId.IsValid)
            {
                throw new ArgumentException($"实体ID无效 (IsNull: {entityId.IsNull}, IsValid: {entityId.IsValid})。", nameof(entityId));
            }

            if(basePoint == null)
            {
                throw new ArgumentNullException(nameof(basePoint), "基点不能为空。");
            }

            // 2. 将角度从度转换为弧度
            double radian = degree * Math.PI / 180.0;

            // 如果角度为0，则无需进行任何操作
            if(radian == 0)
            {
                Debug.WriteLine("旋转角度为零，无需执行旋转操作。");
                return;
            }

            // 3. 使用事务来旋转实体
            using(Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 以写模式打开实体
                    DBObject dbObj = trans.GetObject(entityId, OpenMode.ForWrite);

                    // 检查对象是否为 null 或已被删除
                    if(dbObj == null || dbObj.IsErased)
                    {
                        trans.Abort();
                        throw new ArgumentException($"ID为 {entityId} 的实体不存在或已被删除。");
                    }

                    // 检查对象类型是否为 Entity
                    if(!(dbObj is Entity entityToRotate))
                    {
                        trans.Abort();
                        throw new ArgumentException($"ID为 {entityId} 的对象类型为 '{dbObj.GetType().Name}'，不是一个实体 (Entity)，无法旋转。");
                    }

                    // 检查实体是否可写
                    if(!entityToRotate.IsWriteEnabled)
                    {
                        trans.Abort();
                        throw new Exception($"ID为 {entityId} 的实体 '{entityToRotate.GetType().Name}' 不可写（可能被锁定或在外部参照中）。");
                    }

                    // 执行核心操作：旋转实体
                    // 创建绕Z轴旋转的矩阵
                    Matrix3d rotationMatrix = Matrix3d.Rotation(radian, Vector3d.ZAxis, basePoint);
                    entityToRotate.TransformBy(rotationMatrix);

                    // 提交事务
                    trans.Commit();
                    Debug.WriteLine($"成功旋转实体 {entityId} {degree} 度。");
                }
                catch(Exception ex)
                {
                    // 发生异常时回滚事务
                    trans.Abort();
                    Debug.WriteLine($"旋转实体失败: {ex.Message}");

                    // 将异常向上抛出，让调用者决定如何处理
                    throw;
                }
            }
        }

    }

}

