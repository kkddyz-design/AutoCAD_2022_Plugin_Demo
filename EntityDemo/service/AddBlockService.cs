using AutoCAD_2022_Plugin_Demo.EntityDemo.domain;
using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block;
using AutoCAD_2022_Plugin_Demo.files;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
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
            Rect_Plate rectPlate = new Rect_Plate(height, width, thick, count, remarkStr);

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

        /// <summary>
        /// 调用FileTools.ReadEntityFromTXT，传入将contents(数据行)转换为Entity[]的委托方法
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static Entity[] AddRectPlateToModelSpaceByTxt(this Database db)
        {
            // 选择文件
            string filePath = FileTools.OpenFile();

            // 读取文件数据
            string[] contents = FileTools.readTxtData(filePath);

            // 调用convertor转换数据
            return BlockConvertorImpl.RectPlateConvertor_TXT(db, contents);
        }

        public static void AddRibPlateToModelSpaceByExcel(this Database db)
        {
            // 选择文件
            string filePath = FileTools.OpenFile();

            // 读取文件数据
            List<List<string>> excelData = FileTools.ReadExcelData(filePath, "肋板汇总表");

            // 创建对象数组
            List<Rib_Plate> rib_plates = new List<Rib_Plate>();
            string[] rowData = new string[excelData[0].Count];

            // 遍历list 创建块定义
            for(int rowIndex = 1; rowIndex < excelData.Count; rowIndex++) {
                List<string> row = excelData[rowIndex];
                if(row == null || row.Count == 0) {
                    Console.WriteLine($"第{rowIndex + 1}行：空行");
                    continue;
                }

                Console.Write($"第{rowIndex + 1}行：");
                for(int colIndex = 0; colIndex < row.Count; colIndex++) {
                    string cellValue = row[colIndex] ?? string.Empty;

                    // 读取一行数据到内存 
                    rowData[colIndex] = cellValue;
                }

                // 创建rib_plate对象
                string tag = rowData[0];
                double OD = double.Parse(rowData[1]);
                double H = double.Parse(rowData[2]);
                string material = rowData[3];
                double offset = double.Parse(rowData[4]);
                string rectSpec = rowData[5];
                double ribPlateThick = double.Parse(rowData[6]);
                double buttomMatgin = double.Parse(rowData[7]);
                string upperDistance = rowData[8];
                double cnt = double.Parse(rowData[9]);

                double textHeight = 10;
                double textMargin = 5;

                Rib_Plate rib_Plate = new Rib_Plate(tag, OD, H, material, offset, rectSpec, ribPlateThick,
                    buttomMatgin, upperDistance, cnt, textHeight, textMargin);

                // rib_Plate存入内存
                rib_plates.Add(rib_Plate);
            }

            // 初始化插入位置 当换行时,Y改变,X从1000开始
            double init_X = 1000;
            double cur_X = 1000;
            double cur_Y = 1000;
            bool isFirstLoop = true;

            // 确保原数据中的OD按顺序排
            double curOD = rib_plates.First().Rib_Plate_OD;

            // 写入块定义
            foreach(Rib_Plate rib_Plate_item in rib_plates) {
                // 查询是否已经写入
                ObjectId rectPlateId = db.GetBlockIdByName(rib_Plate_item.blockName);
                if(rectPlateId == ObjectId.Null) {
                    // 写入块定义
                    rectPlateId = db.AddBlockTableRecord(rib_Plate_item.blockName, rib_Plate_item.entityList);
                }

                // 计算插入nextPosition

                if(isFirstLoop) {
                    isFirstLoop = false;
                }
                else {
                    if(rib_Plate_item.Rib_Plate_OD == curOD) {
                        // 更新curPosition
                        cur_X += rib_Plate_item.Rib_Plate_L + 50;
                    }
                    else {
                        cur_X = init_X;
                        cur_Y -= rib_Plate_item.Rib_Plate_H + 100;

                        curOD = rib_Plate_item.Rib_Plate_OD;
                    }
                }

                // 写入块参照
                db.AddBlockReferenceToModelSpace(rectPlateId, new Point3d(cur_X, cur_Y, 0));
            }
        }

    }

}
