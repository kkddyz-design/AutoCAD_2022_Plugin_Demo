using AutoCAD_2022_Plugin_Demo.interactionDemo;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;


[assembly: CommandClass(typeof(Test))]


namespace AutoCAD_2022_Plugin_Demo.interactionDemo
{

    public static class Test
    {

        public static Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        [CommandMethod("TestGetPoint1")]
        public static void TestGetPoint1()
        {
            string promptStr = "\n请选择点:[确认(A) /  取消(B)]";
            string[] keywordStr = { "A", "B" };
            Point3d point = new Point3d(100, 100, 0);
            PromptPointResult result = ed.GetPoint(promptStr, keywordStr);
            ed.WriteMessage($"Status: {result.Status},point: {result.Value.ToString()}");
        }

    }

}
