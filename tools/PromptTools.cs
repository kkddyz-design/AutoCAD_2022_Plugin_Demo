using Autodesk.AutoCAD.EditorInput;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.tools
{

    /// <summary>
    /// 用户获取用户输入参数的工具类
    /// </summary>
    public static class PromptTools
    {

        /// <summary>
        /// 通过PromptStringOptions
        /// </summary>
        /// <returns>返回输入的块名，取消或失败返回 string.Empty</returns>
        public static string PromptWithString(this Editor ed, string prompt_message)
        {
            // 获取当前文档与编辑器

            // 设置提示词
            PromptStringOptions pso = new PromptStringOptions(prompt_message);
            pso.AllowSpaces = true;  // 允许用户在命令行输入的内容里包含空格，不会按空格自动结束输入
            PromptResult res = ed.GetString(pso);

            // 判断输入状态
            if(res.Status != PromptStatus.OK) {
                ed.WriteMessage("\n输入取消或无效。");
                return string.Empty;
            }

            // 返回去空格后的块名
            return res.StringResult.Trim();
        }

    }

}
