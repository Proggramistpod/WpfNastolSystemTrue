using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class AccountEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _accountId;
        private int? _personId; // ID связанной записи persons
        private bool _isDataChanged = false;

        // Роль обычного пользователя, которую исключаем из выбора
        private const int EXCLUDED_ROLE_ID = 1;

        public class RoleItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public AccountEditWindow(int? id = null)
        {
            InitializeComponent();
            _accountId = id;
            ConfigureWindow();
            LoadRoles();
            AttachFloatingHints();

            if (_accountId.HasValue)
                LoadAccountData();
            else
                FullNameTextBox.Focus();

            Loaded += (s, e) => UpdateAllHints();
        }

        private void ConfigureWindow()
        {
            bool editMode = _accountId.HasValue;
            Title = editMode ? "Редактирование работника" : "Добавление работника";
            TitleText.Text = Title;

            if (editMode)
            {
                // Скрываем поля пароля
                PasswordPanel.Visibility = Visibility.Collapsed;
                ConfirmPasswordPanel.Visibility = Visibility.Collapsed;
                InfoGrid.Visibility = Visibility.Visible;
            }
        }

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(FullNameTextBox, HintFullName, FullNameTransform);
            FloatingHintHelper.Attach(PhoneTextBox, HintPhone, PhoneTransform);
            FloatingHintHelper.Attach(EmailTextBox, HintEmail, EmailTransform);
            FloatingHintHelper.Attach(NotesTextBox, HintNotes, NotesTransform);
            FloatingHintHelper.Attach(LoginTextBox, HintLogin, LoginTransform);

            // DatePicker
            BirthDatePicker.GotFocus += (s, e) => MoveHintUp(HintBirthDate, BirthDateTransform);
            BirthDatePicker.LostFocus += (s, e) => UpdateDatePickerHint();
            BirthDatePicker.SelectedDateChanged += (s, e) => UpdateDatePickerHint();

            // PasswordBoxes (только при создании)
            if (!_accountId.HasValue)
            {
                AttachPasswordBox(PasswordBox, HintPassword, PasswordTransform);
                AttachPasswordBox(ConfirmPasswordBox, HintConfirmPassword, ConfirmPasswordTransform);
            }

            // ComboBox
            RoleComboBox.SelectionChanged += (s, e) => UpdateComboBoxHint();
            RoleComboBox.GotFocus += (s, e) => UpdateComboBoxHint();
            RoleComboBox.LostFocus += (s, e) => UpdateComboBoxHint();

            // Отслеживание изменений
            FullNameTextBox.TextChanged += OnFieldChanged;
            PhoneTextBox.TextChanged += OnFieldChanged;
            EmailTextBox.TextChanged += OnFieldChanged;
            NotesTextBox.TextChanged += OnFieldChanged;
            LoginTextBox.TextChanged += OnFieldChanged;
            BirthDatePicker.SelectedDateChanged += OnFieldChanged;
            IsBannedCheckBox.Checked += OnFieldChanged;
            IsBannedCheckBox.Unchecked += OnFieldChanged;
            RoleComboBox.SelectionChanged += OnFieldChanged;
            if (!_accountId.HasValue)
            {
                PasswordBox.PasswordChanged += OnFieldChanged;
                ConfirmPasswordBox.PasswordChanged += OnFieldChanged;
            }
        }

        private void AttachPasswordBox(PasswordBox pb, TextBlock hint, TranslateTransform transform)
        {
            pb.GotFocus += (s, e) => MoveHintUp(hint, transform);
            pb.LostFocus += (s, e) => UpdatePasswordHint(pb, hint, transform);
            pb.PasswordChanged += (s, e) => UpdatePasswordHint(pb, hint, transform);
        }

        private void MoveHintUp(TextBlock hint, TranslateTransform transform)
        {
            transform.Y = -24;
            hint.FontSize = 12;
            hint.Foreground = (System.Windows.Media.Brush)FindResource("PrimaryBlue");
        }

        private void MoveHintDown(TextBlock hint, TranslateTransform transform)
        {
            transform.Y = 0;
            hint.FontSize = 14;
            hint.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136));
        }

        private void UpdatePasswordHint(PasswordBox pb, TextBlock hint, TranslateTransform transform)
        {
            if (pb.SecurePassword.Length > 0 || pb.IsFocused)
                MoveHintUp(hint, transform);
            else
                MoveHintDown(hint, transform);
        }

        private void UpdateDatePickerHint()
        {
            if (BirthDatePicker.SelectedDate.HasValue)
                MoveHintUp(HintBirthDate, BirthDateTransform);
            else
                MoveHintDown(HintBirthDate, BirthDateTransform);
        }

        private void UpdateComboBoxHint()
        {
            // Для ComboBox подсказка не используется, но можно оставить как есть
        }

        private void UpdateAllHints()
        {
            if (!string.IsNullOrEmpty(FullNameTextBox.Text))
                MoveHintUp(HintFullName, FullNameTransform);
            if (!string.IsNullOrEmpty(PhoneTextBox.Text))
                MoveHintUp(HintPhone, PhoneTransform);
            if (!string.IsNullOrEmpty(EmailTextBox.Text))
                MoveHintUp(HintEmail, EmailTransform);
            if (!string.IsNullOrEmpty(NotesTextBox.Text))
                MoveHintUp(HintNotes, NotesTransform);
            if (!string.IsNullOrEmpty(LoginTextBox.Text))
                MoveHintUp(HintLogin, LoginTransform);
            UpdateDatePickerHint();
            if (!_accountId.HasValue)
            {
                UpdatePasswordHint(PasswordBox, HintPassword, PasswordTransform);
                UpdatePasswordHint(ConfirmPasswordBox, HintConfirmPassword, ConfirmPasswordTransform);
            }
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            _isDataChanged = true;
            SaveButton.IsEnabled = true;
        }

        private void LoadRoles()
        {
            try
            {
                var table = _db.GetRolesForGrid();
                var roles = new List<RoleItem>();

                foreach (DataRow row in table.Rows)
                {
                    int id = Convert.ToInt32(row["role_id"]);
                    if (id == EXCLUDED_ROLE_ID) continue; // исключаем обычных пользователей

                    roles.Add(new RoleItem
                    {
                        Id = id,
                        Name = row["name"]?.ToString() ?? "Без названия"
                    });
                }

                RoleComboBox.ItemsSource = roles;
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки ролей", ex);
                RoleComboBox.IsEnabled = false;
            }
        }

        private void LoadAccountData()
        {
            try
            {
                // Получаем данные аккаунта
                var accountTable = _db.GetAccountById(_accountId!.Value);
                if (accountTable.Rows.Count == 0)
                {
                    MessageBox.Show("Аккаунт не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                var accRow = accountTable.Rows[0];
                _personId = Convert.ToInt32(accRow["person_id"]);
                LoginTextBox.Text = accRow["login"]?.ToString() ?? "";
                CreatedAtText.Text = accRow["created_at"] != DBNull.Value
                    ? Convert.ToDateTime(accRow["created_at"]).ToString("dd.MM.yyyy HH:mm")
                    : "Не указано";

                // Получаем данные персоны
                var personTable = _db.GetPersonById(_personId.Value);
                if (personTable.Rows.Count == 0)
                {
                    MessageBox.Show("Связанный пользователь не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                var perRow = personTable.Rows[0];
                FullNameTextBox.Text = perRow["full_name"]?.ToString() ?? "";
                RoleComboBox.SelectedValue = perRow["role_id"];
                PhoneTextBox.Text = perRow["phone"]?.ToString() ?? "";
                EmailTextBox.Text = perRow["email"]?.ToString() ?? "";
                if (perRow["birth_date"] != DBNull.Value && DateTime.TryParse(perRow["birth_date"].ToString(), out var bd))
                    BirthDatePicker.SelectedDate = bd;
                IsBannedCheckBox.IsChecked = perRow["is_banned"] != DBNull.Value && Convert.ToBoolean(perRow["is_banned"]);
                NotesTextBox.Text = perRow["notes"]?.ToString() ?? "";

                _isDataChanged = false;
                SaveButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных", ex);
            }
        }

        private bool ValidateFields()
        {
            // Роль
            if (RoleComboBox.SelectedValue == null)
                return ShowWarning("Выберите роль", RoleComboBox);

            // ФИО
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                return ShowWarning("Введите ФИО", FullNameTextBox);

            // Телефон
            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                return ShowWarning("Введите номер телефона", PhoneTextBox);
            string cleanPhone = new string(PhoneTextBox.Text.Where(char.IsDigit).ToArray());
            if (cleanPhone.Length < 10 || !cleanPhone.All(char.IsDigit))
                return ShowWarning("Некорректный номер телефона", PhoneTextBox);

            // Email
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                return ShowWarning("Введите email", EmailTextBox);
            if (!IsValidEmail(EmailTextBox.Text))
                return ShowWarning("Некорректный email", EmailTextBox);

            // Дата рождения
            if (!BirthDatePicker.SelectedDate.HasValue)
                return ShowWarning("Укажите дату рождения", BirthDatePicker);
            if (BirthDatePicker.SelectedDate.Value > DateTime.Now)
                return ShowWarning("Дата рождения не может быть в будущем", BirthDatePicker);
            if (BirthDatePicker.SelectedDate.Value < DateTime.Now.AddYears(-120))
                return ShowWarning("Некорректная дата рождения", BirthDatePicker);

            // Логин
            if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
                return ShowWarning("Введите логин", LoginTextBox);
            if (LoginTextBox.Text.Length > 50)
                return ShowWarning("Логин не может быть длиннее 50 символов", LoginTextBox);


            // Для нового аккаунта проверяем пароль
            if (!_accountId.HasValue)
            {
                if (PasswordBox.SecurePassword.Length == 0)
                    return ShowWarning("Введите пароль", PasswordBox);
                if (PasswordBox.SecurePassword.Length < 6)
                    return ShowWarning("Пароль должен быть не менее 6 символов", PasswordBox);
                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                    return ShowWarning("Пароли не совпадают", ConfirmPasswordBox);
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool ShowWarning(string message, UIElement element)
        {
            MessageBox.Show(message, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            element.Focus();
            return false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                if (_accountId.HasValue)
                {
                    // Обновление существующего работника
                    UpdatePerson();
                    UpdateAccount();
                    ShowInfo("Данные обновлены");
                }
                else
                {
                    // Создание нового работника
                    int newPersonId = InsertPerson();
                    InsertAccount(newPersonId);
                    ShowInfo("Работник добавлен");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения", ex);
            }
        }

        private int InsertPerson()
        {
            var parameters = new Dictionary<string, object>
            {
                ["@full_name"] = FullNameTextBox.Text.Trim(),
                ["@role_id"] = RoleComboBox.SelectedValue,
                ["@phone"] = PhoneTextBox.Text.Trim(),
                ["@email"] = EmailTextBox.Text.Trim(),
                ["@birth_date"] = BirthDatePicker.SelectedDate.Value,
                ["@is_banned"] = IsBannedCheckBox.IsChecked ?? false,
                ["@notes"] = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? DBNull.Value : NotesTextBox.Text.Trim()
            };
            return _db.InsertPerson(parameters);
        }

        private void UpdatePerson()
        {
            var parameters = new Dictionary<string, object>
            {
                ["@person_id"] = _personId!.Value,
                ["@full_name"] = FullNameTextBox.Text.Trim(),
                ["@role_id"] = RoleComboBox.SelectedValue,
                ["@phone"] = PhoneTextBox.Text.Trim(),
                ["@email"] = EmailTextBox.Text.Trim(),
                ["@birth_date"] = BirthDatePicker.SelectedDate.Value,
                ["@is_banned"] = IsBannedCheckBox.IsChecked ?? false,
                ["@notes"] = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? DBNull.Value : NotesTextBox.Text.Trim()
            };
            _db.UpdatePerson(parameters);
        }

        private void InsertAccount(int personId)
        {
            var parameters = new Dictionary<string, object>
            {
                ["@person_id"] = personId,
                ["@login"] = LoginTextBox.Text.Trim(),
                ["@password"] = HashPassword(PasswordBox.Password)
            };
            _db.InsertAccount(parameters);
        }

        private void UpdateAccount()
        {
            var parameters = new Dictionary<string, object>
            {
                ["@account_id"] = _accountId!.Value,
                ["@login"] = LoginTextBox.Text.Trim()
                // пароль не меняем
            };
            _db.UpdateAccount(parameters);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataChanged)
            {
                var result = MessageBox.Show("Изменения не сохранены. Закрыть?", "Подтверждение",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }
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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isDataChanged && DialogResult != true)
            {
                var result = MessageBox.Show("Изменения не сохранены. Закрыть?", "Подтверждение",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    e.Cancel = true;
            }
            base.OnClosing(e);
        }
    }
}