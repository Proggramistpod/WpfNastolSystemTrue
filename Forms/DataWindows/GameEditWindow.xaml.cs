using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using WpfNastolSystem.Moduls.DB;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class GameEditWindow : Window
    {
        private DataBaseQuery dataBaseQuery = new DataBaseQuery();
        private int? gameId;
        public GameEditWindow(int? id = null)
        {
            InitializeComponent();
            gameId = id;
            LoadCategories();
            if (gameId.HasValue)
            {
                TitleText.Text = "Редактирование игры";
                LoadGameData();
            }
        }
        private void LoadCategories()
        {
            try
            {
                DataTable categories = dataBaseQuery.GetTableData("categories");
                CategoryComboBox.ItemsSource = categories.DefaultView;
                CategoryComboBox.DisplayMemberPath = "name";
                CategoryComboBox.SelectedValuePath = "category_id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
            }
        }
        private void LoadGameData()
        {
            try
            {
                DataTable game = dataBaseQuery.GetGameById(gameId.Value);
                if (game.Rows.Count > 0)
                {
                    DataRow row = game.Rows[0];
                    TitleTextBox.Text = row["title"].ToString();
                    DescriptionTextBox.Text = row["description"].ToString();
                    YearTextBox.Text = row["publish_year"].ToString();
                    PublisherTextBox.Text = row["publisher"].ToString();
                    MinPlayersTextBox.Text = row["min_players"].ToString();
                    MaxPlayersTextBox.Text = row["max_players"].ToString();
                    PlayTimeTextBox.Text = row["play_time_min"].ToString();
                    AgeRatingTextBox.Text = row["age_rating"].ToString();
                    BggRatingTextBox.Text = row["bgg_rating"].ToString();
                    if (row["category_id"] != DBNull.Value)
                        CategoryComboBox.SelectedValue = row["category_id"];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
                {
                    MessageBox.Show("Введите название игры");
                    return;
                }
                var parameters = new Dictionary<string, object>
                {
                    { "@title", TitleTextBox.Text },
                    { "@description", DescriptionTextBox.Text },
                    { "@publish_year", Convert.ToInt32(YearTextBox.Text) },
                    { "@publisher", PublisherTextBox.Text },
                    { "@category_id", CategoryComboBox.SelectedValue },
                    { "@min_players", Convert.ToInt32(MinPlayersTextBox.Text) },
                    { "@max_players", Convert.ToInt32(MaxPlayersTextBox.Text) },
                    { "@play_time_min", Convert.ToInt32(PlayTimeTextBox.Text) },
                    { "@age_rating", Convert.ToInt32(AgeRatingTextBox.Text) },
                    { "@bgg_rating", Convert.ToDecimal(BggRatingTextBox.Text) }
                };
                if (gameId.HasValue)
                {
                    parameters.Add("@game_id", gameId.Value);
                    dataBaseQuery.UpdateGame(parameters);
                    MessageBox.Show("Игра успешно обновлена");
                }
                else
                {
                    dataBaseQuery.InsertGame(parameters);
                    MessageBox.Show("Игра успешно добавлена");
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}