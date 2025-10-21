namespace Sample.WPF
{
    using System;
    using System.Windows;

    /// <summary>
    /// WPF application entry class for the sample application.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            var app = new App();
            var wnd = new MainWindow();
            app.Run(wnd);
        }
    }
}
