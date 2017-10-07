#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class NesUGame : NesMiniApplication {
		public NesUGame(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "(nes | famicom)";
	}
}