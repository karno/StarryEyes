using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using StarryEyes.Settings;

namespace StarryEyes.Models
{
    internal static class BehaviorLogger
    {
        private static IDisposable _disposable;

        private static Action<string> _writer;

        static BehaviorLogger()
        {
            App.ApplicationFinalize += Cleanup;
        }

        internal static void Initialize()
        {
#if DEBUG
            _writer = s => Debug.WriteLine(s);
            return;
#endif
            if (!Setting.IsLoaded || !Setting.IsBehaviorLogEnabled.Value)
            {
                return;
            }

            var disposables = new CompositeDisposable();
            try
            {
                var file = new FileStream(
                    Path.Combine(App.ConfigurationDirectoryPath, App.BehaviorLogFileName),
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite);
                var writer = new StreamWriter(file) { AutoFlush = true };
                _writer = writer.WriteLine;
                disposables.Add(Disposable.Create(() => _writer = null));
                disposables.Add(writer);
                disposables.Add(file);
            }
            finally
            {
                _disposable = disposables;
            }
        }

        internal static void Log(string log)
        {
            LogCore(String.Empty, log);
        }

        internal static void Log(string tag, string log)
        {
            if (tag.Length > 5)
            {
                tag = tag.Substring(0, 5);
            }
            var tagstr = "[" + tag + "]";
            LogCore(tagstr, log);
        }

        internal static void LogCore(string tag, string log)
        {
            var writer = _writer;
            try
            {
                writer?.Invoke(
                    $"{DateTime.Now:yy/MM/dd HH:mm:ss} > {tag,-7} {log}");
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }

        static void Cleanup()
        {
            if (_disposable != null)
            {
                _disposable.Dispose();
                _disposable = null;
            }
        }
    }
}