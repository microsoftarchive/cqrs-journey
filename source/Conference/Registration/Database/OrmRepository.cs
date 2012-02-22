// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Data;
using System.Linq.Expressions;

namespace Registration.Database
{
	public class OrmRepository : DbContext, IRepository
	{
		public OrmRepository()
			: base("ConferenceRegistration")
		{
		}

		public T Find<T>(Guid id) where T : class, IAggregateRoot
		{
			return this.Set<T>().Find(id);
		}

		public void Save<T>(T aggregate) where T : class, IAggregateRoot
		{
			var entry = this.Entry(aggregate);
			
			// Add if the object was not loaded from the repository.
			if (entry.State == EntityState.Detached)
				this.Set<T>().Add(aggregate);

			// Otherwise, do nothing as the ORM already tracks 
			// attached entities that need to be saved (or not).

			this.SaveChanges();
		}

		// Define the available entity sets for the database.
		public virtual DbSet<ConferenceSeatsAvailability> ConferenceSeats { get; private set; }
	}
}
