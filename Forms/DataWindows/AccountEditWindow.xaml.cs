using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class AccountEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _accountId;

        public class PersonItem
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public override string ToString() => FullName ?? "(без имени)";
        }

        public AccountEditWindow(int? id = null)
        {
            InitializeComponent();
            _accountId = id;
            ConfigureWindow();
            LoadPersons();
            if (_accountId.HasValue)
                LoadAccountData();
            AttachFloatingHints();

            if (!_accountId.HasValue)
                LoginTextBox.Focus();
        }

        private void ConfigureWindow()
        {
            bool editMode = _accountId.HasValue;
            Title = editMode ? "Редактирование аккаунта" : "Добавление аккаунта";
            TitleText.Text = Title;

            // В режиме редактирования скрываем поля пароля
            if (editMode)
            {
                IsEditMode.Visibility = Visibility.Collapsed;
            }
        }

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(LoginTextBox, HintLogin, LoginTransform);

            if (!_accountId.HasValue)
            {
                // Для PasswordBox нужна специальная обработка
                PasswordBox.GotFocus += (s, e) => AnimateHintUp(HintPassword, PasswordTransform);
                PasswordBox.LostFocus += (s, e) => UpdatePasswordHintState();
                PasswordBox.PasswordChanged += (s, e) => UpdatePasswordHintState();

                ConfirmPasswordBox.GotFocus += (s, e) => AnimateHintUp(HintConfirmPassword, ConfirmPasswordTransform);
                ConfirmPasswordBox.LostFocus += (s, e) => UpdateConfirmPasswordHintState();
                ConfirmPasswordBox.PasswordChanged += (s, e) => UpdateConfirmPasswordHintState();
            }
        }

        private void AnimateHintUp(TextBlock hint, TranslateTransform transform)
        {
            var anim = new DoubleAnimation
            {
                To = -24,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            transform.BeginAnimation(TranslateTransform.YProperty, anim);
            hint.FontSize = 12;
            hint.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 153, 255));
        }

        private void AnimateHintDown(TextBlock hint, TranslateTransform transform)
        {
            var anim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            transform.BeginAnimation(TranslateTransform.YProperty, anim);
            hint.FontSize = 14;
            hint.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136));
        }

        private void UpdatePasswordHintState()
        {
            if (PasswordBox.Password.Length > 0 || PasswordBox.IsFocused)
                AnimateHintUp(HintPassword, PasswordTransform);
            else
                AnimateHintDown(HintPassword, PasswordTransform);
        }

        private void UpdateConfirmPasswordHintState()
        {
            if (ConfirmPasswordBox.Password.Length > 0 || ConfirmPasswordBox.IsFocused)
                AnimateHintUp(HintConfirmPassword, ConfirmPasswordTransform);
            else
                AnimateHintDown(HintConfirmPassword, ConfirmPasswordTransform);
        }

        private void LoadPersons()
        {
            try
            {
                // Загружаем пользователей без аккаунтов (для создания) или всех (для редактирования)
                DataTable table;
                if (_accountId.HasValue)
                {
                    table = _db.GetPersonsForGrid();
                }
                else
                {
                    table = _db.GetPersonsWithoutAccounts();
                }

                if (table == null || table.Rows.Count == 0)
                {
                    MessageBox.Show("Нет доступных пользователей для создания аккаунта.");
                    PersonComboBox.IsEnabled = false;
                    return;
                }

                var items = new List<PersonItem>();
                foreach (DataRow row in table.Rows)
                {
                    items.Add(new PersonItem
                    {
                        Id = Convert.ToInt32(row["person_id"]),
                        FullName = row["full_name"]?.ToString() ?? "(без имени)"
                    });
                }
                PersonComboBox.ItemsSource = items;
                PersonComboBox.DisplayMemberPath = nameof(PersonItem.FullName);
                PersonComboBox.SelectedValuePath = nameof(PersonItem.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке пользователей:\n" + ex.Message);
                PersonComboBox.IsEnabled = false;
            }
        }

        private void LoadAccountData()
        {
            try
            {
                var table = GetAccountById(_accountId!.Value);
                if (table.Rows.Count == 0) return;

                var row = table.Rows[0];
                PersonComboBox.SelectedValue = Convert.ToInt32(row["person_id"]);
                LoginTextBox.Text = row["login"]?.ToString() ?? "";

                if (row["created_at"] != DBNull.Value)
                {
                    CreatedAtText.Text = Convert.ToDateTime(row["created_at"]).ToString("dd.MM.yyyy HH:mm");
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных аккаунта", ex);
            }
        }

        private DataTable GetAccountById(int id)
        {
            string query = @"SELECT * FROM accounts WHERE account_id = @id";
            return new DbManager().Select(query, new Dictionary<string, object> { { "@id", id } });
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidate(out var accountData))
                return;

            try
            {
                if (_accountId.HasValue)
                {
                    UpdateAccount(accountData);
                    ShowInfo("Аккаунт успешно обновлен");
                }
                else
                {
                    InsertAccount(accountData);
                    ShowInfo("Аккаунт успешно создан");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при сохранении", ex);
            }
        }

        private void InsertAccount(Dictionary<string, object> parameters)
        {
            string query = @"INSERT INTO accounts 
                (person_id, login, password, created_at)
                VALUES 
                (@person_id, @login, @password, NOW())";

            new DbManager().NonQuery(query, parameters);
        }

        private void UpdateAccount(Dictionary<string, object> parameters)
        {
            string query = @"UPDATE accounts SET
                person_id = @person_id,
                login = @login
                WHERE account_id = @account_id";

            new DbManager().NonQuery(query, parameters);
        }

        private bool TryValidate(out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();

            if (PersonComboBox.SelectedValue == null)
                return Fail("Выберите пользователя", PersonComboBox);

            if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
                return Fail("Введите логин", LoginTextBox);

            // Проверка длины login (VARCHAR(50))
            if (LoginTextBox.Text.Trim().Length > 50)
                return Fail("Логин не может быть длиннее 50 символов", LoginTextBox);

            if (!IsLoginUnique(LoginTextBox.Text.Trim(), _accountId))
                return Fail("Этот логин уже занят", LoginTextBox);

            // Для нового аккаунта проверяем пароль
            if (!_accountId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                    return Fail("Введите пароль", PasswordBox);

                if (PasswordBox.Password.Length < 6)
                    return Fail("Пароль должен быть не менее 6 символов", PasswordBox);

                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                    return Fail("Пароли не совпадают", ConfirmPasswordBox);

                // Проверка длины password (VARCHAR(255) для хеша)
                if (PasswordBox.Password.Length > 50) // До хеширования
                    return Fail("Пароль слишком длинный", PasswordBox);
            }

            parameters = new Dictionary<string, object>
            {
                ["@person_id"] = PersonComboBox.SelectedValue,
                ["@login"] = LoginTextBox.Text.Trim()
            };

            if (!_accountId.HasValue)
            {
                parameters["@password"] = HashPassword(PasswordBox.Password);
            }
            else
            {
                parameters["@account_id"] = _accountId.Value;
            }

            return true;
        }

        private bool IsLoginUnique(string login, int? excludeAccountId)
        {
            string query = @"SELECT COUNT(*) FROM accounts WHERE login = @login" +
                          (excludeAccountId.HasValue ? " AND account_id != @account_id" : "");

            var parameters = new Dictionary<string, object> { { "@login", login } };
            if (excludeAccountId.HasValue)
                parameters["@account_id"] = excludeAccountId.Value;

            object result = new DbManager().Scalar(query, parameters);
            return Convert.ToInt32(result) == 0;
        }

        private string HashPassword(string password)
        {
            // Простое хеширование SHA256 для примера
            // В реальном проекте лучше использовать BCrypt или другой надежный алгоритм
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool Fail(string message, UIElement element)
        {
            MessageBox.Show(message, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            element.Focus();
            return false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string title, Exception ex)
        {
            MessageBox.Show($"{title}\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}