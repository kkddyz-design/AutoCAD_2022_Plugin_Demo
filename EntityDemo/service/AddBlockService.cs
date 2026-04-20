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
            double OD,
            double height,
            double width,
            int thick,
            int count,
            string remarkStr
        )
        {
            // 创建块定义 
            Rect_Plate rectPlate = new Rect_Plate(OD, height, width, thick, count, remarkStr);

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


        public static void AddRectPlateToModelSpaceByExcel(this Database db)
        {
            // 选择文件

            string filePath = FileTools.OpenFileWithSheetSelect();

            string[] paths = filePath.Split('|');

            // 读取文件数据
            List<List<string>> excelData = FileTools.ReadExcelData(paths[0], paths[1]);

            // 创建对象字典
            Dictionary<string, Rect_Plate> rectPlate_dictionary = new Dictionary<string, Rect_Plate>();

            // 创建对象列表
            List<Rect_Plate> rect_plates = new List<Rect_Plate>();

            Dictionary<string, Rect_Plate> rect_plates_dictionary = new Dictionary<string, Rect_Plate>();

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

                // 读取表格数据
                string specStr = rowData[0];        // 方板规格
                int count = int.Parse(rowData[1]);  // 方板数量
                int OD = int.Parse(rowData[2]);     // 方板OD

                // 创建Rect_palte对象
                string material;
                int thick;
                double rectH;
                double rectL;
                ConvertRectPlate(specStr, out material, out thick, out rectH, out rectL); // 解析规格，初始化变量
                Rect_Plate rect_Plate = new Rect_Plate(OD, rectH, rectL, thick, count, material);

                // 如果不存在，加入字典；如果已经存在，累加数量,
                Rect_Plate exist_rect_plate = null;

                if(rect_plates_dictionary.TryGetValue(rect_Plate.Tag, out exist_rect_plate)) {
                    exist_rect_plate.SetBlockNameAndCount(exist_rect_plate.BlockCount + rect_Plate.BlockCount);
                }
                else {
                    rect_plates_dictionary.Add(rect_Plate.Tag, rect_Plate);
                }
            }

            // 将去重后的对象放入数组
            foreach(Rect_Plate plate in rect_plates_dictionary.Values) {
                rect_plates.Add(plate);
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


        private static bool ConvertRectPlate(string inputStr, out string material, out int thick, out double rectH, out double rectL)
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

            // 创建对象字典
            Dictionary<string, Rib_Plate> rib_dictionary = new Dictionary<string, Rib_Plate>();

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

                // 解析Excel数据
                string tag = rowData[0];
                double OD = double.Parse(rowData[1]);
                double H = double.Parse(rowData[2]);
                string material = rowData[3];
                double offset = double.Parse(rowData[4]);
                string rectSpec = rowData[5];
                int ribPlateThick = int.Parse(rowData[6]);
                double buttomMatgin = double.Parse(rowData[7]);
                double upperDistance = double.Parse(rowData[8]);
                int cnt = int.Parse(rowData[9]);

                double textHeight = 14;
                double textMargin = 10;

                // rib_plate对象
                Rib_Plate rib_Plate = new Rib_Plate(tag, OD, H, material, offset, rectSpec, ribPlateThick,
                    buttomMatgin, upperDistance, cnt, textHeight, textMargin);

                // 如果不存在，加入字典；如果已经存在，累加数量,
                Rib_Plate exist_rib_plate = null;

                if(rib_dictionary.TryGetValue(rib_Plate.Tag, out exist_rib_plate)) {
                    exist_rib_plate.SetBlockNameAndCount(exist_rib_plate.BlockCount + rib_Plate.BlockCount);
                }
                else {
                    rib_dictionary.Add(rib_Plate.Tag, rib_Plate);
                }
            }

            // 将去重后的对象放入数组
            foreach(Rib_Plate plate in rib_dictionary.Values) {
                rib_plates.Add(plate);
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


        /// <summary>
        /// 通过命令行创建带有信息的矩形 - 先创建块再插入点
        /// </summary>
        /// <param name="db"></param>
        public static void AddRectWithInfoFromCmd1(this Database db)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // ============= 1. 解析命令行参数（如 89-100） =============
            double rectH = 0, rectL = 0;
            int thick = 0, count = 0;

            // 在 ParseCommandArgs 方法里解析出 89、100，然后把这两个数字赋值给外面的 rectH 和 rectL。
            bool hasVaildGrgs = ParseCommandArgs(ed, ref rectH, ref rectL, ref thick, ref count);

            if(hasVaildGrgs) {
                ed.WriteMessage($"成功获取到参数rectH:{rectH},rectL{rectL}");

                // public Rect_Plate(double od, double rectH, double rectL, int thick, int count, string material)
                Rect_Plate plateWithInfo = new Rect_Plate(rectH, rectL, thick, count);

                // 创建块定义
                string btrName = $"PlateWithInfo_板厚10_个数8_{rectH}-{rectL}";
                ObjectId btrId = db.AddBlockTableRecord(btrName, plateWithInfo.entityList);

                // 创建块参照
                BlockReference br_plateWithInfo = new BlockReference(new Point3d(0, 0, 0), btrId);

                // 调用Jig类

                // 1. 直接实例化，不需要 using
                BlockJig jig = new BlockJig(br_plateWithInfo, new Point3d(0, 0, 0));

                // 2. 执行拖拽
                PromptResult result = ed.Drag(jig);

                // 3. 结果处理
                if(result.Status == PromptStatus.OK) {
                    // 用户点击确认，正式将块加入数据库
                    using(Transaction trans = db.TransactionManager.StartTransaction()) {
                        BlockTableRecord ms = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                        ms.AppendEntity(br_plateWithInfo);
                        trans.AddNewlyCreatedDBObject(br_plateWithInfo, true);
                        trans.Commit();
                    }
                    ed.WriteMessage("\n插入成功！");
                }
                else {
                    // 用户取消，tempBlock 自动被垃圾回收
                    ed.WriteMessage("\n已取消。");
                }
            }
            else {
                ed.WriteMessage("未获取到合法参数！！！");
            }
        }

        /// <summary>
        /// 过命令行创建带有信息的矩形 - 先插入点再创建块
        /// </summary>
        /// <param name="db"></param>
        public static void AddRectWithInfoFromCmd(this Database db)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try {
                // ============= 【第一步：先让用户点插入点】=============
                PromptPointOptions ppo = new PromptPointOptions("\n请指定块的插入点: ");
                PromptPointResult pointRes = ed.GetPoint(ppo);

                // 如果用户取消或点错
                if(pointRes.Status != PromptStatus.OK) {
                    ed.WriteMessage("\n已取消操作。");
                    return;
                }
                Point3d insertPos = pointRes.Value; // 拿到用户点的坐标

                // ============= 【第二步：再输入参数】=============
                double rectH = 0, rectL = 0;
                int thick = 0, count = 0;

                bool hasVaildGrgs = ParseCommandArgs(ed, ref rectH, ref rectL, ref thick, ref count);

                if(!hasVaildGrgs) {
                    ed.WriteMessage("\n未获取到合法参数！");
                    return;
                }

                ed.WriteMessage($"\n成功获取参数：H={rectH}, L={rectL}, 厚={thick}, 数量={count}");

                // ============= 【第三步：创建块 + 插入到刚才点的位置】=============
                using(Transaction trans = db.TransactionManager.StartTransaction()) {
                    // 1. 创建带参数的块对象
                    Rect_Plate plateWithInfo = new Rect_Plate(rectH, rectL, thick, count);

                    // 2. 创建块定义
                    string btrName = $"Plate_{thick}厚_{count}个_{rectH}-{rectL}";
                    ObjectId btrId = db.AddBlockTableRecord(btrName, plateWithInfo.entityList);

                    // 3. 创建块参照（用用户刚才点的坐标）
                    BlockReference br = new BlockReference(insertPos, btrId);

                    // 4. 加入模型空间
                    BlockTableRecord ms = trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    ms.AppendEntity(br);
                    trans.AddNewlyCreatedDBObject(br, true);

                    trans.Commit();
                    ed.WriteMessage("\n✅ 块已成功插入！");
                }
            }
            catch(Exception ex) {
                ed.WriteMessage($"\n错误：{ex.Message}");
            }
        }


        #region AddRectWithInfoFromCmd的工具方法

        /// <summary>
        /// 解析命令行参数（如 addr 89-100 → 长89 宽100）
        /// </summary>
        private static bool ParseCommandArgs(Editor ed, ref double len, ref double wid, ref int thick, ref int count)
        {
            // 1. 获取用户输入
            // 提示语更新，说明支持 '-' 或 '空格'
            PromptStringOptions strOpts = new PromptStringOptions(
                "\n请输入参数 (格式: 长 宽 厚 数量) 或 (长-宽-厚-数量): ");
            strOpts.AllowSpaces = true;

            PromptResult res = ed.GetString(strOpts);

            if(res.Status != PromptStatus.OK) {
                return false;
            }

            string userInput = res.StringResult;

            if(string.IsNullOrWhiteSpace(userInput)) {
                ed.WriteMessage("\n 输入不能为空。");
                return false;
            }

            // 2. 解析参数 (关键修改部分)
            // 定义分隔符：横杠和空格
            char[] separators = new char[] { '-', ' ' };

            // 使用 RemoveEmptyEntries 防止因连续空格或两端空格导致的空字符串错误
            string[] parts = userInput.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            // 必须严格等于 4 个参数
            if(parts.Length != 4) {
                ed.WriteMessage("\n 格式错误：请输入 4 个参数 (长 宽 厚 数量)，可用空格或横杠分隔。");
                return false;
            }

            // 3. 尝试转换并校验数值
            double tempLen, tempWid;
            int tempThick, tempCount;

            // 参数 1: 长 (double)
            if(!double.TryParse(parts[0], out tempLen) || tempLen <= 0) {
                ed.WriteMessage("\n 错误：第一个参数(长)必须是大于0的数字。");
                return false;
            }

            // 参数 2: 宽 (double)
            if(!double.TryParse(parts[1], out tempWid) || tempWid <= 0) {
                ed.WriteMessage("\n 错误：第二个参数(宽)必须是大于0的数字。");
                return false;
            }

            // 参数 3: 厚 (int)
            if(!int.TryParse(parts[2], out tempThick) || tempThick <= 0) {
                ed.WriteMessage("\n 错误：第三个参数(厚)必须是大于0的整数。");
                return false;
            }

            // 参数 4: 数量 (int)
            if(!int.TryParse(parts[3], out tempCount) || tempCount <= 0) {
                ed.WriteMessage("\n 错误：第四个参数(数量)必须是大于0的整数。");
                return false;
            }

            // 4. 赋值给 ref 参数
            len = tempLen;
            wid = tempWid;
            thick = tempThick;
            count = tempCount;

            return true;
        }
        #endregion

    }

}

