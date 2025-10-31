using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Utility
{
    /// <summary>
    /// 命令行工具类，用于执行系统命令和打开文件夹
    /// </summary>
    public static class CMD
    {
        /// <summary>
        /// 执行命令并返回输出结果
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <param name="args">参数</param>
        /// <param name="workingDir">工作目录</param>
        /// <returns>命令输出结果</returns>
        public static string Execute(string cmd, string args = "", string workingDir = "")
        {
            try
            {
                using (var process = CreateProcess(cmd, args, workingDir))
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CMD] 执行命令失败: {cmd} {args}, 错误: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 执行命令并返回输出和错误信息
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <param name="args">参数</param>
        /// <param name="workingDir">工作目录</param>
        /// <returns>[0]输出内容 [1]错误信息 [2]退出码</returns>
        public static string[] ExecuteWithError(string cmd, string args = "", string workingDir = "")
        {
            try
            {
                using (var process = CreateProcess(cmd, args, workingDir))
                {
                    UnityEngine.Debug.Log($"[CMD] 启动进程: {cmd} {args}");
                    process.Start();
                    
                    // 异步读取输出和错误流，避免死锁
                    var outputTask = Task.Run(() => process.StandardOutput.ReadToEnd());
                    var errorTask = Task.Run(() => process.StandardError.ReadToEnd());
                    
                    UnityEngine.Debug.Log($"[CMD] 等待进程结束...");
                    process.WaitForExit();
                    
                    // 等待读取任务完成
                    string output = outputTask.Result;
                    string error = errorTask.Result;
                    
                    UnityEngine.Debug.Log($"[CMD] 进程已结束，退出码: {process.ExitCode}");
                    
                    return new string[] { output, error, process.ExitCode.ToString() };
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CMD] 执行命令失败: {cmd} {args}, 错误: {e.Message}");
                UnityEngine.Debug.LogError($"[CMD] 异常堆栈: {e.StackTrace}");
                return new string[] { "", e.Message, "-1" };
            }
        }

        /// <summary>
        /// 执行命令并传入标准输入
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <param name="args">参数</param>
        /// <param name="input">标准输入内容</param>
        /// <param name="workingDir">工作目录</param>
        /// <returns>命令输出结果</returns>
        public static string ExecuteWithInput(string cmd, string args, string[] input, string workingDir = "")
        {
            try
            {
                using (var process = CreateProcess(cmd, args, workingDir))
                {
                    process.Start();
                    
                    // 写入输入内容
                    if (input != null && input.Length > 0)
                    {
                        foreach (string line in input)
                        {
                            process.StandardInput.WriteLine(line);
                        }
                        process.StandardInput.Close();
                    }
                    
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CMD] 执行命令失败: {cmd} {args}, 错误: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 打开文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        public static void OpenFolder(string path)
        {
            try
            {
                // 替换'/'为'\'，以兼容Windows路径
                if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    path = path.Replace('/', '\\');
                }
                
                UnityEngine.Debug.Log($"[CMD] 打开文件夹: {path}");

                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                        Execute("explorer.exe", $"\"{path}\"");
                        break;

                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                        Execute("open", $"\"{path}\"");
                        break;

                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.LinuxPlayer:
                        Execute("xdg-open", $"\"{path}\"");
                        break;

                    default:
                        UnityEngine.Debug.LogWarning($"[CMD] 当前平台不支持打开文件夹: {Application.platform}");
                        break;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CMD] 打开文件夹失败: {path}, 错误: {e.Message}");
            }
        }

        /// <summary>
        /// 创建进程对象
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <param name="args">参数</param>
        /// <param name="workingDir">工作目录</param>
        /// <returns>配置好的进程对象</returns>
        private static Process CreateProcess(string cmd, string args, string workingDir)
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
                StandardErrorEncoding = GetSystemEncoding()
            };

            if (!string.IsNullOrEmpty(workingDir))
            {
                startInfo.WorkingDirectory = workingDir;
            }

            return new Process { StartInfo = startInfo };
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
                    return Encoding.GetEncoding("gb2312");
                default:
                    return Encoding.UTF8;
            }
        }
    }
}