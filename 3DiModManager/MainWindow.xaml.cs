using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using Microsoft.Win32;

namespace _3DiModManager
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public ModManager Manager { get; set; }
		public List<CarEntity> Cars { get; set; }
		private string gamePath = 
			Path.GetDirectoryName(
			(Path.GetDirectoryName(
			Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)))) + @"\TestEnvironment";//@"D:\games\3D Instructor 2 Home";//games\3D Instructor 2 Home";

		public MainWindow()
		{
			Manager = new ModManager(gamePath);
			InitializeComponent();
			MyListView.SizeChanged += (s, e) =>
			{
				((GridViewColumn)((GridView)MyListView.View).Columns[0]).Width = 50;
				((GridViewColumn)((GridView)MyListView.View).Columns[1]).Width = (e.NewSize.Width - 50) / 1.5;
				((GridViewColumn)((GridView)MyListView.View).Columns[2]).Width = (e.NewSize.Width - 50) - (e.NewSize.Width - 50) / 1.5; ;
			};
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!Manager.changed)
				return;

			var result = MessageBox.Show("Сохранить изменения?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if(result==MessageBoxResult.Yes)
			{
				Manager.SaveChanges();
			}
			Manager.CleanCarsFiles();
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
			{
				var fileName = e.Data.GetData(DataFormats.FileDrop) as IEnumerable<string>;
				foreach (var file in fileName)
				{
					Manager.LoadCarFromArchieve(file);
				}
			}
			e.Handled = true;
		}

		private void deleteCommand(object sender, RoutedEventArgs e)
		{
			if(MyListView.SelectedItem!=null)
			{
				Manager.DeleteCar((MyListView.SelectedItem as CarEntity).Name);
			} else
			{
				MessageBox.Show("Сначала нужно выбрать автомобили для удаления", "Ошибка", MessageBoxButton.OK,
				                MessageBoxImage.Error);
			}
		}

		private void addCommand(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openDlg = new OpenFileDialog();
			openDlg.Multiselect = true;
			openDlg.Filter += "Архивы |*.rar;*.zip";

			openDlg.ShowDialog();
			

			if (openDlg.FileNames!=null)
			{
				foreach(var file in openDlg.FileNames)
				{
					Manager.LoadCarFromArchieve(file);
				}
			}
		}
	}
}
