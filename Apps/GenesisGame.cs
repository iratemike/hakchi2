#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class GenesisGame : NesMiniApplication {
		public GenesisGame(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "(genesis | mega drive)";
	}
}