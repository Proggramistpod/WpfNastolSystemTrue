using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class GameEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _gameId;

        // Вложенный класс для элементов ComboBox категорий
        public class CategoryItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;

            // Переопределяем ToString, чтобы избежать вывода { Id = 1, Name = ... }
            public override string ToString() => Name ?? "(без названия)";
        }

        public GameEditWindow(int? id = null)
        {
            InitializeComponent();

            _gameId = id;
            ConfigureWindow();
            LoadCategories();

            if (_gameId.HasValue)
                LoadGameData();

            AttachFloatingHints();
            TitleTextBox.Focus();
        }

        #region Инициализация

        private void ConfigureWindow()
        {
            bool editMode = _gameId.HasValue;
            Title = editMode ? "Редактирование игры" : "Добавление игры";
            TitleText.Text = Title;
        }

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(TitleTextBox, HintTitle, TitleTransform);
            FloatingHintHelper.Attach(DescriptionTextBox, HintDescription, DescriptionTransform);
            FloatingHintHelper.Attach(YearTextBox, HintYear, YearTransform);
            FloatingHintHelper.Attach(PublisherTextBox, HintPublisher, PublisherTransform);
            FloatingHintHelper.Attach(MinPlayersTextBox, HintMinPlayers, MinPlayersTransform);
            FloatingHintHelper.Attach(MaxPlayersTextBox, HintMaxPlayers, MaxPlayersTransform);
            FloatingHintHelper.Attach(PlayTimeTextBox, HintPlayTime, PlayTimeTransform);
            FloatingHintHelper.Attach(AgeRatingTextBox, HintAgeRating, AgeRatingTransform);
            FloatingHintHelper.Attach(BggRatingTextBox, HintBggRating, BggRatingTransform);
        }

        #endregion

        #region Загрузка данных

        private void LoadCategories()
        {
            try
            {
                var table = _db.GetAllCategories();

                if (table == null || table.Rows.Count == 0)
                {
                    MessageBox.Show("В таблице categories нет записей.");
                    CategoryComboBox.IsEnabled = false;
                    return;
                }

                var items = new List<CategoryItem>();

                foreach (DataRow row in table.Rows)
                {
                    items.Add(new CategoryItem
                    {
                        Id = Convert.ToInt32(row["category_id"]),
                        Name = row["name"]?.ToString() ?? "(без названия)"
                    });
                }

                CategoryComboBox.ItemsSource = items;
                CategoryComboBox.DisplayMemberPath = nameof(CategoryItem.Name);
                CategoryComboBox.SelectedValuePath = nameof(CategoryItem.Id);

                // Если нужно, чтобы при добавлении новой игры ничего не было выбрано по умолчанию
                // CategoryComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке категорий:\n" + ex.Message);
                CategoryComboBox.IsEnabled = false;
            }
        }

        private void LoadGameData()
        {
            try
            {
                var table = _db.GetGameById(_gameId!.Value);
                if (table.Rows.Count == 0) return;

                var row = table.Rows[0];

                SetText(TitleTextBox, row["title"]);
                SetText(DescriptionTextBox, row["description"]);
                SetText(PublisherTextBox, row["publisher"]);
                SetText(YearTextBox, row["publish_year"]);
                SetText(MinPlayersTextBox, row["min_players"]);
                SetText(MaxPlayersTextBox, row["max_players"]);
                SetText(PlayTimeTextBox, row["play_time_min"]);
                SetText(AgeRatingTextBox, row["age_rating"]);
                SetText(BggRatingTextBox, row["bgg_rating"]);

                if (row["category_id"] != DBNull.Value)
                {
                    CategoryComboBox.SelectedValue = Convert.ToInt32(row["category_id"]);
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных игры", ex);
            }
        }

        private void SetText(System.Windows.Controls.TextBox box, object value)
        {
            box.Text = value == DBNull.Value || value == null ? "" : value.ToString();
        }

        #endregion

        #region Сохранение

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidate(out var gameData))
                return;

            try
            {
                if (_gameId.HasValue)
                {
                    gameData["@game_id"] = _gameId.Value;
                    _db.UpdateGame(gameData);
                    ShowInfo("Игра успешно обновлена");
                }
                else
                {
                    _db.InsertGame(gameData);
                    ShowInfo("Игра успешно добавлена");
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

            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
                return Fail("Введите название игры", TitleTextBox);

            if (!TryParseInt(YearTextBox.Text, 1900, DateTime.Now.Year + 2, out int year))
                return Fail("Некорректный год выпуска", YearTextBox);

            if (!TryParseInt(MinPlayersTextBox.Text, 1, 20, out int min))
                return Fail("Мин. игроков: 1–20", MinPlayersTextBox);

            if (!TryParseInt(MaxPlayersTextBox.Text, min, 50, out int max))
                return Fail($"Макс. игроков должно быть ≥ {min} и ≤ 50", MaxPlayersTextBox);

            if (!TryParseInt(PlayTimeTextBox.Text, 5, 1440, out int time))
                return Fail("Время игры: 5–1440 минут", PlayTimeTextBox);

            if (!TryParseInt(AgeRatingTextBox.Text, 3, 99, out int age))
                return Fail("Возраст: 3–99", AgeRatingTextBox);

            if (!TryParseDecimal(BggRatingTextBox.Text, 0, 10, out decimal rating))
                return Fail("Рейтинг BGG: 0.0–10.0", BggRatingTextBox);

            if (CategoryComboBox.SelectedValue == null)
                return Fail("Выберите категорию", CategoryComboBox);

            parameters = new Dictionary<string, object>
            {
                ["@title"] = TitleTextBox.Text.Trim(),
                ["@description"] = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? DBNull.Value : DescriptionTextBox.Text.Trim(),
                ["@publish_year"] = year,
                ["@publisher"] = PublisherTextBox.Text.Trim(),
                ["@min_players"] = min,
                ["@max_players"] = max,
                ["@play_time_min"] = time,
                ["@age_rating"] = age,
                ["@bgg_rating"] = rating,
                ["@category_id"] = CategoryComboBox.SelectedValue
            };

            return true;
        }

        #endregion

        #region Валидация

        private bool TryParseInt(string text, int min, int max, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (!int.TryParse(text, out value)) return false;
            return value >= min && value <= max;
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
}