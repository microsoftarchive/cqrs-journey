// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Registration.Database
{
    using System;
    using System.Data.Entity;

    public class OrmRepositoryInitializer : IDatabaseInitializer<OrmRepository>
    {
        private IDatabaseInitializer<OrmRepository> innerInitializer;

        // NOTE: we use decorator pattern here because the Seed logic is typically reused 
        // on tests which have a different requirement than production (they drop DBs on 
        // every run, regardless of change or AppDomain-wide caching of initialization).
        // Decorating makes it clear than inheriting from the built-in ones (two at least) 
        // and then extracting the Seed behavior in a strategy.
        public OrmRepositoryInitializer(IDatabaseInitializer<OrmRepository> innerInitializer)
        {
            this.innerInitializer = innerInitializer;
        }

        public void InitializeDatabase(OrmRepository context)
        {
            this.innerInitializer.InitializeDatabase(context);

            // Create views, seed reference data, etc.

            // TODO: remove hardcoded seats availability.
            if (context.Set<SeatsAvailability>().Find(Guid.Empty) == null)
            {
                var availability = new SeatsAvailability(Guid.Empty);
                //availability.AddSeats(50);
                context.Save(availability);
            }

            context.SaveChanges();
        }
    }
}
