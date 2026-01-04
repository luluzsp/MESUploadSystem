using System.Collections.Generic;

namespace MESUploadSystem.Models
{
    public class MesBaseResponse
    {
        public string RESULT { get; set; }
        public string MESSAGE { get; set; }
        public int? CODE { get; set; }
    }

    public class MesSnQueryResponse : MesBaseResponse
    {
        public MesSnData DATA { get; set; }
    }

    public class MesSnData
    {
        public long id { get; set; }
        public string sn { get; set; }
        public string shoporder { get; set; }
        public string processName { get; set; }
        public string routeName { get; set; }
        public string status { get; set; }
    }

    public class MesBomResponse : MesBaseResponse
    {
        public MesBomData DATA { get; set; }
    }

    public class MesBomData
    {
        public MesBom bom { get; set; }
        public List<MesBomItem> bomItems { get; set; }
    }

    public class MesBom
    {
        public string name { get; set; }
        public string version { get; set; }
    }

    public class MesBomItem
    {
        public string rawMatNo { get; set; }
        public string rawMatVersion { get; set; }
        public string processName { get; set; }
        public int qty { get; set; }
    }

    // 设备绑定工单查询响应
    public class MesShoporderResponse : MesBaseResponse
    {
        public MesShoporderData DATA { get; set; }
    }

    public class MesShoporderData
    {
        public long id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string lineName { get; set; }
        public string processName { get; set; }
        public string shoporder { get; set; }
    }

    // 产品收录响应
    public class MesRecruitResponse : MesBaseResponse
    {
    }

    // ASSEMBLY_COMP_SN 接口响应
    public class MesAssemblyResponse : MesBaseResponse
    {
        /// <summary>
        /// 原始响应文本
        /// </summary>
        public string RawResponse { get; set; }
    }
}