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

namespace Registration
{
    using System;
    using Registration.Commands;
    using Common;
    using System.Collections.Generic;

    public class RegistrationProcessSaga : IAggregateRoot, ICommandPublisher
    {
        public enum SagaState
        {
            NotStarted,
            AwaitingReservationConfirmation,
            Completed, 
			Deleted
        }

        private List<ICommand> commands = new List<ICommand>();

        public SagaState State { get; private set; }

        public void Handle(RegisterToConference command)
        {
            if (this.State == SagaState.NotStarted)
            {
                this.State = SagaState.AwaitingReservationConfirmation;
                this.commands.Add(new MakeReservation());
            }
            else
            {
               throw new InvalidOperationException(); 
            }
        }

		public Guid Id { get; private set; }

        public IEnumerable<ICommand> Commands
        {
            get { return this.commands; }
        }
    }
}
