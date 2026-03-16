using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class GameCopyEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _copyId;

        public class GameItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public override string ToString() => Title ?? "(без названия)";
        }

        // Enum для состояний игры (соответствует ENUM в БД: 'good', 'fair', 'bad')
        public enum GameCondition
        {
            [Description("Отличное")]
            good,

            [Description("Хорошее")]
            fair,

            [Description("Плохое")]
            bad
        }

        public GameCopyEditWindow(int? id = null)
        {
            InitializeComponent();
            _copyId = id;
            ConfigureWindow();
            InitializeConditionComboBox();
            LoadGames();
            if (_copyId.HasValue)
                LoadCopyData();
            AttachFloatingHints();
            GameComboBox.Focus();
        }

        private void ConfigureWindow()
        {
            bool editMode = _copyId.HasValue;
            Title = editMode ? "Редактирование копии игры" : "Добавление копии игры";
            TitleText.Text = Title;
        }

        private void InitializeConditionComboBox()
        {
            ConditionComboBox.Items.Clear();

            // Заполняем ComboBox значениями из enum с описаниями
            foreach (GameCondition condition in Enum.GetValues(typeof(GameCondition)))
            {
                var item = new ComboBoxItem
                {
                    Content = GetEnumDescription(condition),
                    Tag = condition
                };
                ConditionComboBox.Items.Add(item);
            }

            ConditionComboBox.SelectedIndex = 0; // Отличное по умолчанию
        }

        private string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;

            return attribute?.Description ?? value.ToString();
        }

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(InventoryNumberTextBox, HintInventoryNumber, InventoryNumberTransform);
            FloatingHintHelper.Attach(LocationTextBox, HintLocation, LocationTransform);
            FloatingHintHelper.Attach(NotesTextBox, HintNotes, NotesTransform);
        }

        private void LoadGames()
        {
            try
            {
                var table = _db.GetGamesForGrid();
                if (table == null || table.Rows.Count == 0)
                {
                    MessageBox.Show("Нет доступных игр. Сначала добавьте игру.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    GameComboBox.IsEnabled = false;
                    return;
                }

                var items = new List<GameItem>();
                foreach (DataRow row in table.Rows)
                {
                    items.Add(new GameItem
                    {
                        Id = Convert.ToInt32(row["game_id"]),
                        Title = row["title"]?.ToString() ?? "(без названия)"
                    });
                }

                GameComboBox.ItemsSource = items;
                GameComboBox.DisplayMemberPath = nameof(GameItem.Title);
                GameComboBox.SelectedValuePath = nameof(GameItem.Id);

                if (!_copyId.HasValue && items.Count > 0)
                {
                    GameComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при загрузке игр", ex);
                GameComboBox.IsEnabled = false;
            }
        }

        private void LoadCopyData()
        {
            try
            {
                // Используем метод из DataBaseQuery
                var table = _db.GetGameCopyById(_copyId!.Value);

                if (table.Rows.Count == 0)
                {
                    MessageBox.Show("Копия игры не найдена", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    Close();
                    return;
                }

                var row = table.Rows[0];

                // Заполняем данные
                GameComboBox.SelectedValue = Convert.ToInt32(row["game_id"]);
                InventoryNumberTextBox.Text = row["inventory_number"]?.ToString() ?? "";

                if (row["acquired_date"] != DBNull.Value &&
                    DateTime.TryParse(row["acquired_date"].ToString(), out DateTime date))
                {
                    AcquiredDatePicker.SelectedDate = date;
                }

                LocationTextBox.Text = row["location"]?.ToString() ?? "";

                IsAvailableCheckBox.IsChecked = row["is_available"] != DBNull.Value &&
                                               Convert.ToInt32(row["is_available"]) == 1;

                // Загружаем состояние (в БД хранится как 'good', 'fair', 'bad')
                if (row["conditions"] != DBNull.Value)
                {
                    string conditionValue = row["conditions"].ToString() ?? "good";

                    // Парсим строку в enum
                    if (Enum.TryParse<GameCondition>(conditionValue, true, out GameCondition condition))
                    {
                        // Находим соответствующий элемент в ComboBox
                        foreach (ComboBoxItem item in ConditionComboBox.Items)
                        {
                            if (item.Tag is GameCondition cond && cond == condition)
                            {
                                ConditionComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

                NotesTextBox.Text = row["notes"]?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных копии", ex);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidate(out var copyData))
                return;

            try
            {
                if (_copyId.HasValue)
                {
                    copyData["@copy_id"] = _copyId.Value;
                    await System.Threading.Tasks.Task.Run(() => UpdateGameCopy(copyData));
                    ShowInfo("Копия игры успешно обновлена");
                }
                else
                {
                    await System.Threading.Tasks.Task.Run(() => InsertGameCopy(copyData));
                    ShowInfo("Копия игры успешно добавлена");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при сохранении", ex);
            }
        }

        private void InsertGameCopy(Dictionary<string, object> parameters)
        {
            // Используем метод из DataBaseQuery
            _db.InsertGameCopy(parameters);
        }

        private void UpdateGameCopy(Dictionary<string, object> parameters)
        {
            // Используем метод из DataBaseQuery
            _db.UpdateGameCopy(parameters);
        }

        private bool TryValidate(out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();

            // Проверка выбора игры
            if (GameComboBox.SelectedValue == null)
                return Fail("Выберите игру", GameComboBox);

            // Проверка инвентарного номера
            if (string.IsNullOrWhiteSpace(InventoryNumberTextBox.Text))
                return Fail("Введите инвентарный номер", InventoryNumberTextBox);

            string inventoryNumber = InventoryNumberTextBox.Text.Trim();

            if (inventoryNumber.Length > 50)
                return Fail("Инвентарный номер не может быть длиннее 50 символов", InventoryNumberTextBox);

            // Проверка уникальности инвентарного номера (используем метод из DataBaseQuery)
            if (!_db.IsInventoryNumberUnique(inventoryNumber, _copyId))
                return Fail("Инвентарный номер уже существует", InventoryNumberTextBox);

            // Получение выбранного состояния
            GameCondition selectedCondition = GameCondition.good;
            if (ConditionComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is GameCondition condition)
            {
                selectedCondition = condition;
            }

            // Проверка длины location
            string? location = string.IsNullOrWhiteSpace(LocationTextBox.Text) ? null : LocationTextBox.Text.Trim();
            if (location?.Length > 100)
                return Fail("Расположение не может быть длиннее 100 символов", LocationTextBox);

            // Формирование параметров
            parameters = new Dictionary<string, object>
            {
                ["@game_id"] = GameComboBox.SelectedValue,
                ["@inventory_number"] = inventoryNumber,
                ["@acquired_date"] = AcquiredDatePicker.SelectedDate.HasValue
                    ? (object)AcquiredDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value,
                ["@location"] = location ?? (object)DBNull.Value,
                ["@is_available"] = (IsAvailableCheckBox.IsChecked ?? true) ? 1 : 0,
                ["@condition"] = selectedCondition.ToString(), // Сохраняем как 'good', 'fair' или 'bad'
                ["@notes"] = string.IsNullOrWhiteSpace(NotesTextBox.Text)
                    ? DBNull.Value : NotesTextBox.Text.Trim()
            };

            return true;
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
            MessageBox.Show($"{title}\n{ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Обработчики для ограничения длины ввода
        private void InventoryNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InventoryNumberTextBox.Text.Length > 50)
            {
                InventoryNumberTextBox.Text = InventoryNumberTextBox.Text.Substring(0, 50);
                InventoryNumberTextBox.CaretIndex = InventoryNumberTextBox.Text.Length;
            }
        }

        private void LocationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LocationTextBox.Text.Length > 100)
            {
                LocationTextBox.Text = LocationTextBox.Text.Substring(0, 100);
                LocationTextBox.CaretIndex = LocationTextBox.Text.Length;
            }
        }
    }
}