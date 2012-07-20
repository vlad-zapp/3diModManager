using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _3DiModManager.Worklogs
{
	class Worklog
	{
		public List<Action> Actions = new List<Action>();

		private bool _hasWork;
		public bool HasWork
		{
			get { return Actions.Count > 0 || _hasWork; }
			set { _hasWork = value; }
		}

		public void AddCar(string fileName, CarEntity car)
		{
			var AddAction = new Action()
			                	{
									Car=car,
									FileName = fileName,
									Type = ActionType.Add
			                	};
			Actions.Add(AddAction);
		}

		public void EditCar(CarEntity car)
		{
			//parameter is not needed now.
			_hasWork = true;
		}

		public void DeleteCar(CarEntity car)
		{
			var DeleteAction = new Action()
			{
				Car = car,
				Type = ActionType.Delete
			};
			Actions.Add(DeleteAction);
		}

		public void Optimize()
		{
			var filteredActions = new List<Action>();
			
			foreach (var action in Actions)
			{
				var lastWithTheSameCar = filteredActions.LastOrDefault(m => m.Car == action.Car);
				
				if(lastWithTheSameCar==null)
					filteredActions.Add(action);
				else
				{
					filteredActions.Remove(lastWithTheSameCar);
					if(lastWithTheSameCar.Type==action.Type)
					{
						filteredActions.Add(action);
					}
				}
			}

			Actions = filteredActions;
		}
	}
}
