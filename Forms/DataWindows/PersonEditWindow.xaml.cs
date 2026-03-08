using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;
using static WpfNastolSystem.Forms.Edit.GameEditWindow;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class PersonEditWindow : Window
    {
        private readonly DataBaseQuery _db = new DataBaseQuery();
        private readonly int? _personId;
        private bool _isDataChanged = false;
        private Dictionary<int, string> _roles = new Dictionary<int, string>();

        public PersonEditWindow(int? id = null)
        {
            InitializeComponent();

            _personId = id;
            ConfigureWindow();
            LoadRoles();
            AttachFloatingHints();

            if (id.HasValue)
            {
                LoadPersonData(id.Value);
            }

            // Устанавливаем фокус на первое поле после загрузки
            Loaded += (s, e) => FullNameTextBox.Focus();
            RoleComboBox.SelectionChanged += (s, e) => UpdateRoleHint();
            RoleComboBox.GotFocus += (s, e) => UpdateRoleHint();
            RoleComboBox.LostFocus += (s, e) => UpdateRoleHint();
            Loaded += (s, e) => UpdateRoleHint();
        }

        #region Инициализация

        private void ConfigureWindow()
        {
            bool editMode = _personId.HasValue;
            Title = editMode ? "Редактирование пользователя" : "Добавление пользователя";
            TitleText.Text = Title;
        }

        private void AttachFloatingHints()
        {
            // Обычные TextBox
            FloatingHintHelper.Attach(FullNameTextBox, HintFullName, FullNameTransform);
            FloatingHintHelper.Attach(PhoneTextBox, HintPhone, PhoneTransform);
            FloatingHintHelper.Attach(EmailTextBox, HintEmail, EmailTransform);
            FloatingHintHelper.Attach(NotesTextBox, HintNotes, NotesTransform);

            // Для ComboBox (ручная обработка, так как FloatingHintHelper не поддерживает ComboBox)
            RoleComboBox.LostFocus += (s, e) => UpdateComboBoxHintState();
            RoleComboBox.SelectionChanged += (s, e) => UpdateComboBoxHintState();

            // Для DatePicker (ручная обработка)
            BirthDatePicker.GotFocus += (s, e) => MoveHintUp(HintBirthDate, BirthDateTransform);
            BirthDatePicker.LostFocus += (s, e) => UpdateDatePickerHintState();
            BirthDatePicker.SelectedDateChanged += (s, e) => UpdateDatePickerHintState();

            // Подписываемся на изменения данных
            FullNameTextBox.TextChanged += Field_TextChanged;
            PhoneTextBox.TextChanged += Field_TextChanged;
            EmailTextBox.TextChanged += Field_TextChanged;
            NotesTextBox.TextChanged += Field_TextChanged;
            RoleComboBox.SelectionChanged += Field_TextChanged;
            BirthDatePicker.SelectedDateChanged += Field_TextChanged;
            IsBannedCheckBox.Checked += Field_TextChanged;
            IsBannedCheckBox.Unchecked += Field_TextChanged;

            // Устанавливаем начальное состояние подсказок
            UpdateComboBoxHintState();
            UpdateDatePickerHintState();
        }

        private void MoveHintUp(TextBlock hint, System.Windows.Media.TranslateTransform transform)
        {
            transform.Y = -24;
            hint.FontSize = 12;
            hint.Foreground = (System.Windows.Media.Brush)FindResource("PrimaryBlue");
        }

        private void MoveHintDown(TextBlock hint, System.Windows.Media.TranslateTransform transform)
        {
            transform.Y = 0;
            hint.FontSize = 14;
            hint.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136));
        }

        private void UpdateComboBoxHintState()
        {
        
        }

        private void UpdateDatePickerHintState()
        {
            if (BirthDatePicker.SelectedDate.HasValue)
                MoveHintUp(HintBirthDate, BirthDateTransform);
            else
                MoveHintDown(HintBirthDate, BirthDateTransform);
        }

        #endregion
        private void UpdateRoleHint()
        {

        }

        #region Загрузка данных

        private void LoadRoles()
        {
            try
            {
                DataTable rolesTable = _db.GetRolesForGrid();

                var rolesList = new List<RoleItem>();

                foreach (DataRow row in rolesTable.Rows)
                {
                    rolesList.Add(new RoleItem
                    {
                        Id = Convert.ToInt32(row["role_id"]),
                        Name = row["name"]?.ToString() ?? "Без названия"
                    });
                }

                RoleComboBox.ItemsSource = rolesList;
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки ролей", ex);
                RoleComboBox.IsEnabled = false;
            }
        }

        private void LoadPersonData(int personId)
        {
            try
            {
                DataTable personData = _db.GetPersonById(personId);

                if (personData.Rows.Count == 0)
                {
                    MessageBox.Show("Пользователь не найден",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                DataRow row = personData.Rows[0];

                // Заполняем поля
                FullNameTextBox.Text = row["full_name"]?.ToString() ?? "";

                if (row["role_id"] != DBNull.Value)
                    RoleComboBox.SelectedValue = Convert.ToInt32(row["role_id"]);

                PhoneTextBox.Text = row["phone"]?.ToString() ?? "";
                EmailTextBox.Text = row["email"]?.ToString() ?? "";

                if (row["birth_date"] != DBNull.Value)
                {
                    if (DateTime.TryParse(row["birth_date"].ToString(), out DateTime birthDate))
                        BirthDatePicker.SelectedDate = birthDate;
                }

                IsBannedCheckBox.IsChecked = row["is_banned"] != DBNull.Value && Convert.ToBoolean(row["is_banned"]);
                NotesTextBox.Text = row["notes"]?.ToString() ?? "";

                // Информация о регистрации
                RegisteredAtText.Text = row["registered_at"] != DBNull.Value
                    ? Convert.ToDateTime(row["registered_at"]).ToString("dd.MM.yyyy HH:mm")
                    : "Не указано";


                _isDataChanged = false;
                SaveButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных пользователя", ex);
            }
        }

        #endregion

        #region Валидация

        private bool ValidateFields()
        {
            if (!BirthDatePicker.SelectedDate.HasValue)
            {
                ShowWarning("Дата рождения является обязательным полем", BirthDatePicker);
                return false;
            }

            if (BirthDatePicker.SelectedDate.Value > DateTime.Now)
            {
                ShowWarning("Дата рождения не может быть в будущем", BirthDatePicker);
                return false;
            }

            if (BirthDatePicker.SelectedDate.Value < DateTime.Now.AddYears(-120))
            {
                ShowWarning("Некорректная дата рождения", BirthDatePicker);
                return false;
            }
            // ФИО
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                ShowWarning("Поле 'ФИО' обязательно для заполнения", FullNameTextBox);
                return false;
            }

            if (FullNameTextBox.Text.Length < 3)
            {
                ShowWarning("ФИО должно содержать минимум 3 символа", FullNameTextBox);
                return false;
            }

            // Роль
            if (RoleComboBox.SelectedValue == null)
            {
                ShowWarning("Выберите роль пользователя", RoleComboBox);
                return false;
            }

            // ===== Email (обязательное) =====
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                ShowWarning("Email является обязательным полем", EmailTextBox);
                return false;
            }

            if (!IsValidEmail(EmailTextBox.Text))
            {
                ShowWarning("Введен некорректный email адрес", EmailTextBox);
                return false;
            }

            if (!_db.IsEmailUnique(EmailTextBox.Text, _personId))
            {
                ShowWarning("Пользователь с таким email уже существует", EmailTextBox);
                return false;
            }

            // ===== Телефон (обязательное) =====
            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                ShowWarning("Номер телефона является обязательным полем", PhoneTextBox);
                return false;
            }

            string phone = CleanPhoneNumber(PhoneTextBox.Text);

            if (phone.Length < 10 || !phone.All(char.IsDigit))
            {
                ShowWarning("Введен некорректный номер телефона\nФормат: +7 (999) 123-45-67", PhoneTextBox);
                return false;
            }

            if (!_db.IsPhoneUnique(PhoneTextBox.Text, _personId))
            {
                ShowWarning("Пользователь с таким телефоном уже существует", PhoneTextBox);
                return false;
            }

            // Дата рождения
            if (BirthDatePicker.SelectedDate.HasValue)
            {
                if (BirthDatePicker.SelectedDate.Value > DateTime.Now)
                {
                    ShowWarning("Дата рождения не может быть в будущем", BirthDatePicker);
                    return false;
                }

                if (BirthDatePicker.SelectedDate.Value < DateTime.Now.AddYears(-120))
                {
                    ShowWarning("Некорректная дата рождения", BirthDatePicker);
                    return false;
                }
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string CleanPhoneNumber(string phone)
        {
            return new string(phone.Where(char.IsDigit).ToArray());
        }

        #endregion

        #region Сохранение

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "@full_name", FullNameTextBox.Text.Trim() },
                    { "@role_id", RoleComboBox.SelectedValue },
                    { "@phone", string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? DBNull.Value : PhoneTextBox.Text.Trim() },
                    { "@email", string.IsNullOrWhiteSpace(EmailTextBox.Text) ? DBNull.Value : EmailTextBox.Text.Trim() },
                    { "@birth_date", BirthDatePicker.SelectedDate ?? (object)DBNull.Value },
                    { "@is_banned", IsBannedCheckBox.IsChecked ?? false },
                    { "@notes", string.IsNullOrWhiteSpace(NotesTextBox.Text) ? DBNull.Value : NotesTextBox.Text.Trim() }
                };

                if (_personId.HasValue)
                {
                    parameters.Add("@person_id", _personId.Value);
                    _db.UpdatePerson(parameters);
                    ShowInfo("Данные пользователя успешно обновлены");
                }
                else
                {
                    _db.InsertPerson(parameters);
                    ShowInfo("Пользователь успешно добавлен");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка сохранения данных", ex);
            }
        }

        #endregion

        #region Вспомогательные методы

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataChanged)
            {
                var result = MessageBox.Show("Изменения не сохранены. Закрыть окно?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DialogResult = false;
                    Close();
                }
            }
            else
            {
                DialogResult = false;
                Close();
            }
        }

        private void Field_TextChanged(object sender, EventArgs e)
        {
            _isDataChanged = true;
            SaveButton.IsEnabled = true;
        }

        private void ShowWarning(string message, UIElement element)
        {
            MessageBox.Show(message, "Предупреждение",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            element.Focus();
        }

        private void ShowError(string title, Exception ex)
        {
            MessageBox.Show($"{title}:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isDataChanged && DialogResult != true)
            {
                var result = MessageBox.Show("Изменения не сохранены. Закрыть окно?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
            base.OnClosing(e);
        }

        #endregion
    }
}