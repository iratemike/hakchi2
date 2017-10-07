#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class GameGearGame : NesMiniApplication {
		public GameGearGame(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "game gear";
	}
}