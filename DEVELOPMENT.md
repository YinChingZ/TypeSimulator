# 本地开发

项目是 Windows WPF 应用，运行目标保持 **.NET Framework 4.8**。
本地构建入口 `TypeSimulator/TypeSimulator/TypeSimulator.Local.csproj` 使用 .NET 8 SDK，
自动从 NuGet 还原 4.8 引用程序集，不依赖 Visual Studio、旧版 ILMerge 或作者的 ClickOnce 签名证书。
原解决方案和发布工程保留，用于原有 Visual Studio/ClickOnce 流程。

## 一次性配置

在仓库根目录打开 PowerShell：

```powershell
./scripts/setup.ps1
```

脚本从微软官方下载固定版本 8.0.424 SDK 到 `.tools/dotnet`，不会更改系统 PATH。
首次运行需联网访问微软下载站和 nuget.org。已安装 .NET 8 SDK 的电脑可直接运行构建脚本。
SDK、NuGet 缓存及本地构建输出均放在仓库内并由 `.gitignore` 排除。

## 构建与测试

```powershell
./scripts/build.ps1 -Test
./scripts/build.ps1 -Configuration Debug
./artifacts/app/net48/TypeSimulator.exe
```

测试是 .NET Framework 4.8 STA 控制台程序，通过真实 WPF Dispatcher 验证计时，
使用替代字符输出器，不会向当前窗口发送按键，也不会修改剪贴板。
覆盖 Unicode 输入结构、特殊符号、换行/Tab、10–6000 CPM、延迟、暂停/恢复、停止及发送失败，
并通过真实 XAML 窗口验证随机延迟设置传递、按钮状态及模拟剪贴板的新任务/恢复/空内容行为。

## 本次 issue 变更

- #1：启动时读取随机延迟复选框；按实际时间安排字符，减少计时回调开销累积。
- #2：以 `SendInput` Unicode 按下/释放事件替代 `SendKeys.SendWait`；过滤注入的键盘钩子事件，避免再次映射。Enter/Tab 保留虚拟按键语义。
- #3：勾选“启动时直接读取剪贴板”，在目标窗口按 Ctrl+Alt+T；每次新任务读取最新文本，暂停/恢复不重新读取，空剪贴板不会回退到旧文本。
- #4：启动等待 0–60 秒，默认 3 秒；显示倒计时，等待期间支持暂停、继续、停止和 F12 重置。

可选速度为 **10–6000 CPM**（字符/分钟），滑块步进 10；聚焦滑块后，方向键微调 10，PageUp/PageDown 调整 100。
随机延迟开启时，标点停顿会降低平均 CPM；关闭时 400 CPM 对应约每 150ms 一个 UTF-16 字符，6000 CPM 对应每 10ms 一个。
Windows 定时器可能合并到期时间，高速时同一回调最多输出 4 个到期字符。
遇到长时间 UI 卡顿会放弃补齐超出的时间欠账，保留全部文本，因此过载时会降速，不会无限突发补发。
设置对下一次新任务生效。剪贴板模式目前用于自动打字，按键映射仍使用文本框。

## 人工兼容性验收

1. 打开记事本，准备中英文和符号文本，例如 `abc中文ABC{}+^%~`，附加多行、Tab 和 emoji。
2. 分别在英文键盘、微软拼音及搜狗中文模式下打字，确认文本顺序、候选框和焦点行为。
3. 等待设置为 3 秒，启动后切换到记事本；确认首字符完整，暂停/恢复不重复输出，停止后不再输入。
4. 勾选剪贴板模式，复制 A 后热键启动，再复制 B 后启动；分别得到 A 和 B。空/非文本剪贴板应仅记录警告。
5. 映射开启后确认一次按键只输出一个映射字符，最后一次不会漏出原始按键；F10/F11/F12 仍有效。

自动化测试不代表第三方输入法已实测。目标程序若使用原始键盘输入或以更高权限运行，仍需单独验证。

## 本机验证记录

2026-09-07：Windows x64，项目内 SDK 8.0.424。
Release 应用和测试编译均为 0 警告、0 错误，全部 59 项回归检查通过。
扩展前的引擎在 6000 CPM 档无法按时完成；现已通过每回调最多 4 字符的到期处理修复。
关闭随机延迟，使用混合中英文和符号文本、带少量计算开销的内存输出器，按首末字符时间差测得：

| 设定 CPM | 实测 CPM | 误差 |
| --- | --- | --- |
| 10 | 10.0 | 0.05% |
| 50 | 50.2 | 0.48% |
| 400 | 401.5 | 0.37% |
| 1200 | 1203.5 | 0.30% |
| 3000 | 2995.9 | 0.14% |
| 6000 | 6007.7 | 0.13% |

各档完整输出，没有遗漏、重复或顺序变化。6000 CPM 下模拟 200ms 阻塞后，
字符仍完整且连续补发不超过 4 个；批次中途停止不会多输出字符。
每档测试约 3–12 秒，这些结果验证引擎调度，不能代替实际目标窗口的吞吐量测试。
可在构建后单独复测速度：`./tests/bin/Release/net48/TypeSimulator.Tests.exe --speed-only`。
测试未向其他应用实际注入输入；搜狗输入法和目标应用兼容性仍需按以上步骤验收。

实现参考：[Microsoft SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)、
[KEYBDINPUT 的 Unicode 行为](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput)。
