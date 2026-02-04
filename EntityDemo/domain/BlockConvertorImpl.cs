using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.domain
{

    /// <summary>
    /// 定义convertor接口：将数据转换为Entity对象
    /// </summary>
    public static class BlockConvertorImpl

    {

        /// <summary>
        /// 将格式"40-100-6-1-不锈钢"的数据转换为RectPlate实体
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        public static Entity[] RectPlateConvertor_TXT(Database db, string[] contents)
        {
            Entity[] entities = new Entity[contents.Length];

            string remarkStr = string.Empty;
            double Y = 100;

            // 读一行参数创建一个Entity
            for(int i = 0; i < contents.Length; i++) {
                if(contents[i] != string.Empty) {
                    // 读取参数
                    string[] args = contents[i].Split('-');
                    double.TryParse(args[0], out double height);
                    double.TryParse(args[1], out double width);
                    int.TryParse(args[2], out int thick);
                    int.TryParse(args[3], out int count);
                    if(!args[4].Equals("空值")) {
                        remarkStr = args[4];
                    }
                    else {
                        // 必须置空,否则下一次循环会拿到上一次的未置空的值
                        remarkStr = string.Empty;
                    }

                    // 创建块定义 
                    Rect_Plate rectPlate = new Rect_Plate(height, width, thick, count, remarkStr);

                    // 查询块定义是否存在
                    ObjectId rectPlateId = db.GetBlockIdByName(rectPlate.BlockName);
                    if(rectPlateId == ObjectId.Null) {
                        // 写入块定义
                        rectPlateId = db.AddBlockTableRecord(rectPlate.BlockName, rectPlate.entityList);
                    }

                    // 计算position 需要一个起点 // 换行计数 20 
                    // Point3d startPoint = new Point3d(100, 100, 0);
                    Point3d curPosition = new Point3d(100, Y, 0);

                    // 写入块参照
                    db.AddBlockReferenceToModelSpace(rectPlateId, curPosition);

                    // 
                    // 更新Y,Y表示下一个矩形的插入点
                    // 由于是从下往上排列,应该保证铁片的规格是从大到小
                    // 需要对插入数据重排序
                    Y += height + 20;
                }
            }
            return entities;
        }

    }

}
