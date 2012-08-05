using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using Livet;

namespace StarryEyes.Mystique
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            DispatcherHelper.UIDispatcher = Dispatcher;
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            var avltree = new StarryEyes.Albireo.Data.AVLTree<long>();
            Enumerable.Range(1, 50).ForEach(i => avltree.Add(i));
            avltree.ForEach(i => System.Diagnostics.Debug.WriteLine(i));
            Enumerable.Range(1, 50).Where(i => i % 3 == 0).Where(i => !avltree.Remove(i)).ForEach(i => System.Diagnostics.Debug.WriteLine("FAIL!!!:" + i));
            avltree.ForEach(i => System.Diagnostics.Debug.WriteLine(i));
            Enumerable.Range(1, 10).Select(i => i * 10).ForEach(i => avltree.Add(i));
            avltree.ForEach(i => System.Diagnostics.Debug.WriteLine(i));

        }

        //集約エラーハンドラ
        //private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    //TODO:ロギング処理など
        //    MessageBox.Show(
        //        "不明なエラーが発生しました。アプリケーションを終了します。",
        //        "エラー",
        //        MessageBoxButton.OK,
        //        MessageBoxImage.Error);
        //
        //    Environment.Exit(1);
        //}

        #region Triggers

        /// <summary>
        /// Call on kernel systems are ready<para />
        /// (But UI is not prepared)
        /// </summary>
        public static event Action OnSystemReady;
        internal static void RaiseSystemReady()
        {
            var osr = OnSystemReady;
            OnSystemReady = null;
            if (osr != null)
                osr();
        }

        /// <summary>
        /// Call on user interfaces are ready
        /// </summary>
        public static event Action OnUserInterfaceReady;
        internal static void RaiseUserInterfaceReady()
        {
            var usr = OnUserInterfaceReady;
            OnUserInterfaceReady = null;
            if (usr != null)
                usr();
        }

        /// <summary>
        /// Call on aplication is exit from user action<para />
        /// (On crash app, this handler won't call!)
        /// </summary>
        public static event Action OnApplicationExit;
        internal static void RaiseApplicationExit()
        {
            var apx = OnApplicationExit;
            OnApplicationExit = null;
            if (apx != null)
                apx();
        }

        /// <summary>
        /// Call on application is exit from user action or crashed
        /// </summary>
        public static event Action OnApplicationFinalize;
        internal static void RaiseApplicationFinalize()
        {
            var apf = OnApplicationFinalize;
            OnApplicationFinalize = null;
            if (apf != null)
                apf();
        }

        #endregion
    }
}
