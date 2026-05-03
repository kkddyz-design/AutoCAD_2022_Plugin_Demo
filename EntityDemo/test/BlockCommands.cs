using AutoCAD_2022_Plugin_Demo.EntityDemo.service;
using AutoCAD_2022_Plugin_Demo.EntityDemo.test;
using AutoCAD_2022_Plugin_Demo.tools;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using WinForms = System.Windows.Forms;

[assembly: CommandClass(typeof(BlockCommands))]


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.test
{

    public static  class BlockCommands
    {

        private static Database db = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Database;


        [CommandMethod("WriteToTXT")]
        public static void WriteToTXT()
        {
            // Document doc = Application.DocumentManager.MdiActiveDocument;
            ////Database db = doc.Database;
            // Editor editor = doc.Editor;

            string filePath = "ClampHoleMargin:\\desktop\\";
            string fileName = "test";

            // 使用system.windows.form中的对话框
            WinForms.SaveFileDialog saveFileDialog = new WinForms.SaveFileDialog
            {
                Title = "保存图形数据",
                Filter = "文本文件(*.txt)|*.txt",   // 设置保存类型
                InitialDirectory = filePath,        // 设置保存路径
                FileName = fileName                 // 设置默认文件名
            };

            // string filename = db.Filename;           // 获取dwg文件绝对路径

            // 点击，处理返回结果
            WinForms.DialogResult dialogResult = saveFileDialog.ShowDialog();

            if(dialogResult == WinForms.DialogResult.OK) {
                // 进行文件处理   
                /*
                 * File.WriteAllLines 是 System.IO 命名空间下的静态方法，
                 * 用于一次性将字符串数组（或可枚举的字符串集合）写入指定文件，
                 * 核心特性是：若文件不存在，自动创建；若已存在，覆盖原有内容；
                 * 写入完成后自动关闭文件，无需手动释放资源；
                 * 每行字符串对应文件中的一行（自动添加换行符）
                 * 
                 */

                // 模拟读取当前dwg中的数据
                string[] contents = new string[] { "1111", "22222" };

                // 写入TXT文件
                File.WriteAllLines(saveFileDialog.FileName, contents);
            }
        }

        // [CommandMethod("AddRectPlateDemo1")]
        // public static void AddRectPlateDemo1()
        // {
        // db.AddRectPlateToModelSpace(new Point3d(100, 100, 0), 34, 100, 200, 8, 22, string.Empty);
        // db.AddRectPlateToModelSpace(new Point3d(300, 300, 0), 34, 100, 200, 8, 22, "不锈钢");
        // }


        [CommandMethod("AddRectPlate")]
        public static void AddRectPlateDemo2()
        {
            db.AddRectPlateToModelSpaceByExcel();
        }

        [CommandMethod("AddLeiBan")]
        public static void AddRibPlateByExcel()
        {
            db.AddRibPlateToModelSpaceByExcel();
        }

        [CommandMethod("RecWithInfo")]
        public static void AddRectWithInfo()
        {
            db.AddRectWithInfoFromCmd();
        }

        [CommandMethod("OpenFileWithSheetSelect")]
        public static void TestOpenExcel()
        {
            string filePath = FileTools.OpenFileWithSheetSelect();
        }

        [CommandMethod("addTwoHoleClamp")]
        public static void AddTwoHoleClamp()
        {
            db.AddTwoHoleClampToModelSpaceByExcel();
        }

        [CommandMethod("TestGetBlockByName")]
        public static void GetBlockByName()
        {
            // 获取当前文档和编辑器
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 1. 设置用户输入选项：提示输入块名
            PromptStringOptions pso = new PromptStringOptions("\n请输入要获取的块名称：");
            pso.AllowSpaces = true; // 允许块名包含空格
            PromptResult res = ed.GetString(pso);

            // 2. 判断用户输入是否有效
            if(res.Status != PromptStatus.OK) {
                ed.WriteMessage("\n输入取消或无效。");
                return;
            }

            // 3. 获取用户输入的块名
            string blockName = res.StringResult.Trim();

            BlockTableRecord btr = db.GetBlockTableRecordByName(blockName);

            // ==============================================
            // 遍历块定义里的所有实体
            // ==============================================
            using(Transaction trans = db.TransactionManager.StartTransaction()) {
                ed.WriteMessage($"\n===== 块 {btr.Name} 包含的实体 =====");

                // 遍历块里的所有 ObjectId , 找到文本为+=的
                foreach(ObjectId entId in btr) {
                    // 通过事务打开实体
                    Entity ent = trans.GetObject(entId, OpenMode.ForWrite) as Entity;

                    if(ent != null) {
                        // 输出实体类型
                        ed.WriteMessage($"\n实体类型：{ent.GetType().Name}");

                        // ==============================================
                        // 你可以在这里判断实体类型，做任何操作
                        // ==============================================
                        if(ent is Line line) {
                            ed.WriteMessage($" → 直线：起点 {line.StartPoint}");
                        }
                        else if(ent is Circle circle) {
                            ed.WriteMessage($" → 圆：圆心 {circle.Center}，半径 {circle.Radius}");
                        }
                        else if(ent is Polyline poly) {
                            ed.WriteMessage($" → 多段线，顶点数：{poly.NumberOfVertices}");
                        }
                        else if(ent is BlockReference blkRef) {
                            ed.WriteMessage($" → 嵌套块：{blkRef.Name}");
                        }
                        else if(ent is DBText text) {
                            ed.WriteMessage($"获取到文本:{text.TextString}");

                            // 修改块定义数量
                            if(text.TextString.Equals("+=8")) {
                                text.TextString = "+=100";
                            }
                        }
                    }
                }

                trans.Commit();
                ed.WriteMessage("\n===== 遍历完成 =====");
            }
        }

        [CommandMethod("DBTextToExcel")]
        public static void TestGetDBText()
        {
            // 1. 获取编辑器
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // 2. 获取选中文字 
            List<DBText> textList = SelectionHelper.GetSelectedTexts(ed);

            // 将选中DB对象二维排序
            List<List<DBText>> cadRows = SelectionHelper.SortTextsByPosition(textList);

            // 3. 创建 二维字符串列表（对应表格行+列）
            List<List<string>> tableContents = new List<List<string>>();

            // 4. 双层遍历：逐行 → 逐列 提取文字
            foreach(var row in cadRows) {
                List<string> rowContents = new List<string>();
                foreach(DBText text in row) {
                    if(text != null && !string.IsNullOrEmpty(text.TextString)) {
                        rowContents.Add(text.TextString);
                    }
                }

                // 将一行的文字添加到二维列表
                tableContents.Add(rowContents);
            }

            // 5. 写入Excel

            // 1. 【核心】自动获取系统桌面路径（无需手动写盘符，自适应所有电脑）
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 2. 拼接文件名 + 生成时间戳（年月日时分秒，保证唯一）
            string fileName = $"CAD表格_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            // 3. 组合最终的完整保存路径（桌面 + 带时间戳的文件名）
            // Path.Combine 智能、安全地拼接文件夹路径和文件名 / 子文件夹，自动处理路径分隔符
            string savePath = Path.Combine(desktopPath, fileName);

            FileTools.WriteToExcel(tableContents, savePath);
        }

        [CommandMethod("DBText2DToExcel")]
        public static void GetDBText2DToExcel()
        {
            // 0. 获取编辑器
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // List<DBText> textList = SelectionHelper.GetSelectedTexts(ed);

            // 1. 框选实体
            List<DBObject> entities = SelectionHelper.GetSelectedEntities(ed);

            // 2. 筛选文本、水平线、竖直线
            List<DBText> dbTexts = new List<DBText>();
            List<Line> verticalLines = new List<Line>();
            List<Line> horizontalLines = new List<Line>();
            SelectionHelper.FilterEntitiesToContainers(entities, ref dbTexts, ref verticalLines, ref horizontalLines);

            // 3. 构建表格坐标（去重、排序）
            double[][] tableCoords = SelectionHelper.BuildTableAllPointsCoordinate(horizontalLines, verticalLines);

            // 4. 核心：文本匹配单元格，生成二维列表
            List<List<DBText>> cadTable = SelectionHelper.MatchTextsToTableCells(tableCoords, dbTexts);

            // 
            // ==============================================
            // 下面是我为你写的：转成 string 二维数组 string[,]
            // ==============================================

            List<List<string>> stringTable = new List<List<string>>();

            foreach(var row in cadTable) {
                List<string> stringRow = new List<string>();

                if(row != null) {
                    foreach(var dbText in row) {
                        // 安全获取文本，空值自动填空字符串
                        stringRow.Add(dbText?.TextString ?? string.Empty);
                    }
                }

                stringTable.Add(stringRow);
            }

            // 5. 将数据写入excel
            // 自动获取系统桌面路径（无需手动写盘符，自适应所有电脑）
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 拼接文件名 + 生成时间戳（年月日时分秒，保证唯一）
            string fileName = $"CAD表格_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            // 组合最终的完整保存路径（桌面 + 带时间戳的文件名）
            // Path.Combine 智能、安全地拼接文件夹路径和文件名 / 子文件夹，自动处理路径分隔符

            string savePath = Path.Combine(desktopPath, fileName);
            FileTools.WriteToExcel(stringTable, savePath);
        }


        [CommandMethod("CPDBTextToClipboard")]
        public static void GetDBTextClipboard()
        {
            // 0. 获取编辑器
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // 1. 框选实体
            List<DBObject> entities = SelectionHelper.GetSelectedEntities(ed);

            // 2. 筛选文本、水平线、竖直线
            List<DBText> dbTexts = new List<DBText>();
            List<Line> verticalLines = new List<Line>();
            List<Line> horizontalLines = new List<Line>();
            SelectionHelper.FilterEntitiesToContainers(entities, ref dbTexts, ref verticalLines, ref horizontalLines);

            // 3. 构建表格坐标（去重、排序）
            double[][] tableCoords = SelectionHelper.BuildTableAllPointsCoordinate(horizontalLines, verticalLines);

            // 4. 核心：文本匹配单元格，生成二维列表
            List<List<DBText>> cadTable = SelectionHelper.MatchTextsToTableCells(tableCoords, dbTexts);

            // 5. 将二维DBText列表转换为二维字符串列表（空值安全处理）

            List<List<string>> stringTable = new List<List<string>>();

            foreach(var row in cadTable) {
                List<string> stringRow = new List<string>();

                if(row != null) {
                    foreach(var dbText in row) {
                        // 安全获取文本，空值自动填空字符串
                        stringRow.Add(dbText?.TextString ?? string.Empty);
                    }
                }

                stringTable.Add(stringRow);
            }

            // 6. 将二维字符串列表转换为制表符分隔的文本（适合粘贴到Excel）
            try {
                ClipboardTools.CopyToExcelClipboard(stringTable);
            }
            catch(System.Exception e) {
                ed.WriteMessage("用户未选择任何文本");
            }
        }

    }

}
