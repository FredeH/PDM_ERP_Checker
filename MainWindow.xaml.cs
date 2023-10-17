using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RestSharp;
using RestSharp.Authenticators;
using ERP;
using PDM_ERP_Checker.ERP;

namespace PDM_ERP_Checker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        PDMMethods pDMMethods = new PDMMethods();

        public MainWindow()
        {
            InitializeComponent();
            pDMMethods.InitializePDM();
        }


        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            
        }

        private void Button_Login(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_Search(object sender, RoutedEventArgs e)
        {
            ERPMethods methods = new ERPMethods();
            

            output.Text = methods.GetERPDescription(Search.Text, Username.Text, Password.Password);
            output_pdm.Text = pDMMethods.GetPDMDescription(Search.Text);
            var vars = pDMMethods.GetPDMVariables(Search.Text);

            foreach (var fieldInfo in typeof(PDMVariables).GetFields())
            {
                var fieldName = fieldInfo.Name;
                var fieldValue = fieldInfo.GetValue(vars);

                Console.WriteLine($"{fieldName}: {fieldValue}");
            }

        }
    }
}
