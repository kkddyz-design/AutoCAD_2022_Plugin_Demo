using AutoCAD_2022_Plugin_Demo.files;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using acad_AppService = Autodesk.AutoCAD.ApplicationServices;
using WinForms = System.Windows.Forms;


[assembly: CommandClass(typeof(FileTools))]


namespace AutoCAD_2022_Plugin_Demo.files
{

    /// <summary>
    /// 这个类专门用于从数据源读取数据，仅仅封装读取的原始数据，而不包括数据格式转换等。
    /// </summary>
    public static  class FileTools
    {

        public static Database db = acad_AppService.Application.DocumentManager.MdiActiveDocument.Database;


        /// <summary>
        /// 通过OpenFileDialog选择文件
        /// </summary>
        /// <returns></returns>
        public static string OpenFile()
        {
            // 选择文件
            WinForms.OpenFileDialog openFileDialog = new WinForms.OpenFileDialog()
            {
                Title = "打开文件",

                Filter = "表格(*.xlsx)|*.xlsx|文本文件(*.txt)|*.txt",

                InitialDirectory = "E:\\desktop\\",
            };

            // 显示Form
            WinForms.DialogResult dialogResult = openFileDialog.ShowDialog();

            if(dialogResult == WinForms.DialogResult.OK) {
                return openFileDialog.FileName;
            }
            else {
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

                // 3. 打开Excel文件（using自动释放文件句柄，避免文件被占用）
                using(var excelPackage = new ExcelPackage(new FileInfo(filePath))) {
                    // 4. 获取指定名称的工作表
                    ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[sheetName];
                    if(worksheet == null) {
                        throw new InvalidOperationException($"工作表【{sheetName}】不存在于Excel文件中");
                    }

                    // 5. 获取数据范围（自动识别有数据的行/列，避免空遍历）
                    // 处理空工作表：Dimension为null表示无任何数据
                    var dimension = worksheet.Dimension;
                    if(dimension == null) {
                        Console.WriteLine($"警告：工作表【{sheetName}】无任何数据");
                        return excelData; // 返回空列表
                    }

                    // 6. 遍历所有行和列，填充数据到嵌套List
                    int startRow = dimension.Start.Row;    // 数据起始行（通常是1）
                    int endRow = dimension.End.Row;        // 数据结束行
                    int startCol = dimension.Start.Column; // 数据起始列（通常是1）
                    int endCol = dimension.End.Column;     // 数据结束列

                    for(int row = startRow; row <= endRow; row++) {
                        // 存储当前行的所有列数据
                        var rowData = new List<string>();
                        for(int col = startCol; col <= endCol; col++) {
                            // 读取单元格值：Text=格式化文本，Value=原始值（按需选择）
                            // ?? "" 处理空单元格，避免null值
                            string cellValue = worksheet.Cells[row, col].Text?.Trim() ?? string.Empty;
                            rowData.Add(cellValue);
                        }

                        // 跳过全空的行
                        if(rowData.Count > 0 && !rowData.TrueForAll(string.IsNullOrEmpty)) {
                            excelData.Add(rowData);
                        }
                    }
                }

                // Console.WriteLine($"成功读取Excel文件【{filePath}】的工作表【{sheetName}】，共{excelData.Count}行有效数据");
            }
            catch(System.Exception ex) {
                // 异常兜底：打印详细信息并抛出，便于上层处理
                Console.WriteLine($"读取Excel数据失败：{ex.Message}");
                throw; // 抛出异常让调用方感知，也可根据需求返回空列表
            }

            return excelData;
        }

    }

}
