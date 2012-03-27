using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace _3DiModManager
{
	public class CarEntity:INotifyPropertyChanged
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
				if (this.PropertyChanged!=null)
					PropertyChanged(this, new PropertyChangedEventArgs("IsIngame"));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
