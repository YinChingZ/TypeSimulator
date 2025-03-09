using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.ComponentModel;

namespace TypeSimulator.Utilities
{
    /// <summary>
    /// 管理全局热键的注册和处理
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly Window _window;
        private readonly IntPtr _windowHandle;
        private readonly Dictionary<int, Action> _hotkeyActions;
        private int _hotkeyId;
        private HwndSource _source;
        private bool _isDisposed = false;

        // 修饰键常量
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        // Windows消息
        private const int WM_HOTKEY = 0x0312;

        public HotkeyManager(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            _window = window;
            _windowHandle = new WindowInteropHelper(_window).EnsureHandle();
            _hotkeyActions = new Dictionary<int, Action>();
            _hotkeyId = 0;

            try
            {
                // 获取HwndSource
                _source = HwndSource.FromHwnd(_windowHandle);
                if (_source == null)
                {
                    throw new InvalidOperationException("无法获取窗口句柄源");
                }
                _source.AddHook(WndProc);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化热键管理器失败: {ex.Message}");
                throw;
            }
        }

        public bool RegisterHotKey(ModifierKeys modifiers, Keys key, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                _hotkeyId++;
                
                uint modifiersU = 0;
                
                if (modifiers.HasFlag(ModifierKeys.Alt))
                    modifiersU |= MOD_ALT;
                if (modifiers.HasFlag(ModifierKeys.Control))
                    modifiersU |= MOD_CONTROL;
                if (modifiers.HasFlag(ModifierKeys.Shift))
                    modifiersU |= MOD_SHIFT;
                if (modifiers.HasFlag(ModifierKeys.Windows))
                    modifiersU |= MOD_WIN;

                // 添加MOD_NOREPEAT防止按键重复
                modifiersU |= MOD_NOREPEAT;

                bool registered = RegisterHotKey(_windowHandle, _hotkeyId, modifiersU, (uint)key);
                
                if (!registered)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Console.WriteLine($"注册热键失败，错误码: {errorCode}");
                    return false;
                }
                
                _hotkeyActions[_hotkeyId] = action;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注册热键时出错: {ex.Message}");
                return false;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_HOTKEY)
                {
                    int id = wParam.ToInt32();
                    if (_hotkeyActions.ContainsKey(id))
                    {
                        _hotkeyActions[id]?.Invoke();
                        handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理热键消息时出错: {ex.Message}");
            }
            
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    if (_source != null)
                    {
                        _source.RemoveHook(WndProc);
                        _source = null;
                    }
                }

                // 注销所有热键
                for (int i = 1; i <= _hotkeyId; i++)
                {
                    try
                    {
                        UnregisterHotKey(_windowHandle, i);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"注销热键 {i} 失败: {ex.Message}");
                    }
                }
                
                _isDisposed = true;
            }
        }

        ~HotkeyManager()
        {
            Dispose(false);
        }
    }
}