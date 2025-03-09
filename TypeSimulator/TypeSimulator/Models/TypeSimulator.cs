using System;
using System.Windows.Threading;
using TypeSimulator.Utilities;

namespace TypeSimulator.Models
{
    /// <summary>
    /// 模拟人类打字的引擎
    /// </summary>
    public class TypeSimulatorEngine
    {
        // 打字参数
        private string _textToType;
        private int _typingSpeed; // 字符/分钟
        private bool _enableRandomDelay;

        // 状态追踪
        private int _currentPosition;
        private readonly DispatcherTimer _typingTimer;
        private readonly Random _random;
        private readonly object _lockObject = new object();

        // 状态属性
        public bool IsTyping { get; private set; }
        public bool IsPaused { get; private set; }
        public int CharactersTyped => _currentPosition;
        public int TotalCharacters => _textToType?.Length ?? 0;

        // 事件
        public event EventHandler<char> CharacterTyped;
        public event EventHandler TypingCompleted;

        public TypeSimulatorEngine()
        {
            _typingTimer = new DispatcherTimer();
            _typingTimer.Tick += TypeNextCharacter;
            _random = new Random();
            _textToType = string.Empty;

            IsTyping = false;
            IsPaused = false;
        }

        /// <summary>
        /// 配置模拟打字参数
        /// </summary>
        public void Configure(string text, int speed, bool enableRandomDelay)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (speed <= 0)
                throw new ArgumentOutOfRangeException(nameof(speed), "打字速度必须大于0");

            lock (_lockObject)
            {
                _textToType = text;
                _typingSpeed = speed;
                _enableRandomDelay = enableRandomDelay;
                _currentPosition = 0;

                // 计算打字速度的基本间隔（毫秒）
                int intervalMs = CalculateTypingInterval(speed);
                _typingTimer.Interval = TimeSpan.FromMilliseconds(intervalMs);
            }
        }

        /// <summary>
        /// 开始模拟打字
        /// </summary>
        public void Start()
        {
            lock (_lockObject)
            {
                if (string.IsNullOrEmpty(_textToType))
                    throw new InvalidOperationException("没有可打字的文本");

                if (IsTyping && !IsPaused)
                    return; // 已经在运行中

                IsTyping = true;
                IsPaused = false;
                _currentPosition = 0;

                _typingTimer.Start();
            }
        }

        /// <summary>
        /// 暂停模拟打字
        /// </summary>
        public void Pause()
        {
            lock (_lockObject)
            {
                if (IsTyping && !IsPaused)
                {
                    _typingTimer.Stop();
                    IsPaused = true;
                }
            }
        }

        /// <summary>
        /// 继续模拟打字
        /// </summary>
        public void Resume()
        {
            lock (_lockObject)
            {
                if (IsTyping && IsPaused)
                {
                    _typingTimer.Start();
                    IsPaused = false;
                }
            }
        }

        /// <summary>
        /// 停止模拟打字
        /// </summary>
        public void Stop()
        {
            lock (_lockObject)
            {
                _typingTimer.Stop();
                IsTyping = false;
                IsPaused = false;
            }
        }

        /// <summary>
        /// 定时器回调，输入下一个字符
        /// </summary>
        private void TypeNextCharacter(object sender, EventArgs e)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_currentPosition >= _textToType.Length)
                    {
                        _typingTimer.Stop();
                        IsTyping = false;
                        IsPaused = false;
                        OnTypingCompleted();
                        return;
                    }

                    char charToType = _textToType[_currentPosition];

                    // 使用键盘模拟器输出字符
                    KeyboardSimulator.TypeCharacter(charToType);

                    _currentPosition++;
                    OnCharacterTyped(charToType);

                    // 计算下一个字符的延迟时间
                    if (_enableRandomDelay)
                    {
                        int baseInterval = CalculateTypingInterval(_typingSpeed);
                        int nextDelay = RandomDelay.CalculateSpecialCharDelay(charToType, baseInterval);
                        _typingTimer.Interval = TimeSpan.FromMilliseconds(nextDelay);
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误并尝试继续
                Console.WriteLine($"输入字符时出错: {ex.Message}");

                // 如果发生错误，可能需要暂停或停止
                try
                {
                    Pause(); // 出错时暂停打字
                }
                catch
                {
                    // 忽略暂停时的错误
                }
            }
        }

        /// <summary>
        /// 根据打字速度计算基本间隔时间
        /// </summary>
        private int CalculateTypingInterval(int charactersPerMinute)
        {
            // 将字符/分钟转换为毫秒/字符，确保不会除以零
            return 60000 / Math.Max(charactersPerMinute, 1);
        }

        protected virtual void OnCharacterTyped(char character)
        {
            try
            {
                CharacterTyped?.Invoke(this, character);
            }
            catch (Exception ex)
            {
                // 防止事件处理程序中的异常导致整个操作失败
                Console.WriteLine($"字符输入事件处理出错: {ex.Message}");
            }
        }

        protected virtual void OnTypingCompleted()
        {
            try
            {
                TypingCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // 防止事件处理程序中的异常导致整个操作失败
                Console.WriteLine($"打字完成事件处理出错: {ex.Message}");
            }
        }
    }
}