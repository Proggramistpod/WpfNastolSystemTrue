using System.Windows;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Windows;

namespace WpfNastolSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var config = DatabaseConfig.Load();
            if (config == null)
            {
                var setupWindow = new DatabaseSetupWindow();
                if (setupWindow.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }
                config = DatabaseConfig.Load();
            }
        }
    }
}
