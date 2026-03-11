using System.Windows;
using System.Windows.Media;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Windows
{
    public partial class CategoryEditWindow : Window
    {
            private readonly DataBaseQuery db = new();
            private readonly int? _id;
            public string WindowTitle => _id.HasValue ? "Редактировать категорию" : "Новая категория";

            public CategoryEditWindow(int? id = null)
            {
                _id = id;
                InitializeComponent();
                DataContext = this;

                if (_id.HasValue)
                {
                    LoadData(_id.Value);
                }

            // Подключаем floating hint
                FloatingHintHelper.Attach(tbDescription, hintD, (TranslateTransform)hintName.RenderTransform);
                FloatingHintHelper.Attach(tbName, hintName, (TranslateTransform)hintName.RenderTransform);
            }

            private void LoadData(int id)
            {
                var dt = db.GetAllCategories(); // или отдельный метод GetCategoryById
                var row = dt.Select($"category_id = {id}").FirstOrDefault();
                if (row != null)
                {
                    tbName.Text = row["name"].ToString();
                    tbDescription.Text = row["description"]?.ToString() ?? "";
                }
            }

            private void BtnSave_Click(object sender, RoutedEventArgs e)
            {
                if (string.IsNullOrWhiteSpace(tbName.Text))
                {
                    MessageBox.Show("Название категории обязательно", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var param = new Dictionary<string, object>
            {
                { "@name", tbName.Text.Trim() },
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

            private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
        }
    }