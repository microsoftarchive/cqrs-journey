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
    using System.Data.Entity;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Infrastructure.Sql.Messaging;
    using Infrastructure.Sql.Messaging.Implementation;
    using Microsoft.Practices.Unity;

    partial class MvcApplication
    {
        static partial void OnCreateContainer(UnityContainer container)
        {
            var serializer = new JsonTextSerializer();
            container.RegisterInstance<ITextSerializer>(serializer);

            container.RegisterType<IMessageSender, MessageSender>(
                "Commands", new TransientLifetimeManager(), new InjectionConstructor(Database.DefaultConnectionFactory, "SqlBus", "SqlBus.Commands"));
            container.RegisterType<ICommandBus, CommandBus>(
                new ContainerControlledLifetimeManager(), new InjectionConstructor(new ResolvedParameter<IMessageSender>("Commands"), serializer));
        }
    }
}