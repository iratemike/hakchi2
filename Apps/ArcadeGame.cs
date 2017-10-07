#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class ArcadeGame : NesMiniApplication {
		public ArcadeGame(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "arcade";
	}
}