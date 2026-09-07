using System;
using System.Diagnostics;
using System.Windows.Threading;
using TypeSimulator.Utilities;

namespace TypeSimulator.Models
{
    public class TypeSimulatorEngine
    {
        public const int MinimumSpeed = 10;
        public const int MaximumSpeed = 6000;
        private const int MaxCharactersPerTick = 4;
        private string _textToType = string.Empty;
        private int _typingSpeed;
        private bool _enableRandomDelay;
        private int _startDelaySeconds;
        private int _currentPosition;
        private double _nextCharacterAt;
        private readonly DispatcherTimer _typingTimer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly Action<char> _typeCharacter;

        public bool IsTyping { get; private set; }
        public bool IsPaused { get; private set; }
        public int CharactersTyped => _currentPosition;
        public int TotalCharacters => _textToType.Length;
        public int RemainingDelaySeconds => IsTyping && _currentPosition == 0
            ? Math.Max(0, (int)Math.Ceiling((_nextCharacterAt - _clock.Elapsed.TotalMilliseconds) / 1000)) : 0;

        public event EventHandler<char> CharacterTyped;
        public event EventHandler TypingCompleted;
        public event EventHandler<int> StartDelayChanged;
        public event EventHandler<Exception> TypingFailed;

        public TypeSimulatorEngine() : this(KeyboardSimulator.TypeCharacter) { }

        public TypeSimulatorEngine(Action<char> typeCharacter)
        {
            _typeCharacter = typeCharacter ?? throw new ArgumentNullException(nameof(typeCharacter));
            _typingTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _typingTimer.Interval = TimeSpan.FromMilliseconds(10);
            _typingTimer.Tick += TypeNextCharacter;
        }

        public void Configure(string text, int speed, bool enableRandomDelay, int startDelaySeconds = 0)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (speed < MinimumSpeed || speed > MaximumSpeed)
                throw new ArgumentOutOfRangeException(nameof(speed), $"速度范围为 {MinimumSpeed}–{MaximumSpeed} CPM。");
            if (startDelaySeconds < 0 || startDelaySeconds > 60)
                throw new ArgumentOutOfRangeException(nameof(startDelaySeconds));
            if (IsTyping) throw new InvalidOperationException("请先停止当前打字任务。");
            _textToType = text.Replace("\r\n", "\n").Replace('\r', '\n');
            _typingSpeed = speed;
            _enableRandomDelay = enableRandomDelay;
            _startDelaySeconds = startDelaySeconds;
            _currentPosition = 0;
        }

        public void Start()
        {
            if (IsTyping) return;
            if (_textToType.Length == 0) throw new InvalidOperationException("没有可打字的文本");
            _currentPosition = 0;
            _nextCharacterAt = _startDelaySeconds * 1000.0;
            IsTyping = true;
            IsPaused = false;
            _clock.Restart();
            _typingTimer.Start();
            StartDelayChanged?.Invoke(this, RemainingDelaySeconds);
        }

        public void Pause()
        {
            if (!IsTyping || IsPaused) return;
            _typingTimer.Stop();
            _clock.Stop();
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsTyping || !IsPaused) return;
            IsPaused = false;
            _clock.Start();
            _typingTimer.Start();
        }

        public void Stop()
        {
            _typingTimer.Stop();
            _clock.Stop();
            IsTyping = false;
            IsPaused = false;
        }

        private void TypeNextCharacter(object sender, EventArgs e)
        {
            if (!IsTyping || IsPaused) return;
            try
            {
                if (_currentPosition == 0) StartDelayChanged?.Invoke(this, RemainingDelaySeconds);
                // Windows timer ticks can be coarser than a character interval. Drain a
                // bounded number of due characters without making the rate tick-dependent.
                double now = _clock.Elapsed.TotalMilliseconds;
                int emitted = 0;
                while (IsTyping && !IsPaused && now >= _nextCharacterAt && emitted < MaxCharactersPerTick)
                {
                    char character = _textToType[_currentPosition];
                    _typeCharacter(character);
                    _currentPosition++;
                    emitted++;
                    double interval = 60000.0 / _typingSpeed;
                    if (_enableRandomDelay)
                        interval = RandomDelay.CalculateSpecialCharDelay(character, Math.Max(1, (int)interval));
                    _nextCharacterAt += interval;
                    CharacterTyped?.Invoke(this, character);
                    if (!IsTyping) return;
                    if (_currentPosition == _textToType.Length)
                    {
                        Stop();
                        TypingCompleted?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                }
                // A long UI stall must not produce an unbounded burst. Keep every character,
                // but forgive overdue time once the per-tick budget is exhausted.
                if (emitted == MaxCharactersPerTick && now >= _nextCharacterAt)
                    _nextCharacterAt = now + 60000.0 / _typingSpeed;
            }
            catch (Exception ex)
            {
                Pause();
                TypingFailed?.Invoke(this, ex);
            }
        }
    }
}
