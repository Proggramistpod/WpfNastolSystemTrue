using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        // Соответствие между русскими отображаемыми значениями и английскими значениями ENUM
        private readonly Dictionary<string, string> _conditionMapping = new()
        {
            ["Отличное"] = "good",
            ["Хорошее"] = "fair",
            ["Удовлетворительное"] = "fair", // Если нет точного соответствия, можно использовать fair
            ["Плохое"] = "bad",
            ["Требует ремонта"] = "bad",
            ["Списана"] = "bad"
        };

        // Обратное соответствие для отображения
        private readonly Dictionary<string, string> _russianConditionMapping = new()
        {
            ["good"] = "Отличное",
            ["fair"] = "Хорошее",
            ["bad"] = "Плохое"
        };

        public GameCopyEditWindow(int? id = null)
        {
            InitializeComponent();
            _copyId = id;
            ConfigureWindow();
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

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(InventoryNumberTextBox, HintInventoryNumber, InventoryNumberTransform);
            FloatingHintHelper.Attach(AcquiredDatePicker, HintAcquiredDate, AcquiredDateTransform);
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
                    MessageBox.Show("Нет доступных игр. Сначала добавьте игру.");
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

                // Если редактирование, не выбираем автоматически
                if (!_copyId.HasValue && items.Count > 0)
                {
                    GameComboBox.SelectedIndex = 0; // Выбираем первую игру по умолчанию
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке игр:\n" + ex.Message);
                GameComboBox.IsEnabled = false;
            }
        }

        private void LoadCopyData()
        {
            try
            {
                var table = GetGameCopyById(_copyId!.Value);
                if (table.Rows.Count == 0) return;

                var row = table.Rows[0];
                GameComboBox.SelectedValue = Convert.ToInt32(row["game_id"]);
                SetText(InventoryNumberTextBox, row["inventory_number"]);

                if (row["acquired_date"] != DBNull.Value && row["acquired_date"] != null)
                {
                    if (DateTime.TryParse(row["acquired_date"].ToString(), out DateTime date))
                    {
                        AcquiredDatePicker.SelectedDate = date;
                    }
                }

                SetText(LocationTextBox, row["location"]);

                if (row["is_available"] != DBNull.Value)
                {
                    IsAvailableCheckBox.IsChecked = Convert.ToInt32(row["is_available"]) == 1;
                }

                // Загружаем состояние из ENUM и конвертируем в русский для отображения
                if (row["Condition"] != DBNull.Value && row["Condition"] != null)
                {
                    string conditionValue = row["Condition"].ToString(); // Это будет 'good', 'fair' или 'bad'

                    // Конвертируем английское значение в русское для отображения
                    if (_russianConditionMapping.TryGetValue(conditionValue, out string? russianValue))
                    {
                        // Ищем соответствующий элемент в ComboBox по русскому тексту
                        foreach (ComboBoxItem item in ConditionComboBox.Items)
                        {
                            if (item.Content.ToString() == russianValue)
                            {
                                ConditionComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

                SetText(NotesTextBox, row["notes"]);
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных копии", ex);
            }
        }

        private DataTable GetGameCopyById(int id)
        {
            string query = @"SELECT * FROM game_copies WHERE copy_id = @id";
            return new DbManager().Select(query, new Dictionary<string, object> { { "@id", id } });
        }

        private void SetText(TextBox box, object value)
        {
            box.Text = value == DBNull.Value || value == null ? "" : value.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidate(out var copyData))
                return;

            try
            {
                if (_copyId.HasValue)
                {
                    copyData["@copy_id"] = _copyId.Value;
                    UpdateGameCopy(copyData);
                    ShowInfo("Копия игры успешно обновлена");
                }
                else
                {
                    InsertGameCopy(copyData);
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
            // Используем обратные кавычки для зарезервированного слова Condition
            string query = @"INSERT INTO game_copies 
                (game_id, inventory_number, acquired_date, location, is_available, condition, notes)
                VALUES 
                (@game_id, @inventory_number, @acquired_date, @location, @is_available, @condition, @notes)";

            new DbManager().NonQuery(query, parameters);
        }

        private void UpdateGameCopy(Dictionary<string, object> parameters)
        {
            // Используем обратные кавычки для зарезервированного слова Condition
            string query = @"UPDATE game_copies SET
                game_id = @game_id,
                inventory_number = @inventory_number,
                acquired_date = @acquired_date,
                location = @location,
                is_available = @is_available,
                condition = @condition,
                notes = @notes
                WHERE copy_id = @copy_id";

            new DbManager().NonQuery(query, parameters);
        }

        private bool TryValidate(out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();

            if (GameComboBox.SelectedValue == null)
                return Fail("Выберите игру", GameComboBox);

            if (string.IsNullOrWhiteSpace(InventoryNumberTextBox.Text))
                return Fail("Введите инвентарный номер", InventoryNumberTextBox);

            // Проверка длины inventory_number (VARCHAR(50) в вашей БД)
            if (InventoryNumberTextBox.Text.Trim().Length > 50)
                return Fail("Инвентарный номер не может быть длиннее 50 символов", InventoryNumberTextBox);

            // Проверка уникальности инвентарного номера (опционально)
            if (!IsInventoryNumberUnique(InventoryNumberTextBox.Text.Trim(), _copyId))
                return Fail("Инвентарный номер уже существует", InventoryNumberTextBox);

            // Получаем выбранное русское значение из ComboBox
            string? selectedRussianCondition = ConditionComboBox.SelectedItem != null
                ? ((ComboBoxItem)ConditionComboBox.SelectedItem).Content.ToString()
                : null;

            // Конвертируем в английское значение для ENUM в БД
            string? englishConditionValue = null;
            if (selectedRussianCondition != null && _conditionMapping.TryGetValue(selectedRussianCondition, out string? mappedValue))
            {
                englishConditionValue = mappedValue;
            }

            parameters = new Dictionary<string, object>
            {
                ["@game_id"] = GameComboBox.SelectedValue,
                ["@inventory_number"] = InventoryNumberTextBox.Text.Trim(),
                ["@acquired_date"] = AcquiredDatePicker.SelectedDate.HasValue
                    ? (object)AcquiredDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value,
                ["@location"] = string.IsNullOrWhiteSpace(LocationTextBox.Text)
                    ? DBNull.Value : LocationTextBox.Text.Trim(),
                ["@is_available"] = (IsAvailableCheckBox.IsChecked ?? true) ? 1 : 0, // TINYINT(1)
                ["@condition"] = englishConditionValue,
                ["@notes"] = string.IsNullOrWhiteSpace(NotesTextBox.Text)
                    ? DBNull.Value : NotesTextBox.Text.Trim()
            };

            // Проверка длины location (VARCHAR(100))
            if (parameters["@location"] != DBNull.Value &&
                ((string)parameters["@location"]).Length > 100)
                return Fail("Расположение не может быть длиннее 100 символов", LocationTextBox);

            return true;
        }

        private bool IsInventoryNumberUnique(string inventoryNumber, int? excludeCopyId)
        {
            string query = @"SELECT COUNT(*) FROM game_copies WHERE inventory_number = @inventory_number" +
                          (excludeCopyId.HasValue ? " AND copy_id != @copy_id" : "");

            var parameters = new Dictionary<string, object> { { "@inventory_number", inventoryNumber } };
            if (excludeCopyId.HasValue)
                parameters["@copy_id"] = excludeCopyId.Value;

            object result = new DbManager().Scalar(query, parameters);
            return Convert.ToInt32(result) == 0;
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