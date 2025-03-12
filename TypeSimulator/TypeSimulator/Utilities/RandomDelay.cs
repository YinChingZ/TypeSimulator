using System;
using System.Threading;

namespace TypeSimulator.Utilities
{
    /// <summary>
    /// 提供随机延迟计算功能，模拟人类打字节奏
    /// </summary>
    public static class RandomDelay
    {
        private static readonly Random _random = new Random();
        private static readonly object _lockObject = new object();
        
        /// <summary>
        /// 计算下一个字符的延迟时间
        /// </summary>
        /// <param name="baseDelayMs">基础延迟毫秒数</param>
        /// <returns>应用随机因素后的延迟时间（毫秒）</returns>
        public static int CalculateNextDelay(int baseDelayMs)
        {
            if (baseDelayMs <= 0)
                return 50; // 最小延迟
            
            lock (_lockObject) // 确保线程安全
            {
                try
                {
                    // 基本延迟时间的70%-130%范围内随机
                    double factor = 0.7 + (_random.NextDouble() * 0.6);
                    int delay = (int)(baseDelayMs * factor);
                    
                    // 偶尔添加较大的停顿，模拟思考
                    if (_random.Next(100) < 5) // 5%的几率
                    {
                        delay += _random.Next(300, 800);
                    }
                    
                    return Math.Max(delay, 10); // 确保最小延迟
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"计算延迟时出错: {ex.Message}");
                    return baseDelayMs; // 出错时返回基础延迟
                }
            }
        }
        
        /// <summary>
        /// 为特殊字符计算延迟时间
        /// </summary>
        public static int CalculateSpecialCharDelay(char c, int baseDelayMs)
        {
            lock (_lockObject)
            {
                try
                {
                    // 换行或特殊符号后添加额外延迟
                    if (c == '\n' || c == '.' || c == ',' || c == ';' || c == '!' || c == '?')
                    {
                        return baseDelayMs + _random.Next(100, 400);
                    }
                    
                    return CalculateNextDelay(baseDelayMs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"计算特殊字符延迟时出错: {ex.Message}");
                    return baseDelayMs; // 出错时返回基础延迟
                }
            }
        }
        
        /// <summary>
        /// 执行一个随机延迟
        /// </summary>
        public static void Execute(int baseDelayMs)
        {
            int delay = CalculateNextDelay(baseDelayMs);
            Thread.Sleep(delay);
        }
    }
}