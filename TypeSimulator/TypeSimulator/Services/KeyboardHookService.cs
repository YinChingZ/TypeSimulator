using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Input;

namespace TypeSimulator.Services
{
    public class KeyboardHookEventArgs : EventArgs
    {
        public Keys Key { get; }
        public Keys ModifierKeys { get; }
        public bool HasModifiers => ModifierKeys != Keys.None;

        public KeyboardHookEventArgs(Keys key, Keys modifierKeys)
        {
            Key = key;
            ModifierKeys = modifierKeys;
        }
    }

    /// <summary>
    /// 提供全局键盘钩子功能
    /// </summary>
    public class KeyboardHookService : IDisposable
    {
        // Win32 API常量和结构体
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private bool _blockOriginalKeypress;
        private bool _isPaused;  // 新增暂停状态标志

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // 钩子实例和委托
        private IntPtr _hookID = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;
        private bool _isDisposed = false;

        // 临时禁用标志
        private bool _isTemporarilyDisabled = false;

        // 事件
        public event EventHandler<KeyboardHookEventArgs> KeyDown;


        public KeyboardHookService()
        {
            _proc = HookCallback;

            try
            {
                _hookID = SetHook(_proc);

                if (_hookID == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "键盘钩子设置失败。请尝试以管理员身份运行程序。");
                }

                // 使用命名事件处理方法
                Utilities.KeyboardSimulator.SimulationStarting += HandleSimulationStarting;
                Utilities.KeyboardSimulator.SimulationCompleted += HandleSimulationCompleted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"键盘钩子初始化失败: {ex.Message}");
                throw; // 重新抛出异常，因为没有钩子程序将无法正常工作
            }
        }

        private void HandleSimulationStarting(object sender, EventArgs e)
        {
            _isTemporarilyDisabled = true;
        }

        private void HandleSimulationCompleted(object sender, EventArgs e)
        {
            _isTemporarilyDisabled = false;
        }

        // 新增: 设置是否阻止原始按键
        public void SetBlockOriginalKeypress(bool block)
        {
            _blockOriginalKeypress = block;
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);

                if (hookId == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Console.WriteLine($"SetWindowsHookEx 失败，错误码: {errorCode}");
                }

                return hookId;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                // 如果钩子临时禁用，直接传递给下一个钩子
                if (_isTemporarilyDisabled)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    Keys key = (Keys)vkCode;  // 添加这一行定义key变量

                    // 获取当前修饰键状态
                    Keys modifiers = Control.ModifierKeys;

                    // 使用修饰键信息调用OnKeyDown
                    OnKeyDown(key, modifiers);

                    // 修改: 仅对可映射的普通按键阻止原始按键传播
                    // 对于功能键、控制键等特殊按键，始终允许原始按键事件传递
                    // 修改: 在映射暂停时不阻止原始按键
                    if (_blockOriginalKeypress && !_isPaused && IsRegularKey(key) && modifiers == Keys.None)
                    {
                        return (IntPtr)1;  // 返回非零值阻止按键事件传递
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"键盘钩子回调错误: {ex.Message}");
                // 即使出错也继续处理链中的下一个钩子
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // 新增: 判断是否为普通可映射按键(字母、数字、标点)
        private bool IsRegularKey(Keys key)
        {
            // 获取基本键码
            Keys baseKey = key & Keys.KeyCode;

            // 功能键、控制键、导航键返回false
            if (IsFunctionKey(baseKey) || IsControlKey(baseKey) || IsNavigationKey(baseKey))
            {
                return false;
            }

            return true;
        }

        // 添加IsFunctionKey, IsControlKey, IsNavigationKey方法
        // 如果这些方法在KeyMapper中已经存在，可以复制过来
        private bool IsFunctionKey(Keys key)
        {
            return key >= Keys.F1 && key <= Keys.F24;
        }

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

        private bool IsNavigationKey(Keys key)
        {
            // 导航键列表
            Keys[] navKeys = new Keys[]
            {
                Keys.Up, Keys.Down, Keys.Left, Keys.Right,
                Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown,
                Keys.Insert, Keys.Delete,
                Keys.Escape, Keys.Tab,
                Keys.PrintScreen, Keys.Pause // 删除了无效的Keys.Break
            };

            return Array.IndexOf(navKeys, key) >= 0;
        }

        // 添加这个方法，处理窗口消息
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 这个方法在当前实现中并不需要实际处理什么，但需要存在以避免编译错误
            return IntPtr.Zero;
        }

        protected virtual void OnKeyDown(Keys key, Keys modifiers)
        {
            try
            {
                KeyDown?.Invoke(this, new KeyboardHookEventArgs(key, modifiers));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KeyDown事件处理出错: {ex.Message}");
                // 继续处理，不中断钩子链
            }
        }

        // 添加这个重载以保持向后兼容
        protected virtual void OnKeyDown(Keys key)
        {
            OnKeyDown(key, Control.ModifierKeys);
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
                    // 取消订阅事件
                    Utilities.KeyboardSimulator.SimulationStarting -= HandleSimulationStarting;
                    Utilities.KeyboardSimulator.SimulationCompleted -= HandleSimulationCompleted;
                }

                // 注销所有热键
                if (_hookID != IntPtr.Zero)
                {
                    try
                    {
                        UnhookWindowsHookEx(_hookID);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"注销键盘钩子失败: {ex.Message}");
                    }
                    finally
                    {
                        _hookID = IntPtr.Zero;
                    }
                }

                _isDisposed = true;
            }
        }

        ~KeyboardHookService()
        {
            Dispose(false);
        }
    }
}