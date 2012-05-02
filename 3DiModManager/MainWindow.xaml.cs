using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
		private string gamePath;
			
		//Path.GetDirectoryName(
			//(Path.GetDirectoryName(
			//Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)))) + @"\TestEnvironment";//@"D:\games\3D Instructor 2 Home";//games\3D Instructor 2 Home";

		public MainWindow()
		{
			if(App.Current.Properties.Contains("GamePath"))
			{
				gamePath = App.Current.Properties["GamePath"] as string;
			} 
			else
			{
				gamePath=Registry.GetValue(@"HKEY_CURRENT_USER\Software\Forward Development\3D Инструктор 2 Домашняя версия", "path", null) as string;
			}

			if(String.IsNullOrEmpty(gamePath))
			{
				MessageBox.Show(
					"Игра не найдена. Если она все таки установлена - укажите ее путь в качестве параметра коммандной строки.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Stop);
				
				Close();
				return;
			}

			Manager = new ModManager(gamePath);
			Manager.onSaved = () =>
			                  	{
			                  		this.Dispatcher.InvokeShutdown();
			                  	};

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
				e.Cancel = true;
			}
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
			if(MyListView.SelectedItems.Count!=0)
			{
				var selectedCars = new List<string>(MyListView.SelectedItems.OfType<CarEntity>().Select(m => m.Name));
				foreach (var item in selectedCars)
				{
					Manager.DeleteCar(item);
				}
			} 
			else
			{
				MessageBox.Show("Сначала нужно выбрать автомобили для удаления", "Ошибка", MessageBoxButton.OK,MessageBoxImage.Error);
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

		private void showCarDetailsCommand(object sender, RoutedEventArgs e)
		{
			CarSettingsWindow a = new CarSettingsWindow(MyListView.SelectedItem as CarEntity);
			a.Show();
		}
	}
}
