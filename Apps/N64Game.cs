#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class N64Game : NesMiniApplication {
		public N64Game(string path, bool ignoreEmptyConfig = false)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "nintendo 64";
	}
}