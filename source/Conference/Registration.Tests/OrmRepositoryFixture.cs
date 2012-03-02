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

namespace Registration.Tests
{
    using System;
    using Registration.Database;
    using Xunit;

    public class OrmRepositoryFixture
    {
        public OrmRepositoryFixture()
        {
            using (var context = new OrmRepository())
            {
                if (context.Database.Exists()) context.Database.Delete();

                context.Database.Create();
            }
        }

        [Fact]
        public void WhenSavingEntity_ThenCanRetrieveIt()
        {
            var id = Guid.NewGuid();

            using (var context = new OrmRepository())
            {
                var conference = new ConferenceSeatsAvailability(id);
                context.Save(conference);
            }

            using (var context = new OrmRepository())
            {
                var conference = context.Find<ConferenceSeatsAvailability>(id);

                Assert.NotNull(conference);
            }
        }

		[Fact]
		public void WhenSavingEntityTwice_ThenCanReloadIt()
		{
			var id = Guid.NewGuid();

			using (var context = new OrmRepository())
			{
				var conference = new ConferenceSeatsAvailability(id);
				context.Save(conference);
			}

			using (var context = new OrmRepository())
			{
				var conference = context.Find<ConferenceSeatsAvailability>(id);
				conference.RemainingSeats = 20;

				context.Save(conference);

				context.Entry(conference).Reload();

				Assert.Equal(20, conference.RemainingSeats);
			}
		}
    }
}
