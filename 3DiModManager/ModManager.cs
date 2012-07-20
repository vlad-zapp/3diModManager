using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using SharpCompress.Reader;
using _3DiModManager.Worklogs;

namespace _3DiModManager
{
	public sealed class ModManager
	{
		#region fields & stuff

		private const string PlayerCarsPath = @"\data\config\player_cars.xml";
		private readonly IEnumerable<string> _carFilePaths = new[]
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

		private readonly string _rootPath;
		private readonly Worklog _worklog;

		public bool SafeMode { get; set; }

		public ObservableCollection<CarEntity> Cars { get; set; }
		public bool Changed { 
			get
			{
				_worklog.Optimize();
				return _worklog.HasWork;
			}
		}

		public delegate void VoidDelegate();
		public VoidDelegate OnSaved;


		#endregion

		#region ctor

		public ModManager(string rootPath)
		{
			SafeMode = true;
			CheckRoot(rootPath);
			_rootPath = rootPath;
			_worklog = new Worklog();

			Cars = new ObservableCollection<CarEntity>();
			var playerCarsConfig = XElement.Load(_rootPath + PlayerCarsPath);

			if (playerCarsConfig.HasElements)
			{
				var playerCars = playerCarsConfig.Elements().ToList();
				foreach (var carXml in playerCars)
				{
					Cars.Add(CarEntity.FromXml(carXml));
					TrackCarChanges(Cars.Last());
				}
			}
		}

		private void TrackCarChanges(CarEntity car)
		{
			car.PropertyChanged += (s, a) => _worklog.EditCar(car);
		}

		private void CheckRoot(string rootPath)
		{
			//rootPath.Last()=='/'?
		}

		#endregion

		#region add car

		public void LoadCarFromArchieve(string fileName)
		{
			var car = LoadCarConfigFromArchieve(fileName);
			
			if (car == null)
			{
				MessageBox.Show("Данный архив не содержит данных автомобиля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Stop);
				return;
			}
			
			if (car.Description == null || car.DisplayName == null)
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
				while (Cars.Remove(Cars.FirstOrDefault(m => m.Name == car.Name)))
				{
				}
			}
			Cars.Add(car);
			_worklog.AddCar(fileName,car);
		}

		private CarEntity LoadCarConfigFromArchieve(string fileName)
		{
			var nameCandidates = new Dictionary<string, int>();

			var input = new FileStream(fileName, FileMode.Open);
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
				else if (_carFilePaths.Any(m => ("\\"+Path.GetDirectoryName(archieveReader.Entry.FilePath)+"\\").Contains(m)))
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
				return CarEntity.FromXml(carNode);

			if (nameCandidates.Count > 0)
				return 
					new CarEntity
						{
							Name = nameCandidates.OrderBy(m => m.Value).Last().Key
						};

			return null;
		}

		private void UnpackCarToGame(string fileName, string carName)
		{
			//TODO: Speed it up somehow
			var input = new FileStream(fileName, FileMode.Open);
			var archieveReader = ReaderFactory.Open(input);

			while (archieveReader.MoveToNextEntry())
			{
				var path = System.IO.Path.GetDirectoryName(@"\" + archieveReader.Entry.FilePath);

				//in safe mode - don't unpack files not related to a car!
				if(SafeMode && !path.Contains(carName))
					continue;

				if (path.Contains(@"\data\"))
					path = path.Substring(path.IndexOf(@"\data\"));
				
				else if (path.Contains(@"\export\"))
					path = path.Substring(path.IndexOf(@"\export\"));
				
				else
					continue;
				
				Directory.CreateDirectory(_rootPath + path);
				archieveReader.WriteEntryToDirectory(_rootPath + path);
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
				_worklog.DeleteCar(carToRemove);
			}
		}

		public void DeleteCarFromGame(string name)
		{
			foreach (var carPath in _carFilePaths)
			{
				try
				{
					Directory.Delete(_rootPath + carPath + name, true);
				}
				catch
				{
				}
			}
		}

		#endregion

		public void EditCar(CarEntity car)
		{
			_worklog.EditCar(car);
		}

		public void SaveChanges()
		{
			WaitCallback x = state =>
			                 	{
			                 		foreach (var action in _worklog.Actions)
			                 		{
			                 			if (action.Type == ActionType.Add)
			                 			{
			                 				UnpackCarToGame(action.FileName, action.Car.Name);
			                 			}
			                 			else if (action.Type == ActionType.Delete)
			                 			{
			                 				DeleteCarFromGame(action.Car.Name);
			                 			}
			                 		}

									var playerCarsConfig = new XElement("Cars");
									foreach (var car in Cars)
									{
										playerCarsConfig.Add(car.AsXml());
										car.UpdateVersion();
									}

									//TODO: force to save it in utf8?
									playerCarsConfig.Save(_rootPath + PlayerCarsPath);
			                 		OnSaved();
			                 	};

			//it will not block UI. This is for the future features.
			ThreadPool.QueueUserWorkItem(x);
		}
	}
}
