using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class PersonEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _personId;
        private bool _isDataChanged = false;

        // Фиксированная роль для обычных пользователей (ID = 1)
        private const int DEFAULT_ROLE_ID = 1;

        public PersonEditWindow(int? id = null)
        {
            InitializeComponent();
            _personId = id;
            ConfigureWindow();
            AttachFloatingHints();

            if (_personId.HasValue)
                LoadPersonData();
            else
                FullNameTextBox.Focus();

            Loaded += (s, e) => UpdateAllHints();
        }

        private void ConfigureWindow()
        {
            bool editMode = _personId.HasValue;
            Title = editMode ? "Редактирование пользователя" : "Добавление пользователя";
            TitleText.Text = Title;

            if (editMode)
                InfoGrid.Visibility = Visibility.Visible;
        }

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(FullNameTextBox, HintFullName, FullNameTransform);
            FloatingHintHelper.Attach(PhoneTextBox, HintPhone, PhoneTransform);
            FloatingHintHelper.Attach(EmailTextBox, HintEmail, EmailTransform);
            FloatingHintHelper.Attach(NotesTextBox, HintNotes, NotesTransform);

            // Для DatePicker своя логика
            BirthDatePicker.GotFocus += (s, e) => MoveHintUp(HintBirthDate, BirthDateTransform);
            BirthDatePicker.LostFocus += (s, e) => UpdateDatePickerHint();
            BirthDatePicker.SelectedDateChanged += (s, e) => UpdateDatePickerHint();

            // Отслеживание изменений
            FullNameTextBox.TextChanged += OnFieldChanged;
            PhoneTextBox.TextChanged += OnFieldChanged;
            EmailTextBox.TextChanged += OnFieldChanged;
            NotesTextBox.TextChanged += OnFieldChanged;
            BirthDatePicker.SelectedDateChanged += OnFieldChanged;
            IsBannedCheckBox.Checked += OnFieldChanged;
            IsBannedCheckBox.Unchecked += OnFieldChanged;
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

        private void UpdateDatePickerHint()
        {
            if (BirthDatePicker.SelectedDate.HasValue)
                MoveHintUp(HintBirthDate, BirthDateTransform);
            else
                MoveHintDown(HintBirthDate, BirthDateTransform);
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
            UpdateDatePickerHint();
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            _isDataChanged = true;
            SaveButton.IsEnabled = true;
        }

        private void LoadPersonData()
        {
            try
            {
                var table = _db.GetPersonById(_personId!.Value);
                if (table.Rows.Count == 0)
                {
                    MessageBox.Show("Пользователь не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                var row = table.Rows[0];
                FullNameTextBox.Text = row["full_name"]?.ToString() ?? "";
                PhoneTextBox.Text = row["phone"]?.ToString() ?? "";
                EmailTextBox.Text = row["email"]?.ToString() ?? "";
                if (row["birth_date"] != DBNull.Value && DateTime.TryParse(row["birth_date"].ToString(), out var bd))
                    BirthDatePicker.SelectedDate = bd;
                IsBannedCheckBox.IsChecked = row["is_banned"] != DBNull.Value && Convert.ToBoolean(row["is_banned"]);
                NotesTextBox.Text = row["notes"]?.ToString() ?? "";
                RegisteredAtText.Text = row["registered_at"] != DBNull.Value
                    ? Convert.ToDateTime(row["registered_at"]).ToString("dd.MM.yyyy HH:mm")
                    : "Не указано";

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

            // Уникальность (только для новых или если изменилось)
            if (!_db.IsPhoneUnique(PhoneTextBox.Text, _personId))
                return ShowWarning("Этот телефон уже используется", PhoneTextBox);
            if (!_db.IsEmailUnique(EmailTextBox.Text, _personId))
                return ShowWarning("Этот email уже используется", EmailTextBox);

            // Дата рождения
            if (!BirthDatePicker.SelectedDate.HasValue)
                return ShowWarning("Укажите дату рождения", BirthDatePicker);
            if (BirthDatePicker.SelectedDate.Value > DateTime.Now)
                return ShowWarning("Дата рождения не может быть в будущем", BirthDatePicker);
            if (BirthDatePicker.SelectedDate.Value < DateTime.Now.AddYears(-120))
                return ShowWarning("Некорректная дата рождения", BirthDatePicker);

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

            var parameters = new Dictionary<string, object>
            {
                ["@full_name"] = FullNameTextBox.Text.Trim(),
                ["@role_id"] = DEFAULT_ROLE_ID,
                ["@phone"] = PhoneTextBox.Text.Trim(),
                ["@email"] = EmailTextBox.Text.Trim(),
                ["@birth_date"] = BirthDatePicker.SelectedDate.Value,
                ["@is_banned"] = IsBannedCheckBox.IsChecked ?? false,
                ["@notes"] = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? DBNull.Value : NotesTextBox.Text.Trim()
            };

            try
            {
                if (_personId.HasValue)
                {
                    parameters["@person_id"] = _personId.Value;
                    _db.UpdatePerson(parameters);
                    ShowInfo("Данные обновлены");
                }
                else
                {
                    _db.InsertPerson(parameters);
                    ShowInfo("Пользователь добавлен");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения", ex);
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