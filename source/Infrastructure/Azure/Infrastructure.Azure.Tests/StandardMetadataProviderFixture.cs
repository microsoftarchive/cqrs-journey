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

namespace Infrastructure.Azure.Tests.StandardMetadataProviderFixture
{
    using System;
    using Infrastructure.Messaging;
    using Xunit;

    public class given_a_metadata_provider
    {
        [Fact]
        public void when_getting_metadata_then_returns_type_name()
        {
            var provider = new StandardMetadataProvider();
            var expected = typeof(given_a_metadata_provider).Name;

            var metadata = provider.GetMetadata(this);

            Assert.Contains(expected, metadata.Values);
            Assert.Contains(StandardMetadata.TypeName, metadata.Keys);
        }

        [Fact]
        public void when_getting_metadata_then_returns_type_fullname()
        {
            var provider = new StandardMetadataProvider();
            var expected = typeof(given_a_metadata_provider).FullName;

            var metadata = provider.GetMetadata(this);

            Assert.Contains(expected, metadata.Values);
            Assert.Contains(StandardMetadata.FullName, metadata.Keys);
        }

        [Fact]
        public void when_getting_metadata_then_returns_assembly_name()
        {
            var provider = new StandardMetadataProvider();
            var expected = typeof(given_a_metadata_provider).Assembly.GetName().Name;

            var metadata = provider.GetMetadata(this);

            Assert.Contains(expected, metadata.Values);
            Assert.Contains(StandardMetadata.AssemblyName, metadata.Keys);
        }

        [Fact]
        public void when_getting_metadata_then_returns_namespace()
        {
            var provider = new StandardMetadataProvider();
            var expected = typeof(given_a_metadata_provider).Namespace;

            var metadata = provider.GetMetadata(this);

            Assert.Contains(expected, metadata.Values);
            Assert.Contains(StandardMetadata.Namespace, metadata.Keys);
        }

        private class FakeEvent : IEvent
        {
            public Guid SourceId { get; set; }
        }
    }
}
