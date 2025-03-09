using System;
using System.Windows.Forms;

namespace TypeSimulator.Models
{
    /// <summary>
    /// 提供按键映射功能
    /// </summary>
    public class KeyMapper
    {
        private char[] _mappableChars;
        private int _currentIndex;
        private bool _isEnabled;

        // 当所有字符都被映射后触发此事件
        public event EventHandler MappingCompleted;

        public KeyMapper()
        {
            _mappableChars = new char[0];
            _currentIndex = 0;
            _isEnabled = false;
        }

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
            _isEnabled = enabled;
            _currentIndex = 0;
        }

        // 移除SetMappingMode方法，不再需要

        public char? MapKey(Keys key)
        {
            if (!_isEnabled || _mappableChars.Length == 0)
            {
                return null;
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

        protected virtual void OnMappingCompleted()
        {
            MappingCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}