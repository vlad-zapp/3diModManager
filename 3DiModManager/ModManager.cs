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

			playerCarsConfig = XElement.Load(RootPath + PlayerCarsPath);

			if (playerCarsConfig.HasElements)
			{
				playerCars = playerCarsConfig.Elements().ToList();
				foreach (var carXml in playerCars)
				{
					_cars.Add(MakeCarFromXml(carXml));
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

		public CarEntity LoadCarXmlFromArchieve(string fileName)
		{
			FileStream input = new FileStream(fileName, FileMode.Open);
			var archieveReader = ReaderFactory.Open(input);
			XElement carNode = null;

			while (archieveReader.MoveToNextEntry() && carNode==null)
			{
				var name = Path.GetFileName(archieveReader.Entry.FilePath);
				if (name.EndsWith(".xml") || name.EndsWith(".txt"))
				{
					StreamReader input_config = null;
					try
					{
						archieveReader.WriteEntryToDirectory(Path.GetTempPath());

						//TODO:need to select encoding here?
						if (name.EndsWith(".xml"))
							input_config = new StreamReader(Path.GetTempPath() + name);
						else
							input_config = new StreamReader(Path.GetTempPath() + name, Encoding.Default);

						XElement someXml = XElement.Load(input_config, LoadOptions.PreserveWhitespace);

						if (someXml.Name == "Car")
							carNode = someXml;
						else
							carNode = someXml.Descendants("Car").FirstOrDefault();
					}
					finally
					{
						if (input_config != null)
							input_config.Close();
					}
				}
			}

			input.Close();
			
			if (carNode == null)
				{
					//create node!
					//handle car existance here too!
				}

			var existingCar = _cars.FirstOrDefault(m => m.Name == carNode.Attribute("Name").Value);

			if (existingCar != null)
			{
				var result = MessageBox.Show(
					String.Format("Автомобиль {0} ({1}) уже установлен в игре. Заменить?", existingCar.DisplayName, existingCar.Name),
					"Внимание", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.No)
					return null;

				//remove all cars matched by name
				while (_cars.Remove(_cars.FirstOrDefault(m => m.Name == carNode.Attribute("Name").Value))) ;
			}

			return MakeCarFromXml(carNode);
		}

		public void LoadCar(string fileName)
		{
			var car=LoadCarXmlFromArchieve(fileName);
			if (car == null)
				return;

			FileStream input = new FileStream(fileName, FileMode.Open);
			var archieveReader = ReaderFactory.Open(input);

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
				} else
				{
					continue;
				}

				Directory.CreateDirectory(RootPath+path);
				archieveReader.WriteEntryToDirectory(RootPath+path);
			}

			_cars.Add(car);
			changed = true;
			input.Close();
		}

		public CarEntity MakeCarFromXml(XElement carNode, bool trackChanges=true)
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
			
			if (trackChanges)
			{
				currentCar.PropertyChanged += (e, k) => changed = true;
			}
			return currentCar;
		}

	}
}
