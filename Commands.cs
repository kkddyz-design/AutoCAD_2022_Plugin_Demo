/*
 * AutoCAD .NET 插件中定义自定义命令的标准模板
 * 它的核心作用是向AutoCAD注册一个名为 TEST 的命令，当用户在 AutoCAD 命令行输入TEST并回车时，就会执行Test方法中的代码。
 */

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AcCoreAp = Autodesk.AutoCAD.ApplicationServices.Core.Application;


/*
 * 程序集级别的特性（Attribute），是告诉 AutoCAD：“我这个插件（.dll 文件）里包含了自定义命令，请你加载它们。
 */
[assembly: CommandClass(typeof(AutoCAD_2022_Plugin_Demo.Commands))]

/*
 * 定义了一个命名空间，用于组织和隔离代码，避免与其他插件或 AutoCAD 内部代码发生命名冲突。
 */
namespace AutoCAD_2022_Plugin_Demo
{
    /*
     * 在Commands这个类中自定义命令
     */
    public class Commands
    {
        /*
         * [CommandMethod("TEST")] 是 Autodesk AutoCAD .NET API 中的一个特性（Attribute），
         * 用来将一个方法标记为可在AutoCAD命令行中直接调用的命令
         * 
         * 特性和它所修饰的成员之间不能有其他代码或成员隔开
         * 
         * 方法的签名必须是public static void 方法名()
         * 
         */
        [CommandMethod("TEST")]
        public static void Test()
        {
            var doc = AcCoreAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
        }
    }
}
