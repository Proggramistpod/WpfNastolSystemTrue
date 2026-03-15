using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfNastolSystem.Forms.Edit;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Windows;

namespace WpfNastolSystem.Forms.List
{
    public partial class MainMenu : Page
    {
        private readonly DataBaseQuery _db = new();
        private DataTable? _currentData;
        private string _currentTable = "games";

        public MainMenu()
        {
            InitializeComponent();
            InitializePage();
        }

        private void InitializePage() => LoadTable("games");

        private void ProfileButton_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Профиль");
        private void ThemButton_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Тема");

        #region Загрузка таблицы
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string table })
                LoadTable(table);
        }

        private void LoadTable(string table)
        {
            _currentTable = table;
            TitleTextBlock.Text = GetRussianName(table);
            LoadData();
            LoadFilters();
        }

        private void LoadData()
        {
            try
            {
                _currentData = _db.GetTableForGrid(_currentTable);
                if (_currentData == null || _currentData.Rows.Count == 0)
                {
                    DataGrid.ItemsSource = null;
                    DataGrid.Columns.Clear();
                    return;
                }
                DataGrid.ItemsSource = _currentData.DefaultView;
                GenerateColumns(_currentData);
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных", ex);
            }
        }

        private void GenerateColumns(DataTable table)
        {
            DataGrid.Columns.Clear();

            // Колонки, которые не нужно показывать (ID и служебные даты)
            var hiddenColumns = new HashSet<string>
            {
                "game_id", "person_id", "session_id", "category_id", "copy_id", "table_id", "account_id", "role_id",
                "last_login", "created_at", "registered_at"
            };

            foreach (DataColumn column in table.Columns)
            {
                if (hiddenColumns.Contains(column.ColumnName))
                    continue;

                DataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = GetRussianColumnName(column.ColumnName),
                    Binding = new System.Windows.Data.Binding(column.ColumnName),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
                });
            }
        }
        #endregion

        #region Фильтрация и поиск
        private void LoadFilters()
        {
            FilterComboBox.ItemsSource = GetFilterOptions(_currentTable);
            FilterComboBox.SelectedIndex = 0;
        }

        private List<string> GetFilterOptions(string table) =>
            table switch
            {
                "games" => new() { "Все", "По году", "По рейтингу", "По количеству игроков" },
                "persons" => new() { "Все", "Активные", "Забаненные" },
                "sessions" => new() { "Все", "Активные", "Завершенные" },
                _ => new() { "Все" }
            };

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (_currentData == null) return;
            var view = _currentData.DefaultView;
            string search = SearchTextBox.Text?.Trim() ?? "";
            string selectedFilter = FilterComboBox.SelectedItem?.ToString() ?? "Все";

            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var escapedSearch = search.Replace("'", "''");
                var stringColumns = _currentData.Columns
                    .Cast<DataColumn>()
                    .Where(c => c.DataType == typeof(string))
                    .Select(c => $"[{c.ColumnName}] LIKE '%{escapedSearch}%'");
                filters.Add("(" + string.Join(" OR ", stringColumns) + ")");
            }

            string? additional = GetAdditionalFilter(selectedFilter);
            if (!string.IsNullOrEmpty(additional))
                filters.Add(additional);

            view.RowFilter = string.Join(" AND ", filters);
        }

        private string? GetAdditionalFilter(string filter) =>
            _currentTable switch
            {
                "sessions" when filter == "Активные" => "ended_at IS NULL",
                "sessions" when filter == "Завершенные" => "ended_at IS NOT NULL",
                _ => null
            };
        #endregion

        #region CRUD
        private void AddButton_Click(object sender, RoutedEventArgs e) => OpenEditWindow(null);

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите запись для удаления",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Удалить запись?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            string? idColumn = GetIdColumnName(_currentTable);
            if (idColumn == null) return;

            if (!int.TryParse(row.Row[idColumn]?.ToString(), out int id))
                return;

            try
            {
                _db.DeleteRecord(_currentTable, id);
                LoadData();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка удаления", ex);
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGrid.SelectedItem is not DataRowView row)
                return;

            string? idColumn = GetIdColumnName(_currentTable);
            if (idColumn == null) return;

            if (!int.TryParse(row.Row[idColumn]?.ToString(), out int id))
                return;

            OpenEditWindow(id);
        }

        private void OpenEditWindow(int? id)
        {
            Window? window = GetEditWindow(_currentTable, id);
            if (window == null)
            {
                MessageBox.Show($"Окно редактирования для таблицы '{_currentTable}' не реализовано.",
                                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
            LoadData();
        }
        #endregion

        #region Вспомогательные методы
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var loginWindow = new MainWindow();
            loginWindow.Show();
            Window.GetWindow(this)?.Close();
        }

        private void ShowError(string title, Exception ex)
        {
            MessageBox.Show($"{title}: {ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private Window? GetEditWindow(string table, int? id) => table switch
            {
                "games" => new GameEditWindow(id),
                "persons" => new PersonEditWindow(id),
                "sessions" => new SessionEditWindow(id),
                "categories" => new CategoryEditWindow(id),
                "game_copies" => new GameCopyEditWindow(id),
                "tables" => new TableEditWindow(id),
                "accounts" => new AccountEditWindow(id),
                "roles" => new RoleEditWindow(id),
                _ => null
            };

        private string? GetIdColumnName(string table) =>
            table switch
            {
                "games" => "game_id",
                "persons" => "person_id",
                "sessions" => "session_id",
                "categories" => "category_id",
                "game_copies" => "copy_id",
                "tables" => "table_id",
                "accounts" => "account_id",
                "roles" => "role_id",
                _ => null
            };

        private string GetRussianName(string table) =>
            table switch
            {
                "games" => "Игры",
                "persons" => "Пользователи",
                "sessions" => "Сессии",
                "categories" => "Категории",
                "game_copies" => "Копии игр",
                "tables" => "Столы",
                "accounts" => "Аккаунты",
                "roles" => "Роли",
                _ => table
            };

        // Переводы только для отображаемых колонок
        private string GetRussianColumnName(string column) =>
            column switch
            {
                // Games
                "title" => "Название",
                "publish_year" => "Год выпуска",
                "publisher" => "Издатель",
                "min_players" => "Мин. игроков",
                "max_players" => "Макс. игроков",
                "play_time_min" => "Время (мин)",
                "age_rating" => "Возраст",
                "bgg_rating" => "Рейтинг BGG",
                "is_active" => "Активна",
                "description" => "Описание",

                // Persons
                "full_name" => "ФИО",
                "role_name" => "Роль",
                "phone" => "Телефон",
                "email" => "Email",
                "birth_date" => "Дата рождения",
                "is_banned" => "Заблокирован",
                "notes" => "Примечания",

                // Sessions
                "organizer_name" => "Организатор",
                "table_number" => "Стол",
                "game_title" => "Игра",
                "started_at" => "Начало",
                "ended_at" => "Окончание",
                "comment" => "Комментарий",
                "paid" => "Оплачено",
                "cost" => "Заплатить",
                "payment_method" => "Способ оплаты",

                // Categories
                "name" => "Название",

                // Game copies
                "inventory_number" => "Инв. номер",
                "acquired_date" => "Дата приобретения",
                "Condition" => "Состояние",
                "location" => "Расположение",
                "is_available" => "Доступность",

                // Tables
                "capacity" => "Вместимость",
                "zone" => "Зона",

                // Accounts
                "login" => "Логин",
                "person_name" => "Владелец",

                // Roles
                "code" => "Код",

                // Если колонка не найдена
                _ => column
            };
        #endregion
    }
}