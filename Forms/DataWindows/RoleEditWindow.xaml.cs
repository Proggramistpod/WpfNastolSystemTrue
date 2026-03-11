using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem.Forms.Edit
{
    public partial class RoleEditWindow : Window
    {
        private readonly DataBaseQuery _db = new();
        private readonly int? _roleId;

        public RoleEditWindow(int? id = null)
        {
            InitializeComponent();
            _roleId = id;
            ConfigureWindow();
            if (_roleId.HasValue)
                LoadRoleData();
            AttachFloatingHints();
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

        private void LoadRoleData()
        {
            try
            {
                var table = GetRoleById(_roleId!.Value);
                if (table.Rows.Count == 0) return;

                var row = table.Rows[0];
                CodeTextBox.Text = row["code"]?.ToString() ?? "";
                NameTextBox.Text = row["name"]?.ToString() ?? "";
                DescriptionTextBox.Text = row["description"]?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки данных роли", ex);
            }
        }

        private DataTable GetRoleById(int id)
        {
            string query = @"SELECT * FROM roles WHERE role_id = @id";
            return new DbManager().Select(query, new Dictionary<string, object> { { "@id", id } });
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
                    UpdateRole(roleData);
                    ShowInfo("Роль успешно обновлена");
                }
                else
                {
                    InsertRole(roleData);
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

        private void InsertRole(Dictionary<string, object> parameters)
        {
            string query = @"INSERT INTO roles 
                (code, name, description)
                VALUES 
                (@code, @name, @description)";

            new DbManager().NonQuery(query, parameters);
        }

        private void UpdateRole(Dictionary<string, object> parameters)
        {
            string query = @"UPDATE roles SET
                code = @code,
                name = @name,
                description = @description
                WHERE role_id = @role_id";

            new DbManager().NonQuery(query, parameters);
        }

        private bool TryValidate(out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();

            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
                return Fail("Введите код роли", CodeTextBox);

            // Проверка длины code (VARCHAR(50))
            if (CodeTextBox.Text.Trim().Length > 50)
                return Fail("Код роли не может быть длиннее 50 символов", CodeTextBox);

            if (!IsCodeUnique(CodeTextBox.Text.Trim(), _roleId))
                return Fail("Роль с таким кодом уже существует", CodeTextBox);

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                return Fail("Введите название роли", NameTextBox);

            // Проверка длины name (VARCHAR(100))
            if (NameTextBox.Text.Trim().Length > 100)
                return Fail("Название роли не может быть длиннее 100 символов", NameTextBox);

            // Проверка длины description (VARCHAR(250))
            if (!string.IsNullOrWhiteSpace(DescriptionTextBox.Text) &&
                DescriptionTextBox.Text.Trim().Length > 250)
                return Fail("Описание не может быть длиннее 250 символов", DescriptionTextBox);

            parameters = new Dictionary<string, object>
            {
                ["@code"] = CodeTextBox.Text.Trim().ToUpper(),
                ["@name"] = NameTextBox.Text.Trim(),
                ["@description"] = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                    ? DBNull.Value : DescriptionTextBox.Text.Trim()
            };

            return true;
        }

        private bool IsCodeUnique(string code, int? excludeRoleId)
        {
            string query = @"SELECT COUNT(*) FROM roles WHERE code = @code" +
                          (excludeRoleId.HasValue ? " AND role_id != @role_id" : "");

            var parameters = new Dictionary<string, object> { { "@code", code } };
            if (excludeRoleId.HasValue)
                parameters["@role_id"] = excludeRoleId.Value;

            object result = new DbManager().Scalar(query, parameters);
            return Convert.ToInt32(result) == 0;
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