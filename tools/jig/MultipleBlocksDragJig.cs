using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using System.Collections.Generic;


namespace AutoCAD_2022_Plugin_Demo.tools.jig
{

    public class MultipleBlocksDragJig : DrawJig
    {

        private Point3d _currentPosition;
        private Point3d _basePosition; // 记录拖拽开始时的基准点
        private List<BlockReference> _blockRefs;
        private List<Vector3d> _offsets;

        // 公开最终插入点
        public Point3d FinalPosition { get; private set; }

        // 修复1：继承 DrawJig，并使用无参构造函数
        public MultipleBlocksDragJig(List<BlockReference> blockRefs, Point3d startPosition)
        {
            _blockRefs = blockRefs;
            _basePosition = startPosition;
            _currentPosition = startPosition;

            _offsets = new List<Vector3d>();
            foreach(var br in _blockRefs) {
                // 计算每个块参照相对于起始点的偏移量
                _offsets.Add(br.Position - startPosition);
            }
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions opts = new JigPromptPointOptions("\n指定插入点:");
            opts.Cursor = CursorType.Crosshair;
            opts.UseBasePoint = true;
            opts.BasePoint = _basePosition;

            PromptPointResult result = prompts.AcquirePoint(opts);

            if(result.Status == PromptStatus.OK) {
                Point3d newPos = result.Value;

                // 修复2：使用 IsEqualTo 判断位置是否真正改变，减少不必要的重绘闪烁
                if(_currentPosition.IsEqualTo(newPos)) {
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

        // DrawJig 不需要重写 Update() 方法，逻辑直接在 WorldDraw 中处理
        // protected override bool Update() { ... } 

        protected override bool WorldDraw(WorldDraw draw)
        {
            if(_blockRefs == null || _blockRefs.Count == 0) {
                return false;
            }

            // 把当前鼠标位置赋值给 FinalPosition 
            FinalPosition = _currentPosition;

            // 计算从基准点到当前鼠标位置的位移向量
            Vector3d displacement = _currentPosition - _basePosition;
            Matrix3d mat = Matrix3d.Displacement(displacement);

            WorldGeometry geo = draw.Geometry;
            if(geo != null) {
                // 修复3：使用矩阵变换来预览多个实体的移动
                // PushModelTransform 会将变换矩阵压入堆栈，后续的 Draw 都会受此矩阵影响
                geo.PushModelTransform(mat);

                foreach(var br in _blockRefs) {
                    geo.Draw(br);
                }

                // 绘制完成后弹出矩阵，恢复坐标系，避免影响其他图形
                geo.PopModelTransform();
            }

            return true;
        }


        // ==============================================
        // 👇 【新增：关键方法】真正更新块的坐标到鼠标位置
        // ==============================================
        public void UpdateBlockPositions()
        {
            Vector3d moveVector = FinalPosition - _basePosition;

            for(int i = 0; i < _blockRefs.Count; i++) {
                _blockRefs[i].Position = _blockRefs[i].Position + moveVector;
            }
        }

    }

}
