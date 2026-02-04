using AutoCAD_2022_Plugin_Demo.EntityDemo.service;
using AutoCAD_2022_Plugin_Demo.EntityDemo.test;
using AutoCAD_2022_Plugin_Demo.tools;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.IO;
using System.Linq;

using WinForms = System.Windows.Forms;

[assembly: CommandClass(typeof(AddBlockDemo))]


namespace AutoCAD_2022_Plugin_Demo.EntityDemo.test
{

    public static  class AddBlockDemo
    {

        private static Database db = Application.DocumentManager.MdiActiveDocument.Database;


        [CommandMethod("WriteToTXT")]
        public static void WriteToTXT()
        {
            // Document doc = Application.DocumentManager.MdiActiveDocument;
            ////Database db = doc.Database;
            // Editor editor = doc.Editor;

            string filePath = "E:\\desktop\\";
            string fileName = "test";

            // 使用system.windows.form中的对话框
            WinForms.SaveFileDialog saveFileDialog = new WinForms.SaveFileDialog
            {
                Title = "保存图形数据",
                Filter = "文本文件(*.txt)|*.txt",   // 设置保存类型
                InitialDirectory = filePath,        // 设置保存路径
                FileName = fileName                 // 设置默认文件名
            };

            // string filename = db.Filename;           // 获取dwg文件绝对路径

            // 点击，处理返回结果
            WinForms.DialogResult dialogResult = saveFileDialog.ShowDialog();

            if(dialogResult == WinForms.DialogResult.OK) {
                // 进行文件处理   
                /*
                 * File.WriteAllLines 是 System.IO 命名空间下的静态方法，
                 * 用于一次性将字符串数组（或可枚举的字符串集合）写入指定文件，
                 * 核心特性是：若文件不存在，自动创建；若已存在，覆盖原有内容；
                 * 写入完成后自动关闭文件，无需手动释放资源；
                 * 每行字符串对应文件中的一行（自动添加换行符）
                 * 
                 */

                // 模拟读取当前dwg中的数据
                string[] contents = new string[] { "1111", "22222" };

                // 写入TXT文件
                File.WriteAllLines(saveFileDialog.FileName, contents);
            }
        }

        [CommandMethod("AddRectPlateDemo1")]
        public static void AddRectPlateDemo1()
        {
            db.AddRectPlateToModelSpace(new Point3d(100, 100, 0), 100, 200, 8, 22, string.Empty);
            db.AddRectPlateToModelSpace(new Point3d(300, 300, 0), 100, 200, 8, 22, "不锈钢");
        }


        [CommandMethod("AddRectPlate")]
        public static void AddRectPlateDemo2()
        {
            db.AddRectPlateToModelSpaceByExcel();
        }

        [CommandMethod("AddLeiBan")]
        public static void AddRibPlateByExcel()
        {
            db.AddRibPlateToModelSpaceByExcel();
        }

        [CommandMethod("OpenFileWithSheetSelect")]
        public static void TestOpenExcel()
        {
            string filePath = FileTools.OpenFileWithSheetSelect();
        }

    }

}
