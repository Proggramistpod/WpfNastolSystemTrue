using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class SessionEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly DbManager _dbManager = new(); // Добавляем прямой доступ к DbManager
        private readonly int? _sessionId;
        private readonly int? _currentUserId;

        // Вложенные классы для элементов ComboBox
        public class PersonItem
        {
            public int Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public bool IsBanned { get; set; }
            public override string ToString() => FullName;
        }

        public class TableItem
        {
            public int Id { get; set; }
            public int TableNumber { get; set; }
            public int Capacity { get; set; }
            public string Zone { get; set; } = string.Empty;
            public bool IsAvailable { get; set; }
            public string DisplayName => $"Стол {TableNumber} ({Zone}, {Capacity} чел.)";
            public override string ToString() => DisplayName;
        }

        public class PaymentMethodItem
        {
            public string Value { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public override string ToString() => DisplayName;
        }

        public SessionEditWindow(int? id = null, int? currentUserId = null)
        {
            InitializeComponent();

            _sessionId = id;
            _currentUserId = currentUserId;
            ConfigureWindow();
            LoadPersons();
            LoadTables();
            LoadPaymentMethods();

            if (_sessionId.HasValue)
                LoadSessionData();
            else
                SetDefaultValues();

            AttachFloatingHints();
        }

        #region Инициализация

        private void ConfigureWindow()
        {
            bool editMode = _sessionId.HasValue;
            Title = editMode ? "Редактирование сессии" : "Добавление сессии";
            TitleText.Text = Title;
        }

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(StartedDatePicker, HintStartedAt, StartedAtTransform);
            FloatingHintHelper.Attach(StartedHourTextBox, HintStartedHour, StartedHourTransform);
            FloatingHintHelper.Attach(StartedMinuteTextBox, HintStartedMinute, StartedMinuteTransform);
            FloatingHintHelper.Attach(EndedDatePicker, HintEndedAt, EndedAtTransform);
            FloatingHintHelper.Attach(EndedHourTextBox, HintEndedHour, EndedHourTransform);
            FloatingHintHelper.Attach(EndedMinuteTextBox, HintEndedMinute, EndedMinuteTransform);
            FloatingHintHelper.Attach(CostTextBox, HintCost, CostTransform);
            FloatingHintHelper.Attach(NotesTextBox, HintNotes, NotesTransform);
        }

        private void SetDefaultValues()
        {
            StartedDatePicker.SelectedDate = DateTime.Today;
            StartedHourTextBox.Text = DateTime.Now.Hour.ToString("D2");
            StartedMinuteTextBox.Text = "00";
            CostTextBox.Text = "0.00";

            if (_currentUserId.HasValue)
            {
                SelectCurrentUser();
            }
        }

        private void SelectCurrentUser()
        {
            if (!_currentUserId.HasValue || PersonComboBox.ItemsSource == null)
                return;

            var items = PersonComboBox.ItemsSource as IEnumerable<PersonItem>;
            var currentUser = items?.FirstOrDefault(p => p.Id == _currentUserId.Value);
            if (currentUser != null)
                PersonComboBox.SelectedItem = currentUser;
        }

        #endregion

        #region Загрузка данных

        private void LoadPersons()
        {
            try
            {
                var table = _db.GetActivePersons();

                if (table == null || table.Rows.Count == 0)
                {
                    MessageBox.Show("Нет доступных организаторов.");
                    PersonComboBox.IsEnabled = false;
                    return;
                }

                var items = new List<PersonItem>();

                foreach (DataRow row in table.Rows)
                {
                    items.Add(new PersonItem
                    {
                        Id = Convert.ToInt32(row["person_id"]),
                        FullName = row["full_name"]?.ToString() ?? "Без имени",
                        IsBanned = false
                    });
                }

                PersonComboBox.ItemsSource = items;
                PersonComboBox.DisplayMemberPath = "FullName";
                PersonComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке организаторов:\n" + ex.Message);
                PersonComboBox.IsEnabled = false;
            }
        }

        private void LoadTables()
        {
            try
            {
                var table = _db.GetTablesForGrid();

                if (table == null || table.Rows.Count == 0)
                {
                    MessageBox.Show("Нет доступных столов.");
                    TableComboBox.IsEnabled = false;
                    return;
                }

                var items = new List<TableItem>();

                foreach (DataRow row in table.Rows)
                {
                    items.Add(new TableItem
                    {
                        Id = Convert.ToInt32(row["table_id"]),
                        TableNumber = Convert.ToInt32(row["table_number"]),
                        Capacity = Convert.ToInt32(row["capacity"]),
                        Zone = row["zone"]?.ToString() ?? "Без зоны",
                        IsAvailable = Convert.ToBoolean(row["is_available"])
                    });
                }

                TableComboBox.ItemsSource = items;
                TableComboBox.DisplayMemberPath = "DisplayName";
                TableComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке столов:\n" + ex.Message);
                TableComboBox.IsEnabled = false;
            }
        }

        private void LoadPaymentMethods()
        {
            var methods = new List<PaymentMethodItem>
            {
                new PaymentMethodItem { Value = "cash", DisplayName = "Наличные" },
                new PaymentMethodItem { Value = "card", DisplayName = "Банковская карта" },
                new PaymentMethodItem { Value = "online", DisplayName = "Онлайн оплата" },
                new PaymentMethodItem { Value = "bonus", DisplayName = "Бонусы" }
            };

            PaymentMethodComboBox.ItemsSource = methods;
            PaymentMethodComboBox.SelectedValuePath = "Value";
            PaymentMethodComboBox.DisplayMemberPath = "DisplayName";
            PaymentMethodComboBox.SelectedIndex = 0;
        }

        private void LoadSessionData()
        {
            try
            {
                var table = _db.GetSessionById(_sessionId!.Value);
                if (table.Rows.Count == 0) return;

                var row = table.Rows[0];

                // Организатор
                if (row["person_id"] != DBNull.Value)
                    PersonComboBox.SelectedValue = Convert.ToInt32(row["person_id"]);

                // Стол
                if (row["table_id"] != DBNull.Value)
                    TableComboBox.SelectedValue = Convert.ToInt32(row["table_id"]);

                // Дата и время начала
                if (row["started_at"] != DBNull.Value)
                {
                    DateTime startedAt = Convert.ToDateTime(row["started_at"]);
                    StartedDatePicker.SelectedDate = startedAt.Date;
                    StartedHourTextBox.Text = startedAt.Hour.ToString("D2");
                    StartedMinuteTextBox.Text = startedAt.Minute.ToString("D2");
                }

                // Дата и время окончания
                if (row["ended_at"] != DBNull.Value)
                {
                    DateTime endedAt = Convert.ToDateTime(row["ended_at"]);
                    EndedDatePicker.SelectedDate = endedAt.Date;
                    EndedHourTextBox.Text = endedAt.Hour.ToString("D2");
                    EndedMinuteTextBox.Text = endedAt.Minute.ToString("D2");
                    IsActiveSessionCheckBox.IsChecked = false;
                }
                else
                {
                    IsActiveSessionCheckBox.IsChecked = true;
                }

                // Стоимость
                if (row["cost"] != DBNull.Value)
                    CostTextBox.Text = Convert.ToDecimal(row["cost"]).ToString("F2");

                // Оплата
                if (row["paid"] != DBNull.Value)
                    PaidCheckBox.IsChecked = Convert.ToBoolean(row["paid"]);

                // Способ оплаты
                if (row["payment_method"] != DBNull.Value)
                    PaymentMethodComboBox.SelectedValue = row["payment_method"].ToString();

                // Примечания
                if (row["notes"] != DBNull.Value)
                    NotesTextBox.Text = row["notes"].ToString();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных сессии", ex);
            }
        }

        #endregion

        #region Обработчики событий

        private void IsActiveSessionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            bool isActive = IsActiveSessionCheckBox.IsChecked == true;

            EndedDatePicker.IsEnabled = !isActive;
            EndedHourTextBox.IsEnabled = !isActive;
            EndedMinuteTextBox.IsEnabled = !isActive;

            if (isActive)
            {
                EndedDatePicker.SelectedDate = null;
                EndedHourTextBox.Text = "";
                EndedMinuteTextBox.Text = "";
            }
        }

        #endregion

        #region Сохранение

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidate(out var sessionData))
                return;

            try
            {
                if (_sessionId.HasValue)
                {
                    sessionData["@session_id"] = _sessionId.Value;
                    UpdateSession(sessionData);
                    ShowInfo("Сессия успешно обновлена");
                }
                else
                {
                    InsertSession(sessionData);
                    ShowInfo("Сессия успешно добавлена");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при сохранении", ex);
            }
        }

        private bool TryValidate(out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();

            // Проверка организатора
            if (PersonComboBox.SelectedValue == null)
                return Fail("Выберите организатора", PersonComboBox);

            // Проверка стола
            if (TableComboBox.SelectedValue == null)
                return Fail("Выберите стол", TableComboBox);

            // Проверка даты начала
            if (!StartedDatePicker.SelectedDate.HasValue)
                return Fail("Выберите дату начала", StartedDatePicker);

            // Проверка времени начала
            if (!TryParseTime(StartedHourTextBox.Text, StartedMinuteTextBox.Text, 0, 23, 0, 59, out int startHour, out int startMinute))
                return Fail("Некорректное время начала (часы: 0-23, минуты: 0-59)", StartedHourTextBox);

            DateTime startedAt = StartedDatePicker.SelectedDate.Value.Date
                .AddHours(startHour)
                .AddMinutes(startMinute);

            // Проверка времени окончания (если указано)
            DateTime? endedAt = null;
            if (IsActiveSessionCheckBox.IsChecked == false)
            {
                if (!EndedDatePicker.SelectedDate.HasValue)
                    return Fail("Выберите дату окончания", EndedDatePicker);

                if (!TryParseTime(EndedHourTextBox.Text, EndedMinuteTextBox.Text, 0, 23, 0, 59, out int endHour, out int endMinute))
                    return Fail("Некорректное время окончания", EndedHourTextBox);

                endedAt = EndedDatePicker.SelectedDate.Value.Date
                    .AddHours(endHour)
                    .AddMinutes(endMinute);

                if (endedAt <= startedAt)
                    return Fail("Время окончания должно быть позже времени начала", EndedDatePicker);
            }

            // Проверка стоимости
            if (!TryParseDecimal(CostTextBox.Text, 0, 100000, out decimal cost))
                return Fail("Некорректная стоимость (0-100000)", CostTextBox);

            parameters = new Dictionary<string, object>
            {
                ["@person_id"] = PersonComboBox.SelectedValue,
                ["@table_id"] = TableComboBox.SelectedValue,
                ["@started_at"] = startedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["@ended_at"] = endedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                ["@cost"] = cost,
                ["@paid"] = PaidCheckBox.IsChecked == true ? 1 : 0,
                ["@payment_method"] = PaidCheckBox.IsChecked == true && PaymentMethodComboBox.SelectedValue != null
                    ? PaymentMethodComboBox.SelectedValue.ToString()
                    : DBNull.Value,
                ["@created_by"] = _currentUserId,
                ["@notes"] = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? DBNull.Value : NotesTextBox.Text.Trim()
            };

            return true;
        }

        private void InsertSession(Dictionary<string, object> parameters)
        {
            string query = @"
                INSERT INTO sessions 
                (person_id, table_id, started_at, ended_at, cost, paid, payment_method, created_by, notes)
                VALUES 
                (@person_id, @table_id, @started_at, @ended_at, @cost, @paid, @payment_method, @created_by, @notes)";

            _dbManager.NonQuery(query, parameters); // Используем _dbManager.NonQuery
        }

        private void UpdateSession(Dictionary<string, object> parameters)
        {
            string query = @"
                UPDATE sessions SET
                    person_id = @person_id,
                    table_id = @table_id,
                    started_at = @started_at,
                    ended_at = @ended_at,
                    cost = @cost,
                    paid = @paid,
                    payment_method = @payment_method,
                    notes = @notes
                WHERE session_id = @session_id";

            _dbManager.NonQuery(query, parameters); // Используем _dbManager.NonQuery
        }

        #endregion

        #region Валидация

        private bool TryParseTime(string hourText, string minuteText,
            int minHour, int maxHour, int minMinute, int maxMinute,
            out int hour, out int minute)
        {
            hour = 0;
            minute = 0;

            if (!int.TryParse(hourText, out hour) || hour < minHour || hour > maxHour)
                return false;

            if (!int.TryParse(minuteText, out minute) || minute < minMinute || minute > maxMinute)
                return false;

            return true;
        }

        private bool TryParseDecimal(string text, decimal min, decimal max, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(text)) return false;

            text = text.Replace(",", ".");
            if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return false;

            return value >= min && value <= max;
        }

        private bool Fail(string message, UIElement element)
        {
            MessageBox.Show(message, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            element.Focus();
            return false;
        }

        #endregion

        #region Вспомогательные методы

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

        #endregion
    }

    // Extension method для FloatingHintHelper с DatePicker
    public static class FloatingHintHelperExtensions
    {
        public static void Attach(DatePicker datePicker, TextBlock hint, TranslateTransform transform)
        {
            datePicker.GotFocus += (s, e) => AnimateUp(hint, transform);
            datePicker.LostFocus += (s, e) => UpdateState(datePicker, hint, transform);
            datePicker.SelectedDateChanged += (s, e) => UpdateState(datePicker, hint, transform);

            UpdateState(datePicker, hint, transform);
        }

        private static void UpdateState(DatePicker dp, TextBlock hint, TranslateTransform transform)
        {
            bool hasText = dp.SelectedDate.HasValue;
            bool isFocused = dp.IsFocused;

            if (hasText || isFocused)
                AnimateUp(hint, transform);
            else
                AnimateDown(hint, transform);
        }

        private static void AnimateUp(TextBlock hint, TranslateTransform transform)
        {
            var anim = new DoubleAnimation
            {
                To = -24,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(TranslateTransform.YProperty, anim);
            hint.FontSize = 12;
            hint.Foreground = new SolidColorBrush(Color.FromRgb(51, 153, 255));
        }

        private static void AnimateDown(TextBlock hint, TranslateTransform transform)
        {
            var anim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(TranslateTransform.YProperty, anim);
            hint.FontSize = 14;
            hint.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
        }
    }
}