#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class GbaGame : NesMiniApplication {
		public GbaGame(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "gba";
	}
}