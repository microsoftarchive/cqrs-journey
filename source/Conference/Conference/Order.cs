// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Conference
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;

    public class Order
    {
        public enum OrderStatus
        {
            Pending,
            Paid,
        }

        public Order(Guid conferenceId, Guid orderId, string accessCode)
            : this()
        {
            this.Id = orderId;
            this.ConferenceId = conferenceId;
            this.AccessCode = accessCode;
        }

        protected Order()
        {
            this.Seats = new ObservableCollection<OrderSeat>();
        }

        [Key]
        public Guid Id { get; set; }
        public Guid ConferenceId { get; set; }

        /// <summary>
        /// Used for correlating with the seat assignments.
        /// </summary>
        public Guid? AssignmentsId { get; set; }

        [Display(Name = "Order Code")]
        public string AccessCode { get; set; }
        [Display(Name = "Registrant Name")]
        public string RegistrantName { get; set; }
        [Display(Name = "Registrant Email")]
        public string RegistrantEmail { get; set; }
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// This pattern is typical for EF 4 since it does 
        /// not support native enum persistence. EF 4.5 does.
        /// </summary>
        [NotMapped]
        public OrderStatus Status
        {
            get { return (OrderStatus)this.StatusValue; }
            set { this.StatusValue = (int)value; }
        }
        public int StatusValue { get; set; }

        public ICollection<OrderSeat> Seats { get; set; }
    }
}
