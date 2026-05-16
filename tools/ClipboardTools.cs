using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Windows.Forms;


namespace AutoCAD_2022_Plugin_Demo.tools
{

    /// <summary>
    /// 系统剪贴板工具类,用于在  内存 -- Clipboard -- Excel之间进行数据交互
    /// </summary>
    public static class ClipboardTools
    {

        /// <summary>
        /// 将二维字符串列表复制到剪贴板，粘贴到Excel自动分列分行 空值 / 空字符串 自动转为 ""
        /// </summary>
        /// <param name="table">二维字符串列表</param>
        public static void CopyToExcelClipboard(List<List<string>> table)
        {
            if(table == null) {
                throw new ArgumentNullException(nameof(table), "二维列表不能为null");
            }

            try {
                string excelText = ConvertToExcelFormat(table);
                SetTextSafe(excelText);
            }
            catch(Exception ex) {
                throw new InvalidOperationException("复制到Excel剪贴板失败", ex);
            }
        }

        /// <summary>
        /// 从剪贴板读取 Excel 复制的数据，自动按 \t 分列、\r\n 分行，转为二维字符串列表
        /// </summary>
        /// <returns>二维字符串列表 List&lt;List&lt;string&gt;&gt;</returns>
        public static List<List<string>> ReadFromExcelClipboard()
        {
            try {
                // 1. STA线程安全读取剪贴板文本
                string clipboardText = GetTextSafe();
                if(string.IsNullOrWhiteSpace(clipboardText)) {
                    return new List<List<string>>();
                }

                // 2. 解析为二维列表
                return ParseExcelFormatText(clipboardText);
            }
            catch(Exception ex) {
                throw new InvalidOperationException("从剪贴板读取Excel格式数据失败", ex);
            }
        }


        #region 私有方法
        /// <summary>
        /// CAD兼容的剪贴板文本设置（STA线程）
        /// </summary>
        private static void SetTextSafe(string text)
        {
            if(string.IsNullOrWhiteSpace(text)) {
                throw new ArgumentNullException(nameof(text), "剪贴板写入失败：没有可复制的有效内容！");
            }

            Thread staThread = new Thread(() =>
                {
                    Clipboard.SetText(text);
                });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }


        /// <summary>
        /// 转换为Excel识别的剪贴板格式：\t分列，\r\n分行，空值→""
        /// </summary>
        private static string ConvertToExcelFormat(List<List<string>> table)
        {
            StringBuilder sb = new StringBuilder();

            foreach(var row in table) {
                var validRow = row ?? new List<string>();
                List<string> cells = new List<string>();

                foreach(var cell in validRow) {
                    // 核心：null / 空字符串 都转为 ""
                    cells.Add(string.IsNullOrEmpty(cell) ? "\"\"" : cell);
                }

                sb.AppendLine(string.Join("\t", cells));
            }

            return sb.ToString();
        }


        /// <summary>
        /// 【新增】CAD兼容的剪贴板文本读取（STA线程）
        /// </summary>
        /// <returns>剪贴板文本</returns>
        private static string GetTextSafe()
        {
            string result = string.Empty;
            Thread staThread = new Thread(() =>
                {
                    if(Clipboard.ContainsText()) {
                        result = Clipboard.GetText();
                    }
                });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
            return result;
        }

        /// <summary>
        /// 【新增】解析Excel格式的文本（\t分列，\r\n分行）→ 二维List
        /// </summary>
        private static List<List<string>> ParseExcelFormatText(string text)
        {
            List<List<string>> table = new List<List<string>>();
            if(string.IsNullOrWhiteSpace(text)) {
                return table;
            }

            // 按行拆分（兼容 \r\n、\n、\r 换行符）
            string[] rows = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            foreach(string row in rows) {
                // 跳过空行（避免Excel末尾空行产生无效数据）
                if(string.IsNullOrEmpty(row)) {
                    continue;
                }

                // 按制表符 \t 拆分单元格
                string[] cells = row.Split('\t');

                // 处理每个单元格：去除Excel自动加的引号，空值还原
                List<string> cellList = new List<string>();
                foreach(string cell in cells) {
                    string cleanCell = cell;

                    // 还原：把 "" 转为空字符串（与CopyToExcelClipboard对应）
                    if(cleanCell == "\"\"") {
                        cleanCell = string.Empty;
                    }
                    cellList.Add(cleanCell);
                }

                table.Add(cellList);
            }

            return table;
        }

    }
    #endregion

}
