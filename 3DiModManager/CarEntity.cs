using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace _3DiModManager
{
	public class CarEntity : INotifyPropertyChanged
	{
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public bool AT { get; set; }
		public bool ABS { get; set; }

		//extra properties, not related to the game
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
			};
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

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
