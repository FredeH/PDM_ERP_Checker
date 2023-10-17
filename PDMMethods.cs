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

        public PDMVariables GetPDMVariableCard(IEdmFile5 file)
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

                var property = typeof(PDMVariables).GetProperty(variableName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(variables, variableValue);
                }
            }

            Console.WriteLine(variables.Description);

            return variables;
        }

        public PDMPart GetPDMFile(string id)
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

            IEdmFile5 file;
            IEdmFolder5 folder;

            file = vault.GetFileFromPath(result.Path, out folder);

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

                var property = typeof(PDMVariables).GetProperty(variableName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(variables, variableValue);
                }
            }

            PDMPart part = new PDMPart() { file = file, folder = folder, variables = variables };

            return part;
        }

        public List<IEdmReference11> GetReferences(IEdmFile5 file, IEdmFolder5 folder)
        {
            IEdmReference11 references = file.GetReferenceTree(folder.ID) as IEdmReference11;

            string copyString = "";


            IEdmPos5 pos = default(IEdmPos5);
            IEdmReference11 @ref = default(IEdmReference11);
            bool Top = true;

            pos = references.GetFirstChildPosition3("", Top, true, (int)EdmRefFlags.EdmRef_File, "", 0);
            List<IEdmReference11> refs = new List<IEdmReference11>();

            while ((!pos.IsNull))
            {
                @ref = (IEdmReference11) references.GetNextChild(pos);
                refs.Add(@ref);
            }

            return refs;
        }
    }

    public class PDMPart
    {
        public IEdmFile5 file { get; set; }
        public IEdmFolder5 folder { get; set; }
        public PDMVariables variables;
    }

    public class PDMVariables
    {
        public string PartNo { get; set; } = "PartNo";
        public string Number { get; set; } = "Number";
        public string Description { get; set; } = "Description";
        public string DrawnBy { get; set; } = "Drawn By";
        public string ItemType { get; set; } = "ItemType";
        public string ComponentGroup1 { get; set; } = "ComponentGroup1";
        public string ComponentGroup2 { get; set; } = "ComponentGroup2";
        public string ComponentGroup3 { get; set; } = "ComponentGroup3";
        public string ComponentGroup4 { get; set; } = "ComponentGroup4";
        public string ComponentGroup5 { get; set; } = "ComponentGroup5";
        public string ConsumedNumber { get; set; } = "Consumed number";
        public string Length { get; set; } = "Length";
        public string Material { get; set; } = "Material";
        public string Manufacture { get; set; } = "Manufacture";
        public string ManufacturingTreatment { get; set; } = "Manufacturing Treatment";
        public string SurfaceTreatment { get; set; } = "Surface Treatment";
        public string Date { get; set; } = "Date";
        public string Weight { get; set; } = "Weight";
        public string State { get; set; } = "State";
        public string TypeNumber { get; set; } = "Type nr.";
        public string UOM { get; set; } = "UnitOfMeasure";
        public string WebDirection { get; set; } = "Web direction";
        public string Revision { get; set; } = "Revision";
        public string RevisionBy { get; set; } = "Revision by";
        public string LastRevision { get; set; } = "Last revision";
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
