using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using SharpCompress.Reader;

namespace _3DiModManager
{
	public class ModManager
	{
		private const string PlayerCarsPath = @"\data\config\player_cars.xml";
		private readonly string RootPath;
		private List<XElement> playerCars;

		public bool working { get; set; }

		public bool changed;

		private ObservableCollection<CarEntity> _cars; 
		public ObservableCollection<CarEntity> Cars {
			get
			{
				UpdateCarsList();
				return _cars;
			}
			set
			{
				_cars = value;
			}
		}

		public ModManager(string rootPath)
		{
			changed = false;
			CheckRoot(rootPath);
			RootPath = rootPath;
		}

		public void UpdateCarsList()
		{
			_cars = new ObservableCollection<CarEntity>();
			XElement playerCarsConfig = null;
			try
			{
				playerCarsConfig = XElement.Load(RootPath + PlayerCarsPath);
			}
			catch (Exception e)
			{

			}
			if (playerCarsConfig.HasElements)
			{
				playerCars = playerCarsConfig.Elements().ToList();
				foreach (var car in playerCars)
				{
					AddCar(car);
				}
			}
		}

		private void CheckRoot(string rootPath)
		{
			//rootPath.Last()=='/'?
		}

		public void SaveChanges()
		{
			var playerCarsConfig = new XElement("Cars");
			foreach (var car in _cars)
			{
				XElement carXml = new XElement(car.IsIngame ? "Car" : "DisabledCar");
				carXml.SetAttributeValue("Name",car.Name);
				carXml.SetAttributeValue("ABS", car.ABS);
				carXml.SetAttributeValue("AT", car.AT);

				carXml.SetElementValue("DisplayName",car.DisplayName);
				carXml.SetElementValue("Description", car.Description);
				carXml.SetElementValue("Author", car.Author);

				playerCarsConfig.Add(carXml);
			}
			//TODO: save it in utf8
			playerCarsConfig.Save(RootPath + PlayerCarsPath);
		}

		public void LoadCar(string fileName)
		{
			FileStream input = new FileStream(fileName, FileMode.Open);
			var archieveReader = ReaderFactory.Open(input);

			List<string> entitylist = new List<string>();

			working = true;

			while (archieveReader.MoveToNextEntry())
			{
				var path = System.IO.Path.GetDirectoryName(@"\" + archieveReader.Entry.FilePath);
				if (path.Contains(@"\data\"))
				{
					path = path.Substring(path.IndexOf(@"\data\"));
				}
				else if (path.Contains(@"\export\"))
				{
					path = path.Substring(path.IndexOf(@"\export\"));
				} 
				else
				{
					var name = archieveReader.Entry.FilePath;
					XElement carNode=null;

					if (name.EndsWith(".xml") || name.EndsWith(".txt"))
					{
						StreamReader input_config=null;
						try
						{
							archieveReader.WriteEntryToDirectory(Path.GetTempPath());

							//TODO:need to select encoding here?
							input_config = new StreamReader(Path.GetTempPath() + name,Encoding.Default);

							XElement someXml = XElement.Load(input_config, LoadOptions.PreserveWhitespace);

							if (someXml.Name == "Car")
								carNode = someXml;
							else
								carNode = someXml.Descendants("Car").FirstOrDefault();
						}
						finally
						{
							if(input_config!=null)
								input_config.Close();
						}
					}
					if (carNode == null)
					{
						//create node!
						//handle car existance here too!
					}
					//and here!

					var existingCar = _cars.FirstOrDefault(m => m.Name == carNode.Attribute("Name").Value);

					if (existingCar != null)
					{
					    var result = MessageBox.Show(
							String.Format("Автомобиль {0} ({1}) уже установлен в игре. Заменить?",existingCar.DisplayName,existingCar.Name), 
							"Внимание", MessageBoxButton.YesNo);
					    if (result == MessageBoxResult.No)
					        return;

						//remove all cars matched by name
						while (_cars.Remove(_cars.FirstOrDefault(m => m.Name == carNode.Attribute("Name").Value))) ;
					}

					AddCar(carNode);
					changed = true;
					continue;
				}

				Directory.CreateDirectory(RootPath+path);
				archieveReader.WriteEntryToDirectory(RootPath+path);
			}

			input.Close();
			//working = false;
		}
			
		public void AddCar(XElement carNode)
		{
			var currentCar = new CarEntity()
			{
				Name = carNode.Attribute("Name").Value,
				DisplayName = carNode.Element("DisplayName").Value,
				ABS = bool.Parse(carNode.Attribute("ABS").Value),
				AT = bool.Parse(carNode.Attribute("AT").Value),
				Description = carNode.Element("Description").Value,
				Author = carNode.Element("Author") != null ? carNode.Element("Author").Value : "",
				IsIngame = carNode.Name.LocalName == "Car" ? true : false,
			};
			currentCar.PropertyChanged += (e, k) => changed = true;
			 _cars.Add(currentCar);
		}

	}
}
