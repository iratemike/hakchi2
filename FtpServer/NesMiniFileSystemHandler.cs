using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using com.clusterrr.clovershell;

namespace mooftpserv {
	public class NesMiniFileSystemHandler : IFileSystemHandler {
		// clovershell
		private readonly ClovershellConnection clovershell;

		// current path as TVFS or unix-like
		private string currentPath;

		// currently used operating system
		private readonly OS os;

		public NesMiniFileSystemHandler(ClovershellConnection clovershell, string startPath) {
			os = OS.Unix;
			currentPath = startPath;
			this.clovershell = clovershell;
		}

		public NesMiniFileSystemHandler(ClovershellConnection clovershell)
			: this(clovershell, "/") {
		}

		private NesMiniFileSystemHandler(string path, OS os, ClovershellConnection clovershell) {
			currentPath = path;
			this.os = os;
			this.clovershell = clovershell;
		}

		public IFileSystemHandler Clone(IPEndPoint peer) {
			return new NesMiniFileSystemHandler(currentPath, os, clovershell);
		}

		public ResultOrError<string> GetCurrentDirectory() {
			return MakeResult(currentPath);
		}

		public ResultOrError<string> ChangeDirectory(string path) {
			var newPath = ResolvePath(path);
			try {
				clovershell.ExecuteSimple("cd \"" + newPath + "\"", 1000, true);
				currentPath = newPath;
			} catch (Exception ex) {
				return MakeError<string>(ex.Message);
			}
			return MakeResult(newPath);
		}

		public ResultOrError<string> CreateDirectory(string path) {
			var newPath = ResolvePath(path);
			try {
				foreach (var c in newPath)
					if (c > 255) throw new Exception("Invalid characters in directory name");
				var newpath = DecodePath(newPath);
				clovershell.ExecuteSimple("mkdir \"" + newpath + "\"");
			} catch (Exception ex) {
				return MakeError<string>(ex.Message);
			}

			return MakeResult(newPath);
		}

		public ResultOrError<bool> RemoveDirectory(string path) {
			var newPath = ResolvePath(path);

			try {
				var rpath = DecodePath(newPath);
				clovershell.ExecuteSimple("rm -rf \"" + rpath + "\"");
			} catch (Exception ex) {
				return MakeError<bool>(ex.Message);
			}

			return MakeResult(true);
		}

		public ResultOrError<Stream> ReadFile(string path) {
			var newPath = ResolvePath(path);
			try {
				var data = new MemoryStream();
				clovershell.Execute("cat \"" + newPath + "\"", null, data, null, 1000, true);
				data.Seek(0, SeekOrigin.Begin);
				return MakeResult<Stream>(data);
			} catch (Exception ex) {
				return MakeError<Stream>(ex.Message);
			}
		}

		public ResultOrError<Stream> WriteFile(string path) {
			var newPath = ResolvePath(path);
			try {
				foreach (var c in newPath)
					if (c > 255) throw new Exception("Invalid characters in directory name");
				return MakeResult<Stream>(new MemoryStream());
			} catch (Exception ex) {
				return MakeError<Stream>(ex.Message);
			}
		}

		public ResultOrError<bool> RemoveFile(string path) {
			var newPath = ResolvePath(path);

			try {
				clovershell.ExecuteSimple("rm -rf \"" + newPath + "\"", 1000, true);
			} catch (Exception ex) {
				return MakeError<bool>(ex.Message);
			}

			return MakeResult(true);
		}

		public ResultOrError<bool> RenameFile(string fromPath, string toPath) {
			fromPath = ResolvePath(fromPath);
			toPath = ResolvePath(toPath);
			try {
				clovershell.ExecuteSimple("mv \"" + fromPath + "\" \"" + toPath + "\"", 1000, true);
			} catch (Exception ex) {
				return MakeError<bool>(ex.Message);
			}

			return MakeResult(true);
		}

		public ResultOrError<FileSystemEntry[]> ListEntries(string path) {
			var newPath = ResolvePath(path);
			var result = new List<FileSystemEntry>();
			try {
				var lines = clovershell.ExecuteSimple("ls -lApe \"" + newPath + "\"", 1000, true)
					.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines) {
					if (line.StartsWith("total")) continue;
					var entry = new FileSystemEntry();
					entry.Name = line.Substring(69).Trim();
					entry.IsDirectory = entry.Name.EndsWith("/");
					if (entry.IsDirectory) entry.Name = entry.Name.Substring(0, entry.Name.Length - 1);
					entry.Size = long.Parse(line.Substring(29, 15).Trim());
					var dt = line.Substring(44, 25).Trim();
					entry.LastModifiedTimeUtc = DateTime.ParseExact(dt, "ddd MMM  d HH:mm:ss yyyy", CultureInfo.InvariantCulture,
						DateTimeStyles.AllowInnerWhite);
					result.Add(entry);
				}
			} catch (Exception ex) {
				return MakeError<FileSystemEntry[]>(ex.Message);
			}
			return MakeResult(result.ToArray());
		}

		public ResultOrError<long> GetFileSize(string path) {
			var newPath = ResolvePath(path);
			try {
				var size = clovershell.ExecuteSimple("stat -c%s \"" + newPath + "\"", 1000, true);
				return MakeResult(long.Parse(size));
			} catch (Exception ex) {
				return MakeError<long>(ex.Message);
			}
		}

		public ResultOrError<DateTime> GetLastModifiedTimeUtc(string path) {
			var newPath = ResolvePath(path);
			try {
				var time = clovershell.ExecuteSimple("stat -c%Z \"" + newPath + "\"", 1000, true);
				return MakeResult(DateTime.FromFileTime(long.Parse(time)));
			} catch (Exception ex) {
				return MakeError<DateTime>(ex.Message);
			}
		}

		public ResultOrError<string> ChangeToParentDirectory() {
			return ChangeDirectory("..");
		}

		public ResultOrError<bool> WriteFileFinalize(string path, Stream str) {
			var newPath = ResolvePath(path);
			try {
				str.Seek(0, SeekOrigin.Begin);
				var directory = "/";
				var p = newPath.LastIndexOf("/");
				if (p > 0)
					directory = newPath.Substring(0, p);
				clovershell.Execute("mkdir -p \"" + directory + "\" && cat > \"" + newPath + "\"", str, null, null, 1000, true);
				str.Dispose();
				return MakeResult(true);
			} catch (Exception ex) {
				return MakeError<bool>(ex.Message);
			}
		}

		public ResultOrError<string> ListEntriesRaw(string path) {
			if (path.StartsWith("-"))
				path = ". " + path;
			var newPath = ResolvePath(path);
			var result = new List<string>();
			try {
				var lines = clovershell.ExecuteSimple("ls " + newPath, 1000, true);
				return MakeResult(lines);
			} catch (Exception ex) {
				return MakeError<string>(ex.Message);
			}
		}

		private string ResolvePath(string path) {
			if (path == null) return currentPath;
			if (path.Contains(" -> "))
				path = path.Substring(path.IndexOf(" -> ") + 4);
			return FileSystemHelper.ResolvePath(currentPath, path);
		}

		private string EncodePath(string path) {
			if (os == OS.WinNT)
				return "/" + path[0] + (path.Length > 2 ? path.Substring(2).Replace(@"\", "/") : "");
			if (os == OS.WinCE)
				return path.Replace(@"\", "/");
			return path;
		}

		private string DecodePath(string path) {
			if (path == null || path == "" || path[0] != '/')
				return null;

			if (os == OS.WinNT) {
				// some error checking for the drive layer
				if (path == "/")
					return null; // should have been caught elsewhere

				if (path.Length > 1 && path[1] == '/')
					return null;

				if (path.Length > 2 && path[2] != '/')
					return null;

				if (path.Length < 4) // e.g. "/C/"
					return path[1] + @":\";
				return path[1] + @":\" + path.Substring(3).Replace("/", @"\");
			}
			if (os == OS.WinCE)
				return path.Replace("/", @"\");
			return path;
		}

		/// <summary>
		///     Shortcut for ResultOrError<T>.MakeResult()
		/// </summary>
		private ResultOrError<T> MakeResult<T>(T result) {
			return ResultOrError<T>.MakeResult(result);
		}

		/// <summary>
		///     Shortcut for ResultOrError<T>.MakeError()
		/// </summary>
		private ResultOrError<T> MakeError<T>(string error) {
			return ResultOrError<T>.MakeError(error);
		}

		public ResultOrError<bool> ChmodFile(string mode, string path) {
			var newPath = ResolvePath(path);
			try {
				clovershell.ExecuteSimple(string.Format("chmod {0} {1}", mode, newPath), 1000, true);
				return ResultOrError<bool>.MakeResult(true);
			} catch (Exception ex) {
				return MakeError<bool>(ex.Message);
			}
		}

		public ResultOrError<bool> SetLastModifiedTimeUtc(string path, DateTime time) {
			var newPath = ResolvePath(path);
			try {
				clovershell.ExecuteSimple(string.Format("touch -ct {0:yyyyMMddHHmm.ss} \"{1}\"", time, newPath), 1000, true);
				return ResultOrError<bool>.MakeResult(true);
			} catch (Exception ex) {
				return MakeError<bool>(ex.Message);
			}
		}

		// list of supported operating systems
		private enum OS {
			WinNT,
			WinCE,
			Unix
		}
	}
}