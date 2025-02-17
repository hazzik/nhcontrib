using System;
using NHibernate.Shards.Session;

namespace NHibernate.Shards.Query
{
	public class SetEnumEvent: IQueryEvent
	{
		private enum CtorType
		{
			PositionVal,
			NameVal
		}

		private readonly CtorType ctorType;
		private readonly int position;
		private readonly Enum val;
		private readonly String name;

		private SetEnumEvent(CtorType ctorType, int position, Enum val, String name)
		{
			this.ctorType = ctorType;
			this.position = position;
			this.val = val;
			this.name = name;
		}

		public SetEnumEvent(int position, Enum val)
			: this(CtorType.PositionVal, position, val, null)
		{
		}

		public SetEnumEvent(String name, Enum val)
			: this(CtorType.NameVal, -1, val, name)
		{
		}

		public void OnEvent(IQuery query)
		{
			switch (ctorType)
			{
				case CtorType.PositionVal:
					query.SetEnum(position, val);
					break;
				case CtorType.NameVal:
					query.SetEnum(name, val);
					break;
				default:
					throw new ShardedSessionException(
						"Unknown ctor type in SetEnumEvent: " + ctorType);
			}
		}

	}
}
