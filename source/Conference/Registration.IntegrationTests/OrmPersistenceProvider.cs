using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Registration.Tests;
using Registration.Database;
using Common;

namespace Registration.IntegrationTests
{
	public class OrmPersistenceProvider : IPersistenceProvider
	{
		private OrmRepository orm;

		private OrmRepository Orm
		{
			get
			{
				if (this.orm == null)
				{
					using (var context = new OrmRepository())
					{
						if (context.Database.Exists())
							context.Database.Delete();

						context.Database.Create();
					}

					this.orm = new OrmRepository();
				}

				return this.orm;
			}
		}

		public T PersistReload<T>(T sut)
			where T : class, IAggregateRoot
		{
			this.Orm.Save(sut);
			this.Orm.Entry(sut).Reload();
			return sut;
		}

		public void Dispose()
		{
			if (this.orm != null)
				this.orm.Dispose();
		}
	}
}
