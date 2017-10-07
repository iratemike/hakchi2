using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using com.clusterrr.Famicom;
using com.clusterrr.hakchi_gui.Properties;

namespace com.clusterrr.hakchi_gui {
	internal class GameGenieCode {
		public delegate void ChangedEvent(GameGenieCode e);

		private string FCode = "";
		private string FDescription = "";

		public GameGenieCode(string ACode, string ADescription) {
			Code = ACode;
			Description = ADescription;
		}

		public string Code {
			get => FCode;
			set {
				if (FCode != value) {
					OldCode = FCode;
					FCode = value;
					if (OldCode != "" && Changed != null)
						Changed(this);
				}
			}
		}

		public string Description {
			get => FDescription;
			set {
				if (FDescription != value) {
					OldDescription = FDescription;
					FDescription = value;
					if (OldDescription != "" && Changed != null)
						Changed(this);
				}
			}
		}

		public string OldCode { get; private set; }

		public string OldDescription { get; private set; }

		public event ChangedEvent Changed;

		public override string ToString() {
			return Description;
		}
	}

	internal class GameGenieDataBase {
		private readonly NesMiniApplication FGame;
		private List<GameGenieCode> FGameCodes;
		private XmlNode FGameNode;
		private readonly XmlDocument FXml = new XmlDocument();

		private readonly string originalDatabasePath =
			Path.Combine(Path.Combine(Program.BaseDirectoryInternal, "data"), "GameGenieDB.xml");

		private readonly string userDatabasePath =
			Path.Combine(Path.Combine(Program.BaseDirectoryExternal, ConfigIni.ConfigDir), "GameGenieDB.xml");

		public GameGenieDataBase(NesMiniApplication AGame) {
			//DataBasePath = Path.Combine(Path.Combine(Program.BaseDirectoryInternal, "data"), "GameGenieDB.xml");
			FGame = AGame;
			//FDBName = DataBasePath;
			if (File.Exists(userDatabasePath))
				FXml.Load(userDatabasePath);
			else if (File.Exists(originalDatabasePath))
				FXml.Load(originalDatabasePath);
			else
				FXml.AppendChild(FXml.CreateElement("database"));
		}

		private XmlNode GameNode {
			get {
				if (FGameNode == null) {
					FGameNode = FXml.SelectSingleNode(string.Format("/database/game[@code='{0}']", FGame.Code));

					if (FGameNode == null) {
						var lGamesDir = Path.Combine(Program.BaseDirectoryExternal, "games");
						var lGame = new NesFile(Path.Combine(Path.Combine(lGamesDir, FGame.Code), FGame.Code + ".nes"));
						XmlAttribute lXmlAttribute;

						FGameNode = FXml.CreateElement("game");
						FXml.DocumentElement.AppendChild(FGameNode);

						lXmlAttribute = FXml.CreateAttribute("code");
						lXmlAttribute.Value = FGame.Code;
						FGameNode.Attributes.Append(lXmlAttribute);

						lXmlAttribute = FXml.CreateAttribute("name");
						lXmlAttribute.Value = FGame.Name;
						FGameNode.Attributes.Append(lXmlAttribute);

						lXmlAttribute = FXml.CreateAttribute("crc");
						lXmlAttribute.Value = lGame.CRC32.ToString("X");
						FGameNode.Attributes.Append(lXmlAttribute);
					}
				}
				return FGameNode;
			}
		}

		public List<GameGenieCode> GameCodes {
			get {
				if (FGameCodes == null) {
					FGameCodes = new List<GameGenieCode>();
					var lCodes = FXml.SelectNodes(string.Format("/database/game[@code='{0}']//gamegenie", FGame.Code));
					foreach (XmlNode lCurNode in lCodes) {
						var lCurCode = new GameGenieCode(lCurNode.Attributes.GetNamedItem("code").Value,
							lCurNode.Attributes.GetNamedItem("description").Value);
						FGameCodes.Add(lCurCode);
					}
				}
				return FGameCodes;
			}
		}

		public bool Modified { get; private set; }

		public void AddCode(GameGenieCode ACode) {
			Modified = true;

			XmlNode lCodeNode = FXml.CreateElement("gamegenie");
			GameNode.AppendChild(lCodeNode);

			XmlAttribute lAttribute;

			lAttribute = FXml.CreateAttribute("code");
			lAttribute.Value = ACode.Code.ToUpper().Trim();
			lCodeNode.Attributes.Append(lAttribute);

			lAttribute = FXml.CreateAttribute("description");
			lAttribute.Value = ACode.Description;
			lCodeNode.Attributes.Append(lAttribute);

			if (FGameCodes == null)
				FGameCodes = new List<GameGenieCode>();

			FGameCodes.Add(ACode);
		}

		public void ModifyCode(GameGenieCode ACode) {
			var lCurCode = GameNode.SelectSingleNode(string.Format("gamegenie[@code='{0}']", ACode.OldCode.ToUpper().Trim()));
			if (lCurCode != null) {
				lCurCode.Attributes.GetNamedItem("code").Value = ACode.Code.ToUpper().Trim();
				lCurCode.Attributes.GetNamedItem("description").Value = ACode.Description;
				Modified = true;
			}
		}

		public void DeleteCode(GameGenieCode ACode) {
			var lCurCode = GameNode.SelectSingleNode(string.Format("gamegenie[@code='{0}']", ACode.Code.ToUpper().Trim()));
			if (lCurCode != null)
				lCurCode.ParentNode.RemoveChild(lCurCode);
			FGameCodes.Remove(ACode);
			Modified = true;
		}

		public void ImportCodes(string AFileName, bool AQuiet = false) {
			if (File.Exists(AFileName)) {
				var lXml = new XmlDocument();
				XmlNodeList lCodes = null;
				XmlNode lCodeNode = null;
				XmlAttribute lAttribute = null;

				lXml.Load(AFileName);
				lCodes = lXml.SelectNodes("//genie/..");

				Modified = true;

				var lDeleteNode = GameNode.FirstChild;
				while (lDeleteNode != null) {
					GameNode.RemoveChild(GameNode.FirstChild);
					lDeleteNode = GameNode.FirstChild;
				}
				GameCodes.Clear();

				var lGameFileName = Path.Combine(Path.Combine(Path.Combine(Program.BaseDirectoryExternal, "games"), FGame.Code),
					FGame.Code + ".nes");
				foreach (XmlNode lCurCode in lCodes) {
					var lGame = new NesFile(lGameFileName);
					try {
						lGame.PRG = GameGeniePatcher.Patch(lGame.PRG, lCurCode["genie"].InnerText);

						lCodeNode = FXml.CreateElement("gamegenie");
						GameNode.AppendChild(lCodeNode);

						lAttribute = FXml.CreateAttribute("code");
						lAttribute.Value = lCurCode["genie"].InnerText.ToUpper().Trim();
						lCodeNode.Attributes.Append(lAttribute);

						lAttribute = FXml.CreateAttribute("description");
						lAttribute.Value = lCurCode["description"].InnerText;
						lCodeNode.Attributes.Append(lAttribute);

						GameCodes.Add(new GameGenieCode(lCurCode["genie"].InnerText.ToUpper().Trim(), lCurCode["description"].InnerText));
					} catch (GameGenieFormatException) {
						if (!AQuiet)
							MessageBox.Show(string.Format(Resources.GameGenieFormatError, lCurCode["genie"].InnerText, FGame.Name),
								Resources.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					} catch (GameGenieNotFoundException) {
						if (!AQuiet)
							MessageBox.Show(string.Format(Resources.GameGenieNotFound, lCurCode["genie"].InnerText, FGame.Name),
								Resources.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		public void Save() {
			if (GameCodes.Count == 0)
				GameNode.ParentNode.RemoveChild(GameNode);
			Directory.CreateDirectory(Path.GetDirectoryName(userDatabasePath));
			FXml.Save(userDatabasePath);
		}
	}
}