using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class PublisherEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _publisherId;

        public PublisherEditWindow(int? id = null)
        {
            InitializeComponent();

            _publisherId = id;
            ConfigureWindow();

            if (_publisherId.HasValue)
                LoadPublisherData();

            AttachFloatingHints();
            NameTextBox.Focus();
        }

        private void ConfigureWindow()
        {
            bool editMode = _publisherId.HasValue;
            Title = editMode ? "Редактирование издателя" : "Добавление издателя";
            TitleText.Text = Title;
        }

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(NameTextBox, HintName, NameTransform);
        }

        private void LoadPublisherData()
        {
            try
            {
                var table = _db.GetPublisherById(_publisherId!.Value);
                if (table.Rows.Count == 0) return;
                var row = table.Rows[0];

                NameTextBox.Text = row["name"]?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных издателя", ex);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidate(out var publisherData))
                return;

            try
            {
                if (_publisherId.HasValue)
                {
                    publisherData["@publisher_id"] = _publisherId.Value;
                    _db.UpdatePublisher(publisherData);
                    ShowInfo("Издатель успешно обновлён");
                }
                else
                {
                    _db.InsertPublisher(publisherData);
                    ShowInfo("Издатель успешно добавлен");
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

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                return Fail("Введите название издателя", NameTextBox);

            parameters["@name"] = NameTextBox.Text.Trim();
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
            MessageBox.Show($"{title}\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowInfo(string message)
        {
            MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}