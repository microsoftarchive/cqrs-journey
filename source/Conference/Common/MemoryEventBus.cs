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

namespace Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class MemoryEventBus : IEventBus
	{
		private object[] handlers;
		private List<IEvent> events = new List<IEvent>();

		public MemoryEventBus(params object[] handlers)
		{
			this.handlers = handlers;
		}

		public void Publish(IEvent @event)
		{
			this.events.Add(@event);

			Task.Factory.StartNew(() =>
			{
				var handlerType = typeof(IEventHandler<>).MakeGenericType(@event.GetType());

				foreach (dynamic handler in this.handlers
					.Where(x => handlerType.IsAssignableFrom(x.GetType())))
				{
					handler.Handle((dynamic)@event);
				}
			});
		}

		public IEnumerable<IEvent> Events { get { return this.events; } }
	}
}
