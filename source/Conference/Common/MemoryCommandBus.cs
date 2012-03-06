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

	/// <summary>
	/// Sample in-memory command bus that is asynchronous.
	/// </summary>
	public class MemoryCommandBus : ICommandBus
	{
		private List<ICommandHandler> handlers = new List<ICommandHandler>();
		private List<ICommand> commands = new List<ICommand>();

		public MemoryCommandBus(params ICommandHandler[] handlers)
		{
			this.handlers.AddRange(handlers);
		}

		public void Register(ICommandHandler handler)
		{
			this.handlers.Add(handler);
		}

		public void Send(ICommand command)
		{
			this.commands.Add(command);

			Task.Factory.StartNew(() =>
			{
				var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());

				foreach (dynamic handler in this.handlers
					.Where(x => handlerType.IsAssignableFrom(x.GetType())))
				{
					handler.Handle((dynamic)command);
				}
			});
		}

		public void Send(IEnumerable<ICommand> commands)
		{
			foreach (var command in commands)
			{
				this.Send(command);
			}
		}

		public IEnumerable<ICommand> Commands
		{
			get { return this.commands; }
		}
	}
}
