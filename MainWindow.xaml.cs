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
using System.Collections.ObjectModel;
using EPDM.Interop.epdm;
using System.Windows.Markup;
using System.Web.UI.WebControls.WebParts;

namespace PDM_ERP_Checker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        PDMMethods pDMMethods = new PDMMethods();
        ERPMethods eRPMethods = new ERPMethods();

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
            
            trvParts.ItemsSource = null;


            var vars = pDMMethods.GetPDMFile(Search.Text);

            output.Text = eRPMethods.GetERPDescription(vars.variables.PartNo, Username.Text, Password.Password);

            

            if (vars == null) { return; }

            if (vars.file.Name.EndsWith("sldasm", StringComparison.CurrentCultureIgnoreCase))
            {

                List<Assembly> assemblyList = new List<Assembly>();
                Assembly assembly = new Assembly() { Name = vars.file.Name };

                assembly.IntegrationError = CheckIntegration(vars);

                var references = pDMMethods.GetReferences(vars.file, vars.folder);

                foreach (var reference in references)
                {
                    PDMVariables variables = pDMMethods.GetPDMVariableCard(reference.File);

                    if(reference.Name.EndsWith("sldasm", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Assembly subassembly = new Assembly() { Name = reference.Name, Variables = variables };

                        //get parts
                        SetPartsInAssembly(subassembly, reference);
                        subassembly.IntegrationError = CheckIntegration(subassembly);

                        assembly.SubassemblyOrParts.Add(subassembly);

                    } else
                    {
                        //Make part
                        Part newPart = new Part() { Name = reference.Name, Variables = variables};
                        newPart.IntegrationError = CheckIntegration(newPart);
                        assembly.SubassemblyOrParts.Add(newPart);
                    }

                }

                assemblyList.Add(assembly);
                trvParts.ItemsSource = assemblyList;

            }


            //foreach (var fieldInfo in typeof(PDMVariables).GetFields())
            //{
            //    var fieldName = fieldInfo.Name;
            //    var fieldValue = fieldInfo.GetValue(vars.variables);

            //    Console.WriteLine($"{fieldName}: {fieldValue}");
            //}



        }

        private void SetPartsInAssembly(Assembly assembly, IEdmReference11 fileRef)
        {

            var references = pDMMethods.GetReferences(fileRef.File, fileRef.Folder);

            foreach (var reference in references)
            {
                PDMVariables variables = pDMMethods.GetPDMVariableCard(reference.File);

                if (reference.Name.EndsWith("sldasm", StringComparison.CurrentCultureIgnoreCase))
                {
                    Assembly subassembly = new Assembly() { Name = reference.Name, Variables = variables };

                    SetPartsInAssembly(subassembly, reference);
                    subassembly.IntegrationError = CheckIntegration(subassembly);
                    assembly.SubassemblyOrParts.Add(subassembly);

                }
                else
                {
                    //Make part
                    Part newPart = new Part() { Name = reference.Name, Variables = variables };
                    newPart.IntegrationError = CheckIntegration(newPart);
                    assembly.SubassemblyOrParts.Add(newPart);
                }
            }

        }

        private bool CheckIntegration(PDMPart part)
        {
            
            if (part.variables.State.Contains("Approve"))
            {
                var partNo = eRPMethods.GetERPPartNo(part.file.Name.Remove(part.file.Name.Length - 7), Username.Text, Password.Password);

                if(partNo != null)
                {
                    return true;
                } else
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckIntegration(Part part)
        {
            if (part.Variables.State.Contains("Approve"))
            {
                var partNo = eRPMethods.GetERPPartNo(part.Name.Remove(part.Name.Length - 7), Username.Text, Password.Password);

                if (partNo != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckIntegration(Assembly assembly)
        {
            if (assembly.Variables.State.Contains("Approve"))
            {
                var partNo = eRPMethods.GetERPPartNo(assembly.Name.Remove(assembly.Name.Length - 7), Username.Text, Password.Password);

                if (partNo != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

    }


    public class Assembly
    {
        public Assembly()
        {
            this.SubassemblyOrParts = new ObservableCollection<object>();
        }

        public string Name { get; set; }
        public PDMVariables Variables { get; set; }
        public bool IntegrationError { get; set; }
        public ObservableCollection<object> SubassemblyOrParts { get; set; }
        public bool IsSubassembly => true; // You can set this property for assembly objects
    }

    public class Part
    {
        public string Name { get; set; }
        public PDMVariables Variables { get; set; }
        public bool IntegrationError { get; set; }
        public bool IsSubassembly => false; // Parts are not sub-assemblies
    }
}
