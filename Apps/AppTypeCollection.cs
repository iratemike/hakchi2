using System;
using System.Drawing;
using System.Text.RegularExpressions;
using com.clusterrr.hakchi_gui.Properties;

namespace com.clusterrr.hakchi_gui {
	internal static class AppTypeCollection {
		public static AppInfo[] ApplicationTypes = {
			new AppInfo {
				Class = typeof(NesGame),
				Extensions = new[] {".nes"},
				DefaultApps = new[] {"/bin/nes", "/bin/clover-kachikachi-wr", "/usr/bin/clover-kachikachi"},
				Prefix = 'H',
				DefaultCover = Resources.blank_nes
			},
			new AppInfo {
				Class = typeof(NesUGame),
				Extensions = new[] {".unf", ".unif", ".nes", ".fds"},
				DefaultApps = new[] {"/bin/nes"},
				Prefix = 'I',
				DefaultCover = Resources.blank_jp
			},
			new AppInfo {
				Class = typeof(FdsGame),
				Extensions = new[] {".fds"},
				DefaultApps = new[] {"/bin/nes", "/bin/clover-kachikachi-wr", "/usr/bin/clover-kachikachi"},
				Prefix = 'D',
				DefaultCover = Resources.blank_fds
			},
			new AppInfo {
				Class = typeof(SnesGame),
				Extensions = new[] {".sfc", ".smc", ".sfrom"},
				DefaultApps = new[] {"/bin/snes", "/bin/clover-canoe-shvc-wr", "/usr/bin/clover-canoe-shvc"},
				Prefix = 'U',
				DefaultCover = Resources.blank_snes_us
			},
			new AppInfo {
				Class = typeof(N64Game),
				Extensions = new[] {".n64", ".z64", ".v64"},
				DefaultApps = new[] {"/bin/n64"},
				Prefix = '6',
				DefaultCover = Resources.blank_n64
			},
			new AppInfo {
				Class = typeof(SmsGame),
				Extensions = new[] {".sms"},
				DefaultApps = new[] {"/bin/sms"},
				Prefix = 'M',
				DefaultCover = Resources.blank_sms
			},
			new AppInfo {
				Class = typeof(GenesisGame),
				Extensions = new[] {".gen", ".md", ".smd"},
				DefaultApps = new[] {"/bin/md"},
				Prefix = 'G',
				DefaultCover = Resources.blank_genesis
			},
			new AppInfo {
				Class = typeof(Sega32XGame),
				Extensions = new[] {".32x"},
				DefaultApps = new[] {"/bin/32x"},
				Prefix = '3',
				DefaultCover = Resources.blank_32x
			},
			new AppInfo {
				Class = typeof(GbGame),
				Extensions = new[] {".gb"},
				DefaultApps = new[] {"/bin/gb"},
				Prefix = 'B',
				DefaultCover = Resources.blank_gb
			},
			new AppInfo {
				Class = typeof(GbcGame),
				Extensions = new[] {".gbc"},
				DefaultApps = new[] {"/bin/gbc"},
				Prefix = 'C',
				DefaultCover = Resources.blank_gbc
			},
			new AppInfo {
				Class = typeof(GbaGame),
				Extensions = new[] {".gba"},
				DefaultApps = new[] {"/bin/gba"},
				Prefix = 'A',
				DefaultCover = Resources.blank_gba
			},
			new AppInfo {
				Class = typeof(PceGame),
				Extensions = new[] {".pce"},
				DefaultApps = new[] {"/bin/pce"},
				Prefix = 'E',
				DefaultCover = Resources.blank_pce
			},
			new AppInfo {
				Class = typeof(GameGearGame),
				Extensions = new[] {".gg"},
				DefaultApps = new[] {"/bin/gg"},
				Prefix = 'R',
				DefaultCover = Resources.blank_gg
			},
			new AppInfo {
				Class = typeof(GameGearGame),
				Extensions = new[] {".a26"},
				DefaultApps = new[] {"/bin/a26"},
				Prefix = 'T',
				DefaultCover = Resources.blank_2600
			},
			new AppInfo {
				Class = typeof(GameGearGame),
				Extensions = new string[] { },
				DefaultApps = new[] {"/bin/fba", "/bin/mame", "/bin/cps2", "/bin/neogeo"},
				Prefix = 'X',
				DefaultCover = Resources.blank_arcade
			}
		};

		public static AppInfo GetAppByExtension(string extension) {
			foreach (var app in ApplicationTypes)
				if (Array.IndexOf(app.Extensions, extension) >= 0)
					return app;
			return null;
		}

		public static AppInfo GetAppByExec(string exec) {
			exec = Regex.Replace(exec, "['\\\"]|(\\.7z)", " ") + " ";
			foreach (var app in ApplicationTypes)
			foreach (var cmd in app.DefaultApps)
				if (exec.StartsWith(cmd + " ")) {
					if (app.Extensions.Length == 0)
						return app;
					foreach (var ext in app.Extensions)
						if (exec.Contains(ext + " "))
							return app;
				}
			return null;
		}
		//public delegate NesMiniApplication 

		public class AppInfo {
			public Type Class;
			public string[] DefaultApps;
			public Image DefaultCover;
			public string[] Extensions;
			public char Prefix;
		}
	}
}