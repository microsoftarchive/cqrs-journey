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

namespace Registration.Handlers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Common;
	using Registration.Commands;
	using System.Threading.Tasks;
	using System.Threading;

	/// <summary>
	/// Handles delayed commands in-memory, sending them to the bus 
	/// after waiting for the specified time.
	/// </summary>
	/// <devdoc>
	/// This will be replaced with an AzureDelayCommandHandler that will 
	/// leverage azure service bus capabilities for sending delayed messages.
	/// </devdoc>
	public class MemoryDelayCommandHandler : ICommandHandler<DelayCommand>
	{
		private ICommandBus commandBus;

		public MemoryDelayCommandHandler(ICommandBus commandBus)
		{
			this.commandBus = commandBus;
		}

		public void Handle(DelayCommand command)
		{
			Task.Factory.StartNew(() =>
			{
				Thread.Sleep(command.SendDelay);
				this.commandBus.Send(command);
			});
		}
	}
}
