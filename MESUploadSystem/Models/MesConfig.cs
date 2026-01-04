using System;

namespace MESUploadSystem.Models
{
    [Serializable]
    public class MesConfig
    {
        /// <summary>
        /// MES接口地址
        /// </summary>
        public string MesUrl { get; set; }

        /// <summary>
        /// MES Token
        /// </summary>
        public string MesToken { get; set; }

        /// <summary>
        /// L边工站名称
        /// </summary>
        public string LStationName { get; set; }

        /// <summary>
        /// L边设备编号
        /// </summary>
        public string LDeviceCode { get; set; }

        /// <summary>
        /// R边工站名称
        /// </summary>
        public string RStationName { get; set; }

        /// <summary>
        /// R边设备编号
        /// </summary>
        public string RDeviceCode { get; set; }

        /// <summary>
        /// 是否启用产品收录功能
        /// </summary>
        public bool EnableProductRecruit { get; set; } = false;

        /// <summary>
        /// 是否去除SN后缀（去除最后一个+号后的内容）
        /// 例如：FM71234+56AT+ANB → FM71234+56AT
        /// </summary>
        public bool RemoveSnSuffix { get; set; } = false;

        /// <summary>
        /// 是否将所有批次物料绑定到同一产品
        /// 启用后，所有批次物料将通过comp_sn1,comp_sn2,comp_sn3...参数一次性绑定
        /// </summary>
        public bool BindAllMaterialsToSameProduct { get; set; } = false;
    }
}
