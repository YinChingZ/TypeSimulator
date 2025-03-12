using System;
using System.Windows.Forms;

namespace TypeSimulator.Models
{
    /// <summary>
    /// 提供按键映射功能
    /// </summary>
    /// 
    public class MappingStateChangedEventArgs : EventArgs
    {
        public bool IsEnabled { get; }
        public bool IsPaused { get; }

        public MappingStateChangedEventArgs(bool isEnabled, bool isPaused)
        {
            IsEnabled = isEnabled;
            IsPaused = isPaused;
        }
    }

    public class KeyMapper
    {
        private char[] _mappableChars;
        private int _currentIndex;
        private bool _isEnabled;
        private bool _isPaused;  // 新增暂停状态
        private bool _temporarilyDisabled;

        // 当所有字符都被映射后触发此事件
        public event EventHandler MappingCompleted;

        // 新增事件
        public event EventHandler<MappingStateChangedEventArgs> MappingStateChanged;

        public KeyMapper()
        {
            _mappableChars = new char[0];
            _currentIndex = 0;
            _isEnabled = false;
            _temporarilyDisabled = false; // 初始化新标志
            _isPaused = false;  // 初始化暂停状态
        }

        public bool IsEnabled => _isEnabled;
        public bool IsPaused => _isPaused;

        public void SetMappableCharacters(char[] characters)
        {
            if (characters == null || characters.Length == 0)
            {
                _mappableChars = new char[0];
            }
            else
            {
                _mappableChars = (char[])characters.Clone();
            }

            _currentIndex = 0;
        }

        public void SetEnabled(bool enabled)
        {
            bool oldState = _isEnabled;
            _isEnabled = enabled;

            // 如果完全禁用，则取消暂停状态
            if (!enabled)
            {
                _isPaused = false;
            }

            if (oldState != _isEnabled)
            {
                OnMappingStateChanged(new MappingStateChangedEventArgs(
                    _isEnabled, _isPaused
                ));
            }
        }

        public void TogglePaused()
        {
            if (_isEnabled) // 只有在启用状态下才能切换暂停
            {
                _isPaused = !_isPaused;

                OnMappingStateChanged(new MappingStateChangedEventArgs(
                    _isEnabled, _isPaused
                ));
            }
        }

        public void UpdateModifierState()
        {
            // 检查是否有任何修饰键被按下
            bool modifierKeyPressed =
                (Control.ModifierKeys & Keys.Control) == Keys.Control ||
                (Control.ModifierKeys & Keys.Alt) == Keys.Alt ||
                (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            _temporarilyDisabled = modifierKeyPressed;
        }

        public char? MapKey(Keys key)
        {
            if (!_isEnabled || _isPaused || _mappableChars.Length == 0 || _temporarilyDisabled)
            {
                return null;
            }

            // 添加这行代码进行按键过滤
            if (!IsMappableKey(key))
            {
                return null; // 不可映射的键直接返回null
            }

            // 顺序映射模式
            if (_currentIndex >= _mappableChars.Length)
            {
                OnMappingCompleted();
                return null;
            }

            char mappedChar = _mappableChars[_currentIndex];
            _currentIndex++;

            // 检查是否所有字符都已被映射
            if (_currentIndex >= _mappableChars.Length)
            {
                OnMappingCompleted();
            }

            return mappedChar;
        }

        public void Reset()
        {
            _currentIndex = 0;
        }

        // 新增方法：判断是否为可映射的按键
        private bool IsMappableKey(Keys key)
        {
            // 获取键的基本代码（排除修饰符）
            Keys baseKey = key & Keys.KeyCode;

            // 检查是否为控制键、功能键或特殊键
            if (IsControlKey(baseKey) || IsFunctionKey(baseKey) || IsNavigationKey(baseKey))
            {
                return false;
            }

            // 允许字母、数字和符号键触发映射
            return true;
        }

        // 检查是否为控制键
        private bool IsControlKey(Keys key)
        {
            // 控制键列表
            Keys[] controlKeys = new Keys[]
            {
                Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
                Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey,
                Keys.Menu, Keys.LMenu, Keys.RMenu, // Alt键
                Keys.LWin, Keys.RWin, // Windows键
                Keys.Apps, // 应用程序键
                Keys.Capital, // Caps Lock
                Keys.NumLock,
                Keys.Scroll // Scroll Lock
            };

            return Array.IndexOf(controlKeys, key) >= 0;
        }

        // 检查是否为功能键
        private bool IsFunctionKey(Keys key)
        {
            return key >= Keys.F1 && key <= Keys.F24;
        }

        // 检查是否为导航键
        private bool IsNavigationKey(Keys key)
        {
            // 导航键列表
            Keys[] navKeys = new Keys[]
            {
                Keys.Up, Keys.Down, Keys.Left, Keys.Right,
                Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown,
                Keys.Insert, Keys.Delete,
                Keys.Escape, Keys.Tab,
                Keys.PrintScreen, Keys.Pause
            };

            return Array.IndexOf(navKeys, key) >= 0;
        }

        protected virtual void OnMappingStateChanged(MappingStateChangedEventArgs e)
        {
            MappingStateChanged?.Invoke(this, e);
        }

        protected virtual void OnMappingCompleted()
        {
            MappingCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}

