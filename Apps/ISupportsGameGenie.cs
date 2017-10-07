namespace com.clusterrr.hakchi_gui {
	internal interface ISupportsGameGenie {
		string GameGeniePath { get; }
		string GameGenie { get; set; }
		void ApplyGameGenie();
	}
}