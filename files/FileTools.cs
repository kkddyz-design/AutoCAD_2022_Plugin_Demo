using AutoCAD_2022_Plugin_Demo.files;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;
using WinFroms = System.Windows.Forms;

[assembly: CommandClass(typeof(FileTools))]


namespace AutoCAD_2022_Plugin_Demo.files
{

    public  class FileTools
    {

        [CommandMethod("WriteToTXT")]
        public static void WriteToTXT(string FilePath)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor editor = doc.Editor;

            string filePath = "E:\\desktop\\";
            string fileName = "test";

            // 使用system.windows.form中的对话框
            WinFroms.SaveFileDialog saveFileDialog = new WinFroms.SaveFileDialog();
            saveFileDialog.Title = "保存图形数据";
            saveFileDialog.Filter = "文本文件(*.txt)|*.txt";    //  设置保存类型
            saveFileDialog.InitialDirectory = filePath; // 设置保存路径
            saveFileDialog.FileName = fileName;         // 设置默认文件名

            // string filename = db.Filename;           // 获取dwg文件绝对路径

            // 点击
            saveFileDialog.ShowDialog();
        }

    }

}
