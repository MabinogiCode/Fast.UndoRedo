namespace Sample.WPF
{
    using System;
    using System.Windows;

    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new App();
            var wnd = new MainWindow();
            app.Run(wnd);
        }
    }
}
