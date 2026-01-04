using System;
using System.Collections.Generic;

namespace MESUploadSystem.Models
{
    [Serializable]
    public class PlcSignalItem
    {
        public string Trigger { get; set; }      // 触发时机
        public string Address { get; set; }      // PLC地址，如W511.11
        public bool Value { get; set; } = true;  // ON/OFF -> 01/00
    }

    [Serializable]
    public class PlcSignalConfig
    {
        public List<PlcSignalItem> Signals { get; set; } = new List<PlcSignalItem>();

        /// <summary>
        /// PLC响应超时时间（毫秒），默认1000ms
        /// </summary>
        public int ResponseTimeout { get; set; } = 1000;

        public static PlcSignalConfig GetDefault()
        {
            return new PlcSignalConfig
            {
                ResponseTimeout = 1000,
                Signals = new List<PlcSignalItem>
                {
                    new PlcSignalItem { Trigger = "L读取执行成功", Address = "W511.11", Value = true },
                    new PlcSignalItem { Trigger = "L读取执行失败", Address = "W511.12", Value = true },
                    new PlcSignalItem { Trigger = "R读取执行成功", Address = "W511.13", Value = true },
                    new PlcSignalItem { Trigger = "R读取执行失败", Address = "W511.14", Value = true }
                }
            };
        }

        public PlcSignalItem GetSignal(string trigger)
        {
            return Signals.Find(s => s.Trigger == trigger);
        }
    }
}