using System;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Windows
{
    public partial class DatabaseSetupWindow : Window
    {
        public DatabaseSetupWindow()
        {
            InitializeComponent();

            FloatingHintHelper.Attach(tbServer, HintServer, ServerTransform);
            FloatingHintHelper.Attach(tbDatabase, HintDatabase, DatabaseTransform);
            FloatingHintHelper.Attach(tbUser, HintUser, UserTransform);
            FloatingHintHelper.Attach(pbPassword, HintPassword, PasswordTransform);

            var existing = DatabaseConfig.Load();
            if (existing != null)
            {
                tbServer.Text = existing.Server;
                tbDatabase.Text = existing.Database;
                tbUser.Text = existing.UserId;
                pbPassword.Password = existing.Password;
            }
            else
            {
                // Значения по умолчанию
                tbServer.Text = "localhost";
                tbDatabase.Text = "nastolclub";
                tbUser.Text = "root";
            }
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            string connString = BuildConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();
                    MessageBox.Show("Подключение успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    tbError.Text = "";
                }
            }
            catch (Exception ex)
            {
                tbError.Text = $"Ошибка подключения: {ex.Message}";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbServer.Text) ||
                string.IsNullOrWhiteSpace(tbDatabase.Text) ||
                string.IsNullOrWhiteSpace(tbUser.Text))
            {
                tbError.Text = "Заполните сервер, базу данных и логин.";
                return;
            }

            // Сохраняем настройки
            var config = new DatabaseConfig
            {
                Server = tbServer.Text.Trim(),
                Database = tbDatabase.Text.Trim(),
                UserId = tbUser.Text.Trim(),
                Password = pbPassword.Password,
                CharSet = "utf8mb4"
            };
            config.Save();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private string BuildConnectionString()
        {
            return $"Server={tbServer.Text.Trim()};Database={tbDatabase.Text.Trim()};Uid={tbUser.Text.Trim()};Pwd={pbPassword.Password};CharSet=utf8mb4;";
        }
    }
}