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
using ERP_LIVE;
using PDM_ERP_Checker.ERP;
using System.Collections.ObjectModel;
using EPDM.Interop.epdm;
using System.Windows.Markup;
using System.Web.UI.WebControls.WebParts;
using System.Collections;
using System.Text.RegularExpressions;
using System.Reflection;
using AppSettings;
using System.ComponentModel;

namespace PDM_ERP_Checker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        PDMMethods pDMMethods { get; set; } = new PDMMethods();
        ERPMethods eRPMethods { get; set; } = new ERPMethods();
        IntegrationMethods integrationMethods { get; set; } = new IntegrationMethods();

        LoginWindow loginWindow;
        public static bool loggedIn { get; set; }
        public static string username { get; set; }
        public static string password { get; set; }

        List<string> ErrorTaskList = new List<string>();
        List<Assembly> assemblyList = new List<Assembly>();

        public ObservableCollection<PDMBOMItem> bomDataCollection { get; set; } = new ObservableCollection<PDMBOMItem>();

        public MainWindow()
        {
            InitializeComponent();
            pDMMethods.InitializePDM();
            InitializeLoginWindow();
            ERPName.Content = Settings.ERPName + " description";
        }

        private void InitializeLoginWindow()
        {
            loginWindow = new LoginWindow();
            loginWindow.eRPMethods = eRPMethods;
            loginWindow.Show();
            loginWindow.Topmost = true;
        }

        private void Button_Login(object sender, RoutedEventArgs e)
        {
            if(loginWindow.IsLoaded != false)
            {
            loginWindow.Show();
            } else
            {
                InitializeLoginWindow();
            }
        }

        private void Button_Search(object sender, RoutedEventArgs e)
        {
            loggedIn = loginWindow.isLoggedin;
            output_pdm.Text = "";


            ErrorTaskList.Clear();
            assemblyList.Clear();
            listErrorTasks.ItemsSource = null;
            trvParts.ItemsSource = null;


            var vars = pDMMethods.GetPDMFile(Search.Text);

            if(vars == null)
            {
                output_pdm.Text = "No files found";
                return;
            }
            output_pdm.Text = vars.variables.Description;
            //var inventoryPart = await eRPMethods.GetERPInventoryPartAsync(vars.variables.PartNo, Username.Text, Password.Password);

            if (vars.variables.PartNo != "" | vars.variables.PartNo != null)
            {
                if (loggedIn)
                {
                    output.Text = eRPMethods.GetERPDescription(vars.variables.PartNo, username, password);
                }
                else
                {
                    output.Text = "ERP not logged in";
                }
            }

            var cfgNames = pDMMethods.GetConfigurations(vars.file);

            if (cfgNames == null)
            {
                output_pdm.Text = "Error finding file";
                return;
            }
            var bom = pDMMethods.GetBOM(vars.file, cfgNames[0]);
            //Create BOM
            var bomList = pDMMethods.CreateBOM(bom, ErrorTaskList);

            BomView.ItemsSource = bomList;

            foreach (var item in bomList)
            {
                var ready = integrationMethods.CheckPDMExport(item, ErrorTaskList, OnlyExport.IsChecked.Value);
            }

            if (OnlyExport.IsChecked.Value)
            {
                if (loggedIn)
                {
                    foreach (var item in bomList)
                    {
                        eRPMethods.CheckERPExportIntegration(item, username, password, ErrorTaskList);
                    }
                }
                BomViewTab.IsSelected = true;
                listErrorTasks.ItemsSource = ErrorTaskList;
                return;
            }

            if (vars == null) { return; }

            if (vars.file.Name.EndsWith("sldasm", StringComparison.CurrentCultureIgnoreCase))
            {
                Assembly assembly = new Assembly() { Name = vars.file.Name, Variables = vars.variables };

                assembly.IntegrationLevel = integrationMethods.CheckIntegration(assembly, ErrorTaskList);

                assemblyList.Add(assembly);

                var references = pDMMethods.GetReferences(vars.file, vars.folder, vars.file.Name, cfgNames[0]);

                foreach (var reference in references)
                {
                    PDMVariables variables = pDMMethods.GetPDMVariableCard(reference);                    

                    if (reference.Name.EndsWith("sldasm", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Assembly subassembly = new Assembly() { Name = reference.Name, Variables = variables, Version = reference.VersionRef, LatestVersion = reference.File.CurrentVersion, pdmID = reference.FileID };
                        //get parts
                        if (!TopLevel.IsChecked.Value)
                        {
                            SetPartsInAssembly(subassembly, reference);
                        }
                        subassembly.IntegrationLevel = integrationMethods.CheckIntegration(subassembly, ErrorTaskList);

                        assembly.SubassemblyOrParts.Add(subassembly);

                    } else
                    {
                        //Make part
                        Part newPart = new Part() { Name = reference.Name, Variables = variables, Version = reference.VersionRef, LatestVersion = reference.File.CurrentVersion, pdmID = reference.FileID };
                        newPart.IsToolboxPart = reference.Folder.LocalPath.Contains(Settings.ToolboxPath, StringComparison.CurrentCultureIgnoreCase);
                        newPart.IntegrationLevel = integrationMethods.CheckIntegration(newPart, ErrorTaskList);
                        assembly.SubassemblyOrParts.Add(newPart);
                    }

                }


                UpdateTreeView(assembly);


            } else if (vars.file.Name.EndsWith("sldprt", StringComparison.CurrentCultureIgnoreCase))
            {
                List<Assembly> partList = new List<Assembly>();


                Assembly assembly = new Assembly() { Name = vars.file.Name, Variables = vars.variables, pdmID = vars.file.ID };
                assembly.IntegrationLevel = integrationMethods.CheckIntegration(assembly, ErrorTaskList);

                output_pdm.Text = vars.variables.Description;

                partList.Add(assembly);

                trvParts.ItemsSource = partList;
            }

            listErrorTasks.ItemsSource = ErrorTaskList;

        }

        private void SetPartsInAssembly(Assembly assembly, IEdmReference11 fileRef)
        {
            var references = pDMMethods.GetReferences(fileRef.File as IEdmFile18, fileRef.Folder as IEdmFolder13, assembly.Name, fileRef.RefConfiguration);

            foreach (var reference in references)
            {
                PDMVariables variables = pDMMethods.GetPDMVariableCard(reference);

                if (reference.Name.EndsWith("sldasm", StringComparison.CurrentCultureIgnoreCase))
                {
                    Assembly subassembly = new Assembly() { Name = reference.Name, Variables = variables, Version = reference.VersionRef, LatestVersion = reference.File.CurrentVersion, pdmID = reference.FileID };

                    SetPartsInAssembly(subassembly, reference);
                    subassembly.IntegrationLevel = integrationMethods.CheckIntegration(subassembly, ErrorTaskList);
                    assembly.SubassemblyOrParts.Add(subassembly);

                }
                else
                {
                    //Make part
                    Part newPart = new Part() { Name = reference.Name, Variables = variables, Version = reference.VersionRef, LatestVersion = reference.File.CurrentVersion, pdmID = reference.FileID };
                    newPart.IsToolboxPart = reference.Folder.LocalPath.Contains(Settings.ToolboxPath, StringComparison.CurrentCultureIgnoreCase);
                    newPart.IntegrationLevel = integrationMethods.CheckIntegration(newPart, ErrorTaskList);
                    assembly.SubassemblyOrParts.Add(newPart);
                }
            }
        }


        private void SearcERP_Click(object sender, RoutedEventArgs e)
        {
            if (loggedIn)
            {
                output.Text = eRPMethods.GetERPDescription(Search.Text, username, password);
            }
        }

        private void UpdateTreeView(Assembly assembly)
        {
            assemblyList.Append(assembly);
            trvParts.ItemsSource = assemblyList;
        }

        private void Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return && e.Key != Key.Enter)
            {
                return;
            }
            e.Handled = true;

            Button_Search(sender, e);
        }
    }

    public enum IntegrationLevel
    {
        Development,
        Dummy,
        IntegrationError,
        Approved,
        Warning,
        Missing,
        Expired,
        Obsolete,
        InternalPart
    }

    

    public class Assembly
    {
        public Assembly()
        {
            this.SubassemblyOrParts = new ObservableCollection<object>();
        }

        public string Name { get; set; }
        public PDMVariables Variables { get; set; }
        public ERPData ERPData { get; set; }
        public IntegrationLevel IntegrationLevel { get; set; }
        public int Version { get; set; }
        public int LatestVersion { get; set; }
        public int pdmID { get; set; }
        public string ErrorMessages { get; set; }
        public ObservableCollection<object> SubassemblyOrParts { get; set; }
        public bool IsSubassembly => true; // You can set this property for assembly objects
    }

    public class Part
    {
        public string Name { get; set; }
        public PDMVariables Variables { get; set; }
        public ERPData ERPData { get; set; }
        public IntegrationLevel IntegrationLevel { get; set; }
        public int Version { get; set; }
        public int LatestVersion { get; set; }
        public int pdmID { get; set; }

        public string ErrorMessages { get; set; }
        public bool IsToolboxPart { get; set; }
        public bool IsSubassembly => false; // Parts are not sub-assemblies
    }

    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}
