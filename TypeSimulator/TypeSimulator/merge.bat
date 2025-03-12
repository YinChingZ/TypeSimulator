@echo off
echo 开始合并TypeSimulator程序集...

REM 设置路径变量
set OUTPUT_DIR=D:\publish
set INPUT_DIR=bin\Release
set ILMERGE_PATH=packages\ILMerge.3.0.41\tools\net452\ILMerge.exe

REM 创建输出目录
if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%

REM 合并程序集
"%ILMERGE_PATH%" /target:winexe /targetplatform:v4 /out:"%OUTPUT_DIR%\TypeSimulator.exe" "%INPUT_DIR%\TypeSimulator.exe" "%INPUT_DIR%\*.dll"

echo 合并完成！单文件版本已创建: %OUTPUT_DIR%\TypeSimulator.exe
pause