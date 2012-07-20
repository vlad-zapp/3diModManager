using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace _3DiModManager
{
	public enum CarVersion
	{
		Unknown,
		Ver227,
		Ver228,
	}

	public class CarEntity : INotifyPropertyChanged
	{
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public bool AT { get; set; }
		public bool ABS { get; set; }

		//extra properties, not related to the game
		public CarVersion Version { get; set; }
		public string Author { get; set; }

		private bool _isIngame;
		public bool IsIngame
		{
			get
			{
				return _isIngame;
			}
			set
			{
				_isIngame = value;

				if (this.PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("IsIngame"));
			}
		}

		public static CarEntity FromXml(XElement carNode, bool trackChanges = true)
		{
			var currentCar = new CarEntity()
			{
				Name = carNode.Attribute("Name").Value,
				DisplayName = carNode.Element("DisplayName").Value,
				ABS = carNode.Attribute("ABS") != null ? bool.Parse(carNode.Attribute("ABS").Value) : false,
				AT = carNode.Attribute("AT") != null ? bool.Parse(carNode.Attribute("AT").Value) : false,
				Description = carNode.Element("Description") != null ? carNode.Element("Description").Value : string.Empty,
				Author = carNode.Element("Author") != null ? carNode.Element("Author").Value : "Неизвестен",
				IsIngame = carNode.Name.LocalName == "Car" ? true : false,
				Version = CarVersion.Unknown
			};

			//determine version
			try
			{
				var targetDir = Path.Combine(App.Current.Properties["GamePath"].ToString(), @"data\physics\cars\", currentCar.Name);
				var settings = Directory.GetFiles(targetDir, "*.ini");

				foreach (var file in settings)
				{
					var text = File.ReadAllText(file);
					if (text.Contains("PhysicsFilenames"))
					{
						currentCar.Version = CarVersion.Ver227;
						break;
					}
					if (text.Contains("PhysicsFiles"))
					{
						currentCar.Version = CarVersion.Ver228;
						break;
					}
				}
			}
			catch
			{
				//don't give a fuck
			}

			return currentCar;
		}

		public XElement AsXml()
		{
			XElement carXml = new XElement(IsIngame ? "Car" : "DisabledCar");

			carXml.SetAttributeValue("Name", Name);
			carXml.SetAttributeValue("ABS", ABS);
			carXml.SetAttributeValue("AT", AT);

			carXml.SetElementValue("DisplayName", DisplayName);
			carXml.SetElementValue("Description", Description);
			carXml.SetElementValue("Author", Author);

			return carXml;
		}

		public void UpdateVersion()
		{
			//TODO: check that update is really needed
			if (Version == CarVersion.Unknown)
				return;

				var targetDir = Path.Combine(App.Current.Properties["GamePath"].ToString(), @"data\physics\cars\", Name);
				var settings = Directory.GetFiles(targetDir, "*.ini");

				foreach (var file in settings)
				{
					try
					{
						var text = File.ReadAllText(file);
						if (Version == CarVersion.Ver227)
						{
							File.WriteAllText(file, text.Replace("PhysicsFiles", "PhysicsFilenames"));
						}
						else if (Version == CarVersion.Ver228)
						{
							File.WriteAllText(file, text.Replace("PhysicsFilenames", "PhysicsFiles"));
						}
					} catch
					{
						//don't give a fuck
						continue;
					}
				}
		}

		public void Update()
		{
			PropertyChanged(this, new PropertyChangedEventArgs("DisplayName"));
			PropertyChanged(this, new PropertyChangedEventArgs("Version"));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
