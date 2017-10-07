namespace com.clusterrr.FelLib {
	internal class AWFELMessage {
		public uint Address;
		public AWFELStandardRequest.RequestType Cmd;
		public uint Flags;
		public uint Len;
		public ushort Tag;

		public AWFELMessage() {
		}

		public AWFELMessage(byte[] data) {
			Cmd = (AWFELStandardRequest.RequestType) (data[0] | (data[1] * 0x100));
			Tag = (ushort) (data[2] | (data[3] * 0x100));
			Address = (uint) (data[4] | (data[5] * 0x100) | (data[6] * 0x10000) | (data[7] * 0x1000000));
			Len = (uint) (data[8] | (data[9] * 0x100) | (data[10] * 0x10000) | (data[11] * 0x1000000));
			Flags = (uint) (data[12] | (data[13] * 0x100) | (data[14] * 0x10000) | (data[15] * 0x1000000));
		}

		public byte[] Data {
			get {
				var data = new byte[16];
				data[0] = (byte) ((ushort) Cmd & 0xFF); // mark
				data[1] = (byte) (((ushort) Cmd >> 8) & 0xFF); // mark
				data[2] = (byte) (Tag & 0xFF); // tag
				data[3] = (byte) ((Tag >> 8) & 0xFF); // tag
				data[4] = (byte) (Address & 0xFF); // address
				data[5] = (byte) ((Address >> 8) & 0xFF); // address
				data[6] = (byte) ((Address >> 16) & 0xFF); // address
				data[7] = (byte) ((Address >> 24) & 0xFF); // address
				data[8] = (byte) (Len & 0xFF); // len
				data[9] = (byte) ((Len >> 8) & 0xFF); // len
				data[10] = (byte) ((Len >> 16) & 0xFF); // len
				data[11] = (byte) ((Len >> 24) & 0xFF); // len
				data[12] = (byte) (Flags & 0xFF); // flags
				data[13] = (byte) ((Flags >> 8) & 0xFF); // flags
				data[14] = (byte) ((Flags >> 16) & 0xFF); // flags
				data[15] = (byte) ((Flags >> 24) & 0xFF); // flags

				return data;
			}
		}
	}
}