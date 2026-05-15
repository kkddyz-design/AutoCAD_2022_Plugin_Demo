using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.tools
{

    /// <summary>
    /// 专门用于解析字符串
    /// </summary>
    public static class PraseTools
    {

        /// <summary>
        /// 将字符串解析为 double，解析失败返回 null
        /// </summary>
        /// <summary>
        /// 解析底板规格字符串（如：δ6=100*200、δ10=200*300）
        /// </summary>
        /// <param name="spec">规格字符串，支持 δ/Δ 前缀、空格等</param>
        /// <param name="thick">输出：厚度（整数）</param>
        /// <param name="width">输出：宽度（double）</param>
        /// <param name="height">输出：高度（double）</param>
        /// <returns>解析是否成功</returns>
        public static bool ParsePlateSpecification(string spec, out int thick, out double width, out double height)
        {
            // 初始化输出参数
            thick = 0;
            width = 0;
            height = 0;

            if(string.IsNullOrWhiteSpace(spec)) {
                return false;
            }

            try {
                // 1. 清理字符串：去除所有无关符号（兼容 δ/Δ/空格/换行等）
                string cleanSpec = spec
                    .Replace("δ", string.Empty)
                    .Replace("Δ", string.Empty)
                    .Trim();

                // 2. 按 '=' 分割：左边是厚度，右边是尺寸
                string[] parts = cleanSpec.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if(parts.Length != 2) {
                    return false;
                }

                // 3. 解析厚度
                if(!int.TryParse(parts[0].Trim(), out thick) || thick <= 0) {
                    return false;
                }

                // 4. 解析尺寸（按 '*' 分割）
                string[] dimensions = parts[1].Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                if(dimensions.Length != 2) {
                    return false;
                }

                if(!double.TryParse(dimensions[0].Trim(), out width) || width <= 0) {
                    return false;
                }

                if(!double.TryParse(dimensions[1].Trim(), out height) || height <= 0) {
                    return false;
                }

                // 所有校验通过
                return true;
            }
            catch {
                // 捕获所有异常，解析失败返回 false
                return false;
            }
        }

    }

}
