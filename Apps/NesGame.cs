#pragma warning disable 0108
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.XPath;
using com.clusterrr.Famicom;
using com.clusterrr.hakchi_gui.Properties;

namespace com.clusterrr.hakchi_gui {
	public class NesGame : NesMiniApplication, ICloverAutofill, ISupportsGameGenie {
		public const char Prefix = 'H';

		private const string DefaultArgs =
			"--guest-overscan-dimensions 0,0,9,3 --initial-fadein-durations 10,2 --volume 75 --enable-armet";

		public const string GameGenieFileName = "gamegenie.txt";
		public static bool? IgnoreMapper;
		private static Dictionary<uint, CachedGameInfo> gameInfoCache;
		private static readonly byte[] supportedMappers = {0, 1, 2, 3, 4, 5, 7, 9, 10, 86, 87, 184};

		private string gameGenie = "";

		public NesGame(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
			GameGeniePath = Path.Combine(path, GameGenieFileName);
			if (File.Exists(GameGeniePath))
				gameGenie = File.ReadAllText(GameGeniePath);
		}

		public override string GoogleSuffix => "(nes | famicom)";

		public bool TryAutofill(uint crc32) {
			CachedGameInfo gameinfo;
			if (gameInfoCache != null && gameInfoCache.TryGetValue(crc32, out gameinfo)) {
				Name = gameinfo.Name;
				Name = Name.Replace("_", " ").Replace("  ", " ").Trim();
				Players = gameinfo.Players;
				if (Players > 1) Simultaneous = true; // actually unknown...
				ReleaseDate = gameinfo.ReleaseDate;
				if (ReleaseDate.Length == 4) ReleaseDate += "-01";
				if (ReleaseDate.Length == 7) ReleaseDate += "-01";
				Publisher = gameinfo.Publisher.ToUpper();
				return true;
			}
			return false;
		}

		public string GameGeniePath { get; }

		public string GameGenie {
			get => gameGenie;
			set {
				if (gameGenie != value) hasUnsavedChanges = true;
				gameGenie = value;
			}
		}

		public void ApplyGameGenie() {
			if (!string.IsNullOrEmpty(GameGenie)) {
				var codes = GameGenie.Split(new[] {',', '\t', ' ', ';'}, StringSplitOptions.RemoveEmptyEntries);
				var nesFiles = Directory.GetFiles(GamePath, "*.nes", SearchOption.TopDirectoryOnly);
				foreach (var f in nesFiles) {
					var nesFile = new NesFile(f);
					foreach (var code in codes)
						nesFile.PRG = GameGeniePatcher.Patch(nesFile.PRG, code.Trim());
					nesFile.Save(f);
				}
			}
		}

		public static bool Patch(string inputFileName, ref byte[] rawRomData, ref char prefix, ref string application,
			ref string outputFileName, ref string args, ref Image cover, ref uint crc32) {
			// Try to patch before mapper check, maybe it will patch mapper
			FindPatch(ref rawRomData, inputFileName, crc32);

			NesFile nesFile;
			try {
				nesFile = new NesFile(rawRomData);
			} catch {
				application = "/bin/nes";
				return true;
			}
			nesFile.CorrectRom();
			crc32 = nesFile.CRC32;
			if (ConfigIni.ConsoleType == MainForm.ConsoleType.NES || ConfigIni.ConsoleType == MainForm.ConsoleType.Famicom)
				application = "/bin/clover-kachikachi-wr";
			else
				application = "/bin/nes";

			//if (nesFile.Mapper == 71) nesFile.Mapper = 2; // games by Codemasters/Camerica - this is UNROM clone. One exception - Fire Hawk
			//if (nesFile.Mapper == 88) nesFile.Mapper = 4; // Compatible with MMC3... sometimes
			//if (nesFile.Mapper == 95) nesFile.Mapper = 4; // Compatible with MMC3
			//if (nesFile.Mapper == 206) nesFile.Mapper = 4; // Compatible with MMC3
			if (!supportedMappers.Contains(nesFile.Mapper) && IgnoreMapper != true)
				if (IgnoreMapper != false) {
					var r = WorkerForm.MessageBoxFromThread(ParentForm,
						string.Format(Resources.MapperNotSupported, Path.GetFileName(inputFileName), nesFile.Mapper),
						Resources.AreYouSure,
						MessageBoxButtons.AbortRetryIgnore,
						MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2, true);
					if (r == DialogResult.Abort)
						IgnoreMapper = true;
					if (r == DialogResult.Ignore)
						return false;
				} else {
					return false;
				}
			if (nesFile.Mirroring == NesFile.MirroringType.FourScreenVram && IgnoreMapper != true) {
				var r = WorkerForm.MessageBoxFromThread(ParentForm,
					string.Format(Resources.FourScreenNotSupported, Path.GetFileName(inputFileName)),
					Resources.AreYouSure,
					MessageBoxButtons.AbortRetryIgnore,
					MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2, true);
				if (r == DialogResult.Abort)
					IgnoreMapper = true;
				if (r == DialogResult.No)
					return false;
			}

			// TODO: Make trainer check. I think that the NES Mini doesn't support it.
			rawRomData = nesFile.GetRaw();
			if (inputFileName.Contains("(J)")) cover = Resources.blank_jp;
			args = DefaultArgs;
			return true;
		}

		public override bool Save() {
			var old = hasUnsavedChanges;
			if (hasUnsavedChanges)
				if (!string.IsNullOrEmpty(gameGenie))
					File.WriteAllText(GameGeniePath, gameGenie);
				else
					File.Delete(GameGeniePath);
			return base.Save() || old;
		}

		public static void LoadCache() {
			try {
				var xmlDataBasePath = Path.Combine(Path.Combine(Program.BaseDirectoryInternal, "data"), "nescarts.xml");
				Debug.WriteLine("Loading " + xmlDataBasePath);

				if (File.Exists(xmlDataBasePath)) {
					var xpath = new XPathDocument(xmlDataBasePath);
					var navigator = xpath.CreateNavigator();
					var iterator = navigator.Select("/database/game");
					gameInfoCache = new Dictionary<uint, CachedGameInfo>();
					while (iterator.MoveNext()) {
						var game = iterator.Current;
						var cartridges = game.Select("cartridge");
						while (cartridges.MoveNext()) {
							var cartridge = cartridges.Current;
							try {
								var crc = Convert.ToUInt32(cartridge.GetAttribute("crc", ""), 16);
								gameInfoCache[crc] = new CachedGameInfo {
									Name = game.GetAttribute("name", ""),
									Players = (byte) (game.GetAttribute("players", "") != "1" ? 2 : 1),
									ReleaseDate = game.GetAttribute("date", ""),
									Publisher = game.GetAttribute("publisher", ""),
									Region = game.GetAttribute("region", "")
								};
							} catch {
							}
						}
						;
					}
				}
				Debug.WriteLine("XML loading done, {0} roms total", gameInfoCache.Count);
			} catch (Exception ex) {
				Debug.WriteLine(ex.Message + ex.StackTrace);
			}
		}

		private struct CachedGameInfo {
			public string Name;
			public byte Players;
			public string ReleaseDate;
			public string Publisher;
			public string Region;
		}
	}
}