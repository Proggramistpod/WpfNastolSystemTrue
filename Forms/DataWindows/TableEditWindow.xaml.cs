using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class TableEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _tableId;

        public TableEditWindow(int? id = null)
        {
            InitializeComponent();
            _tableId = id;
            ConfigureWindow();
            if (_tableId.HasValue)
                LoadTableData();
            AttachFloatingHints();
            TableNumberTextBox.Focus();
        }

        private void ConfigureWindow()
        {
            bool editMode = _tableId.HasValue;
            Title = editMode ? "Редактирование стола" : "Добавление стола";
            TitleText.Text = Title;
        }

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(TableNumberTextBox, HintTableNumber, TableNumberTransform);
            FloatingHintHelper.Attach(CapacityTextBox, HintCapacity, CapacityTransform);
            FloatingHintHelper.Attach(ZoneTextBox, HintZone, ZoneTransform);
            FloatingHintHelper.Attach(NotesTextBox, HintNotes, NotesTransform);
        }

        private void LoadTableData()
        {
            try
            {
                var table = GetTableById(_tableId!.Value);
                if (table.Rows.Count == 0) return;

                var row = table.Rows[0];
                SetText(TableNumberTextBox, row["table_number"]);
                SetText(CapacityTextBox, row["capacity"]);
                SetText(ZoneTextBox, row["zone"]);

                if (row["is_available"] != DBNull.Value)
                {
                    IsAvailableCheckBox.IsChecked = Convert.ToBoolean(row["is_available"]);
                }

                SetText(NotesTextBox, row["notes"]);
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных стола", ex);
            }
        }

        private DataTable GetTableById(int id)
        {
            string query = @"SELECT * FROM tables WHERE table_id = @id";
            return new DbManager().Select(query, new Dictionary<string, object> { { "@id", id } });
        }

        private void SetText(TextBox box, object value)
        {
            box.Text = value == DBNull.Value || value == null ? "" : value.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidate(out var tableData))
                return;

            try
            {
                if (_tableId.HasValue)
                {
                    tableData["@table_id"] = _tableId.Value;
                    UpdateTable(tableData);
                    ShowInfo("Стол успешно обновлен");
                }
                else
                {
                    InsertTable(tableData);
                    ShowInfo("Стол успешно добавлен");
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка при сохранении", ex);
            }
        }

        private void InsertTable(Dictionary<string, object> parameters)
        {
            string query = @"INSERT INTO tables 
                (table_number, capacity, zone, is_available, notes)
                VALUES 
                (@table_number, @capacity, @zone, @is_available, @notes)";

            new DbManager().NonQuery(query, parameters);
        }

        private void UpdateTable(Dictionary<string, object> parameters)
        {
            string query = @"UPDATE tables SET
                table_number = @table_number,
                capacity = @capacity,
                zone = @zone,
                is_available = @is_available,
                notes = @notes
                WHERE table_id = @table_id";

            new DbManager().NonQuery(query, parameters);
        }

        private bool TryValidate(out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();

            if (string.IsNullOrWhiteSpace(TableNumberTextBox.Text))
                return Fail("Введите номер стола", TableNumberTextBox);

            if (!TryParseInt(TableNumberTextBox.Text, 1, 999, out int tableNumber))
                return Fail("Некорректный номер стола", TableNumberTextBox);

            if (!TryParseInt(CapacityTextBox.Text, 1, 20, out int capacity))
                return Fail("Вместимость должна быть от 1 до 20 человек", CapacityTextBox);

            // Проверка длины zone (VARCHAR(50))
            if (!string.IsNullOrWhiteSpace(ZoneTextBox.Text) && ZoneTextBox.Text.Trim().Length > 50)
                return Fail("Название зоны не может быть длиннее 50 символов", ZoneTextBox);

            // Проверка длины notes (VARCHAR(255))
            if (!string.IsNullOrWhiteSpace(NotesTextBox.Text) && NotesTextBox.Text.Trim().Length > 255)
                return Fail("Примечания не могут быть длиннее 255 символов", NotesTextBox);

            parameters = new Dictionary<string, object>
            {
                ["@table_number"] = tableNumber,
                ["@capacity"] = capacity,
                ["@zone"] = string.IsNullOrWhiteSpace(ZoneTextBox.Text)
                    ? DBNull.Value : ZoneTextBox.Text.Trim(),
                ["@is_available"] = (IsAvailableCheckBox.IsChecked ?? true) ? 1 : 0, // TINYINT(1)
                ["@notes"] = string.IsNullOrWhiteSpace(NotesTextBox.Text)
                    ? DBNull.Value : NotesTextBox.Text.Trim()
            };

            return true;
        }

        private bool TryParseInt(string text, int min, int max, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (!int.TryParse(text, out value)) return false;
            return value >= min && value <= max;
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