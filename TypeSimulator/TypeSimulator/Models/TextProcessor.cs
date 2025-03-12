using System;
using System.Text;

namespace TypeSimulator.Models
{
    /// <summary>
    /// 处理输入文本，为模拟打字准备
    /// </summary>
    public class TextProcessor
    {
        /// <summary>
        /// 处理输入文本，执行必要的清理和格式化
        /// </summary>
        /// <param name="inputText">原始输入文本</param>
        /// <returns>处理后的文本</returns>
        public string ProcessText(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
            {
                return string.Empty;
            }

            try
            {
                // 保留原始的换行符，但规范化为统一格式
                string normalizedText = inputText.Replace("\r\n", "\n").Replace("\r", "\n");

                // 移除多余的空格（每行开头和结尾的空格）
                StringBuilder processedText = new StringBuilder();
                string[] lines = normalizedText.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    // 对每行进行处理
                    string trimmedLine = lines[i].TrimEnd();

                    processedText.Append(trimmedLine);

                    // 如果不是最后一行，添加换行符
                    if (i < lines.Length - 1)
                    {
                        processedText.Append('\n');
                    }
                }

                return processedText.ToString();
            }
            catch (Exception ex)
            {
                // 处理失败时返回原始文本，避免阻断程序运行
                Console.WriteLine($"文本处理错误: {ex.Message}");
                return inputText;
            }
        }

        /// <summary>
        /// 为按键映射准备文本
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>可用于按键映射的字符数组</returns>
        public char[] PrepareForKeyMapping(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new char[0];
            }

            try
            {
                // 移除不可见字符和控制字符，只保留可打印字符
                StringBuilder mappableChars = new StringBuilder();
                foreach (char c in text)
                {
                    // 检查是否是可打印字符
                    if (!char.IsControl(c) || c == '\n' || c == '\t')
                    {
                        mappableChars.Append(c);
                    }
                }

                return mappableChars.ToString().ToCharArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"准备映射字符错误: {ex.Message}");
                return new char[0]; // 出错时返回空数组
            }
        }
    }
}