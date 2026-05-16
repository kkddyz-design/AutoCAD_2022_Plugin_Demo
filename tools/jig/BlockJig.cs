using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;


namespace AutoCAD_2022_Plugin_Demo.tools
{

    // 继承自 EntityJig，专门用于拖拽实体
    public class BlockJig : EntityJig
    {

        private Point3d _currentPosition;
        private BlockReference _blockRef; // 缓存引用，避免重复强转

        // 【核心】构造参数直接接收 BlockReference
        public BlockJig(BlockReference blockRef, Point3d startPosition) : base(blockRef)
        {
            _blockRef = blockRef;
            _currentPosition = startPosition;
        }

        // 1. 采样：获取鼠标位置
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions opts = new JigPromptPointOptions("\n指定插入点:");

            // 设置为十字光标，不使用 BasePoint，因此没有橡皮筋虚线
            opts.Cursor = CursorType.Crosshair;

            PromptPointResult result = prompts.AcquirePoint(opts);

            if(result.Status == PromptStatus.OK) {
                Point3d newPos = result.Value;

                // 优化：只有坐标改变时才重绘，防止闪烁
                if(_currentPosition == newPos) {
                    return SamplerStatus.NoChange;
                }

                _currentPosition = newPos;
                return SamplerStatus.OK;
            }
            else if(result.Status == PromptStatus.Cancel) {
                return SamplerStatus.Cancel;
            }

            return SamplerStatus.NoChange;
        }

        // 2. 更新：根据鼠标位置更新块的位置
        protected override bool Update()
        {
            // 更新块参照的位置属性
            // EntityJig 会自动检测这个变化并重绘屏幕
            if(_blockRef != null) {
                _blockRef.Position = _currentPosition;
            }
            return true;
        }

    }

}
