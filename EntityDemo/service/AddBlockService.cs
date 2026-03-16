using AutoCAD_2022_Plugin_Demo.EntityDemo.domain;
using AutoCAD_2022_Plugin_Demo.EntityDemo.domain.block;
using AutoCAD_2022_Plugin_Demo.tools;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
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
            ObjectId rectPlateId = db.GetBlockIdByName(rectPlate.BlockName);
            ObjectId refId;
            if(rectPlateId == ObjectId.Null) {
                // 写入块定义
                rectPlateId = db.AddBlockTableRecord(rectPlate.BlockName, rectPlate.entityList);
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

        public static void AddRectPlateToModelSpaceByExcel(this Database db)
        {
            // 选择文件
            // string filePath = FileTools.OpenFile();

            string filePath = FileTools.OpenFileWithSheetSelect();

            string[] paths = filePath.Split('|');

            // 读取文件数据
            List<List<string>> excelData = FileTools.ReadExcelData(paths[0], paths[1]);

            // 创建对象数组
            List<Rect_Plate> rect_plates = new List<Rect_Plate>();
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

                    // 读取一行数据到内存 第一列规格，第二列数量
                    rowData[colIndex] = cellValue;
                }

                // 创建rib_plate对象
                string specStr = rowData[0];
                int count = int.Parse(rowData[1]);

                // 创建Rect_palte对象
                string material;
                int thick;
                double rectH;
                double rectL;
                ConvertExcelData(specStr, out material, out thick, out rectH, out rectL); // 解析规格，初始化变量
                Rect_Plate rect_Plate = new Rect_Plate(rectH, rectL, thick, count, material);
                rect_plates.Add(rect_Plate); // 存入对象列表
            }

            // 初始化插入位置 当换行时,Y改变,X从1000开始
            double init_Y = 1000;
            double cur_X = 1000;
            double cur_Y = 1000;
            bool isFirstLoop = true;

            // 记录上一个插入块的长度,高度
            double lastBlockL = 0;
            double lastBlockH = 0;
            int curThick = -1;

            // 写入块定义
            foreach(Rect_Plate item in rect_plates) {
                // 查询是否已经写入
                ObjectId plateId = db.GetBlockIdByName(item.BlockName);
                if(plateId == ObjectId.Null) {
                    // 写入块定义
                    plateId = db.AddBlockTableRecord(item.BlockName, item.entityList);
                }

                // 计算插入nextPosition
                if(isFirstLoop) {
                    // 初始化curThick
                    curThick = item.BlockThick;
                    isFirstLoop = false;
                }
                else {
                    if(item.BlockThick == curThick) {
                        // 更新curPosition
                        cur_Y -= lastBlockH + 30;
                    }
                    else {
                        cur_Y = init_Y;
                        cur_X += lastBlockL + 150;

                        curThick = item.BlockThick;
                    }
                }

                // 记录一行最大L
                if(item.Rect_Plate_L > lastBlockL) {
                    lastBlockL = item.Rect_Plate_L;
                }

                lastBlockH = item.Rect_Plate_H;

                // 写入块参照
                db.AddBlockReferenceToModelSpace(plateId, new Point3d(cur_X, cur_Y, 0));
            }
        }


        private static bool ConvertExcelData(string inputStr, out string material, out int thick, out double rectH, out double rectL)
        {
            // 初始化out参数（C#语法要求：out参数必须在方法内全部赋值）
            material = string.Empty;
            thick = 0;
            rectH = 0;
            rectL = 0;

            // 步骤1：前置处理，去除前后空格，判空
            string str = inputStr.Trim();
            if(string.IsNullOrEmpty(str)) {
                Console.WriteLine("输入字符串不能为空或仅含空格！");
                return false;
            }

            // 步骤2：查找所有关键分隔符的索引
            int deltaIndex = str.IndexOf('δ');
            int equalIndex = str.IndexOf('=');
            int starIndex = str.IndexOf('*');

            // 步骤3：校验分隔符是否存在且顺序正确
            if(deltaIndex == -1 || equalIndex == -1 || starIndex == -1 || deltaIndex > equalIndex || equalIndex > starIndex) {
                Console.WriteLine("输入字符串格式错误！缺少关键分隔符δ/=/，或分隔符顺序错误");
                return false;
            }

            // 步骤4：处理材质——δ开头默认碳钢，否则截取δ前的内容
            material = str.StartsWith("δ") ? "碳钢" : str.Substring(0, deltaIndex);

            // 步骤5：截取数字部分的字符串（待转double）
            string thickStr = str.Substring(deltaIndex + 1, equalIndex - deltaIndex - 1);
            string rectHStr = str.Substring(equalIndex + 1, starIndex - equalIndex - 1);
            string rectLStr = str.Substring(starIndex + 1);

            // 步骤6：安全转换为double，转换失败返回false并提示
            if(!int.TryParse(thickStr, out thick)) {
                Console.WriteLine($"厚度部分转换失败！「{thickStr}」不是有效数字（支持整数/小数）");
                return false;
            }
            if(!double.TryParse(rectHStr, out rectH)) {
                Console.WriteLine($"高度部分转换失败！「{rectHStr}」不是有效数字（支持整数/小数）");
                return false;
            }
            if(!double.TryParse(rectLStr, out rectL)) {
                Console.WriteLine($"长度部分转换失败！「{rectLStr}」不是有效数字（支持整数/小数）");
                return false;
            }

            // 所有步骤完成，解析成功
            return true;
        }

        public static void AddRibPlateToModelSpaceByExcel(this Database db)
        {
            // 选择数据表格
            string filePath = FileTools.OpenFileWithSheetSelect();
            string[] paths = filePath.Split('|');

            // 读取文件数据
            List<List<string>> excelData = FileTools.ReadExcelData(paths[0], paths[1]);

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
                double upperDistance = double.Parse(rowData[8]);
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
                ObjectId rectPlateId = db.GetBlockIdByName(rib_Plate_item.BlockName);
                if(rectPlateId == ObjectId.Null) {
                    // 写入块定义
                    rectPlateId = db.AddBlockTableRecord(rib_Plate_item.BlockName, rib_Plate_item.entityList);
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


        public static void AddTwoHoleClampToModelSpaceByExcel(this Database db)
        {
            // 选择数据表格
            string filePath = FileTools.OpenFileWithSheetSelect();
            string[] paths = filePath.Split('|');

            List<List<string>> excelData = null;
            try {
                // 读取文件数据
                excelData = FileTools.ReadExcelData(paths[0], paths[1]);
            }
            catch(IndexOutOfRangeException) {
                Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
                editor.WriteMessage("用户未选择文档");
                return;
            }

            // 创建对象数组
            List<TowHolePipeClamp> towHolePipeClampsList = new List<TowHolePipeClamp>();
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

                // 创建TowHolePipeClamp对象

                double innerDiameter = double.Parse(rowData[1]);
                int thick = int.Parse(rowData[2]);
                int count = int.Parse(rowData[3]);
                double filletRadius = double.Parse(rowData[4]);
                double span = double.Parse(rowData[5]);
                double clampEar = double.Parse(rowData[6]);
                double holeMargin = double.Parse(rowData[7]);
                double clampHole = double.Parse(rowData[8]);

                // rowData[9]是clampLength  rowData[12]是管夹规格 
                double clampWidth = double.Parse(rowData[10]);
                string material = rowData[11];
                TowHolePipeClamp clamp = new TowHolePipeClamp(innerDiameter, thick, count, filletRadius, span, clampEar, holeMargin, clampHole, clampWidth, material);

                // rib_Plate存入内存
                towHolePipeClampsList.Add(clamp);
            }

            // 初始化插入位置 当换行时,Y改变,X从1000开始
            double init_Y = 1000;
            double cur_X = 1000;
            double cur_Y = 1000;
            bool isFirstLoop = true;

            // 记录上一个插入块的长度,高度
            // double lastBlockL = 0;
            // double lastBlockH = 0;
            int curThick = -1;

            // 写入块定义
            foreach(TowHolePipeClamp clamp in towHolePipeClampsList) {
                // 查询是否已经写入
                ObjectId objectId = db.GetBlockIdByName(clamp.BlockName);
                if(objectId == ObjectId.Null) {
                    // 写入块定义
                    objectId = db.AddBlockTableRecord(clamp.BlockName, clamp.entityList);
                }

                // 计算插入nextPosition
                if(isFirstLoop) {
                    // 初始化curThick
                    curThick = clamp.BlockThick;
                    isFirstLoop = false;
                }
                else {
                    if(clamp.BlockThick == curThick) {
                        // 更新curPosition
                        cur_Y -= (clamp.ClampWidth + 50 + clamp.ClampInnerDiameter) * 1.5;
                    }
                    else {
                        curThick = clamp.BlockThick;
                        cur_X += (1000 + clamp.ClampLength * 2);
                        cur_Y = init_Y;
                    }
                }

                // 写入块参照
                db.AddBlockReferenceToModelSpace(objectId, new Point3d(cur_X, cur_Y, 0));
            }
        }

    }

}
