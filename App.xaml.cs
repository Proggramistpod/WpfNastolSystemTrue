using System;
using System.Diagnostics;
using System.Windows;
using MySql.Data.MySqlClient;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Windows;

namespace WpfNastolSystem
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (!CheckAndSetupDatabase())
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private bool CheckAndSetupDatabase()
        {
            while (true)
            {
                var config = DatabaseConfig.Load();
                if (config == null)
                {
                    if (!ShowSetupWindow())
                        return false;
                    continue;
                }

                if (TestConnection(config.GetConnectionString()))
                    return true;

                var result = MessageBox.Show(
                    "Не удалось подключиться к базе данных. Проверьте настройки подключения.",
                    "Ошибка подключения",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Error);

                if (result == MessageBoxResult.Cancel)
                    return false;

                if (!ShowSetupWindow())
                    return false;
            }
        }

        private bool ShowSetupWindow()
        {
            var setupWindow = new DatabaseSetupWindow();
            if (setupWindow.ShowDialog() == true)
            {
                RestartApplication();
                return false;
            }
            return false;
        }

        private bool TestConnection(string connectionString)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void RestartApplication()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(exePath);
            Environment.Exit(0);
        }
    }
}