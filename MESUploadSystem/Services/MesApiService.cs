using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MESUploadSystem.Models;
using Newtonsoft.Json;

namespace MESUploadSystem.Services
{
    public class MesApiService
    {
        private readonly HttpClient _client;
        private readonly MesConfig _config;

        public MesApiService(MesConfig config)
        {
            _config = config;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Token", config.MesToken);
            _client.Timeout = TimeSpan.FromSeconds(30);
        }

        private string BuildUrl(string endpoint) =>
            $"{_config.MesUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        /// <summary>
        /// 从完整URL中提取BaseUrl
        /// 例如: http://10.128.30.62:20007/metal/api/me/mesFlowControl/complete
        /// 返回: http://10.128.30.62:20007
        /// </summary>
        private string GetBaseUrl()
        {
            try
            {
                var uri = new Uri(_config.MesUrl);
                return $"{uri.Scheme}://{uri.Authority}";
            }
            catch
            {
                // 如果解析失败，尝试简单截取
                var url = _config.MesUrl.TrimEnd('/');
                var parts = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return $"{parts[0]}//{parts[1]}";
                }
                return url;
            }
        }

        // 1. 查询产品信息
        public async Task<MesSnQueryResponse> GetSnInfoAsync(string sn)
        {
            var url = BuildUrl($"/mesSn/getOneBySn?sn={Uri.EscapeDataString(sn)}");
            LogService.Info($"→ 调用接口: {url}");

            var response = await _client.PostAsync(url, null);
            var json = await response.Content.ReadAsStringAsync();
            LogService.Info($"← 响应: {json}");

            return JsonConvert.DeserializeObject<MesSnQueryResponse>(json);
        }

        // 2. 产品检查 (Start)
        public async Task<MesBaseResponse> StartAsync(string sn, string shoporder,
            string processName, string resourceCode)
        {
            var url = BuildUrl($"/mesFlowControl/start?sn={Uri.EscapeDataString(sn)}" +
                $"&shoporder={Uri.EscapeDataString(shoporder)}" +
                $"&processName={Uri.EscapeDataString(processName)}" +
                $"&resourceCode={Uri.EscapeDataString(resourceCode)}");

            LogService.Info($"→ 调用接口: {url}");
            var response = await _client.PostAsync(url, null);
            var json = await response.Content.ReadAsStringAsync();
            LogService.Info($"← 响应: {json}");

            return JsonConvert.DeserializeObject<MesBaseResponse>(json);
        }

        // 3. 获取BOM
        public async Task<MesBomResponse> GetBomAsync(string sn, string processName)
        {
            var url = BuildUrl("/mesSn/getBomBySn");
            var body = JsonConvert.SerializeObject(new { sn, processName });

            LogService.Info($"→ 调用接口: {url}, Body: {body}");
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();
            LogService.Info($"← 响应: {json}");

            return JsonConvert.DeserializeObject<MesBomResponse>(json);
        }

        // 4. 物料绑定（单个物料 - ASSEMBLY_COMP_SN 接口）
        public async Task<MesAssemblyResponse> AssemblyCompSnAsync(
            string sn,
            string stationId,
            string testStationName,
            string compSn1)
        {
            var baseUrl = GetBaseUrl();
            var url = $"{baseUrl}/fatp/exi/me/bobcat?c=ASSEMBLY_COMP_SN" +
                      $"&station_id={Uri.EscapeDataString(stationId)}" +
                      $"&sn={Uri.EscapeDataString(sn)}" +
                      $"&test_station_name={Uri.EscapeDataString(testStationName)}" +
                      $"&comp_sn1={Uri.EscapeDataString(compSn1)}";

            LogService.Info($"→ 调用接口: {url}");
            LogService.Info($"  参数: c=ASSEMBLY_COMP_SN, station_id={stationId}, sn={sn}, test_station_name={testStationName}, comp_sn1={compSn1}");

            var response = await _client.PostAsync(url, null);
            var responseText = await response.Content.ReadAsStringAsync();
            LogService.Info($"← 响应: {responseText}");

            return ParseAssemblyResponse(responseText);
        }

        /// <summary>
        /// 4.1 物料绑定（多个物料 - ASSEMBLY_COMP_SN 接口）
        /// 所有批次物料绑定到同一产品SN
        /// </summary>
        /// <param name="sn">产品SN</param>
        /// <param name="stationId">设备编号</param>
        /// <param name="testStationName">工站名称</param>
        /// <param name="compSnList">批次物料编码列表</param>
        /// <returns>绑定结果</returns>
        public async Task<MesAssemblyResponse> AssemblyMultiCompSnAsync(
            string sn,
            string stationId,
            string testStationName,
            List<string> compSnList)
        {
            if (compSnList == null || compSnList.Count == 0)
            {
                return new MesAssemblyResponse
                {
                    RESULT = "FAIL",
                    MESSAGE = "批次物料列表为空"
                };
            }

            var baseUrl = GetBaseUrl();

            // 构建URL，基础参数
            var urlBuilder = new StringBuilder();
            urlBuilder.Append($"{baseUrl}/fatp/exi/me/bobcat?c=ASSEMBLY_COMP_SN");
            urlBuilder.Append($"&station_id={Uri.EscapeDataString(stationId)}");
            urlBuilder.Append($"&sn={Uri.EscapeDataString(sn)}");
            urlBuilder.Append($"&test_station_name={Uri.EscapeDataString(testStationName)}");

            // 添加所有批次物料参数: comp_sn1, comp_sn2, comp_sn3...
            var paramLog = new StringBuilder();
            paramLog.Append($"c=ASSEMBLY_COMP_SN, station_id={stationId}, sn={sn}, test_station_name={testStationName}");

            for (int i = 0; i < compSnList.Count; i++)
            {
                string paramName = $"comp_sn{i + 1}";
                string paramValue = compSnList[i];
                urlBuilder.Append($"&{paramName}={Uri.EscapeDataString(paramValue)}");
                paramLog.Append($", {paramName}={paramValue}");
            }

            var url = urlBuilder.ToString();

            LogService.Info($"→ 调用接口: {url}");
            LogService.Info($"  参数: {paramLog}");
            LogService.Info($"  共绑定 {compSnList.Count} 个批次物料");

            var response = await _client.PostAsync(url, null);
            var responseText = await response.Content.ReadAsStringAsync();
            LogService.Info($"← 响应: {responseText}");

            return ParseAssemblyResponse(responseText);
        }

        /// <summary>
        /// 解析 ASSEMBLY_COMP_SN 响应
        /// 成功: "0 SFC_OK"
        /// 失败: "2 SFC_FATAL_ERROR 组件条码comp_sn1不符合编码规则"
        /// </summary>
        private MesAssemblyResponse ParseAssemblyResponse(string responseText)
        {
            var response = new MesAssemblyResponse
            {
                RawResponse = responseText
            };

            if (string.IsNullOrWhiteSpace(responseText))
            {
                response.RESULT = "FAIL";
                response.MESSAGE = "响应为空";
                return response;
            }

            var parts = responseText.Trim().Split(new[] { ' ' }, 3);
            if (parts.Length >= 2)
            {
                var code = parts[0].Trim();
                var status = parts[1].Trim();

                // 0 SFC_OK 表示成功
                if (code == "0" && status == "SFC_OK")
                {
                    response.RESULT = "PASS";
                    response.MESSAGE = "SFC_OK";
                }
                else
                {
                    response.RESULT = "FAIL";
                    // 如果有第三部分，作为详细错误信息
                    if (parts.Length >= 3)
                    {
                        response.MESSAGE = $"{status} {parts[2].Trim()}";
                    }
                    else
                    {
                        response.MESSAGE = status;
                    }
                }
            }
            else
            {
                response.RESULT = "FAIL";
                response.MESSAGE = responseText;
            }

            return response;
        }

        // 5. 产品完成
        public async Task<MesBaseResponse> CompleteAsync(string sn, string shoporder,
            string processName, string resourceCode)
        {
            var url = BuildUrl($"/mesFlowControl/complete?sn={Uri.EscapeDataString(sn)}" +
                $"&shoporder={Uri.EscapeDataString(shoporder)}" +
                $"&processName={Uri.EscapeDataString(processName)}" +
                $"&resourceCode={Uri.EscapeDataString(resourceCode)}");

            LogService.Info($"→ 调用接口: {url}");
            var response = await _client.PostAsync(url, null);
            var json = await response.Content.ReadAsStringAsync();
            LogService.Info($"← 响应: {json}");

            return JsonConvert.DeserializeObject<MesBaseResponse>(json);
        }

        // 6. 查询设备绑定的工单
        public async Task<MesShoporderResponse> GetSettingShoporderAsync(string processName, string resourceCode)
        {
            var url = BuildUrl($"/mesResource/getSettingShoporder?processName={Uri.EscapeDataString(processName)}&resourceCode={Uri.EscapeDataString(resourceCode)}");

            LogService.Info($"→ 调用接口: {url}");
            var response = await _client.PostAsync(url, null);
            var json = await response.Content.ReadAsStringAsync();
            LogService.Info($"← 响应: {json}");

            return JsonConvert.DeserializeObject<MesShoporderResponse>(json);
        }

        // 7. 产品收录
        public async Task<MesRecruitResponse> RecruitShoporderSnAsync(string sn, string processName, string shoporder)
        {
            var url = BuildUrl("/mesShoporderStatistic/recruitShoporderSn");
            var body = JsonConvert.SerializeObject(new
            {
                sn,
                checkSnRule = "true",
                processName,
                shoporder
            });

            LogService.Info($"→ 调用接口: {url}, Body: {body}");
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();
            LogService.Info($"← 响应: {json}");

            return JsonConvert.DeserializeObject<MesRecruitResponse>(json);
        }
    }
}
