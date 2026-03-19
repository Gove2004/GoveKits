using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Utility
{
    /// <summary>
    /// 持续交互终端核心。
    /// </summary>
    /// <remarks>
    /// 该类会维护一个常驻 shell 进程，可持续发送命令并接收输出。
    /// </remarks>
    public static class Terminal
    {
        private static readonly object SyncRoot = new();
        private static readonly StringBuilder OutputBuffer = new(4096);

        private static Process process;
        private static string shellName = string.Empty;
        private static string shellArgs = string.Empty;
        private static string workingDirectory = string.Empty;

        /// <summary>
        /// 输出事件（每行触发一次）。
        /// </summary>
        public static event Action<string> OutputReceived;

        public static bool IsRunning
        {
            get
            {
                lock (SyncRoot)
                {
                    return process != null && !process.HasExited;
                }
            }
        }

        public static string WorkingDirectory => workingDirectory;
        public static string ShellName => shellName;

        /// <summary>
        /// 启动终端会话。
        /// </summary>
        public static bool Start(string customShell = null, string customArgs = null, string customWorkingDirectory = null)
        {
            lock (SyncRoot)
            {
                try
                {
                    if (process != null && !process.HasExited)
                    {
                        return true;
                    }

                    ResolveShell(customShell, customArgs, out shellName, out shellArgs);
                    workingDirectory = string.IsNullOrWhiteSpace(customWorkingDirectory)
                        ? Directory.GetCurrentDirectory()
                        : customWorkingDirectory;

                    process = CreateShellProcess(shellName, shellArgs, workingDirectory);
                    process.OutputDataReceived += OnOutputData;
                    process.ErrorDataReceived += OnErrorData;
                    process.Exited += OnExited;

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    AppendOutput($"[T] session started: {shellName} {shellArgs}");
                    AppendOutput($"[T] cwd: {workingDirectory}");
                    return true;
                }
                catch (Exception e)
                {
                    LogCore.LogError("T", $"启动终端失败: {e.Message}");
                    AppendOutput($"[T] start failed: {e.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 发送一行命令到当前会话。
        /// </summary>
        public static bool Send(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            lock (SyncRoot)
            {
                if (process == null || process.HasExited)
                {
                    AppendOutput("[T] session is not running.");
                    return false;
                }

                try
                {
                    process.StandardInput.WriteLine(command);
                    process.StandardInput.Flush();
                    AppendOutput($"> {command}");
                    return true;
                }
                catch (Exception e)
                {
                    AppendOutput($"[T] send failed: {e.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 停止当前会话。
        /// </summary>
        public static void Stop(bool forceKill = false)
        {
            lock (SyncRoot)
            {
                if (process == null)
                {
                    return;
                }

                try
                {
                    if (!process.HasExited)
                    {
                        if (forceKill)
                        {
                            process.Kill();
                        }
                        else
                        {
                            process.StandardInput.WriteLine("exit");
                            process.StandardInput.Flush();
                            if (!process.WaitForExit(1500))
                            {
                                process.Kill();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    AppendOutput($"[T] stop failed: {e.Message}");
                }
                finally
                {
                    DisposeProcess();
                    AppendOutput("[T] session stopped.");
                }
            }
        }

        /// <summary>
        /// 清空终端输出缓冲。
        /// </summary>
        public static void ClearOutput()
        {
            lock (SyncRoot)
            {
                OutputBuffer.Clear();
            }
        }

        /// <summary>
        /// 获取完整输出快照。
        /// </summary>
        public static string GetOutputSnapshot()
        {
            lock (SyncRoot)
            {
                return OutputBuffer.ToString();
            }
        }

        /// <summary>
        /// 打开文件夹。
        /// </summary>
        public static void OpenFolder(string path)
        {
            try
            {
                if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    path = path.Replace('/', '\\');
                }
                
                LogCore.Log("T", $"打开文件夹: {path}");

                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{path}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        });
                        break;

                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                        Process.Start("open", $"\"{path}\"");
                        break;

                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.LinuxPlayer:
                        Process.Start("xdg-open", $"\"{path}\"");
                        break;

                    default:
                        LogCore.LogWarning("T", $"当前平台不支持打开文件夹: {Application.platform}");
                        break;
                }
            }
            catch (Exception e)
            {
                LogCore.LogError("T", $"打开文件夹失败: {path}, 错误: {e.Message}");
            }
        }

        private static void ResolveShell(string customShell, string customArgs, out string cmd, out string args)
        {
            if (!string.IsNullOrWhiteSpace(customShell))
            {
                cmd = customShell;
                args = customArgs ?? string.Empty;
                return;
            }

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    cmd = "cmd.exe";
                    args = "/Q /K chcp 65001>nul";
                    break;
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                    cmd = "/bin/bash";
                    args = "-i";
                    break;
                default:
                    cmd = "cmd.exe";
                    args = "/Q /K";
                    break;
            }
        }

        private static Process CreateShellProcess(string cmd, string args, string cwd)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = GetSystemEncoding(),
                StandardErrorEncoding = GetSystemEncoding(),
                WorkingDirectory = cwd
            };

            return new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };
        }

        /// <summary>
        /// 获取系统编码
        /// </summary>
        /// <returns>系统编码</returns>
        private static Encoding GetSystemEncoding()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return Encoding.UTF8;
                default:
                    return Encoding.UTF8;
            }
        }

        private static void OnOutputData(object _, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendOutput(e.Data);
            }
        }

        private static void OnErrorData(object _, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendOutput($"[err] {e.Data}");
            }
        }

        private static void OnExited(object _, EventArgs __)
        {
            AppendOutput("[T] process exited.");
            lock (SyncRoot)
            {
                DisposeProcess();
            }
        }

        private static void AppendOutput(string line)
        {
            lock (SyncRoot)
            {
                OutputBuffer.AppendLine(line);
            }

            try
            {
                OutputReceived?.Invoke(line);
            }
            catch
            {
            }
        }

        private static void DisposeProcess()
        {
            if (process == null)
            {
                return;
            }

            try
            {
                process.OutputDataReceived -= OnOutputData;
                process.ErrorDataReceived -= OnErrorData;
                process.Exited -= OnExited;
                process.Dispose();
            }
            catch
            {
            }

            process = null;
        }
    }
}