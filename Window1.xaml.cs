using AppSettings;
using PDM_ERP_Checker.ERP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PDM_ERP_Checker
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public bool isLoggedin = false;
        public ERPMethods eRPMethods;
        private string ERPName = Settings.ERPName;

        public LoginWindow()
        {
            InitializeComponent();
            ERPLogin.Text = ERPName + " log in";

        }

        private void LoginERP(object sender, RoutedEventArgs e)
        {
            isLoggedin = eRPMethods.CheckERPLogin(Username.Text, Password.Password);

            if (isLoggedin)
            {
                LoginFail.Visibility = Visibility.Hidden;
                MainWindow.password = Password.Password;
                MainWindow.username = Username.Text;
                MainWindow.loggedIn = true;
                Hide();
            }
            else
            {
                LoginFail.Visibility = Visibility.Visible;
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Password_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key != Key.Return && e.Key != Key.Enter) {
                return;
            }
            e.Handled = true;

            LoginERP(sender, e);
        }
    }
}
