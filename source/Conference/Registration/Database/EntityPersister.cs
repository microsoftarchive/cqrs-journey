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

namespace Registration.Database
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
using System.Data.Entity;
	using System.Data;
	using System.Data.Entity.Infrastructure;
	using System.Collections;

	/// <summary>
	/// Persists entity hierarchies to a DbContext automatically.
	/// </summary>
	public class EntityPersister
	{
		private DbContext context;

		public EntityPersister(DbContext context)
		{
			this.context = context;
		}

		public void Persist(object entity)
		{
			var entry = this.context.Entry(entity);

			// Add if the object was not loaded from the repository.
			if (entry.State == EntityState.Detached)
			{
				Add(entry, entity);
			}

			// Otherwise, do nothing as the ORM already tracks 
			// attached entities that need to be saved (or not).
		}

		private void Add(DbEntityEntry entry, object entity)
		{
			entry.State = EntityState.Added;
			AddChildren(entry, entity);
		}

		private void AddChildren(DbEntityEntry entry, object entity)
		{
			var properties = entity.GetType().GetProperties().Where(x => x.CanRead);
			foreach (var property in properties)
			{
				var memberEntry = entry.Member(property.Name);
				if (memberEntry is DbCollectionEntry)
				{
					// Retrieve all child elements from the enumerable collection.
					var collectionEntry = (DbCollectionEntry)memberEntry;
					var collection = (IEnumerable)memberEntry.CurrentValue ?? Enumerable.Empty<object>();
					var references = collection.Cast<object>().ToArray();
					var referenceEntityType = GetEntityType(property.PropertyType);

					// Recursively save or update each referenced entity
					foreach (var reference in references.Where(x => x != null))
					{
						this.Add(this.context.Entry(reference), reference);
					}
				}
				else if (memberEntry is DbReferenceEntry && property.CanWrite)
				{
					var referenceEntityType = property.PropertyType;
					var reference = memberEntry.CurrentValue;
					if (reference != null)
					{
						this.Add(this.context.Entry(reference), reference);
					}
				}
			}
		}

		private Type GetEntityType(Type type)
		{
			var enumerable = type.GetInterfaces()
				.FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
			if (enumerable == null)
				throw new ArgumentException("Type is not a generic enumerable collection.");

			return enumerable.GetGenericArguments()[0];
		}
	}
}
