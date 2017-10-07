#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class PceGame : NesMiniApplication {
		public PceGame(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "(pce | pc engine | turbografx 16)";
	}
}