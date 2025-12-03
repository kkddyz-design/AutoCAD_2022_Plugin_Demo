using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.interactionDemo
{

    /// <summary>
    /// 这个类用于扩展PromptGetPoiont这些方法
    /// </summary>
    public  static class PromptTools
    {

        /// <summary>
        ///
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="promptStr">命令提示词：例如"请选择点:[确认(A)/取消(B)]"</param>
        /// <param name="pointBase">基点,不接收null 用于在在绘制直线时确认上一个点的位置</param>
        /// <param name="keyWord">例如："A,B"</param>
        /// <returns></returns>
        public static PromptPointResult GetPoint(this Editor editor, string promptStr, Point3d pointBase, params string[] keywords)
        {
            if(editor == null) {
                throw new ArgumentNullException("editor为空");
            }

            promptStr = promptStr ?? string.Empty;

            // if(promptStr == null) {
            // // 使用""创建options对象
            // promptStr = string.Empty;
            // }

            // pointBase 合法性检查
            if(double.IsNaN(pointBase.X) || double.IsNaN(pointBase.Y) || double.IsNaN(pointBase.Z)) {
                throw new ArgumentException("基准点坐标不能包含非数字（NaN）", nameof(pointBase));
            }
            if(double.IsInfinity(pointBase.X) || double.IsInfinity(pointBase.Y) || double.IsInfinity(pointBase.Z)) {
                throw new ArgumentException("基准点坐标不能是无限大", nameof(pointBase));
            }

            if(!(pointBase.Z == 0)) {
                throw new ArgumentException("当前命令仅支持2D绘图，基准点 Z 坐标必须为 0", nameof(pointBase));
            }

            // 设置Message
            PromptPointOptions options = new PromptPointOptions(promptStr)
            {
                AppendKeywordsToMessage = false,    // 取消系统自动的关键字显示

                AllowNone = true,       // 允许空值 当回车和空格键,可以在None分支中设置默认值    

                UseBasePoint = true,
                BasePoint = pointBase,

                UseDashedLine = false
            };

            // 设置快捷键
            for(int i = 0; i < keywords.Length; i++) {
                options.Keywords.Add(keywords[i]);
            }

            // 获取用户输入
            return editor.GetPoint(options);
        }


        /// <summary>
        /// 不设置基准点
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="promptStr"></param>
        /// <param name="keywordStr"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static PromptPointResult GetPoint(this Editor editor, string promptStr, params string[] keywords)
        {
            if(editor == null) {
                throw new ArgumentNullException("editor为空");
            }

            promptStr = promptStr ?? string.Empty;

            // 设置Message
            PromptPointOptions options = new PromptPointOptions(promptStr)
            {
                AppendKeywordsToMessage = false,    // 取消系统自动的关键字显示

                AllowNone = true,       // 允许空值 当回车和空格键,可以在None分支中设置默认值    

                UseBasePoint = false,

                UseDashedLine = false
            };

            // 设置快捷键
            for(int i = 0; i < keywords.Length; i++) {
                options.Keywords.Add(keywords[i]);
            }

            // 获取用户输入
            return editor.GetPoint(options);
        }

    }

}
