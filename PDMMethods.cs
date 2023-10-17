using EPDM.Interop.epdm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDM_ERP_Checker
{
    public partial class PDMMethods
    {

        IEdmVault21 vault = new EdmVault5() as IEdmVault21;

        public void InitializePDM()
        {
            vault.LoginAuto("GMPDM", 0);
        }

        private IEdmSearch5 SearchFileName(string fileName)
        {
            IEdmSearch9 search = vault.CreateSearch2() as IEdmSearch9;

            search.FindFiles = true;
            search.FindFolders = false;
            search.SetToken(EdmSearchToken.Edmstok_AllVersions, false);
            search.FileName = fileName;
            return search;
        }

        private IEdmFile17 SearchFile(IEdmSearchResult5 search)
        {
            IEdmFile5 file;
            IEdmFolder5 folder;

            file = vault.GetFileFromPath(search.Path, out folder);
            return file as IEdmFile17;
        }

        private string SearchCardVariable(IEdmFile5 file, string variable)
        {
            if (file.Name.ToLower().EndsWith("sldprt") | file.Name.ToLower().EndsWith("slddrw") | file.Name.ToLower().EndsWith("sldasm"))
            {
                var enumerator = file.GetEnumeratorVariable();
                var configurations = file.GetConfigurations(0);
                var pos = configurations.GetHeadPosition();

                string cfgName = null;

                cfgName = configurations.GetNext(pos);
                cfgName = configurations.GetNext(pos); //Get to default

                enumerator.GetVar(variable, cfgName, out var cardVariable);
                if (cardVariable != null && !file.Name.ToLower().EndsWith("slddrw"))
                {
                    return cardVariable.ToString();
                }
                else
                {
                    enumerator.GetVar(variable, "@", out cardVariable);
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
            else
            {
                return "";
            }
        }

        public string GetPDMDescription(string id)
        {
            var results = SearchFileName(id);

            var result = results.GetFirstResult();

            var file = SearchFile(result);

            var description = SearchCardVariable(file, "Description");
            
            return description;
        }

        public PDMVariables GetPDMVariables(string id)
        {
            string[] pdmVariables = { "PartNo", "Number", "Description",
                "Drawn By", "ItemType", "ComponentGroup1",
                "ComponentGroup2", "ComponentGroup3", "ComponentGroup4",
                "ComponentGroup5", "Consumed number", "Length",
                "Material", "Manufacture", "Manufacturing Treatment",
                "Surface Treatment", "Date", "Weight", "State",
                "Type nr.", "UnitOfMeasure","Web direction","Revision",
                "Revision by","Last revision"};

            PDMVariables variables = new PDMVariables();

            var results = SearchFileName(id);
            var result = results.GetFirstResult();
            var file = SearchFile(result);

            var cardVariables = new Dictionary<string, string>();

            foreach (string variable in pdmVariables)
            {
                var temp = SearchCardVariable(file, variable);
                cardVariables[variable] = temp;
            }

            foreach (var kvp in cardVariables)
            {
                var variableName = kvp.Key;
                var variableValue = kvp.Value;

                var field = typeof(PDMVariables).GetField(variableName);
                if (field != null)
                {
                    field.SetValue(variables, variableValue);
                }
            }

            return variables;
        }
    }

    public class PDMVariables
    {
        public string PartNo = "PartNo";
        public string Number = "Number";
        public string Description = "Description";
        public string DrawnBy = "Drawn By";
        public string ItemType = "ItemType";
        public string ComponentGroup1 = "ComponentGroup1";
        public string ComponentGroup2 = "ComponentGroup2";
        public string ComponentGroup3 = "ComponentGroup3";
        public string ComponentGroup4 = "ComponentGroup4";
        public string ComponentGroup5 = "ComponentGroup5";
        public string ConsumedNumber = "Consumed number";
        public string Length = "Length";
        public string Material = "Material";
        public string Manufacture = "Manufacture";
        public string ManufacturingTreatment = "Manufacturing Treatment";
        public string SurfaceTreatment = "Surface Treatment";
        public string Date = "Date";
        public string Weight = "Weight";
        public string State = "State";
        public string TypeNumber = "Type nr.";
        public string UOM = "UnitOfMeasure";
        public string WebDirection = "Web direction";
        public string Revision = "Revision";
        public string RevisionBy = "Revision by";
        public string LastRevision = "Last revision";
    }

    public enum pdmVariables
    {
        PartNo,
        Number,
        Description,
        DrawnBy,
        ItemType,
        ComponentGroup1,
        ComponentGroup2,
        ComponentGroup3,
        ComponentGroup4,
        ComponentGroup5,
        ConsumedNumber,
        Length,
        Material,
        Manufacture,
        ManufacturingTreatment,
        SurfaceTreatment,
        Date,
        Weight,
        State,
        TypeNumber,
        UOM,
        WebDirection,
        Revision,
        RevisionBy,
        LastRevision,
    }
}
