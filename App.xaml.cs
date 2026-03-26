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
        private static bool _isRestarting = false;

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is MySqlException)
            {
                MessageBox.Show("Ошибка подключения к базе данных. Проверьте настройки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowSetupWindow();
            }
            else
            {
                MessageBox.Show($"Произошла ошибка: {e.Exception.Message}\nПриложение будет закрыто.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
            Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}\nПриложение будет закрыто.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (_isRestarting)
                return;

            if (!CheckAndSetupDatabase())
            {
                Shutdown();
                return;
            }

            var authWindow = new MainWindow();
            authWindow.Show();
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
            bool result = setupWindow.ShowDialog() == true;
            if (result)
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
            if (_isRestarting) return;
            _isRestarting = true;

            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(exePath);
            Environment.Exit(0);
        }
    }
}