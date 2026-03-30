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
        private bool _isDataChanged = false;
        private bool _isLoading = false;

        public TableEditWindow(int? id = null)
        {
            InitializeComponent();
            _tableId = id;
            ConfigureWindow();
            AttachFloatingHints();
            AttachChangeHandlers();

            if (_tableId.HasValue)
                LoadTableData();

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
            FloatingHintHelper.Attach(NotesTextBox, HintNotes, NotesTransform);
        }

        private void AttachChangeHandlers()
        {
            TableNumberTextBox.TextChanged += OnControlChanged;
            CapacityTextBox.TextChanged += OnControlChanged;
            NotesTextBox.TextChanged += OnControlChanged;
            IsAvailableCheckBox.Checked += OnControlChanged;
            IsAvailableCheckBox.Unchecked += OnControlChanged;
        }

        private void OnControlChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                _isDataChanged = true;
        }

        private void LoadTableData()
        {
            _isLoading = true;
            try
            {
                var table = GetTableById(_tableId!.Value);
                if (table.Rows.Count == 0) return;

                var row = table.Rows[0];
                SetText(TableNumberTextBox, row["table_number"]);
                SetText(CapacityTextBox, row["capacity"]);

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
            finally
            {
                _isLoading = false;
            }
        }

        private DataTable GetTableById(int id)
        {
            // Явно перечисляем поля, исключая zone
            string query = @"SELECT table_id, table_number, capacity, is_available, notes FROM tables WHERE table_id = @id";
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
                (table_number, capacity, is_available, notes)
                VALUES 
                (@table_number, @capacity, @is_available, @notes)";

            new DbManager().NonQuery(query, parameters);
        }

        private void UpdateTable(Dictionary<string, object> parameters)
        {
            string query = @"UPDATE tables SET
                table_number = @table_number,
                capacity = @capacity,
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

            if (!TryParseInt(CapacityTextBox.Text, 2, 30, out int capacity))
                return Fail("Вместимость должна быть от 2 до 30 человек", CapacityTextBox);

            if (!_db.IsTableNumberUnique(tableNumber, _tableId))
                return Fail("Стол с таким номером уже существует", TableNumberTextBox);

            if (!string.IsNullOrWhiteSpace(NotesTextBox.Text) && NotesTextBox.Text.Trim().Length > 255)
                return Fail("Примечания не могут быть длиннее 255 символов", NotesTextBox);

            parameters = new Dictionary<string, object>
            {
                ["@table_number"] = tableNumber,
                ["@capacity"] = capacity,
                ["@is_available"] = (IsAvailableCheckBox.IsChecked ?? true) ? 1 : 0,
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