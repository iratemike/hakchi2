using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using com.clusterrr.FelLib;
using com.clusterrr.hakchi_gui.Properties;

namespace com.clusterrr.hakchi_gui {
	public partial class WaitingFelForm : Form {
		private readonly ushort vid, pid;

		public WaitingFelForm(ushort vid, ushort pid) {
			InitializeComponent();
			buttonDriver.Left = label6.Left + label6.Width;
			this.vid = vid;
			this.pid = pid;
			timer.Enabled = true;
		}

		public static bool WaitForDevice(ushort vid, ushort pid, IWin32Window owner) {
			if (Fel.DeviceExists(vid, pid)) return true;
			var form = new WaitingFelForm(vid, pid);
			form.ShowDialog(owner);
			return form.DialogResult == DialogResult.OK;
		}

		private static bool DeviceExists(ushort vid, ushort pid) {
			try {
				using (var fel = new Fel()) {
					fel.Open(vid, pid);
					return true;
				}
			} catch {
				return false;
			}
		}

		private void timer_Tick(object sender, EventArgs e) {
			if (Fel.DeviceExists(vid, pid)) {
				DialogResult = DialogResult.OK;
				timer.Enabled = false;
			}
		}

		private void WaitingForm_FormClosing(object sender, FormClosingEventArgs e) {
			if (!Fel.DeviceExists(vid, pid))
				if (MessageBox.Show(this, Resources.DoYouWantCancel, Resources.AreYouSure, MessageBoxButtons.YesNo,
					    MessageBoxIcon.Warning)
				    == DialogResult.No)
					e.Cancel = true;
				else
					DialogResult = DialogResult.Abort;
		}

		private void buttonDriver_Click(object sender, EventArgs e) {
			try {
				var process = new Process();
				var fileName = Path.Combine(Path.Combine(Program.BaseDirectoryInternal, "driver"), "nesmini_driver.exe");
				process.StartInfo.FileName = fileName;
				process.Start();
			} catch (Exception ex) {
				MessageBox.Show(this, ex.Message, Resources.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void WaitingForm_FormClosed(object sender, FormClosedEventArgs e) {
			timer.Enabled = false;
		}
	}
}