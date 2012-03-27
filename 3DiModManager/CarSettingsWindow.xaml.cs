using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace _3DiModManager
{
	/// <summary>
	/// Interaction logic for CarSettingsWindow.xaml
	/// </summary>
	public partial class CarSettingsWindow : Window, INotifyPropertyChanged
	{
		private CarEntity _car;
		public CarEntity Car
		{
			get
			{
				return _car;
			}
			set
			{
				_car = value;
				PropertyChanged(this, new PropertyChangedEventArgs("Car"));
			}
		}

		private CarSettingsWindow()
		{
			InitializeComponent();
		}

		public CarSettingsWindow(CarEntity source)
			: this()
		{
			Car = source;
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
