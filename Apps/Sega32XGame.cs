#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class Sega32XGame : NesMiniApplication {
		public Sega32XGame(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "sega 32x";
	}
}