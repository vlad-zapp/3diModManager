using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace _3DiModManager
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (e.Args.Length > 0)
			{
				this.Properties["GamePath"] = e.Args[0];
			}
		}
	}
}
