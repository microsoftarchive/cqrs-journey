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

namespace Infrastructure.Sql.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using Infrastructure.Messaging;
    using Infrastructure.Processes;
    using Infrastructure.Serialization;
    using Infrastructure.Sql.Processes;
    using Moq;
    using Xunit;

    public class SqlProcessDataContextFixture : IDisposable
    {
        private readonly string dbName = typeof(SqlProcessDataContextFixture).Name + "-" + Guid.NewGuid();

        public SqlProcessDataContextFixture()
        {
            using (var context = new TestProcessDbContext(dbName))
            {
                context.Database.Delete();
                context.Database.Create();
            }
        }

        public void Dispose()
        {
            using (var context = new TestProcessDbContext(dbName))
            {
                context.Database.Delete();
            }
        }

        [Fact]
        public void WhenSavingEntity_ThenCanRetrieveIt()
        {
            var id = Guid.NewGuid();

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>()))
            {
                var conference = new OrmTestProcess(id);
                context.Save(conference);
            }

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>()))
            {
                var conference = context.Find(id);

                Assert.NotNull(conference);
            }
        }

        [Fact]
        public void WhenSavingEntityTwice_ThenCanReloadIt()
        {
            var id = Guid.NewGuid();

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>()))
            {
                var conference = new OrmTestProcess(id);
                context.Save(conference);
            }

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>()))
            {
                var conference = context.Find(id);
                conference.Title = "CQRS Journey";

                context.Save(conference);
            }

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), Mock.Of<ICommandBus>(), Mock.Of<ITextSerializer>()))
            {
                var conference = context.Find(id);

                Assert.Equal("CQRS Journey", conference.Title);
            }
        }

        [Fact]
        public void WhenEntityExposesCommand_ThenRepositoryPublishesIt()
        {
            var bus = new Mock<ICommandBus>();
            var commands = new List<ICommand>();

            bus.Setup(x => x.Send(It.IsAny<Envelope<ICommand>>()))
                .Callback<Envelope<ICommand>>(x => commands.Add(x.Body));

            var command = new TestCommand();

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), bus.Object, Mock.Of<ITextSerializer>()))
            {
                var aggregate = new OrmTestProcess(Guid.NewGuid());
                aggregate.AddCommand(command);
                context.Save(aggregate);
            }

            Assert.Equal(1, commands.Count);
            Assert.True(commands.Contains(command));
        }

        [Fact]
        public void WhenCommandPublishingThrows_ThenPublishesPendingCommandOnNextFind()
        {
            var bus = new Mock<ICommandBus>();
            var command1 = new Envelope<ICommand>(new TestCommand());
            var command2 = new Envelope<ICommand>(new TestCommand());
            var id = Guid.NewGuid();

            bus.Setup(x => x.Send(command2)).Throws<TimeoutException>();

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), bus.Object, new JsonTextSerializer()))
            {
                var aggregate = new OrmTestProcess(id);
                aggregate.AddEnvelope(command1, command2);

                Assert.Throws<TimeoutException>(() => context.Save(aggregate));
            }

            bus.Verify(x => x.Send(command1));
            bus.Verify(x => x.Send(command2));


            // Clear bus for next run.
            bus = new Mock<ICommandBus>();
            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), bus.Object, new JsonTextSerializer()))
            {
                var aggregate = context.Find(id);

                Assert.NotNull(aggregate);
                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command1.Body.Id)), Times.Never());
                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command2.Body.Id)));
            }
        }

        [Fact]
        public void WhenCommandPublishingThrowsOnFind_ThenThrows()
        {
            var bus = new Mock<ICommandBus>();
            var command1 = new Envelope<ICommand>(new TestCommand());
            var command2 = new Envelope<ICommand>(new TestCommand());
            var id = Guid.NewGuid();

            bus.Setup(x => x.Send(command2)).Throws<TimeoutException>();

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), bus.Object, new JsonTextSerializer()))
            {
                var aggregate = new OrmTestProcess(id);
                aggregate.AddEnvelope(command1, command2);

                Assert.Throws<TimeoutException>(() => context.Save(aggregate));
            }

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), bus.Object, new JsonTextSerializer()))
            {
                bus.Setup(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command2.Body.Id))).Throws<TimeoutException>();

                Assert.Throws<TimeoutException>(() => context.Find(id));
            }
        }

        [Fact]
        public void WhenCommandPublishingFails_ThenThrows()
        {
            var bus = new Mock<ICommandBus>();
            var command1 = new Envelope<ICommand>(new TestCommand());
            var command2 = new Envelope<ICommand>(new TestCommand());
            var command3 = new Envelope<ICommand>(new TestCommand());
            var id = Guid.NewGuid();

            bus.Setup(x => x.Send(command2)).Throws<TimeoutException>();

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), bus.Object, new JsonTextSerializer()))
            {
                var aggregate = new OrmTestProcess(id);
                aggregate.AddEnvelope(command1, command2, command3);

                Assert.Throws<TimeoutException>(() => context.Save(aggregate));
            }
        }

        [Fact]
        public void WhenCommandPublishingThrowsPartiallyOnFind_ThenPublishesPendingCommandOnNextFind()
        {
            var bus = new Mock<ICommandBus>();
            var command1 = new Envelope<ICommand>(new TestCommand());
            var command2 = new Envelope<ICommand>(new TestCommand());
            var command3 = new Envelope<ICommand>(new TestCommand());
            var id = Guid.NewGuid();

            bus.Setup(x => x.Send(command2)).Throws<TimeoutException>();

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), bus.Object, new JsonTextSerializer()))
            {
                var aggregate = new OrmTestProcess(id);
                aggregate.AddEnvelope(command1, command2, command3);

                Assert.Throws<TimeoutException>(() => context.Save(aggregate));
            }

            bus.Verify(x => x.Send(command1));
            bus.Verify(x => x.Send(command2));
            bus.Verify(x => x.Send(command3), Times.Never());


            // Setup bus for failure only on the third deserialized command now.
            // The command2 will pass now as it's a different deserialized instance.
            bus.Setup(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command3.Body.Id))).Throws<TimeoutException>();

            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), bus.Object, new JsonTextSerializer()))
            {
                Assert.Throws<TimeoutException>(() => context.Find(id));

                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command2.Body.Id)));
                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command3.Body.Id)));
            }

            // Clear bus now.
            bus = new Mock<ICommandBus>();
            using (var context = new SqlProcessDataContext<OrmTestProcess>(() => new TestProcessDbContext(dbName), bus.Object, new JsonTextSerializer()))
            {
                var aggregate = context.Find(id);

                Assert.NotNull(aggregate);

                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command2.Body.Id)), Times.Never());
                bus.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == command3.Body.Id)));
            }
        }

        public class TestProcessDbContext : DbContext
        {
            public TestProcessDbContext(string nameOrConnectionString)
                : base(nameOrConnectionString)
            {
            }

            public DbSet<OrmTestProcess> TestProcesses { get; set; }
            public DbSet<UndispatchedMessages> UndispatchedMessages { get; set; }
        }

        public class TestCommand : ICommand
        {
            public TestCommand()
            {
                this.Id = Guid.NewGuid();
            }
            public Guid Id { get; set; }
        }
    }

    public class OrmTestProcess : IProcess
    {
        private readonly List<Envelope<ICommand>> commands = new List<Envelope<ICommand>>();

        protected OrmTestProcess() { }

        public OrmTestProcess(Guid id)
        {
            this.Id = id;
        }

        public Guid Id { get; set; }

        public bool Completed { get; set; }

        public string Title { get; set; }

        public void AddCommand(ICommand command)
        {
            this.commands.Add(Envelope.Create(command));
        }

        public void AddEnvelope(params Envelope<ICommand>[] commands)
        {
            this.commands.AddRange(commands);
        }

        public IEnumerable<Envelope<ICommand>> Commands { get { return this.commands; } }
    }
}
