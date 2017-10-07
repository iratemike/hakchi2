#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class Atari2600Game : NesMiniApplication {
		public Atari2600Game(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "atari 2600";
	}
}