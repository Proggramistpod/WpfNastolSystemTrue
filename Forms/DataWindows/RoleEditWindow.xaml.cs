using System.Windows;
using System.Windows.Controls;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class RoleEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _roleId;
        private bool _isDataChanged = false;
        private bool _isLoading = false;

        public RoleEditWindow(int? id = null)
        {
            InitializeComponent();

            _roleId = id;
            ConfigureWindow();
            AttachFloatingHints();
            AttachChangeHandlers();

            if (_roleId.HasValue)
                LoadRoleData();

            CodeTextBox.Focus();
        }

        private void ConfigureWindow()
        {
            bool editMode = _roleId.HasValue;
            Title = editMode ? "Редактирование роли" : "Добавление роли";
            TitleText.Text = Title;
        }

        private void AttachFloatingHints()
        {
            FloatingHintHelper.Attach(CodeTextBox, HintCode, CodeTransform);
            FloatingHintHelper.Attach(NameTextBox, HintName, NameTransform);
            FloatingHintHelper.Attach(DescriptionTextBox, HintDescription, DescriptionTransform);
        }

        private void AttachChangeHandlers()
        {
            CodeTextBox.TextChanged += OnControlChanged;
            NameTextBox.TextChanged += OnControlChanged;
            DescriptionTextBox.TextChanged += OnControlChanged;
        }

        private void OnControlChanged(object sender, EventArgs e)
        {
            if (!_isLoading)
                _isDataChanged = true;
        }

        private void LoadRoleData()
        {
            _isLoading = true;
            try
            {
                var table = _db.GetRoleById(_roleId!.Value);
                if (table.Rows.Count == 0)
                {
                    MessageBox.Show("Роль не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    Close();
                    return;
                }

                var row = table.Rows[0];
                CodeTextBox.Text = row["code"]?.ToString() ?? "";
                NameTextBox.Text = row["name"]?.ToString() ?? "";
                DescriptionTextBox.Text = row["description"]?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных роли", ex);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidate(out var roleData))
                return;

            try
            {
                if (_roleId.HasValue)
                {
                    roleData["@role_id"] = _roleId.Value;
                    _db.UpdateRole(roleData);
                    ShowInfo("Роль успешно обновлена");
                }
                else
                {
                    _db.InsertRole(roleData);
                    ShowInfo("Роль успешно добавлена");
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

            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
                return Fail("Введите код роли", CodeTextBox);

            string code = CodeTextBox.Text.Trim();
            if (code.Length > 50)
                return Fail("Код роли не может быть длиннее 50 символов", CodeTextBox);

            if (!_db.IsRoleCodeUnique(code, _roleId))
                return Fail("Роль с таким кодом уже существует", CodeTextBox);

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                return Fail("Введите название роли", NameTextBox);

            string name = NameTextBox.Text.Trim();
            if (name.Length > 100)
                return Fail("Название роли не может быть длиннее 100 символов", NameTextBox);

            if (!_db.IsRoleNameUnique(name, _roleId))
                return Fail("Роль с таким названием уже существует", NameTextBox);

            parameters = new Dictionary<string, object>
            {
                ["@code"] = code,
                ["@name"] = name,
                ["@description"] = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? DBNull.Value : DescriptionTextBox.Text.Trim()
            };

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