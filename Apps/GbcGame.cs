#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class GbcGame : NesMiniApplication {
		public GbcGame(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "(gameboy | game boy)";
	}
}