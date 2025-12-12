using System;
using System.Collections.Generic;

public class GRBehaviors<T> : GRBehaviorsBase where T : Enum
{
	public class BehaviorData
	{
		public T behavior;

		public GRAbilityBase ability;
	}

	public List<BehaviorData> behaviorData;

	public void AddBehavior(T behavior, GRAbilityBase ability)
	{
		BehaviorData item = new BehaviorData
		{
			behavior = behavior,
			ability = ability
		};
		behaviorData.Add(item);
	}
}
