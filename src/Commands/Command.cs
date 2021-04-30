using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace SourceGit.Commands {

    /// <summary>
    ///     取消命令执行的对象
    /// </summary>
    public class Cancellable {
        public bool IsCancelRequested { get; set; } = false;
    }

    /// <summary>
    ///     命令接口
    /// </summary>
    public class Command {

        /// <summary>
        ///     读取全部输出时的结果
        /// </summary>
        public class ReadToEndResult {
            public bool IsSuccess { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }

        /// <summary>
        ///     运行路径
        /// </summary>
        public string Cwd { get; set; } = "";

        /// <summary>
        ///     参数
        /// </summary>
        public string Args { get; set; } = "";

        /// <summary>
        ///     使用标准错误输出
        /// </summary>
        public bool TraitErrorAsOutput { get; set; } = false;

        /// <summary>
        ///     用于取消命令指行的Token
        /// </summary>
        public Cancellable Token { get; set; } = null;

        /// <summary>
        ///     运行
        /// </summary>
        public bool Exec() {
            var start = new ProcessStartInfo();
            start.FileName = Models.Preference.Instance.Git.Path;
            start.Arguments = "--no-pager -c core.quotepath=off " + Args;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8;
            start.StandardErrorEncoding = Encoding.UTF8;

            if (!string.IsNullOrEmpty(Cwd)) start.WorkingDirectory = Cwd;

            var progressFilter = new Regex(@"\d+\%");
            var errs = new List<string>();
            var proc = new Process() { StartInfo = start };
            var isCancelled = false;

            proc.OutputDataReceived += (o, e) => {
                if (Token != null && Token.IsCancelRequested) {
                    isCancelled = true;
                    proc.CancelErrorRead();
                    proc.CancelOutputRead();
#if NET48
                    proc.Kill();
#else
                    proc.Kill(true);
#endif
                    return;
                }

                if (e.Data == null) return;
                OnReadline(e.Data);
            };
            proc.ErrorDataReceived += (o, e) => {
                if (Token != null && Token.IsCancelRequested) {
                    isCancelled = true;
                    proc.CancelErrorRead();
                    proc.CancelOutputRead();
#if NET48
                    proc.Kill();
#else
                    proc.Kill(true);
#endif
                    return;
                }

                if (e.Data == null) return;
                if (TraitErrorAsOutput) OnReadline(e.Data);
                
                if (string.IsNullOrEmpty(e.Data)) return;
                if (progressFilter.IsMatch(e.Data)) return;
                if (e.Data.StartsWith("remote: Counting objects:", StringComparison.Ordinal)) return;
                errs.Add(e.Data);
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            int exitCode = proc.ExitCode;
            proc.Close();

            if (!isCancelled && exitCode != 0 && errs.Count > 0) {
                Models.Exception.Raise(string.Join("\n", errs));
                return false;
            } else {
                return true;
            }
        }

        /// <summary>
        ///     直接读取全部标准输出
        /// </summary>
        public ReadToEndResult ReadToEnd() {
            var start = new ProcessStartInfo();
            start.FileName = Models.Preference.Instance.Git.Path;
            start.Arguments = "--no-pager -c core.quotepath=off " + Args;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8;
            start.StandardErrorEncoding = Encoding.UTF8;

            if (!string.IsNullOrEmpty(Cwd)) start.WorkingDirectory = Cwd;
            
            var proc = new Process() { StartInfo = start };
            proc.Start();

            var rs = new ReadToEndResult();
            rs.Output = proc.StandardOutput.ReadToEnd();
            rs.Error = proc.StandardError.ReadToEnd();

            proc.WaitForExit();
            rs.IsSuccess = proc.ExitCode == 0;
            proc.Close();

            return rs;
        }

        /// <summary>
        ///     调用Exec时的读取函数
        /// </summary>
        /// <param name="line"></param>
        public virtual void OnReadline(string line) {}
    }
}