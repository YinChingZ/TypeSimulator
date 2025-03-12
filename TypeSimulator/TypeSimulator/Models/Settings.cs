using System;
using System.IO;
using System.Xml.Serialization;

namespace TypeSimulator.Models
{
    /// <summary>
    /// 存储应用程序设置
    /// </summary>
    [Serializable]
    public class Settings
    {
        // 打字设置
        public int TypingSpeed { get; set; }
        public bool EnableRandomDelay { get; set; }

        // 映射设置
        public bool MappingEnabled { get; set; }

        /// <summary>
        /// 使用默认值初始化设置
        /// </summary>
        public Settings()
        {
            // 设置默认值
            TypingSpeed = 150;
            EnableRandomDelay = true;
            MappingEnabled = false;
        }

        /// <summary>
        /// 将设置保存到文件
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 序列化设置到XML文件
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                    serializer.Serialize(stream, this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置失败: {ex.Message}");
                // 继续使用内存中的设置
            }
        }

        /// <summary>
        /// 从文件加载设置
        /// </summary>
        public static Settings LoadFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    using (FileStream stream = new FileStream(filePath, FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                        return (Settings)serializer.Deserialize(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载设置失败: {ex.Message}");
            }

            // 如果加载失败，返回默认设置
            return new Settings();
        }
    }
}