﻿using System;
using com.clusterrr.Famicom;

namespace com.clusterrr.hakchi_gui {
	public class UnsupportedMapperException : Exception {
		public readonly NesFile ROM;

		public UnsupportedMapperException(NesFile nesFile) {
			ROM = nesFile;
		}
	}
}