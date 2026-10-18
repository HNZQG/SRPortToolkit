using Hacknet;
using Hacknet.Effects;
using Hacknet.Gui;
using Hacknet.UIUtils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Util;
using Pathfinder.Util.XML;
using SRPortToolkit.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SRPortToolkit.Daemon
{
    public class DatabaseDaemonPlus : Pathfinder.Daemon.BaseDaemon
    {
        public MovingBarsEffect sideEffect;
        public DatabaseDaemon.DatabasePermissions Permission;
        public DatabaseDaemonPlus.DatabaseState BeforeState;

        private DatabaseDaemonPlus.DatabaseState _state;
        public DatabaseDaemonPlus.DatabaseState State
        {
            get => _state;
            set
            {
                BeforeState = _state;
                _state = value;
            }
        }
        public Folder DatasetFolder;
        public Type CSDataType;
        public bool FilenameIsPersonName = false;
        public FileEntry ActiveFile = (FileEntry)null;
        public string errorMessage = "None";
        public object DeserializedFile = (object)null;
        public List<object> Dataset = (List<object>)null;
        public Color ThemeColor;
        public bool HadThemeColorApplied = true;
        public Vector2 BlockStartTopLeft;
        public float blockSize;
        public int blocksWide;
        public int blocksHigh;
        public bool HasSpecialCaseDraw = false;
        public Texture2D PlaceholderSprite;
        public Texture2D Triangle;
        public Dictionary<object, Texture2D> WildcardAssets = new Dictionary<object, Texture2D>();
        public ScrollableSectionedPanel ScrollPanel;
        public ScrollableTextRegion TextRegion;
        public string passwordResetHelperString = (string)null;
        public string adminResetEmailHostID = (string)null;
        public string adminResetPassEmailAccount = (string)null;
        public bool AdminEmailResetStarted = false;
        public bool AdminEmailResetHasHappened = false;
        public string passwordResetMessage = "";
        private string searchKeyword;
        private float elapsedTimeThisState = 0f;
        private float totalTimeThisState = 1.6f;

        [XMLStorage]
        public string Name;

        [XMLStorage]
        public string Permissions;

        [XMLStorage]
        public string Foldername;

        [XMLStorage]
        public string DataType;

        [XMLStorage]
        public Color? themeColor;

        [XMLStorage]
        public string AdminEmailAccount;

        [XMLStorage]
        public string AdminEmailHostID;

        [XMLStorage]
        public string SearchField;

        [XMLStorage]
        public bool CanBrowser = true;

        public DatabaseDaemonPlus(Computer computer, string serviceName, OS opSystem) : base(computer, serviceName, opSystem)
        {
            
        }

        public override void LoadFromXml(ElementInfo info)
        {
            base.LoadFromXml(info);
            this.name = Name;
            this.Permission = DatabaseDaemon.GetDatabasePermissionsFromString(Permissions);
            this.HadThemeColorApplied = themeColor.HasValue;
            if (!string.IsNullOrWhiteSpace(AdminEmailAccount))
            {
                adminResetEmailHostID = AdminEmailAccount;
                adminResetPassEmailAccount = AdminEmailHostID;
            }
            this.CSDataType = ObjectSerializer.GetTypeForName(DataType);
            this.CheckSearchField();
            this.LoadDataSetFromXmlInfo(info);
            this.Init(comp, os, Name, Permission, DataType, Foldername ?? "Database", themeColor ?? os.highlightColor);
        }

        public override XElement GetSaveElement()
        {
            var root = base.GetSaveElement();
            foreach (var obj in Dataset)
            {
                if (obj is ElementInfo el)
                {
                    root.Add(XElement.Parse(el.ToString()));
                }
            }
            return root;
        }

        private void CheckSearchField()
        {
            if (this.CSDataType == null)
            {
                throw new ArgumentNullException($"parser datatype[{DataType}] failed");
            }
            List<string> PublicField = new List<string>();
            foreach (var fieldInfo in this.CSDataType.GetFields())
            {
                PublicField.Add(fieldInfo.Name);
            }

            foreach (var property in this.CSDataType.GetProperties())
            {
                PublicField.Add(property.Name);
            }

            if (PublicField.Count == 0)
            {
                throw new ArgumentNullException("datatype not include public field or property");
            }

            if (this.SearchField == null)
            {
                this.SearchField = PublicField.First();
                return;
            }

            if (!PublicField.Contains(this.SearchField))
            {
                throw new ArgumentNullException($"datatype[{DataType}] not include searchField[{SearchField}]");
            }
        }

        private void LoadDataSetFromXmlInfo(ElementInfo info)
        {
            if (info.Children.Count == 0)
            {
                return;
            }

            if (CSDataType == null)
            {
                return;
            }

            List<object> objectList = new List<object>();
            foreach (var elementInfo in info.Children)
            {
                var executor = new EventExecutor(elementInfo.ToString(), false);
                executor.RegisterExecutor("*", (exec, subEleInfo) =>
                {
                    objectList.Add(subEleInfo);
                }, ParseOption.ParseInterior);

                executor.Parse();
            }
            this.Dataset = objectList;
        }

        public void Init(
          Computer c,
          OS os,
          string name,
          DatabaseDaemon.DatabasePermissions permissions,
          string DataTypeIdentifier,
          string Foldername,
          Color themeColor)
        {
            this.sideEffect = new MovingBarsEffect();
            this.Permission = permissions;
            this.State = DatabaseDaemonPlus.DatabaseState.Welcome;
            this.DataType = DataTypeIdentifier;
            this.CSDataType = ObjectSerializer.GetTypeForName(DataTypeIdentifier);
            this.ScrollPanel = new ScrollableSectionedPanel(26, GuiData.spriteBatch.GraphicsDevice);
            this.TextRegion = new ScrollableTextRegion(GuiData.spriteBatch.GraphicsDevice);
            this.Foldername = Foldername == null ? "Database" : Foldername;
            this.ThemeColor = themeColor;
            if (DataTypeIdentifier.EndsWith("NeopalsAccount"))
            {
                this.WildcardAssets.Add((object)Neopal.PetType.Blundo, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Blundo"));
                this.WildcardAssets.Add((object)Neopal.PetType.Chisha, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Chisha"));
                this.WildcardAssets.Add((object)Neopal.PetType.Jubdub, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Jubdub"));
                this.WildcardAssets.Add((object)Neopal.PetType.Kachici, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Kachici"));
                this.WildcardAssets.Add((object)Neopal.PetType.Kyrill, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Kyrill"));
                this.WildcardAssets.Add((object)Neopal.PetType.Myncl, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Myncl"));
                this.WildcardAssets.Add((object)Neopal.PetType.Pageri, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Pageri"));
                this.WildcardAssets.Add((object)Neopal.PetType.Psybunny, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Psybunny"));
                this.WildcardAssets.Add((object)Neopal.PetType.Scorchum, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Scorchum"));
                this.WildcardAssets.Add((object)Neopal.PetType.Unisam, os.content.Load<Texture2D>("DLC/Sprites/Neopals/Unisam"));
            }
            this.PlaceholderSprite = os.content.Load<Texture2D>("Sprites/Chip");
            this.Triangle = os.content.Load<Texture2D>("DLC/Sprites/Triangle");
        }

        public override void initFiles()
        {
            base.initFiles();
            this.DatasetFolder = this.comp.files.root.searchForFolder(this.Foldername);
            if (this.DatasetFolder == null)
            {
                this.DatasetFolder = new Folder(this.Foldername);
                this.comp.files.root.folders.Add(this.DatasetFolder);
            }
            this.InitDataset();
        }

        public override void loadInit()
        {
            base.loadInit();
            this.DatasetFolder = this.comp.files.root.searchForFolder(this.Foldername);
            this.CSDataType = ObjectSerializer.GetTypeForName(this.DataType);
            switch (this.DataType)
            {
                case "NeopalsAccount":
                    this.HasSpecialCaseDraw = true;
                    break;
            }
        }

        public void InitDataset()
        {
            if (this.Dataset != null)
            {
                this.FilenameIsPersonName = this.CSDataType == typeof(Person);
                for (int index = 0; index < this.Dataset.Count; ++index)
                {
                    string dataEntry = this.CleanXMLForFile(ObjectSerializer.SerializeObject(this.Dataset[index]));
                    string filenameForObject = this.GetFilenameForObject(this.Dataset[index]);
                    try
                    {
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Deserialization error for Generic Object: " + (object)ex);
                    }
                    this.DatasetFolder.files.Add(new FileEntry(dataEntry, filenameForObject));
                }
            }
            else if (this.DataType.EndsWith("VehicleInfo"))
            {
                this.FilenameIsPersonName = true;
                this.CSDataType = People.all[0].vehicles.GetType();
                for (int index = 0; index < People.all.Count; ++index)
                {
                    if (People.all[index].vehicles.Count != 0)
                    {
                        string dataEntry = this.CleanXMLForFile(ObjectSerializer.SerializeObject((object)People.all[index].vehicles));
                        List<VehicleRegistration> vehicles = People.all[index].vehicles;
                        try
                        {
                            string filenameForPersonName = this.GetFilenameForPersonName(People.all[index].firstName, People.all[index].lastName);
                            this.DatasetFolder.files.Add(new FileEntry(dataEntry, filenameForPersonName));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Deserialization error for Vehicle: " + (object)ex);
                        }
                    }
                }
            }
            else if (this.DataType.EndsWith("NeopalsAccount"))
            {
                this.FilenameIsPersonName = false;
                this.HasSpecialCaseDraw = true;
                this.CSDataType = typeof(NeopalsAccount);
                if (People.all.Count == 0)
                    return;
                for (int index = 0; index < People.all.Count; ++index)
                {
                    if (People.all[index].NeopalsAccount != null)
                    {
                        string dataEntry = this.CleanXMLForFile(ObjectSerializer.SerializeObject((object)People.all[index].NeopalsAccount));
                        NeopalsAccount neopalsAccount = People.all[index].NeopalsAccount;
                        try
                        {
                            string filenameForObject = this.GetFilenameForObject((object)neopalsAccount);
                            this.DatasetFolder.files.Add(new FileEntry(dataEntry, filenameForObject));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Deserialization error for NeopalsAccount: " + (object)ex);
                        }
                    }
                }
            }
            else
            {
                if (!this.DataType.EndsWith("Person"))
                    return;
                this.FilenameIsPersonName = true;
                this.CSDataType = People.all[0].GetType();
                for (int index = 0; index < People.all.Count; ++index)
                {
                    if (People.all[index].vehicles.Count != 0)
                    {
                        string dataEntry = this.CleanXMLForFile(ObjectSerializer.SerializeObject((object)People.all[index]));
                        Person person = People.all[index];
                        try
                        {
                            string filenameForPersonName = this.GetFilenameForPersonName(People.all[index].firstName, People.all[index].lastName);
                            this.DatasetFolder.files.Add(new FileEntry(dataEntry, filenameForPersonName));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Deserialization error for People: " + (object)ex);
                        }
                    }
                }
            }
        }

        public override void navigatedTo()
        {
            base.navigatedTo();
            this.elapsedTimeThisState = 0.0f;
            this.State = DatabaseDaemonPlus.DatabaseState.Welcome;
            this.AdminEmailResetStarted = false;
        }

        public object GetObjectForRecordName(string recordName)
        {
            for (int index = 0; index < this.DatasetFolder.files.Count; ++index)
            {
                if (this.DatasetFolder.files[index].name.ToLower().Contains(recordName.ToLower()))
                    return ObjectSerializer.DeserializeObject(Hacknet.Utils.GenerateStreamFromString(this.DeCleanXMLForFile(this.DatasetFolder.files[index].data)), this.CSDataType);
            }
            return (object)null;
        }

        public void ResetAccessPassword()
        {
            this.AdminEmailResetHasHappened = true;
            try
            {
                MailServer daemon = (MailServer)Programs.getComputer(this.os, this.adminResetEmailHostID).getDaemon(typeof(MailServer));
                string userTo = this.adminResetPassEmailAccount;
                if (this.adminResetPassEmailAccount.Contains("@"))
                    userTo = this.adminResetPassEmailAccount.Substring(0, this.adminResetPassEmailAccount.IndexOf("@"));
                string str = Hacknet.Utils.getRandomLetter().ToString() + (object)Hacknet.Utils.getRandomLetter() + (object)Hacknet.Utils.getRandomNumberChar() + (object)Hacknet.Utils.getRandomLetter() + (object)Hacknet.Utils.getRandomNumberChar();
                this.comp.adminPass = str;
                this.comp.adminIP = this.comp.ip;
                for (int index = 0; index < this.comp.users.Count; ++index)
                {
                    if (this.comp.users[index].name.ToLower() == "admin")
                    {
                        UserDetail user = this.comp.users[index];
                        user.pass = str;
                        this.comp.users[index] = user;
                    }
                }
                string body = string.Format(Hacknet.Utils.readEntireFile("Content/DLC/Docs/DatabasePasswordResetEmail.txt"), (object)this.name, (object)str);
                daemon.addMail(MailServer.generateEmail(string.Format("{0} Password Reset", (object)this.name), body, "AdminBot"), userTo);
                this.passwordResetMessage = "Password Reset Complete";
            }
            catch (Exception ex)
            {
                this.passwordResetMessage = "CRITICAL RESET ERROR\n" + ex.ToString();
            }
        }

        public string CleanXMLForFile(string data)
        {
            return data.Replace("\t", "  ");
        }

        public string DeCleanXMLForFile(string data) => data.Replace("\t", "  ");

        public string GetFilenameForPersonName(string firstname, string lastname)
        {
            return Hacknet.Utils.GetNonRepeatingFilename((lastname + "_" + firstname).ToLower(), ".rec", this.DatasetFolder);
        }

        public string GetFilenameForObject(object obj)
        {
            return DatabaseDaemonPatch.GetFilenameForObject(obj, this.DatasetFolder);
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);
            this.UpdateStates((float)this.os.lastGameTime.ElapsedGameTime.TotalSeconds);
            Rectangle dest = Hacknet.Utils.InsetRectangle(bounds, 2);
            this.DrawBackground(dest, sb, 6);
            switch (this.State)
            {
                case DatabaseDaemonPlus.DatabaseState.Welcome:
                case DatabaseDaemonPlus.DatabaseState.Search:
                case DatabaseDaemonPlus.DatabaseState.Searching:
                    this.DrawWelcome(dest, sb);
                    break;
                case DatabaseDaemonPlus.DatabaseState.Browse:
                    this.DrawBrowse(dest, sb);
                    break;
                case DatabaseDaemonPlus.DatabaseState.EntryDisplay:
                    this.DrawEntry(dest, sb);
                    break;
                case DatabaseDaemonPlus.DatabaseState.Error:
                    this.DrawError(dest, sb);
                    break;
            }
        }

        public void UpdateStates(float t)
        {
            this.elapsedTimeThisState += t;
            if ((double)this.elapsedTimeThisState < (double)this.totalTimeThisState || this.State != DatabaseState.Searching)
                return;
            this.LookupEntry();
        }


        private void LookupEntry()
        {
            if (string.IsNullOrEmpty(this.searchKeyword))
            {
                this.errorMessage = LocaleTerms.Loc($"Error in {SearchField} input");
                this.State = DatabaseDaemonPlus.DatabaseState.Error;
                this.elapsedTimeThisState = 0f;
                return;
            }

            if (this.Dataset.Count == 0)
            {
                this.errorMessage = LocaleTerms.Loc($"Not Find Record");
                this.State = DatabaseDaemonPlus.DatabaseState.Error;
                this.elapsedTimeThisState = 0f;
                return;
            }

            this.ActiveFile = null;
            for (var i = 0; i < this.Dataset.Count; i++)
            {
                var obj = this.Dataset[i];
                if (obj is ElementInfo elementInfo)
                {
                    if (SearchKwInElementInfo(elementInfo))
                    {
                        this.ActiveFile = this.DatasetFolder.files[i];
                        break;
                    }
                }
                else
                {
                    if (obj.GetType().GetField(SearchField)?.GetValue(obj).ToString() == searchKeyword)
                    {
                        this.ActiveFile = this.DatasetFolder.files[i];
                        break;
                    }

                    if (obj.GetType().GetProperty(SearchField)?.GetValue(obj,null).ToString() == searchKeyword)
                    {
                        this.ActiveFile = this.DatasetFolder.files[i];
                        break;
                    }
                }
            }

            if (this.ActiveFile == null)
            {
                this.errorMessage = LocaleTerms.Loc($"Not Find Record");
                this.State = DatabaseDaemonPlus.DatabaseState.Error;
                this.elapsedTimeThisState = 0f;
                return;
            }

            this.DeserializedFile = null;
            this.elapsedTimeThisState = 0f;
            this.State = DatabaseState.EntryDisplay;
        }

        private bool SearchKwInElementInfo(ElementInfo elementInfo)
        {
            foreach (var child in elementInfo.Children)
            {
                if (child.Name == SearchField && child.Content == this.searchKeyword)
                {
                    return true;
                }

                if (SearchKwInElementInfo(child))
                {
                    return true;
                }
            }

            return false;
        }

        public void DrawWelcome(Rectangle dest, SpriteBatch sb)
        {
            this.TextRegion.UpdateScroll(0.0f);
            Rectangle drawRectForRow1 = this.GetDrawRectForRow(0, 12);
            bool flag = this.Permission == DatabaseDaemon.DatabasePermissions.Public || this.comp.adminIP == this.os.thisComputer.ip;
            TextItem.doCenteredFontLabel(drawRectForRow1, this.name, GuiData.font, Color.White, true);
            PatternDrawer.draw(new Rectangle(dest.X + 1, (int)this.BlockStartTopLeft.Y, dest.Width - 2, (int)((double)this.blockSize * 2.0)), 0.5f, Color.Transparent, (flag ? this.ThemeColor : Hacknet.Utils.AddativeRed) * 0.2f, sb, flag ? PatternDrawer.thinStripe : PatternDrawer.errorTile);
            TextItem.doCenteredFontLabel(this.GetDrawRectForRow(1, 12), flag ? LocaleTerms.Loc("Access Granted") : LocaleTerms.Loc("Administrator Login Required"), GuiData.font, flag ? this.ThemeColor * 0.8f : Hacknet.Utils.AddativeRed * 0.8f, true);
            Rectangle drawRectForRow2 = this.GetDrawRectForRow(3, 14);
            if (CanBrowser)
            {
                if (flag && Button.doButton(73616101, (int)this.BlockStartTopLeft.X, drawRectForRow2.Y, dest.Width / 2, drawRectForRow2.Height, LocaleTerms.Loc("Browse Records"), new Color?(this.ThemeColor)))
                    this.State = DatabaseDaemonPlus.DatabaseState.Browse;
                drawRectForRow2.Y += drawRectForRow2.Height + 10;
            }
            drawRectForRow2.Height /= 2;

            if (this.State == DatabaseState.Welcome)
            {
                // 搜索
                if (flag && Button.doButton(90000001, (int)this.BlockStartTopLeft.X, drawRectForRow2.Y, dest.Width / 2, drawRectForRow2.Height, LocaleTerms.Loc("Search"), new Color?(this.ThemeColor)))
                {
                    this.State = DatabaseState.Search;
                    this.elapsedTimeThisState = 0f;
                    GetStringUIControl.StartGetString(SearchField, (object)this.os);
                }
                drawRectForRow2.Y += drawRectForRow2.Height + 10;
                if (Button.doButton(73616102, (int)this.BlockStartTopLeft.X, drawRectForRow2.Y, dest.Width / 2,
                        drawRectForRow2.Height, LocaleTerms.Loc("Login"), new Color?(this.ThemeColor)))
                {
                    this.os.execute("login");
                    this.elapsedTimeThisState = 0f;
                }

                drawRectForRow2.Y += drawRectForRow2.Height + 6;
            }
            else if (flag && this.State == DatabaseState.Search)
            {
                float num = Hacknet.Utils.QuadraticOutCurve(Math.Min(1f, this.elapsedTimeThisState * 2f));
                Rectangle bounds1 = new Rectangle((int)this.BlockStartTopLeft.X, drawRectForRow2.Y, drawRectForRow2.Width, (int)((double)num * 72.0));
                Rectangle destinationRectangle3 = new Rectangle((int)this.BlockStartTopLeft.X, drawRectForRow2.Y + 14, drawRectForRow2.Width, (int)((double)num * 32.0));
                sb.Draw(Hacknet.Utils.white, destinationRectangle3, this.os.darkBackgroundColor);
                string stringControl = GetStringUIControl.DrawGetStringControl(LocaleTerms.Loc($"Enter {SearchField}") + " :", bounds1, (Action)(() =>
                {
                    this.elapsedTimeThisState = 0.0f;
                    this.State = DatabaseState.Error;
                    this.errorMessage = LocaleTerms.Loc($"Error in {SearchField} input");
                }), (Action)(() =>
                {
                    this.elapsedTimeThisState = 0.0f;
                    this.State = DatabaseState.Welcome;
                    this.os.terminal.executeLine();
                }), sb, (object)this.os, ThemeColor, Color.Black, "", new Color?(Color.Transparent));
                if (stringControl != null)
                {
                    this.searchKeyword = stringControl;
                    this.elapsedTimeThisState = 0.0f;
                    this.State = DatabaseState.Searching;
                    this.totalTimeThisState = 1.6f;
                }

                drawRectForRow2.Y += bounds1.Height + destinationRectangle3.Height + 6;
            }
            else if (flag && this.State == DatabaseState.Searching)
            {
                Rectangle destinationRectangle4 = new Rectangle((int)this.BlockStartTopLeft.X, drawRectForRow2.Y, drawRectForRow2.Width, 24);
                sb.Draw(Hacknet.Utils.white, destinationRectangle4, this.os.darkBackgroundColor);
                destinationRectangle4.Width = (int)((double)destinationRectangle4.Width * (double)Hacknet.Utils.QuadraticOutCurve(this.elapsedTimeThisState / this.totalTimeThisState));
                sb.Draw(Hacknet.Utils.white, destinationRectangle4, this.ThemeColor);
                destinationRectangle4.Y += destinationRectangle4.Height / 2 - 2;
                destinationRectangle4.Height = 4;
                sb.Draw(Hacknet.Utils.white, destinationRectangle4, this.ThemeColor);

                drawRectForRow2.Y = destinationRectangle4.Y + destinationRectangle4.Height + 6;
            }


            Color color = Color.Lerp(Color.Black, this.ThemeColor, 0.5f);
            if (!string.IsNullOrWhiteSpace(this.adminResetPassEmailAccount) && Button.doButton(73616106, (int)this.BlockStartTopLeft.X, drawRectForRow2.Y, dest.Width / 2, drawRectForRow2.Height, this.AdminEmailResetHasHappened ? LocaleTerms.Loc("Password Reset In Cooldown") : (this.AdminEmailResetStarted ? LocaleTerms.Loc("Cancel") : LocaleTerms.Loc("Reset Access Password")), new Color?(this.AdminEmailResetStarted ? (this.AdminEmailResetHasHappened ? Color.Gray : Color.DarkRed) : color)))
                this.AdminEmailResetStarted = !this.AdminEmailResetStarted;
            if (this.AdminEmailResetStarted)
            {
                Rectangle rectangle = this.GetDrawRectForRow(3, 10);
                rectangle.X += dest.Width / 2 + 10;
                rectangle.Width -= dest.Width / 2;
                rectangle.Height = Math.Max(300, (int)((double)this.blockSize * 3.0 - 30.0));
                rectangle.Width += 30;
                if (rectangle.Height == 300)
                    rectangle.Y -= (int)this.blockSize;
                Color backingColor = Color.Gray * 0.3f;
                sb.Draw(Hacknet.Utils.white, rectangle, Color.Black * 0.8f);
                rectangle = Hacknet.Utils.InsetRectangle(rectangle, 2);
                PatternDrawer.draw(rectangle, 0.5f, backingColor, backingColor * 0.4f, sb, PatternDrawer.thinStripe);
                int num = 20;
                Rectangle destinationRectangle = new Rectangle(rectangle.X - num - 2, drawRectForRow2.Y + (drawRectForRow2.Height - num) / 2, num, num);
                sb.Draw(this.Triangle, destinationRectangle, Color.Black * 0.8f);
                Rectangle dest1 = new Rectangle(rectangle.X + 6, rectangle.Y, rectangle.Width - 12, rectangle.Height / 6);
                TextItem.doCenteredFontLabel(dest1, LocaleTerms.Loc("Reset Admin Password"), GuiData.font, Color.LightGray, true);
                Rectangle dest2 = new Rectangle(dest1.X, dest1.Y + dest1.Height + 4, dest1.Width + 9, rectangle.Height / 6 * 4);
                if (this.passwordResetHelperString == null)
                    this.passwordResetHelperString = Hacknet.Utils.readEntireFile("Content/DLC/Docs/PasswordResetText.txt");
                string data = this.passwordResetHelperString.Replace("[PASSWORD]", this.adminResetPassEmailAccount);
                if (this.AdminEmailResetHasHappened)
                    data = this.passwordResetMessage;
                string text = Hacknet.Utils.SuperSmartTwimForWidth(data, dest2.Width, GuiData.UITinyfont);
                TextItem.doFontLabelToSize(dest2, text, GuiData.tinyfont, Color.White, true, true);
                if (!this.AdminEmailResetHasHappened && Button.doButton(73606111, rectangle.X + 4, rectangle.Y + rectangle.Height - 28, rectangle.Width - 8, 26, LocaleTerms.Loc("Reset Password"), new Color?(Color.Red)))
                    this.ResetAccessPassword();
            }
            drawRectForRow2.Y += drawRectForRow2.Height + 6;
            if (!Button.doButton(73616129, (int)this.BlockStartTopLeft.X, drawRectForRow2.Y, dest.Width / 2, drawRectForRow2.Height, LocaleTerms.Loc("Exit"), new Color?(Color.Black)))
                return;
            if (this.State == DatabaseState.Search)
            {
                this.elapsedTimeThisState = 0.0f;
                this.State = DatabaseState.Welcome;
                this.os.terminal.executeLine();
            }
            this.os.display.command = "connect";
        }

        public void DrawError(Rectangle dest, SpriteBatch sb)
        {
            Rectangle drawRectForRow1 = this.GetDrawRectForRow(0, 12);
            bool flag = this.Permission == DatabaseDaemon.DatabasePermissions.Public || this.comp.adminIP == this.os.thisComputer.ip;
            TextItem.doFontLabelToSize(drawRectForRow1, this.name, GuiData.font, Color.White, offsetToTopLeft: true);
            PatternDrawer.draw(new Rectangle(dest.X + 1, (int)this.BlockStartTopLeft.Y, dest.Width - 2, (int)((double)this.blockSize * 2.0)), 0.5f, Color.Transparent, Hacknet.Utils.AddativeRed * 0.2f, sb, flag ? PatternDrawer.thinStripe : PatternDrawer.errorTile);
            TextItem.doFontLabelToSize(this.GetDrawRectForRow(1, 12), LocaleTerms.Loc("ERROR"), GuiData.font, Hacknet.Utils.AddativeRed * 0.8f, true, true);
            Rectangle drawRectForRow2 = this.GetDrawRectForRow(2, 14);
            if (Button.doButton(73616101, (int)this.BlockStartTopLeft.X, drawRectForRow2.Y, dest.Width / 2, drawRectForRow2.Height, LocaleTerms.Loc("Back"), new Color?(this.ThemeColor)))
                this.State = DatabaseDaemonPlus.DatabaseState.Welcome;
            Rectangle rectangle = this.GetDrawRectForRow(3, 14);
            rectangle.Height = (int)(((double)this.blocksHigh - 4.0) * (double)this.blockSize);
            rectangle = Hacknet.Utils.InsetRectangle(rectangle, 2);
            string text = Hacknet.Utils.SuperSmartTwimForWidth(this.errorMessage, rectangle.Width, GuiData.smallfont);
            this.TextRegion.Draw(rectangle, text, sb, Hacknet.Utils.AddativeRed);
        }

        public void DrawBrowse(Rectangle dest, SpriteBatch spriteBatch)
        {
            Rectangle drawRectForRow1 = this.GetDrawRectForRow(0, 12);
            bool flag = this.Permission == DatabaseDaemon.DatabasePermissions.Public || this.comp.adminIP == this.os.thisComputer.ip;
            TextItem.doFontLabelToSize(drawRectForRow1, string.Format(LocaleTerms.Loc("{0} : Records"), (object)this.name), GuiData.font, Color.White, true, true);
            Rectangle drawRectForRow2 = this.GetDrawRectForRow(0, 0);
            PatternDrawer.draw(drawRectForRow2, 0.5f, Color.Transparent, this.ThemeColor * 0.2f, spriteBatch, PatternDrawer.thinStripe);
            Rectangle rectangle = new Rectangle(drawRectForRow2.X + 4, drawRectForRow2.Y + drawRectForRow2.Height / 2 + 12, drawRectForRow2.Width / 3, drawRectForRow2.Height / 2 - 15);
            if (Button.doButton(71118000, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, LocaleTerms.Loc("Back"), new Color?(Color.Gray)))
                this.State = DatabaseDaemonPlus.DatabaseState.Welcome;
            if (this.CSDataType == (Type)null || string.IsNullOrWhiteSpace(this.DataType) || this.DataType.ToLower() == "none")
            {
                Rectangle drawRectForRow3 = this.GetDrawRectForRow(1, 2);
                drawRectForRow3.Height += (int)((double)this.blockSize * (double)(this.blocksHigh - 3));
                PatternDrawer.draw(drawRectForRow3, 0.4f, Color.Black * 0.3f, Hacknet.Utils.makeColorAddative(this.ThemeColor) * 0.25f, spriteBatch, PatternDrawer.binaryTile);
                TextItem.doCenteredFontLabel(this.GetDrawRectForRow(3, 8), " - " + LocaleTerms.Loc("API Access Enabled") + " - ", GuiData.smallfont, Color.White);
            }
            else
            {
                Rectangle ListDest = this.GetDrawRectForRow(1, 4);
                ListDest.Height += (int)((double)this.blockSize * ((double)this.blocksHigh - 2.5));
                this.ScrollPanel.NumberOfPanels = this.DatasetFolder.files.Count;
                this.ScrollPanel.Draw((Action<int, Rectangle, SpriteBatch>)((i, bounds, sb) =>
                {
                    FileEntry file = this.DatasetFolder.files[i];
                    string text = "REC#" + i.ToString("000") + " : " + this.GetAnnounceNameForFileName(file.name);
                    int width = Math.Max(160, bounds.Width / 3);
                    if (Button.doButton(71118100 + i, bounds.X, bounds.Y + 2, width, bounds.Height - 4, string.Format(LocaleTerms.Loc("View Record #{0}"), (object)i.ToString("000")), new Color?(this.ThemeColor)))
                    {
                        this.ActiveFile = this.DatasetFolder.files[i];
                        this.State = DatabaseDaemonPlus.DatabaseState.EntryDisplay;
                        this.DeserializedFile = (object)null;
                    }
                    Vector2 vector2 = GuiData.smallfont.MeasureString(text);
                    Rectangle destinationRectangle = new Rectangle(bounds.X + bounds.Width / 3 + 6, bounds.Y + bounds.Height / 2 - 1, ListDest.Width - (width + (int)vector2.X + 26), 1);
                    sb.Draw(Hacknet.Utils.white, destinationRectangle, Color.White * 0.1f);
                    TextItem.doFontLabel(new Vector2((float)(destinationRectangle.X + destinationRectangle.Width) + 6f, (float)bounds.Y + 2f), text, GuiData.smallfont, new Color?(Color.White), (float)bounds.Width, (float)bounds.Height);
                }), spriteBatch, ListDest);
            }
        }

        public void DrawEntry(Rectangle dest, SpriteBatch spriteBatch)
        {
            if (this.DeserializedFile == null)
            {
                try
                {
                    var executor = new EventExecutor(ActiveFile.data, false);
                    executor.RegisterExecutor("*", (exec, info) =>
                    {
                        this.DeserializedFile = info;
                    }, ParseOption.ParseInterior);
                    executor.Parse();
                }
                catch (Exception ex)
                {
                    this.State = DatabaseDaemonPlus.DatabaseState.Error;
                    this.errorMessage = LocaleTerms.Loc("Error De-serializing Entry") + " : " + this.ActiveFile.name + "\n" + Hacknet.Utils.GenerateReportFromException(ex).Replace("Hacknet", "Database");
                    return;
                }
            }
            Rectangle drawRectForRow = this.GetDrawRectForRow(0, 0);
            drawRectForRow.Height /= 2;
            TextItem.doFontLabelToSize(drawRectForRow, string.Format(LocaleTerms.Loc("Entry for {0}"), (object)this.GetAnnounceNameForFileName(this.ActiveFile.name)), GuiData.font, Color.White, true, true);
            drawRectForRow.Y += drawRectForRow.Height;
            if (Button.doButton(7301991, drawRectForRow.X, drawRectForRow.Y, drawRectForRow.Width / 2, drawRectForRow.Height, LocaleTerms.Loc("Back"), new Color?(Color.Gray)))
            {
                this.State = this.BeforeState == DatabaseState.Searching ? DatabaseState.Welcome : DatabaseDaemonPlus.DatabaseState.Browse;
                this.ActiveFile = (FileEntry)null;
                this.DeserializedFile = (object)null;
                this.elapsedTimeThisState = 0f;
            }
            else
            {
                Rectangle bounds = new Rectangle((int)this.BlockStartTopLeft.X, (int)this.BlockStartTopLeft.Y + (int)this.blockSize, (int)((double)this.blockSize * (double)(this.blocksWide - 1)), (int)((double)this.blockSize * (double)(this.blocksHigh - 2)));
                if (this.HasSpecialCaseDraw)
                    ReflectiveRenderer.PreRenderForObject += (Action<Vector2, Type, string>)((pos, targetType, targetValue) => this.DrawSpecialCase(pos, bounds, targetType, targetValue, spriteBatch));
                ReflectiveRenderer.RenderObject(this.DeserializedFile, bounds, spriteBatch, this.ScrollPanel, this.ThemeColor);
                if (this.HasSpecialCaseDraw)
                    ReflectiveRenderer.PreRenderForObject = (Action<Vector2, Type, string>)null;
            }
        }

        public string GetAnnounceNameForFileName(string filename)
        {
            filename = filename.Replace(".rec", "");
            if (!this.FilenameIsPersonName)
                return filename;
            string[] strArray = filename.Split(new char[1] { '_' }, StringSplitOptions.None);
            return strArray[1] + " " + strArray[0];
        }

        public Rectangle GetDrawRectForRow(int row, int inset)
        {
            return Hacknet.Utils.InsetRectangle(new Rectangle((int)this.BlockStartTopLeft.X, (int)((double)this.BlockStartTopLeft.Y + (double)this.blockSize * (double)row), (int)((double)this.blockSize * (double)(this.blocksWide - 1)), (int)this.blockSize), inset);
        }

        public void DrawBackground(Rectangle dest, SpriteBatch sb, int desiredNumOfBlocks)
        {
            Color color = Color.Gray * 0.4f;
            int num1 = dest.Width / (desiredNumOfBlocks + 2);
            int width = 9;
            float num2 = 1f;
            int x = dest.X + num1 / 2;
            int y = dest.Y + num1 / 2;
            this.BlockStartTopLeft = new Vector2((float)x, (float)y);
            this.blocksWide = this.blocksHigh = 0;
            this.blockSize = (float)num1;
            do
            {
                this.blocksWide = 0;
                do
                {
                    Rectangle destinationRectangle1 = new Rectangle(Math.Max(dest.X, x - width / 2), y - (int)((double)num2 / 2.0 + 0.5), width, (int)num2);
                    sb.Draw(Hacknet.Utils.white, destinationRectangle1, color);
                    Rectangle destinationRectangle2 = new Rectangle(x - (int)((double)num2 / 2.0), Math.Max(dest.Y, y - (int)((double)width / 2.0 + 0.5)), (int)num2, width / 2 - (int)((double)num2 / 2.0));
                    sb.Draw(Hacknet.Utils.white, destinationRectangle2, color);
                    Rectangle destinationRectangle3 = new Rectangle(x - (int)((double)num2 / 2.0), y + (int)((double)num2 / 2.0), (int)num2, width / 2 - (int)((double)num2 / 2.0));
                    sb.Draw(Hacknet.Utils.white, destinationRectangle3, color);
                    x += num1;
                    ++this.blocksWide;
                }
                while (x <= dest.X + dest.Width - num1 / 2);
                x = dest.X + num1 / 2;
                y += num1;
                ++this.blocksHigh;
            }
            while (y <= dest.Y + dest.Height - num1 / 2);
        }

        public void DrawSpecialCase(
          Vector2 currentPos,
          Rectangle totalArea,
          Type objType,
          string drawnValue,
          SpriteBatch sb)
        {
            if (!(sb.Name != "AltRTBatch") || !(objType == typeof(Neopal.PetType)))
                return;
            int num1 = Math.Min(ReflectiveRenderer.GetEntryLineHeight() * 6, totalArea.Width / 4);
            Texture2D sprite = this.PlaceholderSprite;
            Neopal.PetType key = (Neopal.PetType)Enum.Parse(objType, drawnValue);
            if (this.WildcardAssets.ContainsKey((object)key))
                sprite = this.WildcardAssets[(object)key];
            Rectangle petDrawPos = new Rectangle(totalArea.X + totalArea.Width - num1 - 10, (int)currentPos.Y + 4, num1, num1);
            double num2 = (double)sprite.Height / (double)petDrawPos.Height;
            int val2 = totalArea.Y + totalArea.Height - petDrawPos.Y;
            int height = Math.Min(sprite.Height, (int)(num2 * (double)val2));
            Rectangle? clip = new Rectangle?(new Rectangle(0, 0, sprite.Width, height));
            petDrawPos.Height = Math.Min(petDrawPos.Height, val2);
            this.os.postFXDrawActions += (Action)(() => sb.Draw(sprite, petDrawPos, clip, Color.White));
        }




    public enum DatabaseState
        {
            Welcome,
            Search,
            Browse,
            Loading,
            EntryDisplay,
            Error,
            Searching
        }
    }
}
