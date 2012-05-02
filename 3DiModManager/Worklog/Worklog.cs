using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _3DiModManager.Worklog
{
	class Worklog
	{
		public List<Action> Actions = new List<Action>();

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

		//Not needed now
		//public void EditCar(CarEntity car)
		//{
		//    var EditAction = new Action()
		//    {
		//        Car = car,
		//        Type = ActionType.Edit
		//    };
		//    Actions.Add(EditAction);
		//}

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
					Actions.Add(action);
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
