using AutoCAD_2022_Plugin_Demo.tools;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using acad_AppService = Autodesk.AutoCAD.ApplicationServices;
using WinForms = System.Windows.Forms;

[assembly: CommandClass(typeof(FileTools))]


namespace AutoCAD_2022_Plugin_Demo.tools
{

    /// <summary>
    /// 这个类专门用于从数据源读取数据，仅仅封装读取的原始数据，而不包括数据格式转换等。
    /// </summary>
    public static class FileTools
    {

        public static Database db = acad_AppService.Application.DocumentManager.MdiActiveDocument.Database;


        /// <summary>
        /// 通过OpenFileDialog选择文件；该方法被OpenFileWithSheetSelect替代(需要手动设置选择的表格)
        /// </summary>
        /// <returns></returns>
        public static string OpenFile()
        {
            //// 选择文件
            // WinForms.OpenFileDialog openFileDialog = new WinForms.OpenFileDialog()
            // {
            // Title = "打开文件",
            // Filter = "表格(*.xlsx)|*.xlsx|文本文件(*.txt)|*.txt",
            // InitialDirectory = "E:\\dsektop\\",
            // CheckFileExists = true, // 校验文件是否存在，避免选到无效路径
            // RestoreDirectory = true // 关闭对话框后恢复原目录
            // };

            //// 显示Form
            // WinForms.DialogResult dialogResult = openFileDialog.ShowDialog();

            // if(dialogResult == WinForms.DialogResult.OK) {
            // return openFileDialog.FileName;
            // }
            // else {
            // return string.Empty;
            // }

            // 【优化1】使用 using 自动释放控件，避免内存泄漏
            using(OpenFileDialog openFileDialog = new OpenFileDialog()) {
                openFileDialog.Title = "选择文件";

                // 【优化2】你现在需要DWG，所以我给你加上，保留原有格式
                openFileDialog.Filter = "AutoCAD 文件(*.dwg)|*.dwg|表格(*.xlsx)|*.xlsx|文本文件(*.txt)|*.txt|所有文件(*.*)|*.*";

                // 【优化3】自动获取桌面路径（不写死），兼容所有电脑
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                openFileDialog.CheckFileExists = true;
                openFileDialog.RestoreDirectory = true;

                // 【优化4】AutoCAD 中必须用此方式显示模态对话框，防止窗口卡死
                // 完美兼容 AutoCAD + WinForm 对话框
                var owner = new NativeWindow();
                owner.AssignHandle(acad_AppService.Core.Application.MainWindow.Handle);

                if(openFileDialog.ShowDialog(owner) == DialogResult.OK) {
                    return openFileDialog.FileName;
                }
            }

            // 取消选择返回空
            return string.Empty;
        }


        /// <summary>
        /// 通过OpenFileDialog选择文件，并对Excel文件指定读取的工作表
        /// </summary>
        /// <returns>Excel文件返回「文件路径|工作表名」，TXT文件返回「文件路径」，取消选择返回空字符串</returns>

        public static string OpenFileWithSheetSelect()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = "打开文件",
                Filter = "表格(*.xlsx)|*.xlsx|文本文件(*.txt)|*.txt",
                InitialDirectory = "E:\\desktop\\",
                CheckFileExists = true, // 校验文件是否存在，避免选到无效路径
                RestoreDirectory = true // 关闭对话框后恢复原目录
            };
            DialogResult dialogResult = openFileDialog.ShowDialog();
            if(dialogResult != WinForms.DialogResult.OK) {
                return string.Empty;
            }

            string filePath = openFileDialog.FileName;

            // 判断文件类型：如果是TXT，直接返回路径；如果是XLSX，解析工作表并让用户选择
            if(filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) {
                return filePath;
            }
            else if(filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) {
                try {
                    // 设置EPPlus许可证（4.5.3.3及以下版本需加，高版本需单独处理）
                    ExcelPackage.License.SetNonCommercialPersonal("kkddyz");

                    // 读取Excel文件，获取所有工作表名称
                    List<string> sheetNames = new List<string>();
                    using(ExcelPackage package = new ExcelPackage(new FileInfo(filePath))) {
                        foreach(ExcelWorksheet worksheet in package.Workbook.Worksheets) {
                            sheetNames.Add(worksheet.Name); // 遍历所有工作表，存入集合
                        }
                    }

                    // 校验是否有工作表（避免空Excel文件）
                    if(sheetNames.Count == 0) {
                        WinForms.MessageBox.Show("该Excel文件中无任何工作表！", "提示",
                            WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                        return string.Empty;
                    }

                    // 只有1个工作表，直接使用，无需弹窗选择
                    else if(sheetNames.Count == 1) {
                        return $"{filePath}|{sheetNames[0]}";
                    }

                    // 多个工作表，弹出选择框让用户指定
                    else {
                        // 用ListBox做简易工作表选择窗口（也可自定义WinForm窗体）
                        using(Form sheetSelectForm = new Form()) {
                            sheetSelectForm.Text = "选择工作表";
                            sheetSelectForm.Size = new Size(300, 400);
                            sheetSelectForm.StartPosition = WinForms.FormStartPosition.CenterParent; // 居中显示
                            ListBox listBox = new ListBox()
                            {
                                Dock = WinForms.DockStyle.Fill,
                                Font = new System.Drawing.Font("微软雅黑", 10),
                                SelectionMode = WinForms.SelectionMode.One // 只能单选
                            };
                            listBox.Items.AddRange(sheetNames.ToArray()); // 绑定工作表名称
                            listBox.SelectedIndex = 0; // 默认选中第一个工作表
                            Button confirmBtn = new Button()
                            {
                                Text = "确认选择",
                                Dock = WinForms.DockStyle.Bottom,
                                Height = 40,
                                Font = new System.Drawing.Font("微软雅黑", 10)
                            };
                            confirmBtn.Click += (s, e) =>
                                {
                                    sheetSelectForm.DialogResult = WinForms.DialogResult.OK;
                                };

                            // 将控件添加到窗体
                            sheetSelectForm.Controls.Add(listBox);
                            sheetSelectForm.Controls.Add(confirmBtn);
                            sheetSelectForm.AcceptButton = confirmBtn; // 按回车触发确认

                            // 显示工作表选择窗体
                            if(sheetSelectForm.ShowDialog() == WinForms.DialogResult.OK) {
                                string selectedSheet = listBox.SelectedItem.ToString();
                                return $"{filePath}|{selectedSheet}";
                            }
                            else {
                                return string.Empty; // 用户取消选择工作表
                            }
                        }
                    }
                }
                catch(System.Exception ex) {
                    WinForms.MessageBox.Show($"读取Excel工作表失败：{ex.Message}", "错误",
                        WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                    return string.Empty;
                }
            }
            else {
                WinForms.MessageBox.Show("仅支持.xlsx和.txt文件！", "提示",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                return string.Empty;
            }
        }


        /// <summary>
        /// 按行读取txt中的数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>contents[i]表示每一行数据;如果读取失败返回空数组</returns>
        public static string[] readTxtData(string filePath)
        {
            string[] contents = new string[] { };

            // 读取文件数据
            if(filePath != string.Empty) {
                contents = File.ReadAllLines(filePath)
                        .Where(line => !string.IsNullOrWhiteSpace(line)) // 排除空行/仅含空白字符的行
                        .ToArray();
            }

            return contents;
        }

        /// <summary>
        /// 读取指定Excel文件、指定工作表的所有数据到嵌套List中
        /// </summary>
        /// <param name="filePath">Excel文件绝对路径（仅支持.xlsx）</param>
        /// <param name="sheetName">要读取的工作表名称（如"Sheet1"）</param>
        /// <returns>二维列表：外层List=行，内层List=列；空列表=无数据/读取失败</returns>
        /// <exception cref="FileNotFoundException">文件不存在</exception>
        /// <exception cref="NotSupportedException">文件格式不是.xlsx</exception>
        /// <exception cref="ArgumentException">工作表名称为空/无效</exception>
        /// <exception cref="InvalidOperationException">工作表不存在</exception>
        public static List<List<string>> ReadExcelData(string filePath, string sheetName)
        {
            // 初始化返回容器
            var excelData = new List<List<string>>();

            // 1. 入参校验（提前拦截无效输入）
            if(string.IsNullOrWhiteSpace(filePath)) {
                throw new ArgumentNullException(nameof(filePath), "Excel文件路径不能为空");
            }

            if(string.IsNullOrWhiteSpace(sheetName)) {
                throw new ArgumentNullException(nameof(sheetName), "工作表名称不能为空");
            }

            if(!File.Exists(filePath)) {
                throw new FileNotFoundException("指定的Excel文件不存在", filePath);
            }

            if(Path.GetExtension(filePath).Trim().ToLower() != ".xlsx") {
                throw new NotSupportedException("仅支持.xlsx格式的Excel文件，不支持.xls旧格式");
            }

            try {
                // 2. 设置非商业许可证
                ExcelPackage.License.SetNonCommercialPersonal("kkddyz");

                // ==============================================
                // 核心修改：创建无缓存的FileStream，替代默认FileInfo
                // 关键配置：禁用缓存 + 允许共享读写 + 自动释放流
                // ==============================================
                using(var fileStream = new FileStream(
                    path: filePath,
                    mode: FileMode.Open,          // 打开现有文件
                    access: FileAccess.Read,      // 仅读模式（符合Excel读取需求）
                    share: FileShare.ReadWrite,   // 允许其他程序同时读写（修改Excel后不被占用）
                    bufferSize: 4096,             // 标准缓冲区大小
                    options: FileOptions.SequentialScan | FileOptions.WriteThrough // 禁用.NET缓存，强制读磁盘
                )) {
                    // 3. 打开Excel文件：传入无缓存的FileStream，而非FileInfo
                    using(var excelPackage = new ExcelPackage(fileStream)) {
                        // 4. 获取指定名称的工作表（原有逻辑不变）
                        ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[sheetName];
                        if(worksheet == null) {
                            throw new InvalidOperationException($"工作表【{sheetName}】不存在于Excel文件中");
                        }

                        // 5. 获取数据范围（自动识别有数据的行/列，避免空遍历）
                        var dimension = worksheet.Dimension;
                        if(dimension == null) {
                            Console.WriteLine($"警告：工作表【{sheetName}】无任何数据");
                            return excelData; // 返回空列表
                        }

                        // 6. 遍历所有行和列，填充数据到嵌套List（原有逻辑不变）
                        int startRow = dimension.Start.Row;
                        int endRow = dimension.End.Row;
                        int startCol = dimension.Start.Column;
                        int endCol = dimension.End.Column;

                        for(int row = startRow; row <= endRow; row++) {
                            var rowData = new List<string>();
                            for(int col = startCol; col <= endCol; col++) {
                                // 读取单元格文本，空单元格转空字符串，去除首尾空格
                                string cellValue = worksheet.Cells[row, col].Text?.Trim() ?? string.Empty;
                                rowData.Add(cellValue);
                            }

                            // 跳过全空的行（原有逻辑不变）
                            if(rowData.Count > 0 && !rowData.TrueForAll(string.IsNullOrEmpty)) {
                                excelData.Add(rowData);
                            }
                        }
                    }
                }
            }
            catch(System.Exception ex) {
                // 异常兜底：打印详细信息并抛出，便于上层处理（原有逻辑不变）
                Console.WriteLine($"读取Excel数据失败：{ex.Message}");
                throw;
            }
            return excelData;
        }


        /// <summary>
        /// 从外部 DWG 文件中，根据块名获取块定义（后台读取，不打开界面）
        /// </summary>
        /// <param name="dwgFilePath">DWG 文件完整路径</param>
        /// <param name="blockName">要读取的块名称</param>
        /// <returns>返回块定义 BlockTableRecord，不存在则返回 null</returns>
        public static BlockTableRecord GetBlockTableRecordFromDwg(string dwgFilePath, string blockName)
        {
            // 1. 校验文件
            if(!File.Exists(dwgFilePath)) {
                return null;
            }

            if(string.IsNullOrEmpty(blockName)) {
                return null;
            }

            // 2. 创建一个新的后台数据库
            using(Database db = new Database(false, true)) {
                try {
                    // 3. 读取 DWG 文件到数据库（后台静默读取）
                    db.ReadDwgFile(dwgFilePath, FileShare.Read, true, string.Empty);

                    // 4. 开启事务
                    using(Transaction trans = db.TransactionManager.StartTransaction()) {
                        // 5. 打开块表
                        BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                        // 6. 判断块是否存在
                        if(bt != null && bt.Has(blockName)) {
                            // 7. 获取块定义并返回
                            BlockTableRecord btr = trans.GetObject(bt[blockName], OpenMode.ForRead) as BlockTableRecord;
                            trans.Commit();
                            return btr;
                        }

                        trans.Commit();
                    }
                }
                catch {
                    // 读取失败返回 null
                    return null;
                }
            }

            return null;
        }


        // ------------------------文件写入操作-----------------------------------------------------------//


        /// <summary>
        /// 将二维数组按顺序写入excel默认表格sheet1
        /// </summary>
        /// <param name="tableData"></param>
        /// <param name="filePath"></param>
        public static void WriteToExcel(List<List<string>> tableData, string filePath)
        {
            // 内部获取编辑器
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 校验
            if(tableData == null || tableData.Count == 0) {
                ed.WriteMessage("\n 写入失败：数据不能为空！");
                return;
            }
            if(string.IsNullOrWhiteSpace(filePath)) {
                ed.WriteMessage("\n 写入失败：文件路径不能为空！");
                return;
            }

            try {
                ExcelPackage.License.SetNonCommercialPersonal("kkddyz_autocad2022_plugin");

                // 管文件夹，不存在就创建
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using(ExcelPackage package = new ExcelPackage(filePath)) {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets["Sheet1"];
                    if(worksheet == null) {
                        // 如果Sheet1不存在，则新建一个名为"Sheet1"的工作表
                        worksheet = package.Workbook.Worksheets.Add("Sheet1");
                    }

                    // 清空当前工作表中所有单元格的内容和格式
                    worksheet.Cells.Clear();

                    // ==========================================
                    // 核心：按 行+列 写入Excel（二维列表对应）
                    // ==========================================
                    for(int rowIndex = 0; rowIndex < tableData.Count; rowIndex++) {
                        var rowData = tableData[rowIndex];
                        for(int colIndex = 0; colIndex < rowData.Count; colIndex++) {
                            // Excel 行号/列号从 1 开始
                            worksheet.Cells[rowIndex + 1, colIndex + 1].Value = rowData[colIndex];
                        }
                    }

                    // 管Excel 文件，不存在就自动创建
                    package.Save();
                }

                ed.WriteMessage($"\n Excel 保存成功：{filePath}");

                // 自动打开文件
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch(Autodesk.AutoCAD.Runtime.Exception ex) {
                ed.WriteMessage($"\n 写入失败：{ex.Message}");
            }
        }

    }

}