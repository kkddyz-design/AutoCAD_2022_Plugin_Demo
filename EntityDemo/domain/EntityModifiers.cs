using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Diagnostics;
using System.Linq;

/*
 * EntityModifiers定义编辑实体的逻辑,函数统一返回Entity[] entityArray,即操作Entity后的结果
 * 
 * 所有对实体的编辑都需要调用这个类中的方法,因此在业务层不需要在对参数进行校验判断
 * 
 * 实现方法做参数校验！！！！！
 * 
 * 避免二义性,这里不扩展entity
 */

namespace AutoCAD_2022_Plugin_Demo.EntityDemo.modify
{

    public static class EntityModifiers
    {
        /*
         * updater返回值：编辑操作产生的新对象
         * 返回null,说明不产生新对象,修改原对象
         */

        public static Entity[] ChangeColor(Entity originEntity, short colorIndex)
        {
            // 输入参数合法性检验
            if(originEntity == null) {
                throw new ArgumentNullException(nameof(originEntity), "原对象不能为空。");
            }

            if(colorIndex < 1 || colorIndex > 255) {
                throw new ArgumentOutOfRangeException(nameof(colorIndex), "颜色索引必须在1到255之间。");
            }

            // 修改颜色
            originEntity.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
            Debug.WriteLine($"修改实体 '{originEntity.GetType().Name}' (尚未添加到数据库) ,设置颜色为索引 {colorIndex}。");

            return new Entity[0];
        }

        /*
         * 获得拷贝后的新实体
         */
        public static Entity[] CopyEntity(Entity originEntity, Point3d basePoint, Point3d targetPoint)
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

        /*
         * 获取移动后的实体 不能用拷贝后,删除原对象代替,因为ObjectId会改变
         */
        public static Entity[] MoveEntity(Entity originEntity, Point3d basePoint, Point3d targetPoint)
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

            // 通过平移矩阵编辑实体
            Matrix3d translationMatrix = Matrix3d.Displacement(translationVector);
            originEntity.TransformBy(translationMatrix);

            return new Entity[0];
        }

        /*
         * 获取旋转后的实体
         * 
         * 当前实现默认绕 Z 轴 旋转。如果你需要绕 X 轴或 Y 轴旋转，只需将 Vector3d.ZAxis 替换为 Vector3d.XAxis 或 Vector3d.YAxis
         */
        public static Entity[] RotateEntity(Entity originEntity, Point3d basePoint, double degree)
        {
            // 1. 输入参数有效性检查
            if(originEntity == null) {
                throw new ArgumentNullException(nameof(originEntity), "实体对象不能为空。");
            }

            if(basePoint == null) {
                throw new ArgumentNullException(nameof(basePoint), "基点不能为空。");
            }

            // 2. 将角度从度转换为弧度
            double radian = degree * Math.PI / 180.0;

            // 如果角度为0，则无需进行任何操作
            if(radian == 0) {
                Debug.WriteLine("旋转角度为零，无需执行旋转操作。");
            }

            // 3. 创建旋转矩阵并应用
            Matrix3d rotationMatrix = Matrix3d.Rotation(radian, Vector3d.ZAxis, basePoint);
            originEntity.TransformBy(rotationMatrix);

            return new Entity[0];
        }

        /*
         * 获取镜像后的实体
         */
        public static Entity[] MirrorEntity(Entity originEntity, Point3d pointA, Point3d pointB)
        {
            // 1. 输入参数有效性检查
            if(originEntity == null) {
                throw new ArgumentNullException(nameof(originEntity), "实体对象不能为空。");
            }

            if(pointA == null || pointB == null) {
                throw new ArgumentNullException("对称轴不能为空。");
            }

            // 2. 创建镜像对称轴

            Line3d mirrorLine = new Line3d(pointA, pointB);

            // 3. 创建镜像矩阵
            Matrix3d mirrorMatrix = Matrix3d.Mirroring(mirrorLine);

            // 4. 应用镜像
            Entity mirroredEntity = originEntity.Clone() as Entity;
            mirroredEntity.TransformBy(mirrorMatrix);

            return new Entity[] { mirroredEntity };
        }


        public static Entity[] ScaleEntity(Entity originEntity, Point3d basePoint, double scaleFactor)
        {
            // 1. 输入参数有效性检查 (参考你的实现)
            if(originEntity == null) {
                throw new ArgumentNullException(nameof(originEntity), "原始实体对象不能为空。");
            }
            if(basePoint == null) // 在实际API中，Point3d通常是值类型，不会为null，但检查一下更安全
            {
                throw new ArgumentNullException(nameof(basePoint), "缩放基点不能为空。");
            }

            // 也可以对缩放因子做一些合理性检查，例如不能为零
            if(scaleFactor <= 0) {
                throw new ArgumentOutOfRangeException(nameof(scaleFactor), "缩放因子必须是正数。");
            }

            // 2. 创建缩放变换矩阵
            // 缩放矩阵需要考虑基点，因此是一个复合变换：平移 -> 缩放 -> 平移回
            Matrix3d scaleMatrix = Matrix3d.Scaling(scaleFactor, basePoint);

            // 3. 对新实体应用缩放矩阵 (核心修正点)
            originEntity.TransformBy(scaleMatrix);

            // 4. 返回包含新实体的数组 (参考你的实现)
            return new Entity[0];
        }

    }

}