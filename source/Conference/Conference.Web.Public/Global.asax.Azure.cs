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

namespace Conference.Web.Public
{
    using System.Web;
    using Infrastructure;
    using Infrastructure.Azure;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.Practices.Unity;
    using Payments.Handlers;
    using Registration.Handlers;

    partial class MvcApplication
    {
        static partial void OnCreateContainer(UnityContainer container)
        {
            var serializer = new JsonTextSerializer();
            container.RegisterInstance<ITextSerializer>(serializer);

            var settings = InfrastructureSettings.Read(HttpContext.Current.Server.MapPath(@"~\bin\Settings.xml")).ServiceBus;
            new ServiceBusConfig(settings).Initialize();

            var commandBus = new CommandBus(new TopicSender(settings, "conference/commands"), new StandardMetadataProvider(), serializer);

            container.RegisterInstance<ICommandBus>(commandBus);

            // support for executing commands

            container.RegisterType<ICommandHandler, OrderCommandHandler>("OrderCommandHandler");
            container.RegisterType<ICommandHandler, ThirdPartyProcessorPaymentCommandHandler>("ThirdPartyProcessorPaymentCommandHandler");
            container.RegisterType<ICommandHandler, SeatAssignmentsHandler>("SeatAssignmentsHandler");

        }
    }
}