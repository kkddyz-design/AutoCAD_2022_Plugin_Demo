using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.tools
{

    /// <summary>
    /// AutoCAD 选择集工具类 功能：框选/点选获取实体，支持按类型过滤
    /// </summary>
    public static class SelectionHelper
    {

        /// <summary>
        /// 【通用】用户框选，返回所有选中的 DBObject
        /// </summary>
        public static List<DBObject> GetSelectedEntities(Editor ed)
        {
            var result = new List<DBObject>();

            // 框选设置
            var pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\n请框选需要处理的实体：";

            // 获取选择集
            PromptSelectionResult psr = ed.GetSelection(pso);
            if(psr.Status != PromptStatus.OK) {
                return result;
            }

            // 遍历选择集
            using(Transaction tr = ed.Document.Database.TransactionManager.StartTransaction()) {
                foreach(SelectedObject selObj in psr.Value) {
                    if(selObj == null || !selObj.ObjectId.IsValid) {
                        continue;
                    }

                    // 打开实体（只读模式）
                    DBObject obj = selObj.ObjectId.GetObject(OpenMode.ForRead);
                    if(obj != null) {
                        result.Add(obj);
                    }
                }
                tr.Commit();
            }

            return result;
        }


        /// <summary>
        /// 从选择的实体列表中，筛选出DBText、竖直线段、水平线段，分别存入对应ref容器
        /// </summary>
        /// <param name="entities">待筛选的实体列表（从GetSelectedEntities方法获取）</param>
        /// <param name="dbTexts">ref修饰，用于存储筛选出的DBText对象</param>
        /// <param name="verticalLines">ref修饰，用于存储筛选出的竖直线段（Line）</param>
        /// <param name="horizontalLines">ref修饰，用于存储筛选出的水平线段（Line）</param>
        public static void FilterEntitiesToContainers(List<DBObject> entities,
                                                     ref List<DBText> dbTexts,
                                                     ref List<Line> verticalLines,
                                                     ref List<Line> horizontalLines)
        {
            // 校验入参，避免空引用异常（若外部未初始化，直接初始化空列表）
            if(entities == null || entities.Count == 0) {
                dbTexts ??= new List<DBText>();
                verticalLines ??= new List<Line>();
                horizontalLines ??= new List<Line>();
                return;
            }

            // 初始化ref参数（防止外部传入null，导致添加元素时报错）
            dbTexts ??= new List<DBText>();
            verticalLines ??= new List<Line>();
            horizontalLines ??= new List<Line>();

            // 遍历所有实体，按类型筛选并分类存入对应容器
            foreach(var dbObj in entities) {
                // 跳过空实体，避免异常
                if(dbObj == null || !dbObj.ObjectId.IsValid) {
                    continue;
                }

                // 筛选DBText对象，添加到dbTexts列表
                if(dbObj is DBText dbText) {
                    dbTexts.Add(dbText);
                }

                // 筛选Line对象，进一步区分竖直线和水平线段
                else if(dbObj is Line line) {
                    // 判定规则：X坐标相同 → 竖直线；Y坐标相同 → 水平线（容差1e-6，处理CAD浮点误差）
                    double tolerance = 1e-6;

                    // 竖直线：起点和终点X坐标差值极小（视为相等）
                    if(Math.Abs(line.StartPoint.X - line.EndPoint.X) < tolerance) {
                        verticalLines.Add(line);
                    }

                    // 水平线：起点和终点Y坐标差值极小（视为相等）
                    else if(Math.Abs(line.StartPoint.Y - line.EndPoint.Y) < tolerance) {
                        horizontalLines.Add(line);
                    }

                    // 非水平、非垂直的线段，不存入任何容器（可根据需求调整）
                }
            }
        }

        /// <summary>
        /// 根据筛选出的水平、竖直线段，构建表格所有点的二维坐标数组（处理同一直线多线段，去重相同坐标）
        /// </summary>
        /// <param name="horizontalLines">筛选出的水平线段列表（可能包含同一直线的多个线段）</param>
        /// <param name="verticalLines">筛选出的竖直线段列表（可能包含同一直线的多个线段）</param>
        /// <returns>二维坐标数组：[0]为表格行坐标（水平线Y值，去重排序），[1]为表格列坐标（竖直线X值，去重排序）</returns>
        public static double[][] BuildTableAllPointsCoordinate(List<Line> horizontalLines, List<Line> verticalLines)
        {
            // 1. 初始化坐标存储集合，用于自动去重（结合容差处理，避免浮点误差导致的重复）
            // 行坐标：存储所有水平线的Y值（同一直线的多个线段，仅保留一个Y值）
            List<double> rowYCoordinates = new List<double>();

            // 列坐标：存储所有竖直线的X值（同一直线的多个线段，仅保留一个X值）
            List<double> colXCoordinates = new List<double>();

            // CAD浮点坐标容差，避免微小偏差（如100.0000001与100.0000002视为同一坐标）
            double coordinateTolerance = 1e-6;

            // 2. 处理水平线段：提取Y坐标，去重（同一直线的多个线段，仅保留一个Y值）
            if(horizontalLines != null && horizontalLines.Count > 0) {
                foreach(var line in horizontalLines) {
                    // 水平线的起点和终点Y值一致，取任一Y值即可
                    double currentY = line.StartPoint.Y;

                    // 判断当前Y值是否已存在（结合容差），不存在则添加，实现去重
                    /* Any函数 拆解说明
                     * 调用者：rowYCoordinates（存储已去重的表格行 Y 坐标列表）；
                     * 传入参数：lambda 表达式（existY => Math.Abs(existY - currentY< coordinateTolerance），作为 “判断条件”；
                     * 逻辑解读：遍历 rowYCoordinates 中的每一个已存在的 Y 值（existY），判断「当前水平线的 Y 值（currentY）与该值的差值绝对值，是否小于容差（coordinateTolerance）」；
                     * 结果意义：
                     * 若存在这样的 existY → 返回 true（说明当前 Y 值重复，无需添加到列表）；
                     * 若不存在 → 返回 false（说明当前 Y 值是新的，需添加到列表完成去重）。 
                     */

                    bool isDuplicateY = rowYCoordinates.Any(existY => Math.Abs(existY - currentY) < coordinateTolerance);
                    if(!isDuplicateY) {
                        rowYCoordinates.Add(currentY);
                    }
                }
            }

            // 3. 处理竖直线段：提取X坐标，去重（同一直线的多个线段，仅保留一个X值）
            if(verticalLines != null && verticalLines.Count > 0) {
                foreach(var line in verticalLines) {
                    // 竖直线的起点和终点X值一致，取任一X值即可
                    double currentX = line.StartPoint.X;

                    // 判断当前X值是否已存在（结合容差），不存在则添加，实现去重
                    bool isDuplicateX = colXCoordinates.Any(existX => Math.Abs(existX - currentX) < coordinateTolerance);
                    if(!isDuplicateX) {
                        colXCoordinates.Add(currentX);
                    }
                }
            }

            // 4. 坐标排序：适配表格显示逻辑
            // 行坐标（Y值）：降序排列（CAD中Y值越大，位置越靠上，对应表格从上到下的行）
            double[] sortedRowCoordinates = rowYCoordinates.OrderByDescending(y => y).ToArray();

            // 列坐标（X值）：升序排列（CAD中X值越大，位置越靠右，对应表格从左到右的列）
            double[] sortedColCoordinates = colXCoordinates.OrderBy(x => x).ToArray();

            // 5. 构建二维坐标数组，返回表格所有行、列坐标（去重后）
            // 数组结构：[0] = 所有行坐标（Y），[1] = 所有列坐标（X）
            return new double[][] { sortedRowCoordinates, sortedColCoordinates };
        }


        /// <summary>
        /// 根据表格坐标网格，将DBText匹配到对应单元格 生成与CAD表格行列一致的二维列表，空单元格为null
        /// </summary>
        /// <param name="tableCoordinates">BuildTableAllPointsCoordinate返回的坐标数组 [0]行Y坐标 [1]列X坐标</param>
        /// <param name="textList">CAD中筛选出的所有DBText</param>
        /// <returns>二维文本列表（行→列，顺序与CAD一致）</returns>
        public static List<List<DBText>> MatchTextsToTableCells(double[][] tableCoordinates, List<DBText> textList)
        {
            // 1. 提取坐标 + 容错校验
            if(tableCoordinates == null || tableCoordinates.Length < 2) {
                return new List<List<DBText>>();
            }

            double[] rowYs = tableCoordinates[0];   // 行Y坐标（从上到下，降序）
            double[] colXs = tableCoordinates[1];   // 列X坐标（从左到右，升序）
            const double tolerance = 1e-6;          // CAD浮点坐标容差

            // 无表格线/无文本，直接返回空
            if(rowYs.Length < 2 || colXs.Length < 2 || textList == null || textList.Count == 0) {
                return new List<List<DBText>>();
            }

            // 2. 计算表格行列数
            int rowCount = rowYs.Length - 1;
            int colCount = colXs.Length - 1;

            // 3. 初始化二维列表：所有单元格默认为 null（空单元格）
            List<List<DBText>> resultTable = new List<List<DBText>>();
            for(int r = 0; r < rowCount; r++) {
                List<DBText> row = new List<DBText>();
                for(int c = 0; c < colCount; c++) {
                    row.Add(null); // 空单元格占位，保证列对齐
                }
                resultTable.Add(row);
            }

            // 4. 遍历所有文本，匹配到对应单元格
            foreach(DBText text in textList) {
                if(text == null) {
                    continue;
                }

                Point3d textPos = text.Position; // 文本对齐点坐标

                // ======================================
                // 匹配 行索引（Y坐标：从上到下）
                // 单元格Y范围：rowYs[r+1] < Y < rowYs[r]
                // ======================================
                int rowIndex = -1;
                for(int r = 0; r < rowCount; r++) {
                    double topY = rowYs[r];
                    double bottomY = rowYs[r + 1];

                    // 判断文本Y是否在当前行的上下边界内
                    if(textPos.Y <= topY + tolerance && textPos.Y >= bottomY - tolerance) {
                        rowIndex = r;
                        break;
                    }
                }

                // ======================================
                // 匹配 列索引（X坐标：从左到右）
                // 单元格X范围：colXs[c] < X < colXs[c+1]
                // ======================================
                int colIndex = -1;
                for(int c = 0; c < colCount; c++) {
                    double leftX = colXs[c];
                    double rightX = colXs[c + 1];

                    // 判断文本X是否在当前列的左右边界内
                    if(textPos.X >= leftX - tolerance && textPos.X <= rightX + tolerance) {
                        colIndex = c;
                        break;
                    }
                }

                // ======================================
                // 匹配成功 → 填入对应单元格
                // ======================================
                if(rowIndex != -1 && colIndex != -1) {
                    resultTable[rowIndex][colIndex] = text;
                }
            }

            return resultTable;
        }

        /// <summary>
        /// 【固定为 DBText】框选获取所有单行文字
        /// </summary>
        public static List<DBText> GetSelectedTexts(Editor ed)
        {
            List<DBText> textList = new List<DBText>();

            // 过滤：只选文字
            SelectionFilter filter = new SelectionFilter(
                new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "TEXT")
            });

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\n请框选【文字】实体：";

            PromptSelectionResult psr = ed.GetSelection(pso, filter);
            if(psr.Status != PromptStatus.OK) {
                return textList;
            }

            // ==========================================
            // 第一步：先把所有文字 全部读出来（事务内）
            // ==========================================
            using(Transaction tr = ed.Document.Database.TransactionManager.StartTransaction()) {
                foreach(SelectedObject selObj in psr.Value) {
                    if(!selObj.ObjectId.IsValid) {
                        continue;
                    }

                    DBText text = selObj.ObjectId.GetObject(OpenMode.ForRead) as DBText;
                    if(text != null) {
                        textList.Add(text);
                    }
                }
                tr.Commit();
            }

            return textList;

            // ==========================================
            // 第二步：全部读完后 → 统一排序（正确位置！）
            // ==========================================
            // List<DBText> sortedList = textList
            // .OrderByDescending(t => t.Position.Y)  // 先按 Y 降序（上→下）
            // .ThenBy(t => t.Position.X)             // 再按 X 升序（左→右）
            // .ToList();

            // return sortedList;
        }


        /// <summary>
        /// 对DBText按表格行列排序，返回二维列表(从上到下，从左到右)只适用于没有空内容的表格，否则会导致所在列不正确
        /// </summary>
        public static List<List<DBText>> SortTextsByPosition(List<DBText> textList)
        {
            // 空数据返回空二维列表
            if(textList == null || textList.Count == 0) {
                return new List<List<DBText>>();
            }

            // 同一行容差（Y坐标差距 <100 视为同一行，可根据你的图纸调整）
            double rowTolerance = textList.First().Height;

            // 核心逻辑：分组 → 排序行 → 排序列内 → 转二维列表
            var twoDimensionalList = textList
                // 按Y坐标分组（识别每一行）
                .GroupBy(t => Math.Round(t.Position.Y / rowTolerance) * rowTolerance)

                // 行从上到下（Y大的排前面）
                .OrderByDescending(g => g.Key)

                // 每行内部：从左到右排序
                .Select(rowGroup => rowGroup.OrderBy(t => t.Position.X).ToList())

                // 转二维列表
                .ToList();

            return twoDimensionalList;
        }

    }

}