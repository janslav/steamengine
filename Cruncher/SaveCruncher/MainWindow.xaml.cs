using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using CrunchyUtils;
using Microsoft.Win32;
using SaveCruncher.Properties;

namespace SaveCruncher {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		static readonly string dataDirectory = Settings.Default.DataDir;

		public MainWindow() {
			InitializeComponent();
		}


		private void Window_Activated_1(object sender, EventArgs e) {
			if (Directory.Exists(dataDirectory)) {

				var path = Path.Combine(dataDirectory, "sphereworld.scp");
				if (File.Exists(path)) {
					this.tbWorldSavePath.Text = path;
				} else {
					this.tbWorldSavePath.Text = (from full in Directory.GetFiles(dataDirectory)
												 let file = Path.GetFileName(full)
												 where file.StartsWith("sphereb") && file.EndsWith("w.scp")
												 select full).FirstOrDefault();
				}

				path = Path.Combine(dataDirectory, "spherechars.scp");
				if (File.Exists(path)) {
					this.tbCharsSavePath.Text = path;
				} else {
					this.tbCharsSavePath.Text = (from full in Directory.GetFiles(dataDirectory)
												 let file = Path.GetFileName(full)
												 where file.StartsWith("sphereb") && file.EndsWith("c.scp")
												 select full).FirstOrDefault();
				}
			}
		}

		private void bWorldSavePathSelectFile_Click(object sender, RoutedEventArgs e) {
			GetPathFromOpenFileDialog(path => this.tbWorldSavePath.Text = path);
		}

		private void bCharsSavePathSelectFile_Click(object sender, RoutedEventArgs e) {
			GetPathFromOpenFileDialog(path => this.tbCharsSavePath.Text = path);
		}

		private void GetPathFromOpenFileDialog(Action<string> setter) {
			var dlg = new OpenFileDialog();
			dlg.DefaultExt = ".scp";
			if (Directory.Exists(dataDirectory)) {
				dlg.InitialDirectory = dataDirectory;
			}
			dlg.Filter = "sphere script/save file (.scp)|*.scp";

			var result = dlg.ShowDialog();

			if (result.HasValue && result.Value) {
				setter(dlg.FileName);
			}
		}

		private async void bLoadWorldSave_Click(object sender, RoutedEventArgs e) {
			string worldSavePath = this.tbWorldSavePath.Text;
			await SaveParser.Parse(worldSavePath);
		}

		private async void bLoadCharsSave_Click(object sender, RoutedEventArgs e) {
			string charsSavePath = this.tbCharsSavePath.Text;			
			await SaveParser.Parse(charsSavePath);			
		}
	}
}
