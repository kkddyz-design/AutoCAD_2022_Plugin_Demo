using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block;
using AutoCAD_2022_Plugin_Demo.files;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.service
{

    public static class AddBlockService
    {

        public static ObjectId AddRectPlateToModelSpace(
            this Database db,
            Point3d position,
            double height,
            double width,
            int thick,
            int count,
            string remarkStr
        )
        {
            // 创建块定义 
            RectPlate rectPlate = new RectPlate(height, width, thick, count, remarkStr);

            // 查询块定义是否存在
            ObjectId rectPlateId = db.GetBlockIdByName(rectPlate.blockName);
            ObjectId refId;
            if(rectPlateId == ObjectId.Null) {
                // 写入块定义
                rectPlateId = db.AddBlockTableRecord(rectPlate.blockName, rectPlate.entityList);
            }

            refId = db.AddBlockReferenceToModelSpace(rectPlateId, position);

            return refId;
        }

        public static Entity[] AddRectPlateToModelSpaceByTxt(this Database db)

        {
            // 读取文件
            return FileTools.ReadEntityFromTXT(
                (contents) =>
                    {
                        Entity[] entities = new Entity[contents.Length];

                        double height, width;
                        int thick, count;
                        string remarkStr = string.Empty;
                        Point3d curPosition;
                        double Y = 100;

                        // 读一行参数创建一个Entity
                        for(int i = 0; i < contents.Length; i++) {
                            if(contents[i] != string.Empty) {
                                // 读取参数
                                string[] args = contents[i].Split('-');
                                double.TryParse(args[0], out height);
                                double.TryParse(args[1], out width);
                                int.TryParse(args[2], out thick);
                                int.TryParse(args[3], out count);
                                if(!args[4].Equals("空值")) {
                                    remarkStr = args[4];
                                }
                                else {
                                    // 必须置空,否则下一次循环会拿到上一次的未置空的值
                                    remarkStr = string.Empty;
                                }

                                // 创建块定义 
                                RectPlate rectPlate = new RectPlate(height, width, thick, count, remarkStr);

                                // 查询块定义是否存在
                                ObjectId rectPlateId = db.GetBlockIdByName(rectPlate.blockName);
                                if(rectPlateId == ObjectId.Null) {
                                    // 写入块定义
                                    rectPlateId = db.AddBlockTableRecord(rectPlate.blockName, rectPlate.entityList);
                                }

                                // 计算position 需要一个起点 // 换行计数 20 
                                Point3d startPoint = new Point3d(100, 100, 0);
                                curPosition = new Point3d(100, Y, 0);

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
                );
        }

    }

}
