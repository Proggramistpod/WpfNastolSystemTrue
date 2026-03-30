using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Windows
{
    public partial class CategoryEditWindow : Window
    {
        private readonly DataBaseQuery db = new();
        private readonly int? _id;
        private bool _isDataChanged = false;
        private bool _isLoading = false; // флаг загрузки данных

        public string WindowTitle => _id.HasValue ? "Редактировать категорию" : "Новая категория";

        public CategoryEditWindow(int? id = null)
        {
            _id = id;
            InitializeComponent();
            DataContext = this;
            tbName.TextChanged += TextBox_TextChanged;
            tbDescription.TextChanged += TextBox_TextChanged;

            if (_id.HasValue)
            {
                LoadData(_id.Value);
            }

            FloatingHintHelper.Attach(tbName, hintName, (TranslateTransform)hintName.RenderTransform);
            FloatingHintHelper.Attach(tbDescription, hintD, (TranslateTransform)hintD.RenderTransform);
        }

        private void LoadData(int id)
        {
            _isLoading = true; 
            try
            {
                var dt = db.GetAllCategories(); 
                var row = dt.Select($"category_id = {id}").FirstOrDefault();
                if (row != null)
                {
                    tbName.Text = row["name"].ToString();
                    tbDescription.Text = row["description"]?.ToString() ?? "";
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoading)
            {
                _isDataChanged = true;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbName.Text))
            {
                MessageBox.Show("Название категории обязательно", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                tbName.Focus();
                return;
            }

            string name = tbName.Text.Trim();

            if (!db.IsCategoryNameUnique(name, _id))
            {
                MessageBox.Show("Категория с таким названием уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                tbName.Focus();
                return;
            }

            var param = new Dictionary<string, object>
            {
                { "@name", name },
                { "@description", tbDescription.Text?.Trim() ?? "" }
            };

            if (_id.HasValue)
            {
                param["@category_id"] = _id.Value;
                db.UpdateCategory(param);
            }
            else
            {
                db.InsertCategory(param);
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataChanged)
            {
                var result = MessageBox.Show("Изменения не сохранены. Закрыть?", "Подтверждение",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }
            DialogResult = false;
            Close();
        }
    }
}