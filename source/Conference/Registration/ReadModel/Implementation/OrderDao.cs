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

namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class OrderDao : IOrderDao
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        public OrderDao(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public OrderDTO GetOrderDetails(Guid orderId)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                return repository.Query<OrderDTO>().Include(x => x.Lines).FirstOrDefault(dto => dto.OrderId == orderId);
            }
        }

        public Guid? LocateOrder(string email, string accessCode)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var orderProjection = repository
                    .Query<OrderDTO>()
                    .Where(o => o.RegistrantEmail == email && o.AccessCode == accessCode)
                    .Select(o => new { o.OrderId })
                    .FirstOrDefault();

                if (orderProjection != null)
                {
                    return orderProjection.OrderId;
                }

                return null;
            }
        }

        public TotalledOrder GetTotalledOrder(Guid orderId)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                return repository.Query<TotalledOrder>().Include(x => x.Lines).FirstOrDefault(dto => dto.OrderId == orderId);
            }
        }
    }
}