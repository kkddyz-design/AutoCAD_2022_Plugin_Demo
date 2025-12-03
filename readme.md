# READEME






## 架构逻辑



按照功能模块将EntityDemo分为

1. domain  模型层 
   - 自定义Entity实体类或Entity操作类
2. DBTools 数据库层
3. service   业务层 
4. test        测试模块


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





### Demo

```C#
[CommandMethod("CopyDemo1")]
public static void CopyDemo1()
{
    Point3d center = new Point3d(100, 100, 0);
    Point3d targetCenter = new Point3d(200, 200, 0);

    // 先修改,再写入db
    Circle c1 = new Circle(center, new Vector3d(0, 0, 1), 50);
    Entity c2 = c1.CopyEntity(center, targetCenter)[0];
    db.AddEntityToModelSpace(c1);
    db.AddEntityToModelSpace(c2);
}

[CommandMethod("CopyDemo2")]
public static void CopyDemo2()
{
    Point3d center = new Point3d(100, 100, 0);
    Point3d targetCenter = new Point3d(200, 200, 0);

    // 先写入db,再修改
    Circle c1 = new Circle(center, new Vector3d(0, 0, 1), 50);
    db.AddEntityToModelSpace(c1);
    Entity c2 = db.CopyEntity(c1.Id, center, targetCenter);
}

```

我感觉比较丑陋的地方，策略方法匹配Func<Entity,Entity[]> 所以要手动从Entity[]中取出新增对象。

后续可以在业务层再封装一下扩展Entity，那么Modifier中扩展方法就得取消。

#### domain

```C#
public static Entity[] CopyEntity(this Entity originEntity, Point3d basePoint, Point3d targetPoint)
{
    // 输入参数有效性检查
    if(originEntity == null) {
        throw new ArgumentNullException(nameof(basePoint), "原对象不能为空。");
    }

    if(basePoint == null) {
        throw new ArgumentNullException(nameof(basePoint), "基点不能为空。");
    }

    if(targetPoint == null) {
        throw new ArgumentNullException(nameof(targetPoint), "目标点不能为空。");
    }

    // 计算平移向量 如果为0,抛出异常
    Vector3d translationVector = targetPoint - basePoint;
    if(translationVector.IsZeroLength()) {
        string errorMessage = "平移向量为零，复制后的实体将与原实体重叠。";
        Debug.WriteLine(errorMessage);
        throw new ArgumentException(errorMessage, nameof(translationVector));
    }

    // 拷贝实体
    Entity newEntity = originEntity.Clone() as Entity;
    if(newEntity != null) {
        // 通过平移矩阵创建拷贝实体
        Matrix3d translationMatrix = Matrix3d.Displacement(translationVector);
        newEntity.TransformBy(translationMatrix);
    }
    else {
        throw new InvalidOperationException($"无法克隆实体 '{originEntity.GetType().Name}'。");
    }

    return new Entity[] { newEntity }; // 返回新对象
}
```

不论是对象是否在数据库中，都需要先调用domain中的实扩展方法。



#### 业务层

将策略updater注入持久层Update接口

```C#
public static Entity CopyEntity(this Database db, ObjectId entityId, Point3d basePoint, Point3d targetPoint)
{
    return  db.UpdateEntityToModelSpace(entityId, (oriainglEntity) =>
                                                  oriainglEntity.CopyEntity(basePoint, targetPoint))[0];
}
```



#### 持久层

```C#
public static Entity[] UpdateEntityToModelSpace(this Database db, ObjectId entityId, Func<Entity, Entity[]> updater)
{
    // 1. 输入参数有效性检查
    // 2. 通过事务更新实体
    using(Transaction trans = db.TransactionManager.StartTransaction()) {
        try {
            // 2.1 以Write模式打开原实体 并转换为Entity类型
            Entity originEntity = trans.GetObject(entityId, OpenMode.ForWrite) as Entity;
            // 检查实体不存在或已被删除
            // 2.2 获取块表
            // 2.3 获取模型空间
            // 2.4 调用updater获取操作结果
            Entity[] entityArray = updater.Invoke(originEntity);
            if(entityArray == null) {
                throw new ArgumentException("策略方法返回null");
            } else {
                // 添加实体 -- 由updater决定
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
            ...
        }
    }
}

```

### Modifer返回值

前提：内存中的实体对象与数据库中的对象一一对应

比如多边形，我自定一个多边形，保存它的数据。将创建多边形的逻辑封装到它的构造函数中。

这样当我通过addEntityToModelSpace添加了向数据库添加它时,addEntityToModelSpace不需要给我返回实体，因为我先创建实体，再持久化。

当我修改它时

1. 通过domian中扩展的Entity方法修改自己，再持久化。此时同上面，我持有引用，数据库必须与我的引用对象保持一致
2. 已经存入数据库，调用db扩展方法修改它，db拿到业务层传入的策略,调用策略对象(domian),去修改，再持久化，还是一致的

只要当，例如，我对一个实体镜像

1. 没存入数据库，我应该调用domian层的函数，并由我调用者决定是否保留原对象(不保留，originEntity=null)
2. 存入数据库，此时调用db扩展方法,会创建一个内存中不存在的Entity，调用者并不能拿到他。我有两个选择
   1. return Entity
   2. 使用out拿到这个新创建的Entity对象

综上，我的策略方法在做修改时不需要返回原对象。只有当编辑操作产生新对象时，返回新对象数组。

返回数组是为了处理例如阵列这种会产生多个新对象的操作





### 内存修改与DB修改



用户在调用扩展entity的方法后,调用者都不知道是否改变了数据库，这看起似乎很好？

调用者不需要将对象是否已经存在作为考虑对象。

（在service处理内存中和数据库中两者情况，调用者可以通过一个函数入口进行两中操作）



应该将这两种操作区别开来，调用者还不是最终用户，它因该清楚的明白自己是否访问了数据库。

如果用户需要在内存中创建对象,直接调用domain就行。

如果用户需要操作数据库中的对象，那么就调用封装了DB访问逻辑的业务层





用Modifier中的方法扩展Entity(处理实体不在数据库),这个操作只是改变内存。也就是说Entity的扩展方法,只能限制在模型层。

保留业务层处理实体存在数据库，设置update的逻辑，将方法扩展给DB,访问数据库的所有行为都应该由DB对象实现

是否被添加到数据库，这个判断由调用者执行。



业务层

```C#
    public static void ChangeEntityColor(this Database db, ObjectId objectId, short colorIndex)
    {
        db.UpdateEntityToModelSpace(objectId, (originalEntity) =>
                                              originalEntity.ChangeColor(colorIndex));
    } 
  
```





## 带图形的gui插件

不要搞多个项目，就把项目按照

调用Autocad的acad模块

提供用户界面及交互的GUI模块

通用部分common模块

