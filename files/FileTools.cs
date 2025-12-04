using AutoCAD_2022_Plugin_Demo.EntityDemo;
using AutoCAD_2022_Plugin_Demo.files;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.IO;
using System.Linq;
using WinFroms = System.Windows.Forms;

[assembly: CommandClass(typeof(FileTools))]


namespace AutoCAD_2022_Plugin_Demo.files
{

    public static  class FileTools
    {

        public static Database db = Application.DocumentManager.MdiActiveDocument.Database;

        public struct TxtData
        {

            public Point3d position;
            public double radisu;

        }

        // 将传入的contents数组转换为struct TxtData
        public static TxtData[] TransData(string[] contents)
        {
            TxtData[] txtDatas = new TxtData[contents.Length];

            // 遍历传入的contents
            for(int i = 0; i < contents.Length; i++) {
                // 将一行数据用','分隔
                string[] args = contents[i].Split(new char[] { ',' });

                // 用args创建TxtData

                double X, Y, Z; // 解析坐标
                double.TryParse(args[0], out X);
                double.TryParse(args[1], out Y);
                double.TryParse(args[2], out Z);
                txtDatas[i].position = new Point3d(X, Y, Z);

                double R;
                double.TryParse(args[3], out R);
                txtDatas[i].radisu = R;
            }

            return txtDatas;
        }

        public static void CreateEntityByTxtData(this Database db, TxtData txtData)
        {
            Circle circle = new Circle();
            circle.Center = txtData.position;
            circle.Radius = txtData.radisu;

            db.AddEntityToModelSpace(circle);
        }

        [CommandMethod("WriteToTXT")]
        public static void WriteToTXT()
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

            // 点击，处理返回结果
            WinFroms.DialogResult dialogResult = saveFileDialog.ShowDialog();

            if(dialogResult == WinFroms.DialogResult.OK) {
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

        /// <summary>
        /// 从TXT文件中读取,传入一个转换器将contents转换成Entity[]
        /// </summary>
        /// 转换失败返回null
        [CommandMethod("ReadEntityFromTXT")]
        public static Entity[] ReadEntityFromTXT(Func<string[], Entity[]> convertor)
        {
            Entity[] entities = null;

            // 选择文件
            WinFroms.OpenFileDialog openFileDialog = new WinFroms.OpenFileDialog()
            {
                Title = "打开文件",

                Filter = "文本文件(*.txt)|*.txt",

                InitialDirectory = "E:\\desktop\\",
            };

            // 显示Form
            WinFroms.DialogResult dialogResult = openFileDialog.ShowDialog();

            if(dialogResult == WinFroms.DialogResult.OK) {
                // 读取文件数据
                string[] contents = File.ReadAllLines(openFileDialog.FileName)
                        .Where(line => !string.IsNullOrWhiteSpace(line)) // 排除空行/仅含空白字符的行
                        .ToArray();

                entities = convertor.Invoke(contents);
            }

            return entities;
        }

    }

}
