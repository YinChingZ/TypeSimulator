using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using TypeSimulator.Models;
using TypeSimulator.Utilities;

internal static class Program
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Console.WriteLine("PASS: " + message);
    }

    private static void Pump(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (s, e) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == "--speed-only")
            {
                VerifySpeedRange();
                return 0;
            }
            Check(Marshal.SizeOf(typeof(KeyboardSimulator.Input)) == (IntPtr.Size == 8 ? 40 : 28), "native INPUT size");
            foreach (char c in "aA中文{}()+^%~[]\ud83d\ude00")
            {
                var pair = KeyboardSimulator.CreateInputs(c);
                Check(pair.Length == 2 && pair[0].Type == 1 && pair[0].Data.Keyboard.VirtualKey == 0
                    && pair[0].Data.Keyboard.ScanCode == c && pair[0].Data.Keyboard.Flags == 4
                    && pair[1].Data.Keyboard.Flags == 6, "Unicode pair " + ((int)c).ToString("X4"));
            }
            foreach (char c in "\r\n\t")
            {
                var pair = KeyboardSimulator.CreateInputs(c);
                Check(pair[0].Data.Keyboard.VirtualKey == (c == '\t' ? 9 : 13)
                    && pair[0].Data.Keyboard.Flags == 0 && pair[1].Data.Keyboard.Flags == 2, "Enter/Tab pair");
            }

            var output = new List<char>();
            var times = new List<double>();
            var clock = Stopwatch.StartNew();
            var engine = new TypeSimulatorEngine(c => { output.Add(c); times.Add(clock.Elapsed.TotalMilliseconds); });
            int completions = 0;
            engine.TypingCompleted += (s, e) => completions++;
            engine.Configure(new string('.', 20), 400, false);
            engine.Start();
            Pump(3200);
            Check(output.Count == 20 && completions == 1 && !engine.IsTyping, "400 CPM finishes exactly once");
            double duration = times[19] - times[0];
            Check(duration >= 2700 && duration <= 3100, "random OFF punctuation cadence: " + duration.ToString("F0") + " ms / 19 intervals");

            output.Clear();
            engine.Configure("ab", 400, false, 1);
            engine.Start();
            Pump(300);
            Check(output.Count == 0, "no output during startup delay");
            engine.Pause();
            Pump(900);
            Check(output.Count == 0 && engine.IsPaused, "pause freezes startup delay");
            engine.Resume();
            Pump(350);
            Check(output.Count == 0, "resume preserves remaining delay");
            Pump(650);
            Check(new string(output.ToArray()) == "ab", "delayed input completes");

            output.Clear();
            engine.Configure("cancel", 400, false, 1);
            engine.Start();
            Pump(100);
            engine.Stop();
            Pump(1100);
            Check(output.Count == 0, "stop cancels startup delay");

            engine.Configure("a\r\nb\rc", 400, false);
            engine.Start();
            Pump(60);
            engine.Pause();
            int pausedCount = output.Count;
            Pump(400);
            Check(output.Count == pausedCount, "pause stops active input");
            engine.Resume();
            Pump(850);
            Check(new string(output.ToArray()) == "a\nb\nc", "resume preserves position and normalizes CRLF");

            var failure = new TypeSimulatorEngine(c => { throw new InvalidOperationException("test failure"); });
            bool reported = false;
            failure.TypingFailed += (s, e) => reported = true;
            failure.Configure("x", 400, false);
            failure.Start();
            Pump(100);
            Check(reported && failure.IsPaused && failure.CharactersTyped == 0, "failed input pauses without skipping character");
            failure.Stop();
            VerifySpeedRange();
            VerifyHighSpeedControls();

            // Construct the real XAML window without showing it or registering hotkeys.
            // A 60-second delay lets us inspect settings and stop before any native input.
            string clipboard = "first clipboard";
            int clipboardReads = 0;
            var window = new TypeSimulator.MainWindow(() => { clipboardReads++; return clipboard; });
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var windowType = typeof(TypeSimulator.MainWindow);
            try
            {
                Check((bool)windowType.GetField("_isInitialized", flags).GetValue(window), "WPF window initializes");
                Check(window.SpeedSlider.Minimum == TypeSimulatorEngine.MinimumSpeed
                    && window.SpeedSlider.Maximum == TypeSimulatorEngine.MaximumSpeed,
                    "UI exposes 10–6000 CPM");
                window.InputTextBox.Text = "UI regression";
                window.SpeedSlider.Value = 400;
                window.RandomDelayCheckBox.IsChecked = false;
                window.StartDelaySlider.Value = 60;
                windowType.GetMethod("StartTyping", flags).Invoke(window, null);
                var configured = (TypeSimulator.Models.Settings)windowType.GetField("_settings", flags).GetValue(window);
                var active = (TypeSimulatorEngine)windowType.GetField("_typeSimulator", flags).GetValue(window);
                Check(configured.TypingSpeed == 400 && !configured.EnableRandomDelay
                    && configured.StartDelaySeconds == 60 && active.RemainingDelaySeconds == 60,
                    "UI passes speed, unchecked random delay and startup delay to engine");
                Check(!window.StartTypingButton.IsEnabled && window.StopTypingButton.IsEnabled,
                    "countdown offers stop and prevents duplicate start");
                windowType.GetMethod("StopTyping", flags).Invoke(window, null);
                Check(!active.IsTyping && window.StartTypingButton.IsEnabled, "UI stop resets controls");
                window.SpeedSlider.Value = 6000;
                windowType.GetMethod("StartTyping", flags).Invoke(window, null);
                Check(configured.TypingSpeed == 6000, "UI passes maximum speed to engine");
                windowType.GetMethod("StopTyping", flags).Invoke(window, null);
                window.UseClipboardCheckBox.IsChecked = true;
                windowType.GetMethod("StartTyping", flags).Invoke(window, null);
                var textField = typeof(TypeSimulatorEngine).GetField("_textToType", flags);
                Check((string)textField.GetValue(active) == clipboard && clipboardReads == 1,
                    "clipboard mode uses clipboard instead of editor text");
                windowType.GetMethod("PauseTyping", flags).Invoke(window, null);
                clipboard = "second clipboard";
                windowType.GetMethod("ResumeTyping", flags).Invoke(window, null);
                Check(clipboardReads == 1 && (string)textField.GetValue(active) == "first clipboard",
                    "resume keeps clipboard snapshot");
                windowType.GetMethod("StopTyping", flags).Invoke(window, null);
                windowType.GetMethod("StartTyping", flags).Invoke(window, null);
                Check(clipboardReads == 2 && (string)textField.GetValue(active) == clipboard,
                    "new task reads latest clipboard");
                windowType.GetMethod("StopTyping", flags).Invoke(window, null);
                clipboard = string.Empty;
                windowType.GetMethod("StartTyping", flags).Invoke(window, null);
                Check(!active.IsTyping && clipboardReads == 3, "empty clipboard never starts stale editor text");
            }
            finally
            {
                ((TypeSimulatorEngine)windowType.GetField("_typeSimulator", flags).GetValue(window)).Stop();
                ((IDisposable)windowType.GetField("_keyboardHook", flags).GetValue(window)).Dispose();
                ((IDisposable)windowType.GetField("_hotkeyManager", flags).GetValue(window)).Dispose();
            }
            Console.WriteLine("All regression checks passed.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void VerifySpeedRange()
    {
        foreach (int speed in new[] { 10, 50, 400, 1200, 3000, 6000 })
        {
            int count = Math.Max(3, speed / 15);
            var expected = new System.Text.StringBuilder();
            const string sample = "Ab中文{}+.%123";
            for (int i = 0; i < count; i++) expected.Append(sample[i % sample.Length]);
            var received = new System.Text.StringBuilder();
            var clock = Stopwatch.StartNew();
            double first = 0, last = 0;
            var engine = new TypeSimulatorEngine(c =>
            {
                // Include a small per-character cost rather than timing an empty callback.
                if (received.Length == 0) first = clock.Elapsed.TotalMilliseconds;
                last = clock.Elapsed.TotalMilliseconds;
                received.Append(c);
                Thread.SpinWait(1000);
            });
            double theoretical = (count - 1) * 60000.0 / speed;
            try
            {
                engine.Configure(expected.ToString(), speed, false);
                engine.Start();
                Pump((int)(theoretical * 1.15 + 500));
                Check(!engine.IsTyping && received.ToString() == expected.ToString(), speed + " CPM complete, ordered, no missing/duplicate characters");
                double actual = (count - 1) * 60000.0 / (last - first);
                double error = Math.Abs(actual / speed - 1);
                Check(error < 0.05, string.Format("speed {0}: actual {1:F1} CPM, error {2:P2}", speed, actual, error));
            }
            finally { engine.Stop(); }
        }
    }

    private static void VerifyHighSpeedControls()
    {
        var received = new List<double>();
        var clock = Stopwatch.StartNew();
        var engine = new TypeSimulatorEngine(c =>
        {
            received.Add(clock.Elapsed.TotalMilliseconds);
            if (received.Count == 10) Thread.Sleep(200); // Simulate a busy target/UI thread.
        });
        try
        {
            engine.Configure(new string('x', 60), 6000, false);
            engine.Start();
            Pump(1300);
            Check(received.Count == 60 && !engine.IsTyping, "6000 CPM UI stall preserves all characters");
            int burst = 1, largest = 1;
            for (int i = 1; i < received.Count; i++)
            {
                burst = received[i] - received[i - 1] < 5 ? burst + 1 : 1;
                largest = Math.Max(largest, burst);
            }
            Check(largest <= 4, "UI stall recovery limits catch-up burst to 4 characters");
            int stoppedCount = 0;
            engine.CharacterTyped += (s, c) => { stoppedCount++; engine.Stop(); };
            engine.Configure("must stop after first", 6000, false);
            engine.Start();
            Pump(150);
            Check(stoppedCount == 1, "stop during high-speed batch emits no extra characters");
            foreach (int invalid in new[] { 0, 9, 6001, int.MaxValue })
            {
                bool rejected = false;
                try { engine.Configure("x", invalid, false); }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "reject out-of-range speed " + invalid);
            }
        }
        finally { engine.Stop(); }
    }
}
