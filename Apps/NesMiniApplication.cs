using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using com.clusterrr.hakchi_gui.Properties;
using SevenZip;

namespace com.clusterrr.hakchi_gui {
	public class NesMiniApplication : INesMenuElement {
		internal const string DefaultApp = "/bin/path-to-your-app";
		private const string DefaultReleaseDate = "1900-01-01";
		private const string DefaultPublisher = "UNKNOWN";
		public const char DefaultPrefix = 'Z';
		public static Image DefaultCover = Resources.blank_app;
		public static Form ParentForm;
		public static bool? NeedPatch;
		public readonly string ConfigPath;

		public readonly string GamePath;
		public readonly string IconPath;
		public readonly string SmallIconPath;

		protected string code;
		protected string command;
		protected bool hasUnsavedChanges = true;

		private string name;
		private byte players;
		private string publisher;
		private string releaseDate;
		private bool simultaneous;

		protected NesMiniApplication() {
			GamePath = null;
			ConfigPath = null;
			Players = 1;
			Simultaneous = false;
			ReleaseDate = DefaultReleaseDate;
			Publisher = DefaultPublisher;
			Command = "";
		}

		protected NesMiniApplication(string path, bool ignoreEmptyConfig = false) {
			GamePath = path;
			code = Path.GetFileName(path);
			Name = Code;
			ConfigPath = Path.Combine(path, Code + ".desktop");
			IconPath = Path.Combine(path, Code + ".png");
			SmallIconPath = Path.Combine(path, Code + "_small.png");
			Players = 1;
			Simultaneous = false;
			ReleaseDate = DefaultReleaseDate;
			Publisher = DefaultPublisher;
			Command = "";

			if (!File.Exists(ConfigPath)) {
				if (ignoreEmptyConfig) return;
				throw new FileNotFoundException("Invalid application directory: " + path);
			}
			var configLines = File.ReadAllLines(ConfigPath);
			foreach (var line in configLines) {
				var pos = line.IndexOf('=');
				if (pos <= 0) continue;
				var param = line.Substring(0, pos).Trim().ToLower();
				var value = line.Substring(pos + 1).Trim();
				switch (param) {
					case "exec":
						Command = value;
						break;
					case "name":
						Name = value;
						break;
					case "players":
						Players = byte.Parse(value);
						break;
					case "simultaneous":
						Simultaneous = value != "0";
						break;
					case "releasedate":
						ReleaseDate = value;
						break;
					case "sortrawpublisher":
						Publisher = value;
						break;
				}
			}
			hasUnsavedChanges = false;
		}

		public static string GamesDirectory {
			get {
				switch (ConfigIni.ConsoleType) {
					default:
					case MainForm.ConsoleType.NES:
					case MainForm.ConsoleType.Famicom:
						return Path.Combine(Program.BaseDirectoryExternal, "games");
					case MainForm.ConsoleType.SNES:
					case MainForm.ConsoleType.SuperFamicom:
						return Path.Combine(Program.BaseDirectoryExternal, "games_snes");
				}
			}
		}

		public static string GamesCloverPath {
			get {
				switch (ConfigIni.ConsoleType) {
					default:
					case MainForm.ConsoleType.NES:
					case MainForm.ConsoleType.Famicom:
						return "/usr/share/games/nes/kachikachi";
					case MainForm.ConsoleType.SNES:
					case MainForm.ConsoleType.SuperFamicom:
						return "/usr/share/games";
				}
			}
		}

		public virtual string GoogleSuffix => "game";

		public string Command {
			get => command;
			set {
				if (command != value) hasUnsavedChanges = true;
				command = value;
			}
		}

		public byte Players {
			get => players;
			set {
				if (players != value) hasUnsavedChanges = true;
				players = value;
			}
		}

		public bool Simultaneous {
			get => simultaneous;
			set {
				if (simultaneous != value) hasUnsavedChanges = true;
				simultaneous = value;
			}
		}

		public string ReleaseDate {
			get => releaseDate;
			set {
				if (releaseDate != value) hasUnsavedChanges = true;
				releaseDate = value;
			}
		}

		public string Publisher {
			get => publisher;
			set {
				if (publisher != value) hasUnsavedChanges = true;
				publisher = value;
			}
		}

		public Image Image {
			set => SetImage(value);
			get {
				if (File.Exists(IconPath))
					return LoadBitmap(IconPath);
				return null;
			}
		}

		public string Code => code;

		public string Name {
			get => name;
			set {
				if (name != value) hasUnsavedChanges = true;
				name = value;
			}
		}

		public static NesMiniApplication FromDirectory(string path, bool ignoreEmptyConfig = false) {
			var files = Directory.GetFiles(path, "*.desktop", SearchOption.TopDirectoryOnly);
			if (files.Length == 0)
				throw new FileNotFoundException("Invalid app folder");
			var config = File.ReadAllLines(files[0]);
			foreach (var line in config)
				if (line.StartsWith("Exec=")) {
					var command = line.Substring(5);
					var app = AppTypeCollection.GetAppByExec(command);
					if (app != null) {
						var constructor = app.Class.GetConstructor(new[] {typeof(string), typeof(bool)});
						return (NesMiniApplication) constructor.Invoke(new object[] {path, ignoreEmptyConfig});
					}
					break;
				}
			return new NesMiniApplication(path, ignoreEmptyConfig);
		}

		public static NesMiniApplication Import(string inputFileName, string originalFileName = null,
			byte[] rawRomData = null) {
			var extension = Path.GetExtension(inputFileName).ToLower();
			if (extension == ".desktop")
				return ImportApp(inputFileName);
			if (rawRomData == null) // Maybe it's already extracted data?
				rawRomData = File.ReadAllBytes(inputFileName); // If not, reading file
			if (originalFileName == null) // Original file name from archive
				originalFileName = Path.GetFileName(inputFileName);
			var prefix = DefaultPrefix;
			var application = extension.Length > 2 ? "/bin/" + extension.Substring(1) : DefaultApp;
			string args = null;
			var cover = DefaultCover;
			var crc32 = CRC32(rawRomData);
			var outputFileName = Regex.Replace(Path.GetFileName(inputFileName), @"[^A-Za-z0-9()!\[\]\.\-]", "_").Trim();

			// Trying to determine file type
			var appinfo = AppTypeCollection.GetAppByExtension(extension);
			var patched = false;
			if (appinfo != null) {
				if (appinfo.DefaultApps.Length > 0)
					application = appinfo.DefaultApps[0];
				prefix = appinfo.Prefix;
				cover = appinfo.DefaultCover;
				var patch = appinfo.Class.GetMethod("Patch");
				if (patch != null) {
					object[] values = {inputFileName, rawRomData, prefix, application, outputFileName, args, cover, crc32};
					var result = (bool) patch.Invoke(null, values);
					if (!result) return null;
					rawRomData = (byte[]) values[1];
					prefix = (char) values[2];
					application = (string) values[3];
					outputFileName = (string) values[4];
					args = (string) values[5];
					cover = (Image) values[6];
					crc32 = (uint) values[7];
					patched = true;
				}
			}

			if (!patched)
				FindPatch(ref rawRomData, inputFileName, crc32);

			var code = GenerateCode(crc32, prefix);
			var gamePath = Path.Combine(GamesDirectory, code);
			var romPath = Path.Combine(gamePath, outputFileName);
			if (Directory.Exists(gamePath)) {
				var files = Directory.GetFiles(gamePath, "*.*", SearchOption.AllDirectories);
				foreach (var f in files)
					try {
						File.Delete(f);
					} catch {
					}
			}
			Directory.CreateDirectory(gamePath);
			File.WriteAllBytes(romPath, rawRomData);
			var game = new NesMiniApplication(gamePath, true);
			game.Name = Path.GetFileNameWithoutExtension(inputFileName);
			game.Name = Regex.Replace(game.Name, @" ?\(.*?\)", string.Empty).Trim();
			game.Name = Regex.Replace(game.Name, @" ?\[.*?\]", string.Empty).Trim();
			game.Name = game.Name.Replace("_", " ").Replace("  ", " ").Trim();
			game.Command = $"{application} {GamesCloverPath}/{code}/{outputFileName}";
			if (!string.IsNullOrEmpty(args))
				game.Command += " " + args;
			game.FindCover(inputFileName, cover, crc32);
			game.Save();

			var app = FromDirectory(gamePath);
			if (app is ICloverAutofill)
				(app as ICloverAutofill).TryAutofill(crc32);

			if (ConfigIni.Compress)
				app.Compress();

			return app;
		}

		private static NesMiniApplication ImportApp(string fileName) {
			if (!File.Exists(fileName)) // Archives are not allowed
				throw new FileNotFoundException("Invalid app folder");
			var code = Path.GetFileNameWithoutExtension(fileName).ToUpper();
			var targetDir = Path.Combine(GamesDirectory, code);
			DirectoryCopy(Path.GetDirectoryName(fileName), targetDir, true);
			return FromDirectory(targetDir);
		}

		public virtual bool Save() {
			if (!hasUnsavedChanges) return false;
			Debug.WriteLine("Saving application \"{0}\" as {1}", Name, Code);
			Name = Regex.Replace(Name, @"'(\d)",
				@"`$1"); // Apostrophe + any number in game name crashes whole system. What. The. Fuck?
			File.WriteAllText(ConfigPath,
				$"[Desktop Entry]\n" +
				$"Type=Application\n" +
				$"Exec={command}\n" +
				$"Path=/var/lib/clover/profiles/0/{Code}\n" +
				$"Name={Name ?? Code}\n" +
				$"Icon={GamesCloverPath}/{Code}/{Code}.png\n\n" +
				$"[X-CLOVER Game]\n" +
				$"Code={Code}\n" +
				$"TestID=777\n" +
				$"ID=0\n" +
				$"Players={Players}\n" +
				$"Simultaneous={(Simultaneous ? 1 : 0)}\n" +
				$"ReleaseDate={ReleaseDate ?? DefaultReleaseDate}\n" +
				$"SaveCount=0\n" +
				$"SortRawTitle={(Name ?? Code).ToLower()}\n" +
				$"SortRawPublisher={(Publisher ?? DefaultPublisher).ToUpper()}\n" +
				$"Copyright=hakchi2 ©2017 Alexey 'Cluster' Avdyukhin\n");
			hasUnsavedChanges = false;
			return true;
		}

		public override string ToString() {
			return Name;
		}

		private void SetImage(Image image, bool EightBitCompression = false) {
			Bitmap outImage;
			Bitmap outImageSmall;
			Graphics gr;

			// Just keep aspect ratio
			var maxX = 204;
			var maxY = 204;
			if (ConfigIni.ConsoleType == MainForm.ConsoleType.SNES ||
			    ConfigIni.ConsoleType == MainForm.ConsoleType.SuperFamicom) {
				maxX = 228;
				maxY = 228;
			}
			if (image.Width / (double) image.Height > maxX / (double) maxY)
				outImage = new Bitmap(maxX, (int) (maxY * (double) image.Height / image.Width));
			else
				outImage = new Bitmap((int) (maxX * (double) image.Width / image.Height), maxY);

			var maxXsmall = 40;
			var maxYsmall = 40;
			if (image.Width / (double) image.Height > maxXsmall / (double) maxYsmall)
				outImageSmall = new Bitmap(maxXsmall, (int) (maxYsmall * (double) image.Height / image.Width));
			else
				outImageSmall = new Bitmap((int) (maxXsmall * (double) image.Width / image.Height), maxYsmall);

			gr = Graphics.FromImage(outImage);
			gr.CompositingQuality = CompositingQuality.HighQuality;
			gr.DrawImage(image, new Rectangle(0, 0, outImage.Width, outImage.Height),
				new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
			gr.Flush();
			outImage.Save(IconPath, ImageFormat.Png);
			gr = Graphics.FromImage(outImageSmall);
			gr.CompositingQuality = CompositingQuality.HighQuality;
			gr.DrawImage(outImage, new Rectangle(0, 0, outImageSmall.Width, outImageSmall.Height),
				new Rectangle(0, 0, outImage.Width, outImage.Height), GraphicsUnit.Pixel);
			gr.Flush();
			outImageSmall.Save(SmallIconPath, ImageFormat.Png);
		}

		protected bool FindCover(string inputFileName, Image defaultCover, uint crc32 = 0) {
			// Trying to find cover file
			Image cover = null;
			var artDirectory = Path.Combine(Program.BaseDirectoryExternal, "art");
			Directory.CreateDirectory(artDirectory);
			if (!string.IsNullOrEmpty(inputFileName)) {
				if (crc32 != 0) {
					var covers = Directory.GetFiles(artDirectory, string.Format("{0:X8}*.*", crc32), SearchOption.AllDirectories);
					if (covers.Length > 0)
						cover = LoadBitmap(covers[0]);
				}
				var imagePath = Path.Combine(artDirectory, Path.GetFileNameWithoutExtension(inputFileName) + ".png");
				if (File.Exists(imagePath))
					cover = LoadBitmap(imagePath);
				imagePath = Path.Combine(artDirectory, Path.GetFileNameWithoutExtension(inputFileName) + ".jpg");
				if (File.Exists(imagePath))
					cover = LoadBitmap(imagePath);
				imagePath = Path.Combine(Path.GetDirectoryName(inputFileName),
					Path.GetFileNameWithoutExtension(inputFileName) + ".png");
				if (File.Exists(imagePath))
					cover = LoadBitmap(imagePath);
				imagePath = Path.Combine(Path.GetDirectoryName(inputFileName),
					Path.GetFileNameWithoutExtension(inputFileName) + ".jpg");
				if (File.Exists(imagePath))
					cover = LoadBitmap(imagePath);
			}
			if (cover == null) {
				Image = defaultCover;
				return false;
			}
			Image = cover;
			return true;
		}

		protected static bool FindPatch(ref byte[] rawRomData, string inputFileName, uint crc32 = 0) {
			string patch = null;
			var patchesDirectory = Path.Combine(Program.BaseDirectoryExternal, "patches");
			Directory.CreateDirectory(patchesDirectory);
			if (!string.IsNullOrEmpty(inputFileName)) {
				if (crc32 != 0) {
					var patches = Directory.GetFiles(patchesDirectory, string.Format("{0:X8}*.*", crc32), SearchOption.AllDirectories);
					if (patches.Length > 0)
						patch = patches[0];
				}
				var patchesPath = Path.Combine(patchesDirectory, Path.GetFileNameWithoutExtension(inputFileName) + ".ips");
				if (File.Exists(patchesPath))
					patch = patchesPath;
				patchesPath = Path.Combine(Path.GetDirectoryName(inputFileName),
					Path.GetFileNameWithoutExtension(inputFileName) + ".ips");
				if (File.Exists(patchesPath))
					patch = patchesPath;
			}

			if (!string.IsNullOrEmpty(patch)) {
				if (NeedPatch != true)
					if (NeedPatch != false) {
						var r = WorkerForm.MessageBoxFromThread(ParentForm,
							string.Format(Resources.PatchQ, Path.GetFileName(inputFileName)),
							Resources.PatchAvailable,
							MessageBoxButtons.AbortRetryIgnore,
							MessageBoxIcon.Question,
							MessageBoxDefaultButton.Button2, true);
						if (r == DialogResult.Abort)
							NeedPatch = true;
						if (r == DialogResult.Ignore)
							return false;
					} else {
						return false;
					}
				IpsPatcher.Patch(patch, ref rawRomData);
				return true;
			}
			return false;
		}

		protected static string GenerateCode(uint crc32, char prefixCode) {
			return string.Format("CLV-{5}-{0}{1}{2}{3}{4}",
				(char) ('A' + crc32 % 26),
				(char) ('A' + (crc32 >> 5) % 26),
				(char) ('A' + (crc32 >> 10) % 26),
				(char) ('A' + (crc32 >> 15) % 26),
				(char) ('A' + (crc32 >> 20) % 26),
				prefixCode);
		}

		public NesMiniApplication CopyTo(string path) {
			var targetDir = Path.Combine(path, code);
			DirectoryCopy(GamePath, targetDir, true);
			return FromDirectory(targetDir);
		}

		internal static long DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs) {
			long size = 0;
			// Get the subdirectories for the specified directory.
			var dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);

			var dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName))
				Directory.CreateDirectory(destDirName);

			// Get the files in the directory and copy them to the new location.
			var files = dir.GetFiles();
			foreach (var file in files) {
				var temppath = Path.Combine(destDirName, file.Name);
				size += file.CopyTo(temppath, true).Length;
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
				foreach (var subdir in dirs) {
					var temppath = Path.Combine(destDirName, subdir.Name);
					size += DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			return size;
		}

		public long Size(string path = null) {
			if (path == null)
				path = GamePath;
			long size = 0;
			// Get the subdirectories for the specified directory.
			var dir = new DirectoryInfo(path);

			if (!dir.Exists)
				return 0;

			var dirs = dir.GetDirectories();
			var files = dir.GetFiles();
			foreach (var file in files)
				size += file.Length;
			foreach (var subdir in dirs)
				size += Size(subdir.FullName);
			return size;
		}

		protected static uint CRC32(byte[] data) {
			var poly = 0xedb88320;
			var table = new uint[256];
			uint temp = 0;
			for (uint i = 0; i < table.Length; ++i) {
				temp = i;
				for (var j = 8; j > 0; --j)
					if ((temp & 1) == 1)
						temp = (temp >> 1) ^ poly;
					else
						temp >>= 1;
				table[i] = temp;
			}
			var crc = 0xffffffff;
			for (var i = 0; i < data.Length; ++i) {
				var index = (byte) ((crc & 0xff) ^ data[i]);
				crc = (crc >> 8) ^ table[index];
			}
			return ~crc;
		}

		public static Bitmap LoadBitmap(string path) {
			//Open file in read only mode
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
				//Get a binary reader for the file stream
			using (var reader = new BinaryReader(stream)) {
				//copy the content of the file into a memory stream
				var memoryStream = new MemoryStream(reader.ReadBytes((int) stream.Length));
				//make a new Bitmap object the owner of the MemoryStream
				return new Bitmap(memoryStream);
			}
		}

		public string[] CompressPossible() {
			if (!Directory.Exists(GamePath)) return new string[0];
			var result = new List<string>();
			var exec = Regex.Replace(Command, "['/\\\"]", " ") + " ";
			var files = Directory.GetFiles(GamePath, "*.*", SearchOption.TopDirectoryOnly);
			foreach (var file in files) {
				if (Path.GetExtension(file).ToLower() == ".7z")
					continue;
				if (Path.GetExtension(file).ToLower() == ".zip")
					continue;
				if (exec.Contains(" " + Path.GetFileName(file) + " "))
					result.Add(file);
			}
			return result.ToArray();
		}

		public string[] DecompressPossible() {
			if (!Directory.Exists(GamePath)) return new string[0];
			var result = new List<string>();
			var exec = Regex.Replace(Command, "['/\\\"]", " ") + " ";
			var files = Directory.GetFiles(GamePath, "*.7z", SearchOption.TopDirectoryOnly);
			foreach (var file in files)
				if (exec.Contains(" " + Path.GetFileName(file) + " "))
					result.Add(file);
			return result.ToArray();
		}

		public void Compress() {
			SevenZipBase.SetLibraryPath(Path.Combine(Program.BaseDirectoryInternal,
				IntPtr.Size == 8 ? @"tools\7z64.dll" : @"tools\7z.dll"));
			foreach (var filename in CompressPossible()) {
				var archName = filename + ".7z";
				var compressor = new SevenZipCompressor();
				compressor.CompressionLevel = CompressionLevel.High;
				Debug.WriteLine("Compressing " + filename);
				compressor.CompressFiles(archName, filename);
				File.Delete(filename);
				Command = Command.Replace(Path.GetFileName(filename), Path.GetFileName(archName));
			}
		}

		public void Decompress() {
			SevenZipBase.SetLibraryPath(Path.Combine(Program.BaseDirectoryInternal,
				IntPtr.Size == 8 ? @"tools\7z64.dll" : @"tools\7z.dll"));
			foreach (var filename in DecompressPossible()) {
				using (var szExtractor = new SevenZipExtractor(filename)) {
					Debug.WriteLine("Decompressing " + filename);
					szExtractor.ExtractArchive(GamePath);
					foreach (var f in szExtractor.ArchiveFileNames)
						Command = Command.Replace(Path.GetFileName(filename), f);
				}
				File.Delete(filename);
			}
		}

		public class NesMiniAppEqualityComparer : IEqualityComparer<NesMiniApplication> {
			public bool Equals(NesMiniApplication x, NesMiniApplication y) {
				return x.Code == y.Code;
			}

			public int GetHashCode(NesMiniApplication obj) {
				return obj.Code.GetHashCode();
			}
		}
	}
}