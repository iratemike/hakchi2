#pragma warning disable 0108
namespace com.clusterrr.hakchi_gui {
	public class SmsGame : NesMiniApplication {
		public SmsGame(string path, bool ignoreEmptyConfig)
			: base(path, ignoreEmptyConfig) {
		}

		public override string GoogleSuffix => "(sms | sega master system)";
	}
}