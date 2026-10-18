namespace SRPortToolkit.NameType
{
    public class CharacterArchive
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Position { get; set; }
        public string Organization { get; set; }
        public string CharacterIntroduction1 { get; set; }
        public string CharacterIntroduction2 { get; set; }
        public string CharacterIntroduction3 { get; set; }
        public string CharacterIntroduction4 { get; set; }
        public string CharacterIntroduction5 { get; set; }
        public string CharacterRemarks1 { get; set; }
        public string CharacterRemarks2 { get; set; }
        public string CharacterRemarks3 { get; set; }
        public string CharacterRemarks4 { get; set; }
        public string CharacterRemarks5 { get; set; }


        public override string ToString() => this.Name;
    }
    public class PreLibraryOfDocumentFiles
    {
        public string KeyWord { get; set; }
        public string FILETitle1 { get; set; }
        public string FILECodes1 { get; set; }
        public string FILETitle2 { get; set; }
        public string FILECodes2 { get; set; }
        public string FILETitle3 { get; set; }
        public string FILECodes3 { get; set; }
        public string FILETitle4 { get; set; }
        public string FILECodes4 { get; set; }
        public string FILETitle5 { get; set; }
        public string FILECodes5 { get; set; }

        public override string ToString() => this.KeyWord;
    }
    public class FileArchive
    {
        public string FileCode { get; set; }
        public string FileTitle { get; set; }
        public string SecurityLevel { get; set; }
        public string Department { get; set; }
        public string State { get; set; }
        public string FileIntroduction1 { get; set; }
        public string FileIntroduction2 { get; set; }
        public string FileIntroduction3 { get; set; }
        public string FileIntroduction4 { get; set; }
        public string FileIntroduction5 { get; set; }

        public string FileRemarks1 { get; set; }
        public string FileRemarks2 { get; set; }
        public string FileRemarks3 { get; set; }
        public string FileRemarks4 { get; set; }
        public string FileRemarks5 { get; set; }


        public override string ToString() => this.FileCode;
    }
}
