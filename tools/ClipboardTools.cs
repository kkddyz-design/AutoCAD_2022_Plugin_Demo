using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Windows.Forms;


namespace AutoCAD_2022_Plugin_Demo.tools
{

    /// <summary>
    /// 系统剪贴板工具类（CAD专用） 包含：Excel格式数据复制、通用文本复制、空值安全处理
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
        /// CAD兼容的剪贴板文本设置（STA线程）
        /// </summary>
        public static void SetTextSafe(string text)
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

    }

}
