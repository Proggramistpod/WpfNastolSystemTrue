using System.Windows;
using WpfNastolSystem.Forms.List;
using WpfNastolSystem.Moduls.DB;
using WpfNastolSystem.Moduls.Visual;

namespace WpfNastolSystem
{
    public partial class MainWindow : Window
    {
        DataBaseQuery dataBaseQuery = new();
        public MainWindow()
        {
            InitializeComponent();
            FloatingHintHelper.Attach(InputLogin, HintLogin, LoginTransform);
            FloatingHintHelper.Attach(InputPassword, HintPassword, PasswordTransform);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string? foundUser = dataBaseQuery.AtorizationUser(InputLogin.Text, InputPassword.Password).ToString();
            if (foundUser != null)
            {
                this.Content = new MainMenu();
                this.Title = "Настольная система";
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                this.ResizeMode = ResizeMode.CanResize;
            }
            else
                MessageBox.Show("Пароль не вереный");
        }
    }
}