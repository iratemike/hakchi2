using System.IO;

namespace com.clusterrr.util {
	public class TrackableFileStream : FileStream {
		public delegate void OnProgressDelegate(long Position, long Length);

		public TrackableFileStream(string path, FileMode mode) : base(path, mode) {
		}

		public event OnProgressDelegate OnProgress = delegate { };

		public override void Write(byte[] array, int offset, int count) {
			base.Write(array, offset, count);
			OnProgress(Position, Length);
		}

		public override void WriteByte(byte value) {
			base.WriteByte(value);
			OnProgress(Position, Length);
		}

		public override int Read(byte[] array, int offset, int count) {
			var r = base.Read(array, offset, count);
			OnProgress(Position, Length);
			return r;
		}

		public override int ReadByte() {
			var r = base.ReadByte();
			OnProgress(Position, Length);
			return r;
		}
	}
}