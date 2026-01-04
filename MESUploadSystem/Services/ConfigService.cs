using System;
using System.IO;
using System.Xml.Serialization;
using MESUploadSystem.Models;

namespace MESUploadSystem.Services
{
    public class ConfigService
    {
        private static readonly string ConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "config.xml");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var serializer = new XmlSerializer(typeof(AppConfig));
                    using (var reader = new StreamReader(ConfigPath))
                    {
                        return (AppConfig)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log($"加载配置失败: {ex.Message}");
            }
            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AppConfig));
                using (var writer = new StreamWriter(ConfigPath))
                {
                    serializer.Serialize(writer, config);
                }
                LogService.Log("配置保存成功");
            }
            catch (Exception ex)
            {
                LogService.Log($"保存配置失败: {ex.Message}");
            }
        }
    }
}