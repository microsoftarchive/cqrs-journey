### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Reference 4: A CQRS and ES Deep Dive (Chapter Title)

# Introduction

This chapter begins with a brief recap of some of the key points from 
the previous chapters before exploring in more detail the key concepts 
that relate to the CQRS pattern and Event Sourcing (ES). 

## Read-models and write-models

The CQRS pattern assigns the responsibility for modifying your 
application data and querying your application data to different sets of 
objects: a write-model and a read-model. The immediate benefit of this 
segregation is to clarify and simplify your code by applying the 
single-responsibilty principle: objects are responsible for either  
modifying data or querying data. 

However, the most important benefit of this segregation of 
responsibility for reading and writing to different sets of classes is 
that it is an enabler for making further changes to your application 
that will provide additional benefits. 

## Commands and Data Transfer Objects

A typical approach to enabling a user to edit data is to use data transfer objects (DTO): the 
UI retrieves the data to be edited from the application as a DTO, a user 
edits the DTO in the UI, the UI sends the modified DTO back to the 
application, and then the application applies those changes to the data
in the database. For an example of implementing a DTO, see "[Implementing Data Transfer Object in .NET with a DataSet][dtodataset]."

This approach is data-centric and tends to use standard CRUD operations 
throughout. In the UI, the user performs operations that are essentially
CRUD operations on the data in the DTO.

This is a simple, well understood aproach that works well for many 
applications. However, for some applications it is more useful if the UI 
sends commands instead of DTOs back to the application to make changes 
to the data. Commands are behavior-centric instead of data-centric, 
directly represent operations in the domain, maybe more intuitive to
users, and can capture the user's intent more effectively than DTOs.

In a typical CQRS implementation, the read-model returns data to the UI
as a DTO. The UI then sends a command (not a DTO) to the write-model.

## Domain-driven design (DDD) and aggregates

Using commands enables you build a UI that is more closely aligned with 
the behaviors associated with your domain. Related to this are the DDD 
concepts associated with a rich domain model, focusing on aggregates as 
way to model consistency boundaries based on domain concepts. 

One of the advatages of using commands and aggregates instead of DTOs is 
to simplify locking and concurrency management in your application. 

## Data and normalization

One of the changes that the CQRS pattern enables in your application is 
to segregrate your data as well as your objects. The write-model can use 
a database that is optimized for writes by being fully normalized. The 
read-model can use a database that is optimized for reads by being 
de-normalized to suit the specific queries that the application must 
support on the read-side. 

Several benefits flow from this: better performance because each 
database is optimized for a particlar set of operations, better 
scalability because you can scale-out each side independently, and 
simpler locking schemes. On the write side you no longer need to worry 
about how your locks impact on queries, and on the read-side your database 
can be read-only. 

## Events and Event Sourcing

If you use relational databases on both the read-side and write-side you 
will still be performing CRUD operations on the database tables on the 
write-side and you will need a mechanism to push the changes from your 
normalized tables on the write-side to your de-normalized tables on the 
read-side. 

If you capture changes in your write-model as events, your can save all 
of your changes simply by appending those events to your database or 
data store on the write-side using only **Insert** operations. 

You can also use those same events to push your changes to the 
read-side. You can use those events to build projections of the data
that contain the data structured to support the queries on the
read-side. 

## Eventual consistency

If you use a single database in your application, your locking scheme 
determines what version of a record is returned by a query. This can be 
very complex if a query joins records from multiple tables. 

> **MarkusPersona:** Think about the complexities of how transaction
> isolation levels (read uncommitted, read committed, repeatable reads,
> serializable) determine the locking behavior in a database and the
> differences between pessimistic and optimistic concurrency behavior.

Additionally, in a web application you have to consider that as soon as 
data is rendered in the UI it is potentially out of date because some 
other process or user could change it in the data store. 

If you segregate your data into a write-side store and a read-side 
store, you are now making it explicit in your architecture that when you 
query data it may be out of date, but that the data on the read-side 
will be *eventually consistent* with the data on the write-side. This 
helps you to simplify the design of the application and makes it easier 
to implement collaborative applications where multiple users may be 
trying to modify the same data simultaneously on the write-side. 

# Defining aggregates in the domain model  

In Domain-driven Design, an **Aggregate** defines a consistency 
boundary. Typically, when you implement the CQRS pattern, the classes 
in the write-model define your aggregates. Aggregates are the recipients 
of **Commands**, and are units of persistence. After an aggregate 
instance has processed a command and its state has changed, the system 
must persist the new state of the instance to storage. 

An aggregate may consist of multiple related objects, for example an 
order and multiple order lines all of which should be persisted 
together. However, if you have correctly identified your aggregate 
boundaries you should not need to use transactions to persist multiple 
aggregate instances together. 

If an aggregate consists of multiple types, you should identify one type 
as the **Aggregate Root**. You should access all of the objects within 
the aggregate through the aggregate root, and you should only hold 
references to the aggregate root. Every aggregate instance should have a 
unique identifier. 

## Aggregates and object-relational mapping layers

To persist your aggregates when you are using an object-relational mapping (ORM) layer such as Entity 
Framework to manage your persistence requires minimal code in your 
aggregate classes. 

The following code sample shows an **IAggregateRoot** interface and a 
set of classes that define an **Order** aggregate. This illustrates an 
approach to implementing aggregates that can be persisted using an ORM. 

```Cs
public interface IAggregateRoot
{
    Guid Id { get; }
}

public class Order : IAggregateRoot
{
    private List<SeatQuantity> seats;
	
	public Guid Id { get; private set; }
	
	public void UpdateSeats(IEnumerable<OrderItem> seats)
    {
        this.seats = ConvertItems(seats);
    }

	...
}

...

public struct SeatQuantity
{
	...
}
```

## Aggregates and Event Sourcing

If you are using event sourcing, then your aggregates must create events 
to record all of the state changes that result from processing commands. 

The following code sample shows an **IEventSourced** interface, an 
**EventSourced** abstract class, and a set of classes that define an 
**Order** aggregate. This illustrates an approach to implementing 
aggregates that can be persisted using event sourcing. 

```Cs
public interface IEventSourced
{
    Guid Id { get; }

    int Version { get; }

    IEnumerable<IVersionedEvent> Events { get; }
}

...

public abstract class EventSourced : IEventSourced
{
    private readonly Dictionary<Type, Action<IVersionedEvent>> handlers = new Dictionary<Type, Action<IVersionedEvent>>();
    private readonly List<IVersionedEvent> pendingEvents = new List<IVersionedEvent>();

    private readonly Guid id;
    private int version = -1;

    protected EventSourced(Guid id)
    {
        this.id = id;
    }

    public Guid Id
    {
        get { return this.id; }
    }

    public int Version { get { return this.version; } }

    public IEnumerable<IVersionedEvent> Events
    {
        get { return this.pendingEvents; }
    }

    protected void Handles<TEvent>(Action<TEvent> handler)
        where TEvent : IEvent
    {
        this.handlers.Add(typeof(TEvent), @event => handler((TEvent)@event));
    }

    protected void LoadFrom(IEnumerable<IVersionedEvent> pastEvents)
    {
        foreach (var e in pastEvents)
        {
            this.handlers[e.GetType()].Invoke(e);
            this.version = e.Version;
        }
    }

    protected void Update(VersionedEvent e)
    {
        e.SourceId = this.Id;
        e.Version = this.version + 1;
        this.handlers[e.GetType()].Invoke(e);
        this.version = e.Version;
        this.pendingEvents.Add(e);
    }
}

...

public class Order : EventSourced
{
    private List<SeatQuantity> seats;

    protected Order(Guid id) : base(id)
    {
        base.Handles<OrderUpdated>(this.OnOrderUpdated);
		...
    }

    public Order(Guid id, IEnumerable<IVersionedEvent> history) : this(id)
    {
        this.LoadFrom(history);
    }

    public void UpdateSeats(IEnumerable<OrderItem> seats)
    {
        this.Update(new OrderUpdated { Seats = ConvertItems(seats) });
    }
	
	private void OnOrderUpdated(OrderUpdated e)
	{
		this.seats = e.Seats.ToList();
	}
	
	...
}

...

public struct SeatQuantity
{
	...
}
```

In this example, the **UpdateSeats** method creates a new 
**OrderUpdated** event instead of updating the state of the aggregate 
directly. The **Update** method in the abstract base class is 
responsible for adding the event to the list of pending events to be 
appended to the event stream in the store, and for invoking the 
**OnOrderUpdated** event handler to update the state of the aggregate. 
Every event that is handled in this way also updates the version of the 
aggregate. 

The constructor in the aggregate class and the **LoadFrom** method in 
the abstract base class handle replaying the event stream to re-load the 
state of the aggregate. 

> **MarkusPersona:** We tried to avoid polluting the aggregate classes
> with infrastructure related code. These aggregate classes should
> implement the domain model and logic.

# Commands and CommandHandlers 

This section describes the role of commands and command handlers in a 
CQRS implementation and shows an outline of how they might be 
implemented in the C# language. 

## Commands

Commands are imperatives; they are requests for the system to 
perform a task or action. For example, "book two places on conference X" 
or "allocate speaker Y to room Z." Commands are usually processed just 
once, by a single recipient.

Both the sender and the receiver of a Command should be in the same 
bounded context. You should not send a Command to another bounded 
context because you would be instructing that other bounded context, 
which has separate responsibilities in another consistency boundary, to 
perform some work for you. However, a process manager may not belong to any particular bounded context in the system but it still sends commands. Some people also take the view that the UI is not a part of the bounded context, but the UI still sends commands.

> I think that in MOST circumstances (if not all), the command should 
> succeed (and that makes the async story WAY easier and practical). You 
> can validate against the read model before submitting a command, and 
> this way being almost certain that it will succeed.  
> Julian Dominguez (CQRS Advisors Mail List)

> When a user issues a Command, it'll give the best user experience if it 
> rarely fails. However, from an architectural/implementation point of 
> view, commands will fail once in a while, and the application should be 
> able to handle that.  
> Mark Seeman (CQRS Advisors Mail List)

### Example code

The following code sample shows a command and the **ICommand** interface 
that it implements. Notice that a command is a simple *Data Transfer 
Object* and that every instance of a Command has a unique Id. 

```Cs
using System;
	
public interface ICommand
{
	Guid Id { get; }
}

public class MakeSeatReservation : ICommand
{
	public MakeSeatReservation()
	{
		this.Id = Guid.NewGuid();
	}

	public Guid Id { get; set; }

	public Guid ConferenceId { get; set; }
	public Guid ReservationId { get; set; }
	public int NumberOfSeats { get; set; }
}
```

## CommandHandlers

Commands are sent to a specific recipient, typically an aggregate 
instance. The Command Handler performs the following tasks: 

1. It receives a Command instance from the messaging infrastructure.
2. It validates that the Command is a valid Command.
3. It locates the aggregate instance that is the target of the Command.
   This may involve creating a new aggregate instance or locating an
   existing instance.
4. It invokes the appropriate method on the aggregate instance passing
   in any parameters from the command.
5. It persists the new state of the aggregate to storage.

> I don't see the reason to retry the command here. When you see that 
> a command could not always be fulfilled due to race conditions, 
> go talk with your business expert and analyze what happens in this 
> case. How to handle compensation, offer an alternate solution, or deal 
> with overbooking. The only reason to retry I see is for technical 
> transient failures, like accessing the state storage.  
> J&eacute;r&eacute;mie Chassaing (CQRS Advisors Mail List)

Typically, you will organize your command handlers so that you have a 
class that contains all of the handlers for a specific aggregate type. 

You messaging infrastructure should ensure that it delivers just a 
single copy of a command to single command handler. Commands should be 
processed once, by a single recipient. 

The following code sample shows a command handler class that handles 
commands for **Order** instances. 

```Cs
public class OrderCommandHandler :
	ICommandHandler<RegisterToConference>,
	ICommandHandler<MarkSeatsAsReserved>,
	ICommandHandler<RejectOrder>,
	ICommandHandler<AssignRegistrantDetails>,
	ICommandHandler<ConfirmOrder>
{
	private readonly IEventSourcedRepository<Order> repository;

	public OrderCommandHandler(IEventSourcedRepository<Order> repository)
	{
		this.repository = repository;
	}

	public void Handle(RegisterToConference command)
	{
		var items = command.Seats.Select(t => new OrderItem(t.SeatType, t.Quantity)).ToList();
		var order = repository.Find(command.OrderId);
		if (order == null)
		{
			order = new Order(command.OrderId, command.ConferenceId, items);
		}
		else
		{
			order.UpdateSeats(items);
		}

		repository.Save(order, command.Id.ToString());
	}

	public void Handle(ConfirmOrder command)
	{
		var order = repository.Get(command.OrderId);
		order.Confirm();
		repository.Save(order, command.Id.ToString());
	}

	public void Handle(AssignRegistrantDetails command)
	{
		...
	}

	public void Handle(MarkSeatsAsReserved command)
	{
		...
	}

	public void Handle(RejectOrder command)
	{
		...
	}
}
```

This handler handles five different commands for the **Order** 
aggregate. The **RegisterToConference** command is an example of a 
command that creates a new aggregate instance. The **ConfirmOrder** 
command is an example of a command that locates an existing aggregate 
instance. Both examples use the **Save** method to persist the instance. 

If this bounded context uses an ORM, then the **Find** and **Save** 
methods in the repository class will locate and persist the aggregate 
instance in the underlying database. 

If this bounded context uses event sourcing, then the **Find** method 
will replay the aggregate's event stream to recreate the state, and the 
**Save** method will append the new events to the aggregate's event 
stream. 

> **Note:** If the aggregate generated any events when it processed the
> command, then these events are published when the repository saves the
> aggregate instance.

## Commands and optimistic concurrency

A common scenario for commands is that some of the information included 
in the command is provided by the user of the system through the UI, and 
some of the information is retrieved from the read-model. For example, 
the UI builds a list of orders by querying the read-model, the user 
selects one of those orders and modifies the list of attendees 
associated with that order. The UI then sends the command that contains 
the list of attendees associated with the order to the write-model for 
processing. 

However, because of eventual consistency, it is possible that the 
information that the UI retrieves from the read-side is not yet fully 
consistent with changes that have just been made on the write-side 
(perhaps by another user of the system). This raises the possibility 
that the command that is sent to update the list of Attendees results in 
an inconsistent change to the write-mode. For example, someone else 
could have deleted the order, or already modified the list of Attendees. 

A solution to this problem is to use version numbers in the read-model 
and the commands. Whenever the write-model sends details of a change to 
the read-model, it includes the current version number of the aggregate. 
When the UI queries the read-model it receives the version number and 
includes it in the command that it sends to the write-model. The 
write-model can compare the version number in the command with the 
current version number of the aggregate and if they are different it can 
raise a concurrency error and reject the change. 

# Events and EventHandlers 

Events can play two different roles in a CQRS implementation.

* **Event sourcing.** As described previously, event sourcing is an
  approach to persisting the state of aggregate instances by saving the
  stream of events in order to record changes in the state of the
  aggregate.
* **Communication and Integration.** You can also use events to
  communicate between aggregates or process managers in the same or in
  different bounded contexts. Events publish to subscribers information
  about something that has happened.

One event can play both roles: an aggregate may raise an event to record 
a state change and to notify an aggregate in another bounded context of 
the change. 

## Events and intent

As previously mentioned events in event sourcing should capture the 
business intent, in addition to the change in state of the aggregate. 
The concept of intent is hard to pin down, as shown in the following 
conversation: 

> *Developer 1*: One of the claims that I often hear for using event 
> sourcing is that it enables you to capture the user's intent, and that 
> this is valuable data. It may not be valuable right now, but if we 
> capture it, it may turn out to have business value at some point in 
> the future. 
> 
> *Developer 2*: Sure. For example, rather than saving a just a 
> customer's latest address, we might want to store a history of the 
> addresses the customer has had in the past. It may also be useful to 
> know why a customer's address was changed: they moved house or you 
> discovered a mistake with the existing address that you have on file. 
> 
> *Developer 1*: So in this example, the intent might help you to 
> understand why the customer hadn't responded to offers that you sent, 
> or might indicate that now might be a good time to contact the 
> customer about a particular product. But isn't the information about 
> intent, in the end, just data that you should store. If you do your 
> analysis right, you'd capture the fact that the reason an address 
> changes is an important piece of information to store? 
> 
> *Developer 2*: By storing events, we can automatically capture all 
> intent. If we miss something during our analysis, but we have the 
> event history, we can make use of that information later. If we 
> capture events we don't lose any potentially valuable data. 
> 
> *Developer 1*: But what if the event that you stored was just, "the 
> customer address was changed"? That doesn't tell me why the address 
> was changed. 
> 
> *Developer 2*: OK. You still need to make sure that you store useful 
> events that capture what is meaningful from the perspective of the 
> business. 
> 
> *Developer 1*: So what do events and event sourcing give me that I 
> can't get with a well designed relational database that captures 
> everything that I may need? 
> 
> *Developer 2*: It really simplifies things. The schema is simple. 
> With a relational database you have all the problems of versioning if 
> you need to start storing new or different data. With an event 
> sourcing, you just need to define a new event type. 
> 
> *Developer 1*: So what do events and event sourcing give me that I 
> can't get with a standard database transaction log? 
> 
> *Developer 2*: Using events as your primary data model makes it very 
> easy and natural to do time related analysis of data in your system, 
> for example: "what was the balance on the account at a particular 
> point in time?" or, "what would the customer's status be if we'd 
> introduced the reward program six months earlier?" The transactional 
> data is not hidden away and inaccessible on a tape somewhere, it's 
> there in your system. 
> 
> *Developer 1*: So back to this idea of intent. Is it something 
> special that you can capture using events, or is it just some 
> additional data that you save? 
> 
> *Developer 2*: I guess in the end, the intent is really there in the 
> commands that originate from the users of the system. The events 
> record the consequences of those commands. If those events record the 
> consequences in business terms then it makes it easier for you to 
> infer the original intent of user. 

> Thanks to Clemens Vasters and Adam Dymitruk

### How to model intent

This section examines two alternatives for modeling intent with 
reference to SOAP and REST style interfaces to help highlight the 
differences. 

> **Note:** We are using SOAP and REST here as an analogy to help 
explain the differences between the approaches. 

The following two code samples illustrate two, slightly different 
approaches to modeling intent alongside the event data: 

**Example 1. The Event log or SOAP-style approach.**
```
[ 
  { "reserved" : { "seatType" : "FullConference", "quantity" : "5" }},
  { "reserved" : { "seatType" : "WorkshopA", "quantity" : "3" }},
  { "purchased" : { "seatType" : "FullConference", "quantity" : "5" }},
  { "expired" : { "seatType" : "WorkshopA", "quantity" : "3" }},
]
```

**Example 2. The Transaction log or REST-style approach.**

```
[ 
  { "insert" : { "resource" : "reservations", "seatType" : "FullConference", "quantity" : "5" }},
  { "insert" : { "resource" : "reservations", "seatType" : "WorkshopA", "quantity" : "3" }},
  { "insert" : { "resource" : "orders", "seatType" : "FullConference", "quantity" : "5" }},
  { "delete" : { "resource" : "reservations", "seatType" : "WorkshopA", "quantity" : "3" }},
]
```

The first approach uses an action-based contract that couples the events 
to a particular aggregate type. The second approach uses a uniform 
contract, that uses a **resource** field as a hint to associate the 
event with an aggregate type. 

> **Note:** How the events are actually stored is a separate issue. This 
discussion is focusing on how to model your events. 

The advantages of the first approach are:

* Strong typing.
* More expressive code.
* Better testability.

The advantages of the second approach are:

* Simplicity and a generic approach.
* Makes it easier to use existing internet infrastructure.
* Easier to use with dynamic languages and with changing schemas.

> **MarkusPersona:** Variable environment state needs to be stored 
alongside events in order to have an accurate representation of the 
circumstances at the time when the command resulting in the event 
was executed, which means that we need to save everything! 

## Events

Events report that something has happened. An aggregate or process manager publishes one-way, asynchronous messages that are published to multiple recipients. For example: **SeatsUpdated**, **PaymentCompleted**, and **EmailSent**.

### Sample Code

The following code sample shows a possible implementation of an event that is used to communicate between aggregates or process managers. It implements the **IEvent** interface.

```Cs
public interface IEvent
{
    Guid SourceId { get; }
}

...

public class SeatsAdded : IEvent
{
    public Guid ConferenceId { get; set; }

    public Guid SourceId { get; set; }

    public int TotalQuantity { get; set; }

    public int AddedQuantity { get; set; }
}
```

> **Note:** For simplicity, in C# these classes are implemented as DTOs, but they should be treated as being immutable.

The following code sample shows a possible implementation of an event that is used in an event sourcing implementation. It extends the **VersionedEvent** abstract class.

```Cs
public abstract class VersionedEvent : IVersionedEvent
{
    public Guid SourceId { get; set; }

    public int Version { get; set; }
}

...

public class AvailableSeatsChanged : VersionedEvent
{
    public IEnumerable<SeatQuantity> Seats { get; set; }
}
```

The **Version** property refers to the version of the aggregate. The version is incremented whenever the aggregate receives a new event.

## EventHandlers

Events are published to multiple recipients, typically an aggregate 
instances or process managers. The Event Handler performs the following
tasks: 

1. It receives a Event instance from the messaging infrastructure.
2. It locates the aggregate or process manager instance that is the
   target of the Event. This may involve creating a new aggregate
   instance or locating an existing instance.
3. It invokes the appropriate method on the aggregate or process manager
   instance passing in any parameters from the event.
4. It persists the new state of the aggregate or process manager to storage.

### Sample code

```Cs
public void Handle(SeatsAdded @event)
{
    var availability = this.repository.Find(@event.ConferenceId);
    if (availability == null)
        availability = new SeatsAvailability(@event.ConferenceId);

    availability.AddSeats(@event.SourceId, @event.AddedQuantity);
    this.repository.Save(availability);
}
```

If this bounded context uses an ORM, then the **Find** and **Save** 
methods in the repository class will locate and persist the aggregate 
instance in the underlying database. 

If this bounded context uses event sourcing, then the **Find** method 
will replay the aggregate's event stream to recreate the state, and the 
**Save** method will append the new events to the aggregate's event 
stream.

# Embracing eventual consistency 

Maintaining the consistency of business data is a key requirement in all 
enterprise systems. One of the first things that many developers learn 
in relation to database systems is the ACID properties of transactions: 
transactions must ensure that the stored data is consistent as well 
transactions being atomic, isolated, and durable. Developers also become 
familiar with complex concepts such as pessimistic and optimistic 
concurrency, and their performance characteristics in particular 
scenarios. They may also need to understand the different isolation 
levels of transactions: serializable, repeatable reads, read committed, 
and read uncommitted. 

In a distributed computer system, there are some additional factors that 
are relevant to consistency. The CAP theorem states that it is 
impossible for a distributed computer system to provide the following 
three guarantees simultaneously: 

1. Consistency (C). A guarantee that all the nodes in the system see the 
   same data at the same time. 
2. Availability (A). A guarantee that the system can continue to operate
   even if a node is unavailable. 
3. Partition tolerance (P). A guarantee that the system continues to operate
   despite the nodes being unable to communicate.
   
> **GaryPersona:** Cloud providers have broadened the interpretation of
> the CAP theorem in the sense that they consider a system to be
> unavailable if the response time exceeds the latency limit.

> "In larger distributed-scale systems, network partitions are a given;
> therefore, consistency and availability cannot be achieved at the same
> time."  
> Werner Vogels, CTO, Amazon - Vogels, E. Eventually Consistent, Communications of ACM, 52(1): 40-44, Jan 2009.

For more information about the CAP theorem, see [CAP 
theorem][captheorem] on Wikipedia and the article [CAP Twelve Years
Later: How the "Rules" Have Changed][capinfoq] by Eric Brewer on the
InfoQ website.

The concept of *eventual consistency* offers a way to make it appear 
from the outside that we are meeting these three guarantees. In the CAP 
theorem, the consistency guarantee specifies that all the nodes should 
see the same data *at the same time*; instead, with *eventual 
consistency* we state that all the nodes will eventually see the same 
data. It's important that changes are propagated to other nodes in the 
system at a faster rate than new changes arrive in order to avoid the 
differences between the nodes continuing to increase. Another way of 
viewing this is to say that we will accept that, at any given time, some 
of the data seen by users of the system could be stale. For many 
business scenarios, this turns out to be perfectly acceptable: a 
business user will accept that the information they are seeing on a 
screen may be a few seconds, or even minutes out of date. Depending on 
the details of the scenario, the business user can refresh the display a 
bit later on to see what has changed, or simply accept that what they 
see is always slightly out of date. There are some scenarios where this 
delay is unacceptable, but they tend to be the exception rather than the 
rule. 

> Very often people attempting to introduce eventual consistency into a 
> system run into problems from the business side. A very large part of 
> the reason of this is that they use the word consistent or consistency 
> when talking with domain experts / business stakeholders.  
> ...  
> Business users hear 'Consistency' and they tend to think it means that 
> the data will be wrong. That the data will be incoherent and 
> contradictory. This is not actually the case. Instead try using the 
> word 'stale' or 'old', in discussions when the word stale is used the 
> business people tend to realize that it just means that someone could 
> have changed the data, that they may not have the latest copy of it.  
> Greg Young: [Quick Thoughts on Eventual Consistency][youngeventual] 

> **PoePersona:** Domain Name Servers (DNS) use the eventual consistency
> model to refresh themselves, and that's why DNS propagation delay can
> occur that results in some, but not other, users being able to
> navigate to a new or updated domain name. The propagation delay is
> acceptable considering that a coordinated atomic update across all DNS
> servers globally would not be feasible. Eventually, however, all DNS
> servers get updated and domain names get resolved properly.

> **Note:** To better understand the tradeoffs described by the CAP
> theorem, check out the special issue of IEEE Computer magazine
> dedicated to it (Vol.45(no.2), Feb 2012).

# Eventual consistency and CQRS 

How does the concept of eventual consistency relate to the CQRS pattern? 
A typical implementation of the CQRS pattern is a distributed system 
made up of one node for the write-side, and one or more nodes for the 
read-side. Your implementation must provide some mechanism for 
synchronizing data between these two sides. This is not a complex 
synchronization task because all of the changes take place on the 
write-side, so the synchronization process only needs to push changes 
from the write-side to the read-side. 

If you decide that the two sides must always be consistent (the case of 
strong consistency), then you will need to introduce a distributed 
transaction that spans both sides as shown in figure 1. 

![Figure 1][fig1]

**Using a distributed transaction to maintain consistency**

The problems that may result from this approach relate to performance 
and availability. Firstly, both sides will need to hold locks until both 
sides are ready to commit, in other words the transaction can only 
complete as fast as the slowest participant can. 

This transaction may include more than two participants. If we are 
scaling the read-side by adding multiple instances, the transaction must 
span all of those instances. 

Secondly, if one node fails for any reason or does not complete the 
transaction, the transaction cannot complete. In terms of the CAP 
theorem, by guaranteeing consistency, we cannot guarantee the 
availability of the system. 

If you decide to relax your consistency constraint and specify that your 
read-side only needs to be eventually consistent with the write-side, 
you can change the scope of your transaction. Figure 2 shows how you can 
make the read-side eventually consistent with the write-side by using a 
reliable messaging transport to propagate the changes. 

![Figure 2][fig2]

**Using a reliable message transport**

In this example, you can see that there is still a transaction. The 
scope of this transaction includes saving the changes to the data store 
on the write-side, and placing a copy of the change onto the queue that 
pushes the change to the read-side. 

> **GaryPersona:** This eventual consistency might not be able to
> guarantee the same order of updates in the read side as in the write
> side.

This solution does not suffer from the potential performance problems 
that you saw in the original solution if you assume that the messaging 
infrastructure allows you to quickly add messages to a queue. This 
solution is also no longer dependent on all of the read-side nodes being 
constantly available because the queue acts as a buffer for the messages 
addressed to the read-side nodes. 

> **Note:** In practice, the messaging infrastructure is likely to use a 
  publish/subscribe topology rather than a queue to enable multiple 
  read-side nodes to receive the messages. 

This third example in figure z shows a way that you can do away with the 
need for a distributed transaction. 

![Figure 3][fig3]

**No distributed transactions**

This example depends on functionality in the write-side data store: it 
must be able to send a message in response to every update that the 
write-side model makes to the data. This approach lends itself 
particularly well to the scenario where you combine CQRS with event 
sourcing. If the event store can send a copy of every event that it 
saves onto a message queue, then you can make the read-side eventually 
consistent by using this infrastructure feature. 

# Optimizing the read-side 

There are four goals to keep in mind when optimizing the read-side.
You typically want:

* Very fast responses to queries for data.
* To minimize resource utilization.
* To minimize latency.
* To minimize costs.

By separating the read-side from the write-side, the CQRS patern enables 
you to design the read-side so that the data store is optimized for 
reading. You can denormalize your relational tables or choose to store 
the data in some other format that best suits the part of the 
application that will use the data. Ideally, the recipient of the data 
should not need to perform any joins or other complex, 
resource-intensive operations on the data. 

For a discussion of how to discourage any unnecessary operations on the 
data, see the section, "Querying the Read-side" in [Extending and 
Enhancing the Orders and Registrations Bounded Contexts][j_chapter4] in 
the Journey Guide. 

If your system needs to accommodate high volumes of read operations, you 
can scale out the read-side. For example, in Windows Azure by adding 
additional role instances. You can also easily scale out your data store 
on the read-side because it is read-only. You should also consider the 
benefits of caching data on the read-side to further speed up response 
times and reduce processing resource utilization.

For a description of how the team designed the reference implementation 
for scalability, see Chapter 7, "[Adding Resilience and Optimizing 
Performance][j_chapter7]," in the Journey Guide. 

In the section "Embracing Eventual Consistency" earlier in this chapter, 
you saw how when you implement the CQRS pattern that you must accept 
some latency between an update on the write-side and that change 
becoming visible on the read-side. However, you will want to keep that 
delay to a minimum. You can minimize the delay by ensuring that the 
infrastructure that transports update information to the read-side has 
enough resources, and by ensuring that the updates to your read-models 
happen efficiently. 

You should also consider the comparative storage costs for different 
storage models on the read-side such as SQL Database, Windows Azure table 
storage, and Windows Azure blob storage. This may involve a trade-off 
between performance and costs. 

# Optimizing the write-side 

A key goal in optimizing the write-side is to maximize the throughput of 
commands and events. Typically, the write-side performs work when it 
receives commands from the UI or receives integration events from other 
bounded contexts. You need to ensure that your messaging infrastructure 
delivers command and event messages with minimal delay, that the 
processing in the domain-model is efficient, and that interactions with 
the data store are fast. 

Options for optimizing the way that messages are delivered to the
write-side include:

* Delivering commands in-line without using the messaging
  infrastructure. If you can host the domain-model in the same process
  as the command sender, you can avoid using the messaging
  infrastructure. You need to consider the impact this may have on the
  resilience of your system to failures in this process.
* Handling some commands in parallel. You need to consider whether this
  will affect the way that your system manages concurrency.

If you are using event sourcing, you may be able to reduce the time it 
takes to load the state of aggregate by using snapshots. Instead of 
replaying the complete event stream when you load an aggregate, you load 
the most recent snapshot of its state and then only play back the events 
that occurred after the snapshot was taken. You will need to introduce a 
mechanism that creates snapshots for aggregates on a regular basis. 
However, given the simplicity of a typical event store schema, loading 
the state of an aggregate is typically very fast. Using snapshots 
typically only provides a performance benefit when an aggregate has a 
very large number of events. 

Instead of snapshots, you may be able to optimize the access to an 
aggregate with a large number of events by caching it in memory. You 
only need to load the full event stream when it is accessed for the 
first time after a system start. 

## Concurrency and aggregates

A simple implementation of aggregates and command handlers will load an 
aggregate instance into memory for each command that the aggregate must 
process. For aggregates that must process a large number of commands, 
you may decide to cache the aggregate instance in memory to avoid the 
need to reload it for every command. 

If your system only has a single instance of an aggregate loaded into 
memory, that aggregate may need to process commands that are sent from 
multiple clients. By arranging for the system to deliver commands to the 
aggregate instance through a queue, you can ensure that the aggregate 
processes the commands sequentially. Also, there is no requirement to 
make the aggregate thread-safe, because it will only process a single 
command at a time. 

In scenarios with an even higher throughput of commands, you may need to 
have multiple instances of the aggregate loaded into memory, possibly in 
different processes. To handle the concurrency issues here, you can use 
event sourcing and versioning. Each aggregate instance must have a 
version number that is updated whenever the instance persists an event. 

There are two ways to make use of the version number in the aggregate 
instance: 

* **Optimistic:** Append the event to the event-stream if the the latest 
event in the event-stream is the same version as the current, in-memory, 
instance. 
* **Pessimistic:** Load all the events from the event stream that have a 
version number greater than the version of the current, in-memory, 
instance. 

> These are technical performance optimizations that can be implemented 
> on case-by-case basis.  
> Rinat Abdullin (CQRS Advisors Mail List)

# Messaging and CQRS

CQRS and Event Sourcing use two types of messages: Commands and Events. 
Typically, systems that implement the CQRS pattern are large-scale, 
distributed systems and therefore you need a reliable, distributed 
messaging infrastructure to transport the messages between your 
senders/publishers and receivers/subscribers. 

For commands that have a single recipient you will typically use a 
queue topology. For events, that may have multiple recipients you will 
typically use a pub/sub topology. 

The reference implementation that accompanies this guide uses the 
Windows Azure Service Bus for messaging. [Technologies Used in the 
Reference Implementation][r_chapter9] provides additional information 
about the Windows Azure Service Bus. Windows Azure Service Bus brokered
messaging offers a distributed messaging infrastructure in the cloud
that supports both queue and pub/sub topologies.

## Messaging considerations 

Whenever you use messaging, there are a number of issues to consider. 
This section describes some of the most significant issues when you are
working with commands and events in a CQRS implementation. 

### Duplicate messages

An error in the messaging infrastructure or in the message receiving 
code may cause a message to be delivered multiple times to its 
recipient. 

There are two potential approaches to handling this scenario.

1. Design your messages to be idempotent so that duplicate messages have
   no impact on the consistency of your data.
2. Implement duplicate message detection. Some messaging infrastructures
   provide a configurable duplicate detection strategy that you can use
   instead of implementing it yourself.

> **JanaPersona:** Some messaging infrastructures offer a guarantee of
> at least once delivery. This implies that you should explicitly handle
> the duplicate message delivery scenario in your application code.

For a detailed discussion idempotency in reliable systems see the 
article [Idempotence Is Not a Medical Condition][idempotency] by Pat 
Helland. 

### Lost messages

An error in the messaging infrastructure may cause a message not to be 
delivered to its recipient. 

Many messaging infrastructures offer guarantees that messages are not 
lost and are delivered at least once to their recipient. Alternative 
strategies that you could implement to detect when messages have been 
lost include a handshake process to acknowledge receipt of a message to 
the sender, or assigning sequence numbers to messages so that the 
recipient can determine if it has not received a message. 

### Out of order messages

The messaging infrastructure may deliver messages to a recipient in a 
different order to the order that the sender sent the messages. 

In some scenarios, the order that messages are recieved is not 
significant. If message ordering is important, some messaging 
infrastructures can guarantee ordering. Otherwise, you can detect out of 
order messages by assigning sequence numbers to messages as they are 
sent. You could also implement a process manager process in the reciever that 
can hold out of order messages until it can re-assemble messages into 
the correct order. 

If messages need to be ordered within a group, you may be able to send 
the related messages as a single batch. 

### Unprocessed messages

A client may retrieve a message from a queue and then fail while it is 
processing the message. When the client restarts the message has been 
lost. 

Some messaging infrastructures allow to include the read of the message 
from the infrastructure as part of a distributed transaction that you 
can roll back if the message processing fails. 

Another approach, offered by some messaging infrastructures, is to make 
reading a message a two-phase operation. First you lock and read the 
message, then when you have finished processing the message you mark it 
as complete and it is removed from the queue or topic. If the message 
does not get marked as complete, the lock on the message times out and 
it becomes available to read again. 

> **PoePersona:** If a message still cannot be processed after a number
> of retries, it is typically sent to a dead-letter queue for further
> investigation.

## Event versioning

As your system evolves, you may find that you need to make changes to 
the events that you system uses. For example: 

* Some events may become redundant in that they are no longer raised by
  any class in your system.
* You may need to define new events that relate to new features or
  functionality within in your system.
* You may need to modify existing event definitions.

The following sections discuss each of these scenarios in turn.

### Redundant events

If your system no longer uses a particular event type, you may be able 
to simply remove it from the system. However, if you are using event 
sourcing, your event store may hold many instances of this event, and 
these instances may be used to rebuild the state of your aggregates. 
Typically, you treat the events in your event store as immutable. In 
this case, your aggregates must continue to be able to handle these old 
events when they are replayed from the event store even though the 
system will no longer raise new instances of this event type. 

### New event types

If you introduce new event types into your system, this should have no 
impact on existing behavior. Typically, it is only new features or 
functionality that use the new event types. 

### Changing existing event definitions

Handling changes to event type defintions requires more complex changes 
to your system. For example, your event store may hold many instances of 
an old version of an event type while the system raises events that are 
a later version, or different bounded contexts may raise different 
versions of the same event. Your system must be capable of handling 
multiple versions of the same event. 

An event defintion can change in a number of different ways, for example:

* An event gains a new property in the latest version.
* An event loses a property in the latest version.
* A property changes its type or supports a different range of values.

> **Note:** If the semantic meaning of an event changes, then you should
> treat that as new event type, and not as a new version of an existing
> event.

Where you have multiple versions of an event type, you have two basic 
choices of how to handle the multiple versions: you can either continue 
to support multiple versions of the event in your domain classes, or use 
a mechansim to convert old versions of events to the latest version 
whenever they are encountered by the system. 

The first option may be the quickest and simplest approach to adopt 
because it typically doesn't require any changes to your infrastructure. 
However, this approach will eventually polute your domain classes as 
they end up supporting more and more versions of your events, but if you 
don't anticipate many changes to your event definitions this may be 
acceptable. 

The second approach is a cleaner solution: your domain classes only need 
to support the latest version of each event type. However you do need to 
make changes to your infrastructure to translate the old event types to 
the latest type. The issue here is to decide whereabouts in your 
infrastructure to perform this translation. 

One option is to add filtering functionality into your messaging 
infrastructure so that events are translated as they are delivered to 
their recipients; you could also add the translation functionality into 
your event handler classes. If you are using event sourcing, you must 
also ensure that old versions of events are translated as they are read 
from the event store when you are re-hydrating your aggragtes. 

Whatever solution you adopt, it must perform the same translation 
wherever the old version of the event originates from: another bounded 
context, an event store, or even from the same bounded context if you 
are in the middle of a system upgrade. 

Your choice of serialization format may make it easier to handle 
different versions of events: for example, JSON deserialization can 
simply ignore deleted properties, or the class that the object is 
deserialized to can provide a meaningful default value for any new 
property. 

# Task-based UIs

In figure 3 above, you can see that in a typical implementation of the 
CQRS pattern how the UI queries the read-side and receives a DTO, and 
sends commands to the write-side. This section describes some of the 
impact this has on the design of your UI. 

In a typical three-tier architecture or simple CRUD system, the UI also 
receives data in the form of DTOs from the service tier. The user then 
manipulates the DTO through the UI. The UI then sends the modified DTO 
back to the service tier. The service tier is then responsible for 
persisting the changes to the data store. This can be a simple, 
mechanical process of identifying the CRUD operations that the UI 
performed on the DTO and applying equivalent CRUD operations to the data 
store. There are several things to notice about this typical architecture: 

* It uses CRUD operations throughout.
* If you have a domain model you must translate the CRUD operations from
  the UI into something that the domain understands.
* It can lead to complexity in the UI if you want to provide a more
  natural and intuitive UI that uses domain concepts instead of CRUD
  concepts.
* It does not necessarily capture the user's intent.
* It is simple and well-understood.

The following list identifies the changes that occur in your 
architecture if you implement the CQRS pattern and send commands from 
the UI to the write-side: 

* It does not use CRUD style operations.
* The domain can act directly in response to the commands from the UI.
* You can design the UI to construct the commands directly, making it
  easier to build a natural and intuitive UI that uses concepts from the
  domain.
* It is easier to capture the user's intent in a command.
* It is more complex and assumes that you have a domain model in the
  write-side.
* The behavior is typically in one place: the write model.

A task-based UI is a natural, intuitive UI based on domain concepts that 
the users of the system already understand. It does not impose the CRUD 
operations on the UI or the user. If you implement the CQRS pattern, 
your task-based UI can create commands to send to the domain model on 
the write-side. The commands should map very closely onto the mental 
model that your users have of the domain, and should not require any 
translation before the domain-model receieves and processes them. 

> "Every Human-Computer Interaction (HCI) professional I have worked
> with has been in favor of task-based UIs. Every user that I have met
> that has used both styles of UI, task based and 'grid' based, has
> reported that they were more productive when using the task based UI
> for 'interactive work'. Data entry is not interactive work."  
> Udi Dahan - [Tasks, Messages, & Transactions][uditmt]

In many applications, especially where the domain is relatively simple, 
the costs of implementing the CQRS pattern and adding a task-based UI 
will outweigh any benefits. Task-based UIs are particularly useful in 
complex domains. 

There is no requirement to use a task-based UI when you implement the 
CQRS pattern. In some scenarios a simple CRUD-style UI is all that's 
needed.

> "The concept of a task based UI is more often than not assumed to be
> part of CQRS, it is not, it is there so the domain can have verbs but
> also capturing the intent of the user is important in general."  
> Greg Young - [CQRS, Task Based UIs, Event Sourcing agh!][gregaah]

# Taking advantage of Windows Azure

In the chapter "[Introducing the Command Query Responsibility 
Segregation Pattern][r_chapter2]," we suggested that the motivations for 
hosting an application in the cloud were similar to the motivations for 
implementing the CQRS pattern: scalability, elasticity, and agility. 
This section describes in more detail how a CQRS implementation might 
use some of specific features of the Windows Azure platform to provide 
some of the infrastructure that you typically need when you implement 
the CQRS pattern.

## Scaling out using multiple role instances 

When you deploy an application to Windows Azure, you deploy the 
application to roles in your Windows Azure environment; a Windows Azure 
application typically consists of multiple roles. Each role has 
different code and performs a different function within the application. 
In CQRS terms, you might have one role for the implementation of the 
write-side model, one role for the implementation of the read-side 
model, and another role for the UI elements of the application. 

After you deploy the roles that make up your application to Windows 
Azure, you can specify (and change dynamically) the number of running 
instances of each role. By adjusting the number of running instances of 
each role, you can elastically scale your application in response to 
changes in levels of activity. One of the motivations for using the CQRS 
pattern is the ability to scale the read-side and the write-side 
independently given their typically different usage patterns. For 
information about how to automatically scale roles in Windows Azure, see 
"[The Autoscaling Application Block][aab]" on MSDN. 

## Implementing an event store using Windows Azure table storage 

This section shows an event store implementation using Windows Azure 
table storage. It is not intended to show production quality code, but 
to suggest an approach. An event store should: 

* Persist events to a reliable storage medium.
* Enable an individual aggregate to retrieve its stream of events in the
  order that they were originally persisted.
* Guarantee to publish each event "at least once" to a message
  infrastructure. 

Windows Azure tables have two fields that together define the uniqueness 
of a record: the partition key and the row key. 

This implementation uses the value of the aggregate's unique identifier 
as the partition key, and the event version number as the row key. 
Partition keys enable you to retrieve all of the records with the same 
partition key very quickly, and use transactions across rows that share 
the same partition key. 

For more information about Windows Azure table storage see [Data Storage 
Offerings in Windows Azure][azurestorage]. 

### Persisting events

The following code sample shows how the implementation persists an event 
to Windows Azure table storage. 

```Cs
public void Save(string partitionKey, IEnumerable<EventData> events)
{
    var context = this.tableClient.GetDataServiceContext();
    foreach (var eventData in events)
    {
        var formattedVersion = eventData.Version.ToString("D10");
        context.AddObject(
            this.tableName,
            new EventTableServiceEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = formattedVersion,
                    SourceId = eventData.SourceId,
                    SourceType = eventData.SourceType,
                    EventType = eventData.EventType,
                    Payload = eventData.Payload
                });

        ...

    }

    try
    {
        this.eventStoreRetryPolicy.ExecuteAction(() => context.SaveChanges(SaveChangesOptions.Batch));
    }
    catch (DataServiceRequestException ex)
    {
        var inner = ex.InnerException as DataServiceClientException;
        if (inner != null && inner.StatusCode == (int)HttpStatusCode.Conflict)
        {
            throw new ConcurrencyException();
        }

        throw;
    }
}
```

There are two things to note about this code sample:

1. An attempt to save a duplicate event (same aggregate id and same
   event version) results in a concurrency exception.
2. This example uses a retry policy to handle transient faults and to
   improve the reliability of the save operation. See [The Transient
   Fault Handling Application Block][topaz].
   
> **JanaPersona:** The [Transient Fault Handling Application
> Block][topaz] provides extensible retry functionality over and above
> that included in the **Microsoft.WindowsAzure.StorageClient** namespace.
> The block also includes retry policies for Windows Azure SQL Database,
> and Windows Azure Service Bus.


### Retrieving events

The following code sample shows how to retrieve the list of events 
associated with an aggregate. 

```Cs
public IEnumerable<EventData> Load(string partitionKey, int version)
{
    var minRowKey = version.ToString("D10");
    var query = this.GetEntitiesQuery(partitionKey, minRowKey, RowKeyVersionUpperLimit);
    var all = this.eventStoreRetryPolicy.ExecuteAction(() => query.Execute());
    return all.Select(x => new EventData
                                {
                                    Version = int.Parse(x.RowKey),
                                    SourceId = x.SourceId,
                                    SourceType = x.SourceType,
                                    EventType = x.EventType,
                                    Payload = x.Payload
                                });
}
```

The events are returned in the correct order because the version number 
is used as the row key. 

### Publishing events

To guarantee that every event is published as well as persisted, you can 
use the transactional behaviour of Windows Azure table partitions. When 
you save an event, you also add a copy of the event to a virtual queue 
on the same partition as part of a transaction. The following code 
sample shows a complete version of the save method that saves two copies 
of the event. 

```Cs
public void Save(string partitionKey, IEnumerable<EventData> events)
{
    var context = this.tableClient.GetDataServiceContext();
    foreach (var eventData in events)
    {
        var formattedVersion = eventData.Version.ToString("D10");
        context.AddObject(
            this.tableName,
            new EventTableServiceEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = formattedVersion,
                    SourceId = eventData.SourceId,
                    SourceType = eventData.SourceType,
                    EventType = eventData.EventType,
                    Payload = eventData.Payload
                });

        // Add a duplicate of this event to the Unpublished "queue"
        context.AddObject(
            this.tableName,
            new EventTableServiceEntity
            {
                PartitionKey = partitionKey,
                RowKey = UnpublishedRowKeyPrefix + formattedVersion,
                SourceId = eventData.SourceId,
                SourceType = eventData.SourceType,
                EventType = eventData.EventType,
                Payload = eventData.Payload
            });

    }

    try
    {
        this.eventStoreRetryPolicy.ExecuteAction(() => context.SaveChanges(SaveChangesOptions.Batch));
    }
    catch (DataServiceRequestException ex)
    {
        var inner = ex.InnerException as DataServiceClientException;
        if (inner != null && inner.StatusCode == (int)HttpStatusCode.Conflict)
        {
            throw new ConcurrencyException();
        }

        throw;
    }
}
```

You can use a task to process the unpublished events: read the 
unpublished event from the virtual queue, publish the event on the 
messaging infrastructure, and delete the copy of the event from the 
unpublished queue. The following code sample shows a possible 
implementation of this behavior. 

```Cs
private readonly BlockingCollection<string> enqueuedKeys;

public void SendAsync(string partitionKey)
{
    this.enqueuedKeys.Add(partitionKey);
}

public void Start(CancellationToken cancellationToken)
{
    Task.Factory.StartNew(
        () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        this.ProcessNewPartition(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            },
        TaskCreationOptions.LongRunning);
}

private void ProcessNewPartition(CancellationToken cancellationToken)
{
    string key = this.enqueuedKeys.Take(cancellationToken);
    if (key != null)
    {
        try
        {
            var pending = this.queue.GetPending(key).AsCachedAnyEnumerable();
            if (pending.Any())
            {
                foreach (var record in pending)
                {
                    var item = record;
                    this.sender.Send(() => BuildMessage(item));
                    this.queue.DeletePending(item.PartitionKey, item.RowKey);
                }
            }
        }
        catch
        {
            this.enqueuedKeys.Add(key);
            throw;
        }
    }
}
```

There are three points to note about this sample implementation:

1. It is not optimized.
2. Potentially it could fail between publishing a message and deleting
   it from the unpublished queue. You could use duplicate message
   detection in your messaging infrastructure when the message is resent
   after a restart.
3. After a restart, you need code to scan all your partitions for
   unpublished events.

## Implementing a messaging infrastructure using the Windows Azure Service Bus 

The Windows Azure Service Bus offers a robust, cloud-based messaging 
infrastructure that you can use to transport your command and event 
messages when you implement the CQRS pattern. It's brokered messaging 
feature enables you to use either a point-to-point topology using 
queues, or a publish/subscribe topology using topics. 

You can design your application to use the Windows Azure Service Bus to 
guarantee at-least-once delivery of messages, and guarantee message 
ordering by using message sessions. 

The sample application described in the Journey Guidance uses the 
Windows Azure Service Bus for delivering both commands and events. The 
following chapters in the Journey Guidance contain further information. 

* [Orders and Registrations Bounded Context][j_chapter3]
* [Versioning our System ][j_chapter6]
* [Adding Resilience, New Bounded Contexts, and Features][j_chapter7]

You can find references to additional resources in [Technologies Used in 
the Reference Implementation][r_chapter9]. 

## A word of warning

> Often times when writing software that will be cloud deployed you need
> to take on a whole slew of non-functional requirements that you don't
> really have...  
> Greg Young (CQRS Advisors Mail List)

For example, a process manager (described in chapter 6 [A Saga on 
Sagas][r_chapter6]) may process a maximum of two messages per second 
during its busiest periods. Because a process manager must maintain 
consistency when it persists its state and sends messages, it requires 
transactional behavior. In Windows Azure, adding this kind of 
transactional behavior is non-trivial, and you may find yourself writing 
code to support this behavior: using at-least-once messaging and 
ensuring that all of the message recipients are idempotent. This is 
likely to more complex to implement than a simple distributed 
transaction. 

[r_chapter1]:     Reference_01_CQRSContext.markdown
[r_chapter2]:     Reference_02_CQRSIntroduction.markdown
[r_chapter4]:     Reference_04_DeepDive.markdown
[r_chapter6]:     Reference_06_Sagas.markdown
[r_chapter9]:     Reference_09_Technologies.markdown
[j_chapter3]:     Journey_03_OrdersBC.markdown
[j_chapter4]:     Journey_04_ExtendingEnhancing.markdown
[j_chapter6]:     Journey_06_V2Release.markdown
[j_chapter7]:     Journey_07_V3Release.markdown

[captheorem]:     http://en.wikipedia.org/wiki/CAP_theorem
[aab]:            http://msdn.microsoft.com/en-us/library/hh680892(PandP.50).aspx
[youngeventual]:  http://codebetter.com/gregyoung/2010/04/14/quick-thoughts-on-eventual-consistency/
[topaz]:          http://msdn.microsoft.com/en-us/library/hh680934(PandP.50).aspx
[azurestorage]:   https://www.windowsazure.com/en-us/develop/net/fundamentals/cloud-storage/
[capinfoq]:       http://www.infoq.com/articles/cap-twelve-years-later-how-the-rules-have-changed
[idempotency]:    http://queue.acm.org/detail.cfm?id=2187821
[dtodataset]:     http://msdn.microsoft.com/en-us/library/ff649325.aspx
[uditmt]:         http://www.udidahan.com/2007/03/31/tasks-messages-transactions-%E2%80%93-the-holy-trinity/
[gregaah]:        http://codebetter.com/gregyoung/2010/02/16/cqrs-task-based-uis-event-sourcing-agh/

[fig1]:           images/Reference_04_Consistency_01.png?raw=true
[fig2]:           images/Reference_04_Consistency_02.png?raw=true
[fig3]:           images/Reference_04_Consistency_03.png?raw=true
