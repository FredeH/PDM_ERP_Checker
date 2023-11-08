using EPDM.Interop.epdm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppSettings;
using System.Text.RegularExpressions;
using System.Windows.Media.Converters;
using System.Windows.Controls;
using System.Web.UI.WebControls.WebParts;

namespace PDM_ERP_Checker
{
    public partial class PDMMethods
    {

        IEdmVault21 vault = new EdmVault5() as IEdmVault21;

        public void InitializePDM()
        {
            vault.LoginAuto(Settings.VaultName, 0);
        }

        private IEdmSearch9 SearchFileName(string fileName)
        {
            IEdmSearch9 search = vault.CreateSearch2() as IEdmSearch9;

            search.FindFiles = true;
            search.FindFolders = false;
            search.SetToken(EdmSearchToken.Edmstok_AllVersions, false);
            search.FileName = fileName;
            return search;
        }

        private class FileInformation
        {
            public IEdmFile18 file { get; set; }
            public IEdmFolder5 folder {  get; set; }
        }

        private FileInformation SearchFile(IEdmSearchResult6 search)
        {
            IEdmFile18 file;
            IEdmFolder5 folder;
            
            file = vault.GetFileFromPath(search.Path, out folder) as IEdmFile18;

            FileInformation files = new FileInformation();
            files.file = file;
            files.folder = folder as IEdmFolder13;

            return files;
        }

        public IEdmFile18 SearchFileFromPath(string path)
        {
            IEdmFile18 file;
            IEdmFolder5 folder;

            file = vault.GetFileFromPath(path, out folder) as IEdmFile18;

            return file;
        }

        private string SearchCardVariable(IEdmReference11 reference, string variable)
        {
            return SearchCardVariable(reference.File as IEdmFile18, variable, reference.FolderID, 0, reference.RefConfiguration);
        }

        private string SearchCardVariable(IEdmFile18 file, string variable, int folderID, int version = 0, string configuration = "none")
        {
            if(file == null) { return ""; }
            if(file.Name.Contains("^")) { return ""; }
            if (file.Name.ToLower().EndsWith("sldprt") | file.Name.ToLower().EndsWith("slddrw") | file.Name.ToLower().EndsWith("sldasm"))
            {
                var enumerator = file.GetEnumeratorVariable() as IEdmEnumeratorVariable10;

                EdmStrLst5 configurations;
                IEdmPos5 pos;

                if (file.GetConfigurations(version) == null)
                {
                    return "";
                }

                configurations = file.GetConfigurations(version);
                pos = configurations.GetHeadPosition();
                
                string cfgName = null;

                //Console.WriteLine(configuration);
                

                if (configuration != "none") {

                    
                    enumerator.GetVar2(variable, configuration, folderID, out var cardVariable);
                    if (cardVariable != null && !file.Name.ToLower().EndsWith("slddrw"))
                    {
                        return cardVariable.ToString();
                    }
                    else
                    {
                        enumerator.GetVar2(variable, "@", folderID, out cardVariable);
                        if (cardVariable != null)
                        {
                            return cardVariable.ToString();
                        }
                        else
                        {
                            return "";
                        }
                    }

                } else
                {

                cfgName = configurations.GetNext(pos);
                cfgName = configurations.GetNext(pos); //Get to default


                enumerator.GetVar(variable, cfgName, out var cardVariable);
                if (cardVariable != null && !file.Name.ToLower().EndsWith("slddrw"))
                {
                    return cardVariable.ToString();
                }
                else
                {
                    enumerator.GetVar2(variable, "@", folderID, out cardVariable);
                    if (cardVariable != null)
                    {
                        return cardVariable.ToString();
                    }
                    else
                    {
                        return "";
                    }
                }
                }
            }
            else
            {
                return "";
            }
        }

        public string GetPDMDescription(string id)
        {
            var results = SearchFileName(id);

            var result = results.GetFirstResult() as IEdmSearchResult6;

            var file = SearchFile(result);

            var description = SearchCardVariable(file.file, "Description", file.folder.ID);
            
            return description;
        }

        public PDMVariables GetPDMVariableCard(IEdmFile18 file, IEdmFolder13 folder)
        {
            string[] pdmVariables = Settings.pdmVariables;

            PDMVariables variables = new PDMVariables();

            var cardVariables = new Dictionary<string, string>();

            foreach (string variable in pdmVariables)
            {
                var temp = SearchCardVariable(file, variable, folder.ID);
                cardVariables[variable] = temp;
            }

            foreach (var kvp in cardVariables)
            {
                var variableName = kvp.Key;
                var variableValue = kvp.Value;

                var property = typeof(PDMVariables).GetProperty(variableName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(variables, variableValue);
                }
            }

            //Console.WriteLine(variables.Description);

            return variables;
        }
        public PDMVariables GetPDMVariableCard(IEdmReference11 reference)
        {
            string[] pdmVariables = Settings.pdmVariables;

            PDMVariables variables = new PDMVariables();

            var cardVariables = new Dictionary<string, string>();

            foreach (string variable in pdmVariables)
            {
                //var temp = SearchCardVariable(reference.File as IEdmFile18, variable, reference.RefConfiguration, reference.VersionRef);
                var temp = SearchCardVariable(reference, variable);
                cardVariables[variable] = temp;
            }

            foreach (var kvp in cardVariables)
            {
                var variableName = kvp.Key;
                var variableValue = kvp.Value;

                var property = typeof(PDMVariables).GetProperty(variableName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(variables, variableValue);
                }
            }

            //Console.WriteLine(variables.Description);

            return variables;
        }

        public PDMPart GetPDMFile(string id)
        {
            string[] pdmVariables = Settings.pdmVariables;

            PDMVariables variables = new PDMVariables();

            var results = SearchFileName(id);
            var result = results.GetFirstResult();

            if (result == null)
            {
                return null;
            }

            while (result.Name.Contains("slddrw", StringComparison.CurrentCultureIgnoreCase) & result != null)
            {
                result = results.GetNextResult();
            }

            if (result == null)
            {
                return null;
            }

            IEdmFile18 file;
            IEdmFolder5 folder;

            file = vault.GetFileFromPath(result.Path, out folder) as IEdmFile18;

            

            var cardVariables = new Dictionary<string, string>();

            foreach (string variable in pdmVariables)
            {
                var temp = SearchCardVariable(file, variable, folder.ID);
                cardVariables[variable] = temp;
            }

            

            foreach (var kvp in cardVariables)
            {
                var variableName = kvp.Key;
                var variableValue = kvp.Value;

                var property = typeof(PDMVariables).GetProperty(variableName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(variables, variableValue);
                }
            }

            //variables.State = file.CurrentState.Name;

            PDMPart part = new PDMPart() { file = file, folder = (IEdmFolder13)folder, variables = variables };

            return part;
        }

        public List<IEdmReference11> GetReferences(IEdmFile18 file, IEdmFolder13 folder, string projectName, string configuration = "")
        {
            IEdmReference11 references = file.GetReferenceTree(folder.ID) as IEdmReference11;

            IEdmPos5 pos = default(IEdmPos5);
            IEdmReference11 @ref = default(IEdmReference11);
            bool Top = true;

            pos = references.GetFirstChildPosition4(projectName, Top, true, true, (int)EdmRefFlags.EdmRef_File, configuration, references.VersionRef);
            List<IEdmReference11> refs = new List<IEdmReference11>();

            while ((!pos.IsNull))
            {
                @ref = (IEdmReference11) references.GetNextChild(pos);
                refs.Add(@ref);
            }
            return refs;
        }

        public List<string> GetConfigurations(IEdmFile18 file)
        {
            var configs = file.GetConfigurations();
            var pos = configs.GetHeadPosition();

            List<string> cfgNames = new List<string>();
            string @cfgName;

            configs.GetNext(pos);

            while(!pos.IsNull)
            {
                @cfgName = configs.GetNext(pos);
                cfgNames.Add(@cfgName);
            }

            return cfgNames;
        }

        public IEdmBomView4 GetBOM(IEdmFile18 file, string configuration)
        {
            object @bom = "BOM_For_ERP_WithData";
            return file.GetComputedBOM(@bom, 0, configuration, 0) as IEdmBomView4;
        }

        public List<PDMBOMItem> CreateBOM(IEdmBomView4 bom, List<string> ErrorTaskList)
        {
            EdmBomColumn[] ppoColumns;
            object[] ppoRows = null;
            bom.GetColumns(out ppoColumns);
            bom.GetRows(out ppoRows);
            var columnSize = ppoColumns.Length;
            var k = 0;

            List<PDMBOMItem> pdmBOMList = new List<PDMBOMItem>();

            for (int i = 0; i < ppoRows.Length; i++)
            {
                var ppoRow = (IEdmBomCell)ppoRows[i];

                var bomVariables = new BOMVariables();
                var bomItem = new PDMBOMItem();
                var integrationChecks = new IntegrationChecks();
                bomItem.IntegrationChecks = integrationChecks;

                k = 0;

                while (k < columnSize)
                {
                    ppoRow.GetVar(ppoColumns[k].mlVariableID, ppoColumns[k].meType, out var poValue, out var poComputedValue, out var pbsConfig, out var pbReadOnly);
                    Console.WriteLine(ppoColumns[k].mbsCaption);
                    Console.WriteLine(poValue);
                    var property = typeof(BOMVariables).GetProperty(Settings.bomProperty[k]);

                    if(poValue != null)
                    {
                        property.SetValue(bomVariables, poValue.ToString());
                    }

                    if (poValue.ToString() == "Dummy")
                    {
                        var file = SearchFileFromPath(ppoRow.GetPathName());
                        ErrorTaskList.Add(file.Name + " - Exclude from BOM");
                    }

                    k++;
                }
                var path = ppoRow.GetPathName();
                string[] parts = path.Split('\\');
                string filenameWithExtension = parts[parts.Length - 1];
                string[] filename = filenameWithExtension.Split('.');
                bomItem.Name = filename[0];
                bomItem.NameExtension = filenameWithExtension;
                bomItem.BOMVariables = bomVariables;
                bomItem.IsToolboxPart = path.Contains(Settings.ToolboxPath, StringComparison.CurrentCultureIgnoreCase);
                pdmBOMList.Add(bomItem);
            }

            return pdmBOMList;
        }

     
    }

    public class PDMPart
    {
        public IEdmFile18 file { get; set; }
        public IEdmFolder13 folder { get; set; }
        public PDMVariables variables;
    }
}
