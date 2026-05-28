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
        /// 解析方板规格字符串（如：15合金δ6=100*200、δ10=200*300、Q235 δ8=500*600）
        /// </summary>
        /// <param name="spec">规格字符串，支持 δ/Δ 前缀、空格等</param>
        /// <param name="material">输出：材质（如果没有则为空字符串）</param>
        /// <param name="thick">输出：厚度（整数）</param>
        /// <param name="width">输出：宽度（double）</param>
        /// <param name="height">输出：高度（double）</param>
        /// <returns>解析是否成功</returns>
        public static bool ParseRectPlateSpec(string spec, out string material, out int thick, out double height, out double width)
        {
            // 初始化输出参数
            material = string.Empty;
            thick = 0;
            width = 0;
            height = 0;

            if(string.IsNullOrWhiteSpace(spec)) {
                return false;
            }

            try {
                // 1. 核心修改：提前去除字符串中所有的空格（解决 "15合金 δ6" 这种带空格的情况）
                string noSpaceSpec = spec.Replace(" ", string.Empty).Replace("　", string.Empty); // 同时处理全角空格

                // 2. 按 '=' 分割：左边包含材质和厚度，右边是尺寸
                string[] parts = noSpaceSpec.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if(parts.Length != 2) {
                    return false;
                }

                string leftPart = parts[0]; // 例如 "15合金δ6" 或 "δ10"
                string rightPart = parts[1]; // 例如 "100*200"

                // 3. 解析材质和厚度
                // 查找 δ 或 Δ 的位置
                int deltaIndex = leftPart.IndexOfAny(new char[] { 'δ', 'Δ' });

                if(deltaIndex > 0) {
                    // 如果 δ 不在第一位，说明前面有材质（例如 "15合金δ6"，deltaIndex为4）
                    material = leftPart.Substring(0, deltaIndex);

                    // 截取 δ 后面的数字作为厚度字符串
                    string thickStr = leftPart.Substring(deltaIndex + 1);
                    if(!int.TryParse(thickStr, out thick) || thick <= 0) {
                        return false;
                    }
                }
                else if(deltaIndex == 0) {
                    // δ 在第一位，说明没有材质（例如 "δ10"）
                    material = string.Empty;
                    string thickStr = leftPart.Substring(1);
                    if(!int.TryParse(thickStr, out thick) || thick <= 0) {
                        return false;
                    }
                }
                else {
                    // 没有找到 δ，说明整个左边都是材质，或者格式错误
                    // 根据你的业务需求，如果必须有厚度，这里可以返回 false
                    // 这里假设如果没有 δ，则左边整体算作材质，厚度为0或解析失败
                    material = leftPart;

                    // 如果业务强制要求必须有厚度，可以解开下面这行注释：
                    // return false; 
                }

                // 4. 解析尺寸（按 '*' 分割）
                string[] dimensions = rightPart.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                if(dimensions.Length != 2) {
                    return false;
                }

                if(!double.TryParse(dimensions[0].Trim(), out height) || height <= 0) {
                    return false;
                }

                if(!double.TryParse(dimensions[1].Trim(), out width) || width <= 0) {
                    return false;
                }

                // 确保实际绘图的H<W
                double tempH = height, tempW = width;

                // 确保 width >= height，如果 height 更大，则交换两者的值
                if(tempH > tempW) {
                    width = tempH;
                    height = tempW;
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
