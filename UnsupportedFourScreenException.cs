using System;
using com.clusterrr.Famicom;

namespace com.clusterrr.hakchi_gui {
	public class UnsupportedFourScreenException : Exception {
		public readonly NesFile ROM;

		public UnsupportedFourScreenException(NesFile nesFile) {
			ROM = nesFile;
		}
	}
}