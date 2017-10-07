using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Resources;
using com.clusterrr.hakchi_gui.Properties;

namespace com.clusterrr.hakchi_gui {
	public class NesMenuFolder : INesMenuElement {
		public enum Priority {
			Leftmost = 0,
			Left = 1,
			Right = 3,
			Rightmost = 4,
			Back = 5
		}

		private static Random rnd = new Random();
		private static readonly ResourceManager rm = Resources.ResourceManager;
		public static readonly string FolderImagesDirectory = Path.Combine(Program.BaseDirectoryExternal, "folder_images");

		public NesMenuCollection ChildMenuCollection = new NesMenuCollection();
		private Image image;
		private string imageId;
		public string Initial = "";
		private string name;
		private string[] nameParts;

		private byte Players = 2;
		private Priority position;
		private string Publisher = new string('!', 10);
		private string ReleaseDate = "0000-00-00";
		private byte Simultaneous = 1;

		public NesMenuFolder(string name = "Folder", string imageId = "folder") {
			Name = name;
			Position = Priority.Right;
			ImageId = imageId;
		}

		public int ChildIndex { get; set; } = 0;

		public string[] NameParts {
			get => nameParts;
			set {
				nameParts = value;
				if (value != null)
					name = string.Join(" - ", nameParts);
				else
					name = null;
			}
		}

		// It's workaround for sorting
		public Priority Position {
			set {
				// Sort to left
				position = value;
				switch (position) {
					case Priority.Leftmost:
						Players = 2;
						Simultaneous = 1;
						ReleaseDate = "0000-00-00";
						Publisher = new string((char) 1, 10);
						break;
					case Priority.Left:
						Players = 2;
						Simultaneous = 1;
						ReleaseDate = "1111-11-11";
						Publisher = new string((char) 2, 10);
						break;
					case Priority.Right:
						Players = 1;
						Simultaneous = 0;
						ReleaseDate = "7777-77-77";
						Publisher = new string('Z', 9) + "X";
						break;
					case Priority.Rightmost:
						Players = 1;
						Simultaneous = 0;
						ReleaseDate = "8888-88-88";
						Publisher = new string('Z', 9) + "Y";
						break;
					case Priority.Back:
						Players = 1;
						Simultaneous = 0;
						ReleaseDate = "9999-99-99";
						Publisher = new string('Z', 10);
						break;
				}
			}
			get => position;
		}

		public Image Image {
			set {
				if (value == null) {
					ImageId = "folder";
				} else {
					image = Image;
					imageId = null;
				}
			}
			get {
				Bitmap outImage;
				Graphics gr;
				if (image == null)
					ImageId = "folder";
				// Just keep aspect ratio
				const int maxX = 204;
				const int maxY = 204;
				if (image.Width <= maxX && image.Height <= maxY) // Do not upscale
					return image;
				if (image.Width / (double) image.Height > maxX / (double) maxY)
					outImage = new Bitmap(maxX, (int) (maxY * (double) image.Height / image.Width));
				else
					outImage = new Bitmap((int) (maxX * (double) image.Width / image.Height), maxY);
				gr = Graphics.FromImage(outImage);
				gr.DrawImage(image, new Rectangle(0, 0, outImage.Width, outImage.Height),
					new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
				gr.Flush();
				return outImage;
			}
		}

		public Image ImageThumbnail {
			get {
				Bitmap outImage;
				Graphics gr;
				if (image == null)
					ImageId = "folder";
				// Just keep aspect ratio
				const int maxX = 40;
				const int maxY = 40;
				if (image.Width <= maxX && image.Height <= maxY) // Do not upscale
					return image;
				if (image.Width / (double) image.Height > maxX / (double) maxY)
					outImage = new Bitmap(maxX, (int) (maxY * (double) image.Height / image.Width));
				else
					outImage = new Bitmap((int) (maxX * (double) image.Width / image.Height), maxY);
				gr = Graphics.FromImage(outImage);
				gr.DrawImage(image, new Rectangle(0, 0, outImage.Width, outImage.Height),
					new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
				gr.Flush();
				return outImage;
			}
		}

		public string ImageId {
			get => imageId;
			set {
				var folderImagesDirectory = Path.Combine(Program.BaseDirectoryExternal, "folder_images");
				var filePath = Path.Combine(folderImagesDirectory, value + ".png");
				if (File.Exists(filePath))
					image = NesMiniApplication.LoadBitmap(filePath);
				else
					image = (Image) rm.GetObject(value);
				imageId = value;
			}
		}

		public string Code => string.Format("CLV-S-{0:D5}", ChildIndex);

		public string Name {
			get => name;
			set {
				name = value;
				if (!string.IsNullOrEmpty(name))
					nameParts = new[] {name};
				else
					nameParts = new string[0];
			}
		}

		public long Save(string path) {
			Directory.CreateDirectory(path);
			var ConfigPath = Path.Combine(path, Code + ".desktop");
			var IconPath = Path.Combine(path, Code + ".png");
			var ThumnnailIconPath = Path.Combine(path, Code + "_small.png");
			char prefix;
			switch (position) {
				case Priority.Leftmost:
					prefix = (char) 1;
					break;
				default:
				case Priority.Left:
					prefix = (char) 2;
					break;
				case Priority.Right:
					prefix = 'Э';
					break;
				case Priority.Rightmost:
					prefix = 'Ю';
					break;
				case Priority.Back:
					prefix = 'Я';
					break;
			}
			File.WriteAllText(ConfigPath, string.Format(
				"[Desktop Entry]\n" +
				"Type=Application\n" +
				"Exec=/bin/chmenu {1:D3} {8}\n" +
				"Path=/var/lib/clover/profiles/0/FOLDER\n" +
				"Name={2}\n" +
				"Icon={9}/{0}/{0}.png\n\n" +
				"[X-CLOVER Game]\n" +
				"Code={0}\n" +
				"TestID=777\n" +
				"ID=0\n" +
				"Players={3}\n" +
				"Simultaneous={7}\n" +
				"ReleaseDate={4}\n" +
				"SaveCount=0\n" +
				"SortRawTitle={5}\n" +
				"SortRawPublisher={6}\n" +
				"Copyright=hakchi2 ©2017 Alexey 'Cluster' Avdyukhin\n",
				Code, ChildIndex, Name ?? Code, Players, ReleaseDate,
				prefix + (Name ?? Code).ToLower(), (Publisher ?? "").ToUpper(),
				Simultaneous, Initial, NesMiniApplication.GamesCloverPath)
			);
			Image.Save(IconPath, ImageFormat.Png);
			ImageThumbnail.Save(ThumnnailIconPath, ImageFormat.Png);
			return new FileInfo(ConfigPath).Length + new FileInfo(IconPath).Length + new FileInfo(ThumnnailIconPath).Length;
		}

		public override string ToString() {
			return Name ?? Code;
		}
	}
}