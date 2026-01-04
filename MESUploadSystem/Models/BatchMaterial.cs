using System;

namespace MESUploadSystem.Models
{
    [Serializable]
    public class BatchMaterial
    {
        public int Index { get; set; }
        public string Name => $"批次物料{Index}";
        public string Position { get; set; } = "L";        // L或R
        public string ControlUsage { get; set; } = "Y";    // Y或N
        public int? PackageCapacity { get; set; }          // 包装容量
        public double? UnitUsage { get; set; }             // 单位用量
        public double? RemainingUsage { get; set; }        // 剩余用量
        public string PackageCode { get; set; }            // 包装编号
        public bool IsLocked { get; set; } = true;         // 是否锁定
    }
}