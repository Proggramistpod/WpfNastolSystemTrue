using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Forms.Edit;

namespace WpfNastolSystem.Forms.List
{
    public partial class MainMenu : Page
    {
        private DataBaseQuery dataBaseQuery = new DataBaseQuery();
        private string currentTable = "games";
        private DataTable currentData;
        public MainMenu()
        {
            InitializeComponent();
            LoadData();
            LoadFilterOptions();
        }
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            currentTable = button.Tag.ToString();
            TitleTextBlock.Text = GetRussianName(currentTable);
            LoadData();
            LoadFilterOptions();
        }
        private void LoadData()
        {
            try
            {
                currentData = dataBaseQuery.GetTableData(currentTable);
                DataGrid.ItemsSource = currentData.DefaultView;
                DataGrid.Columns.Clear();
                foreach (DataColumn column in currentData.Columns)
                {
                    DataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = GetRussianColumnName(column.ColumnName),
                        Binding = new System.Windows.Data.Binding(column.ColumnName),
                        Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }
        private void LoadFilterOptions()
        {
            FilterComboBox.Items.Clear();
            FilterComboBox.Items.Add("Все");
            switch (currentTable)
            {
                case "games":
                    FilterComboBox.Items.Add("По году");
                    FilterComboBox.Items.Add("По рейтингу");
                    FilterComboBox.Items.Add("По количеству игроков");
                    break;
                case "persons":
                    FilterComboBox.Items.Add("Активные");
                    FilterComboBox.Items.Add("Забаненные");
                    break;
                case "sessions":
                    FilterComboBox.Items.Add("Активные");
                    FilterComboBox.Items.Add("Завершенные");
                    break;
            }
            FilterComboBox.SelectedIndex = 0;
        }
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }
        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }
        private void ApplyFilter()
        {
            if (currentData == null) return;
            string searchText = SearchTextBox.Text.ToLower();
            string filterOption = FilterComboBox.SelectedItem?.ToString() ?? "Все";
            DataView view = currentData.DefaultView;
            view.RowFilter = "";
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string filter = "";
                foreach (DataColumn column in currentData.Columns)
                {
                    if (column.DataType == typeof(string))
                    {
                        if (!string.IsNullOrEmpty(filter))
                            filter += " OR ";
                        filter += $"CONVERT([{column.ColumnName}], 'System.String') LIKE '%{searchText}%'";
                    }
                }
                view.RowFilter = filter;
            }
            if (filterOption != "Все")
            {
                switch (currentTable)
                {
                    case "sessions":
                        if (filterOption == "Активные")
                            view.RowFilter += (string.IsNullOrEmpty(view.RowFilter) ? "" : " AND ") + "ended_at IS NULL";
                        else if (filterOption == "Завершенные")
                            view.RowFilter += (string.IsNullOrEmpty(view.RowFilter) ? "" : " AND ") + "ended_at IS NOT NULL";
                        break;
                }
            }
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window editWindow = GetEditWindow(currentTable, null);
                if (editWindow != null)
                {
                    editWindow.Owner = Window.GetWindow(this);
                    editWindow.ShowDialog();
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления");
                return;
            }
            if (MessageBox.Show("Вы уверены, что хотите удалить эту запись?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    DataRowView row = DataGrid.SelectedItem as DataRowView;
                    string idColumn = GetIdColumnName(currentTable);
                    if (row != null && idColumn != null)
                    {
                        int id = Convert.ToInt32(row[idColumn]);
                        dataBaseQuery.DeleteRecord(currentTable, idColumn, id);
                        LoadData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }
        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataGrid.SelectedItem != null)
            {
                DataRowView row = DataGrid.SelectedItem as DataRowView;
                string idColumn = GetIdColumnName(currentTable);
                if (row != null && idColumn != null)
                {
                    int id = Convert.ToInt32(row[idColumn]);
                    Window editWindow = GetEditWindow(currentTable, id);
                    if (editWindow != null)
                    {
                        editWindow.Owner = Window.GetWindow(this);
                        editWindow.ShowDialog();
                        LoadData();
                    }
                }
            }
        }
        private Window GetEditWindow(string table, int? id)
        {
            switch (table)
            {
                case "games":
                    return new GameEditWindow(id);
                //case "persons":
                //    return new PersonEditWindow(id);
                //case "sessions":
                //    return new SessionEditWindow(id);
                //case "categories":
                //    return new CategoryEditWindow(id);
                //case "game_copies":
                //    return new GameCopyEditWindow(id);
                //case "tables":
                //    return new TableEditWindow(id);
                //case "accounts":
                //    return new AccountEditWindow(id);
                //case "roles":
                //    return new RoleEditWindow(id);
                default:
                    return null;
            }
        }
        private string GetRussianName(string table)
        {
            return table switch
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
        }
        private string GetRussianColumnName(string column)
        {
            return column switch
            {
                "game_id" => "ID",
                "title" => "Название",
                "description" => "Описание",
                "publish_year" => "Год",
                "publisher" => "Издатель",
                "min_players" => "Мин. игроков",
                "max_players" => "Макс. игроков",
                "play_time_min" => "Время игры",
                "age_rating" => "Возраст",
                "bgg_rating" => "Рейтинг",
                "person_id" => "ID пользователя",
                "full_name" => "ФИО",
                "phone" => "Телефон",
                "email" => "Email",
                "birth_date" => "Дата рождения",
                "registered_at" => "Дата регистрации",
                "is_banned" => "Забанен",
                "session_id" => "ID сессии",
                "started_at" => "Начало",
                "ended_at" => "Конец",
                "cost" => "Стоимость",
                "paid" => "Оплачено",
                _ => column
            };
        }
        private string GetIdColumnName(string table)
        {
            return table switch
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
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Подтверждение",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                NavigationService.Navigate(new Uri("MainWindow.xaml", UriKind.Relative));
            }
        }
    }
}