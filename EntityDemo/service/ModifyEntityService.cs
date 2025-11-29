using AutoCAD_2022_Plugin_Demo.EntityDemo.domain;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.service

{
    /*
     * 业务层不同的函数封装了调用不同Modifier作为数据库操作的策略对象
     * 业务层应该将修改后的实体对象返回给调用者，以便调用者查看修改后的信息，而不是再通过id去数据库再获得一个内存对象
     * 
     */

    public static class ModifyEntityService
    {

        // update委托,通过参数将orginEntity修改为newEntity 
        public delegate Entity EntityUpdater(Entity orginEntity);


        /// <summary>
        /// 修改数据库中Entity对象颜色
        /// </summary>
        /// <param name="colorIndex">ACI颜色索引（1-255）</param>
        public static void ChangeEntityColor(this Database db, ObjectId entityId, short colorIndex)
        {
            // UpdateEntityToModelSpace通过Updater在事务内部做修改,Updater中有参数校验
            // 设置数据库更新时的策略
            db.UpdateEntityToModelSpace(entityId, (originalEntity) =>
                                                  originalEntity.ChangeColor(colorIndex));
        }


        /// <summary>
        /// 复制数据库中Entity对象
        /// </summary>
        /// <param name="basePoint">复制的基点（源实体上的参考点）。</param>
        /// <param name="targetPoint">新实体的目标位置点。</param>
        public static Entity CopyEntity(this Database db, ObjectId entityId, Point3d basePoint, Point3d targetPoint)
        {
            return  db.UpdateEntityToModelSpace(entityId, (oriainglEntity) =>
                                                          oriainglEntity.CopyEntity(basePoint, targetPoint))[0];
        }

        /// <summary>
        /// 镜像数据库中Entity对象
        /// </summary>
        /// <param name="pointA">对称轴上一点</param>
        /// <param name="pointB">对称轴上另一点</param>
        /// <param name="keepOriginal">镜像后是否保留原对象</param>
        public static Entity MirrorEntity(this Database db, ObjectId entityId, Point3d pointA, Point3d pointB, bool keepOriginal)
        {
            Entity mirroredEntity = null;

            mirroredEntity = db.
                            UpdateEntityToModelSpace(entityId, (oriainglEntity) =>
                                                               oriainglEntity.MirrorEntity(pointA, pointB))[0];

            // 删除原对象
            if(!keepOriginal) {
                db.DeleteEntityToModelSpace(entityId);
            }

            return mirroredEntity;
        }

        /// <summary>
        /// 移动数据库中Entity对象
        /// </summary>
        /// <param name="basePoint">复制对象基点</param>
        /// <param name="targetPoint">实体要移动到的目标位置点</param>
        public static void MoveEntity(this Database db, ObjectId entityId, Point3d basePoint, Point3d targetPoint)
        {
            db.UpdateEntityToModelSpace(entityId, (originalEntity) =>
                                                  originalEntity.MoveEntity(basePoint, targetPoint));
        }


        /// <summary>
        /// 旋转数据库中Entity对象
        /// </summary>
        /// <param name="basePoint">基点</param>
        /// <param name="degree">旋转角度（以度为单位）</param>
        public static void RotateEntity(this Database db, ObjectId entityId, Point3d basePoint, double degree)
        {
            db.UpdateEntityToModelSpace(entityId, (originalEntity) =>
                                                  originalEntity.RotateEntity(basePoint, degree));
        }

        /// <summary>
        /// 缩放数据库中Entity对象
        /// </summary>
        /// <param name="basePoint">基点</param>
        /// <param name="scaleFactor">缩放因子</param>
        public static void ScaleEntity(this Database db, ObjectId entityId, Point3d basePoint, double scaleFactor)
        {
            db.UpdateEntityToModelSpace(entityId, (originalEntity) =>
                                                  originalEntity.ScaleEntity(basePoint, scaleFactor));
        }


        /*
         * 返回阵列后的数组 
         * 
         * 注意返回的是一维数组,调用者需要手动转换为二维数值
         */
        public static Entity[] ArrayRectEntity(this Database db, ObjectId entityId, int row, int col, double distRow, double distCol)
        {
            return db.UpdateEntityToModelSpace(entityId, (originalEntity) =>
                                                         originalEntity.ArrayRectEntity(row, col, distRow, distCol));
        }

    }

}
