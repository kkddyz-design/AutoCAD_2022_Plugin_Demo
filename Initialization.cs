/*
 *  AutoCAD .NET 插件（.dll） 的初始化逻辑，核心作用是：在插件加载时，向 AutoCAD 命令行输出一条 “插件已加载”的提示信息。
 */
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;
/*
 * AutoCAD .NET API 的核心类，封装了 AutoCAD 应用程序的所有功能（如文档管理、命令执行、事件触发等）
 */
using AcCoreAp = Autodesk.AutoCAD.ApplicationServices.Core.Application;


namespace AutoCAD_2022_Plugin_Demo
{
    /*
     * IExtensionApplication 是 AutoCAD .NET 插件的 强制接口：任何 AutoCAD 插件（.dll）都必须包含一个实现该接口的类，作为插件的 “入口点”。
     * AutoCAD 加载插件时，会自动查找并实例化这个类，然后调用其 Initialize 方法；插件卸载时，会调用 Terminate 方法。
     */

    public class Initialization : IExtensionApplication
    {

        private void OnIdle(object sender, EventArgs e)
        {
            // 获取 AutoCAD 中当前激活的绘图文档（即用户正在操作的 .dwg 文件）
            var doc = AcCoreAp.DocumentManager.MdiActiveDocument;
            if(doc != null)
            {
                // 关键步骤！Idle 事件会频繁触发（只要 AutoCAD 空闲），这里执行一次后立即解绑，避免重复输出提示。
                AcCoreAp.Idle -= OnIdle;
                doc.Editor.WriteMessage("\nAutoCAD_2022_Plugin_Demo loaded.\n");
            }
        }

        /*
         * Initialize 是 IExtensionApplication 的 加载回调方法：AutoCAD 加载插件时，会立即执行这里的代码（相当于插件的 “启动逻辑”）。
         * AcCoreAp.Idle += OnIdle：给 AutoCAD 应用程序的 Idle 事件绑定一个回调函数 OnIdle。
         */
        public void Initialize() { AcCoreAp.Idle += OnIdle; }

        public void Terminate()
        {
        }

    }

}
