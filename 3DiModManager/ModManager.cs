using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using SharpCompress.Reader;
using _3DiModManager.Worklog;
using Action = _3DiModManager.Worklog.Action;

namespace _3DiModManager
{
	public sealed class ModManager
	{
		#region fields & stuff
		private const string PlayerCarsPath = @"\data\config\player_cars.xml";
		private IEnumerable<string> CarFilePaths = new string[]
		                                           	{
														@"\data\gamedata\cars\",
														@"\data\gui\Common\layouts\cars\",
														@"\data\gui\3di_home\imagesets\cars\",
														@"\data\physics\cars\",
														@"\data\physics\Engine\",
														@"\data\physics\Transmission\",
														@"\export\gfxlib\cars\",
														@"\export\texturesdds\cars\",
														@"\export\meshes\cars\"
		                                           	};

		private readonly string RootPath;
		private Worklog.Worklog worklog;

		public ObservableCollection<CarEntity> Cars { get; set; }
		public bool changed {
			get { return worklog.Actions.Count > 0 ? true : false; }
		}

		public delegate void voidDelegate();
		public voidDelegate onSaved;


		#endregion

		public ModManager(string rootPath)
		{
			CheckRoot(rootPath);
			RootPath = rootPath;
			worklog = new Worklog.Worklog();

			Cars = new ObservableCollection<CarEntity>();
			XElement playerCarsConfig = null;

			playerCarsConfig = XElement.Load(RootPath + PlayerCarsPath);

			if (playerCarsConfig.HasElements)
			{
				var playerCars = playerCarsConfig.Elements().ToList();
				foreach (var carXml in playerCars)
				{
					Cars.Add(MakeCarFromXml(carXml));
				}
			}
		}

		private void CheckRoot(string rootPath)
		{
			//rootPath.Last()=='/'?
		}

		#region add car

		public void LoadCarFromArchieve(string fileName)
		{
			var car = LoadCarConfigFromArchieve(fileName);
			if (car == null)
			{
				MessageBox.Show("Данный архив не содержит данных автомобиля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Stop);
				return;
			}
			else if (car.Description == null || car.DisplayName == null)
			{
				var settings = new CarSettingsWindow(car);
				settings.ShowDialog();
				car.IsIngame = true;
			}

			var existingCar = Cars.FirstOrDefault(m => m.Name == car.Name);
			if (existingCar != null)
			{
				var result = MessageBox.Show(
					String.Format("Автомобиль {0} ({1}) уже установлен в игре. Заменить?", existingCar.DisplayName, existingCar.Name),
					"Внимание", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.No)
					return;

				//remove all cars matched by name
				while (Cars.Remove(Cars.FirstOrDefault(m => m.Name == car.Name))) ;
			}
			Cars.Add(car);
			worklog.AddCar(fileName,car);
		}

		private CarEntity LoadCarConfigFromArchieve(string fileName)
		{
			Dictionary<string, int> nameCandidates = new Dictionary<string, int>();

			FileStream input = new FileStream(fileName, FileMode.Open);
			var archieveReader = ReaderFactory.Open(input);
			XElement carNode = null;

			while (archieveReader.MoveToNextEntry() && carNode == null)
			{
				var name = '\\' + Path.GetFileName(archieveReader.Entry.FilePath);
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
					catch
					{
						continue;
					}
					finally
					{
						if (input_config != null)
							input_config.Close();
					}
				}
				else if (CarFilePaths.Any(m => ("\\"+Path.GetDirectoryName(archieveReader.Entry.FilePath)+"\\").Contains(m)))
				{
					var dirName = Path.GetDirectoryName(archieveReader.Entry.FilePath).Split('\\').LastOrDefault();

					if (!string.IsNullOrEmpty(dirName))
					{
						if (!nameCandidates.ContainsKey(dirName))
						{
							nameCandidates[dirName] = 1;
						}
						else
						{
							nameCandidates[dirName]++;
						}
					}
				}
			}

			input.Close();

			if (carNode != null)
			{
				return MakeCarFromXml(carNode);
			}
			else if (nameCandidates.Count > 0)
			{
				return new CarEntity()
				{
					Name = nameCandidates.OrderBy(m => m.Value).Last().Key
				};
			}
			else
			{
				return null;
			}
		}

		private void UnpackCarToGame(string fileName)
		{

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
				}
				else
				{
					continue;
				}

				Directory.CreateDirectory(RootPath + path);
				archieveReader.WriteEntryToDirectory(RootPath + path);
			}
			input.Close();
		}

		#endregion

		#region remove car

		public void DeleteCar(string name)
		{
			var carToRemove = Cars.FirstOrDefault(m => m.Name == name);
			if (Cars.Remove(carToRemove))
			{
				worklog.DeleteCar(carToRemove);
			}
		}

		public void DeleteCarFromGame(string name)
		{
			foreach (var carPath in CarFilePaths)
			{
				try
				{
					Directory.Delete(RootPath + carPath + name, true);
				}
				catch
				{
				}
			}
		}

		#endregion

		#region car-xml convertions
		private CarEntity MakeCarFromXml(XElement carNode, bool trackChanges = true)
		{
			var currentCar = new CarEntity()
			{
				Name = carNode.Attribute("Name").Value,
				DisplayName = carNode.Element("DisplayName").Value,
				ABS = carNode.Attribute("ABS")!=null?bool.Parse(carNode.Attribute("ABS").Value):false,
				AT = carNode.Attribute("AT")!=null?bool.Parse(carNode.Attribute("AT").Value):false,
				Description = carNode.Element("Description") != null ? carNode.Element("Description").Value : string.Empty,
				Author = carNode.Element("Author") != null ? carNode.Element("Author").Value : "Неизвестен",
				IsIngame = carNode.Name.LocalName == "Car" ? true : false,
			};

			return currentCar;
		}

		private XElement MakeXmlFromcar(CarEntity car)
		{
			XElement carXml = new XElement(car.IsIngame ? "Car" : "DisabledCar");

			carXml.SetAttributeValue("Name", car.Name);
			carXml.SetAttributeValue("ABS", car.ABS);
			carXml.SetAttributeValue("AT", car.AT);

			carXml.SetElementValue("DisplayName", car.DisplayName);
			carXml.SetElementValue("Description", car.Description);
			carXml.SetElementValue("Author", car.Author);

			return carXml;
		}

		#endregion

		public void SaveChanges()
		{
			WaitCallback x = state =>
			                 	{
			                 		foreach (var action in worklog.Actions)
			                 		{
			                 			if (action.Type == ActionType.Add)
			                 			{
			                 				UnpackCarToGame(action.FileName);
			                 			}
			                 			else if (action.Type == ActionType.Delete)
			                 			{
			                 				DeleteCarFromGame(action.Car.Name);
			                 			}
			                 		}

									var playerCarsConfig = new XElement("Cars");
									foreach (var car in Cars)
									{
										playerCarsConfig.Add(MakeXmlFromcar(car));
									}

									//TODO: force to save it in utf8?
									playerCarsConfig.Save(RootPath + PlayerCarsPath);

									//worklog.Actions = new List<Action>();
			                 		onSaved();
			                 	};
			ThreadPool.QueueUserWorkItem(x);

		}

	}
}
