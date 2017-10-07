namespace com.clusterrr.hakchi_gui {
	public class NesDefaultGame : INesMenuElement {
		public int Size { get; set; }

		public string Code { get; set; }

		public string Name { get; set; }

		public override string ToString() {
			return Name;
		}
	}
}