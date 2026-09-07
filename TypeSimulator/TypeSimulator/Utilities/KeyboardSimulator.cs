using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace TypeSimulator.Utilities
{
    public static class KeyboardSimulator
    {
        private const uint KeyUp = 0x0002;
        private const uint Unicode = 0x0004;
        private static readonly object Sync = new object();

        [StructLayout(LayoutKind.Sequential)]
        internal struct Input
        {
            internal uint Type;
            internal InputUnion Data;
        }

        // Include MOUSEINPUT so INPUT has the native size on both x86 and x64.
        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)] internal KeyboardInput Keyboard;
            [FieldOffset(0)] internal MouseInput Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KeyboardInput
        {
            internal ushort VirtualKey, ScanCode;
            internal uint Flags, Time;
            internal UIntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MouseInput
        {
            internal int X, Y;
            internal uint MouseData, Flags, Time;
            internal UIntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, Input[] inputs, int size);

        public static event EventHandler SimulationStarting;
        public static event EventHandler SimulationCompleted;

        internal static Input[] CreateInputs(char character)
        {
            ushort virtualKey = character == '\n' || character == '\r' ? (ushort)0x0D
                : character == '\t' ? (ushort)0x09 : (ushort)0;
            return CreatePair(virtualKey, virtualKey == 0 ? (ushort)character : (ushort)0,
                virtualKey == 0 ? Unicode : 0);
        }

        private static Input[] CreatePair(ushort virtualKey, ushort scanCode, uint flags)
        {
            var down = new Input { Type = 1 };
            down.Data.Keyboard = new KeyboardInput
            {
                VirtualKey = virtualKey, ScanCode = scanCode, Flags = flags
            };
            var up = down;
            up.Data.Keyboard.Flags |= KeyUp;
            return new[] { down, up };
        }

        private static void Send(Input[] inputs)
        {
            lock (Sync)
            {
                try
                {
                    SimulationStarting?.Invoke(null, EventArgs.Empty);
                    if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input))) != inputs.Length)
                        throw new Win32Exception(Marshal.GetLastWin32Error(),
                            "发送键盘输入失败，请检查目标窗口及其权限级别。");
                }
                finally
                {
                    SimulationCompleted?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        public static void PressKey(byte keyCode) => Send(CreatePair(keyCode, 0, 0));

        // Send UTF-16 directly instead of feeding English keystrokes to the IME.
        public static void TypeCharacter(char character) => Send(CreateInputs(character));

        public static void TypeText(string text, int delayBetweenCharsMs = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            lock (Sync)
            {
                foreach (char character in text)
                {
                    TypeCharacter(character);
                    if (delayBetweenCharsMs > 0) Thread.Sleep(delayBetweenCharsMs);
                }
            }
        }
    }
}