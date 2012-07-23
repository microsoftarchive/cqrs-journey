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

namespace Infrastructure
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    [Serializable]
    public class EntityNotFoundException : Exception
    {
        private readonly Guid entityId;
        private readonly string entityType;

        public EntityNotFoundException()
        {
        }

        public EntityNotFoundException(Guid entityId) : base(entityId.ToString())
        {
            this.entityId = entityId;
        }

        public EntityNotFoundException(Guid entityId, string entityType)
            : base(entityType + ": " + entityId.ToString())
        {
            this.entityId = entityId;
            this.entityType = entityType;
        }

        public EntityNotFoundException(Guid entityId, string entityType, string message, Exception inner) 
            : base(message, inner)
        {
            this.entityId = entityId;
            this.entityType = entityType;
        }

        protected EntityNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            this.entityId = Guid.Parse(info.GetString("entityId"));
            this.entityType = info.GetString("entityType");
        }

        public Guid EntityId
        {
            get { return this.entityId; }
        }

        public string EntityType
        {
            get { return this.entityType; }
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("entityId", this.entityId.ToString());
            info.AddValue("entityType", this.entityType);
        }
    }
}
