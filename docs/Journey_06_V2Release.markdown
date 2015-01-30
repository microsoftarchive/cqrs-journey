### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Chapter 6: Versioning our System 

_Preparing for the next stop: upgrading and migrating_

> "Variety is the very spice of life," William Cowper

The top-level goal for this stage in the journey is to learn about how 
to upgrade a system that includes bounded contexts that implement the 
CQRS pattern and event sourcing. The user stories that the team 
implemented in this stage of the journey involve both changes to the 
code and changes to the data: some existing data schemas changed and new 
data schemas were added. In addition to upgrading the system and 
migrating the data, the team planned to do the upgrade and migration 
with no downtime for the live system running in Windows Azure. 

## Working definitions for this chapter 

The remainder of this chapter uses the following definitions. 
For more detail, and possible alternative definitions, see the chapter 
[A CQRS and ES Deep Dive][r_chapter4] in the Reference Guide. 

### Command

A command is a request for the system to perform an action that changes 
the state of the system. Commands are imperatives, for example 
**MakeSeatReservation**. In this bounded context, commands originate 
from either the UI as a result of a user initiating a request, or from 
a process manager when the process manager is directing an aggregate to perform an 
action. 

A single recipient processes a Command. A command bus 
transports commands that command handlers then dispatch to aggregates. 
Sending a command is an asynchronous operation with no return value. 

### Event

An event, such as **OrderConfirmed**, describes something that has 
happened in the system, typically as a result of a command. Aggregates 
in the domain model raise events. Events can also come from other
bounded contexts.

Multiple subscribers can handle a specific event. Aggregates publish 
events to an event bus; handlers register for specific types of event on 
the event bus and then deliver the events to the subscriber. In the 
orders and registrations bounded context bounded context, the 
subscribers are a process manager and the read model generators. 

### Idempotent

Idempotency is a characteristic of an operation that means the operation 
can be applied multiple times without changing the result. For example, 
the operation "set the value _x_ to ten" is idempotent, while the 
operation "add one to the value of _x_" is not. In a messaging 
environment, a message is idempotent if it can be delivered multiple 
times without changing the result: either because of the nature of the
message itself, or because of the way that the system handles the
message.

## User stories 

The team implemented the following user stories during this phase of the 
project. 

### No downtime upgrade

The goal for the V2 release is to perform the upgrade, including any 
necessary data migration, without any downtime for the system. If this 
is not feasible with the current implementation, then the downtime 
should be minimized, and the system should be modified to support zero 
downtime upgrades in the future (starting with the V3 release). 

> **BethPersona:** Ensuring that we can perform no downtime upgrades is
> crucial to our credibility in the marketplace.

### Display remaining seat quantities

Currently, when a Registrant creates an order, there is no indication of 
the number of seats remaining for each seat type. The UI should display 
this information when the Registrant is selecting seats for purchase. 

### Handle zero-cost seats

Currently, when a Registrant selects seats that have a zero-cost, the UI 
flow still takes the Registrant to the payments page even though there 
is nothing to pay. The system should detect when there is nothing to pay 
and adjust the flow to take the Registrant directly to the conformation 
page for the order. 

## Architecture 

The application is designed to deploy to Windows Azure. At this stage in 
the journey, the application consists of web roles that contains the 
ASP.NET MVC web applications and a worker role that contains the message 
handlers and domain objects. The application uses SQL Database databases 
for data storage, both on the write-side and the read-side. The 
application uses the Windows Azure Service Bus to provide its messaging 
infrastructure. Figure 1 shows this high-level architecture.

![Figure 1][fig1]

**The top-level architecture in the V2 release**

While you are exploring and testing the solution, you can run it 
locally, either using the Windows Azure compute emulator or by running 
the MVC web application directly and running a console application that 
hosts the handlers and domain objects. When you run the application 
locally, you can use a local SQL Express database instead of SQL Database, 
and use a simple messaging infrastructure implemented in a SQL Express 
database. 

For more information about the options for running the application, see 
[Appendix 1][appendix1]. 

# Patterns and concepts 

During this stage of the journey, most of the key challenges addressed 
by the team related to how best to perform the migration from V1 to V2. 
This section describes some of those challenges. 

## Handling changes to events definitions

When the team examined the requirements for the V2 release, it became 
clear that we would need to change some of the events used in the 
Orders and Registrations bounded context to accommodate some of the new 
features: the **RegistrationProcessManager** would change and the system would 
provide a better user experience when the order had a zero cost. 

The Orders and Registrations bounded context uses event sourcing, so 
after the migration to V2 then event store will contain the old events 
but will start saving the new events. When the system replays events are 
replayed, it must operate correctly when it processes both the old and 
new sets of events. 

The team considered two approaches to handle this type of change in the 
system. 

### Mapping/filtering event messages in the infrastructure

This option handles old event messages and message formats by dealing 
with them somewhere in the infrastructure before they reach the domain. 
You can filter out old messages that are no longer relevant and use 
mapping to transform old format messages to a new format. This approach 
is initially the more complex approach because it requires changes in 
the infrastructure, but has the advantage of keeping the domain _pure_ 
because the domain only needs to understand the current set of events. 

### Handling multiple message versions in the aggregates

This alternative passes all the message types (both old and new) through 
to the domain where each aggregate must be able to handle both the old 
and new messages. This may be an appropriate strategy in the short-term, 
but will eventually cause the domain-model to become polluted with 
legacy event handlers.

The team selected this option for the V2 release because it 
involved the minimum amount of code changes. 

> **JanaPersona:** Dealing with both old and new events in the
> aggregates now does not prevent you from later moving to the first
> option and using a mapping/filtering mechanism in the infrastructure.

## Honoring message idempotency

One of the key issues to address in the V2 release is to make the system 
more robust. In the V1 release, in some scenarios, it is possible that 
some messages might be processed more than once and result in incorrect 
or inconsistent data in the system. 

> **JanaPersona:** Message idempotency is important in any system that
> uses messaging, not just systems that implement the CQRS pattern or use
> event sourcing.

In some scenarios, it would be possible to design idempotent messages, 
for example by using a message that says "set the seat quota to 500" 
rather than a message that says "add 100 to the seat quota." You could 
safely process the first message multiple times, but not the second. 

However, it is not always possible to use idempotent messages, so the 
team decided to use the de-duplication feature of the Windows Azure 
Service Bus to ensure that it delivers messages once only. The team 
made some changes to the infrastructure to ensure that Windows Azure 
Service Bus can detect duplicate messages, and configured Windows Azure 
Service Bus to perform duplicate message detection. 

To understand how Contoso implemented this, see the section "De-duplicating 
Messages" below. 

Additionally, you need to consider how the message handlers in the 
system retrieve messages from queues and topics. The current approach 
uses the Windows Azure Service Bus peek/lock mechanism. This is a
three-stage process: 

1. The handler retrieves a message from the queue or topic and leaves a
   locked copy of the message on the queue or topic. Other clients
   cannot see or access locked messages.
2. The handler processes the message.
3. The handler deletes the locked message from the queue. If a locked
   message is not unlocked or deleted after a fixed time, the message is
   unlocked and made available so that it can be retrieved again.

If step 3 fails for some reason, this means that the system can process 
the message more than once.

> **JanaPersona:** The team plans to address this issue in the next
> stage of the journey. See the chapter [Adding Resilience, new Bounded
> Contexts, and Features ][j_chapter7] for more information.

### Avoiding processing events multiple times

In V1, in certain scenarios it was possible for the system to process an 
event multiple times if an error occurred while the event was being 
processed. To avoid this scenario, the team modified the architecture so 
that every event handler has its own subscription to a Windows Azure 
topic. Figure 2 shows the two different models. 

![Figure 2][fig2]

**Using one subscription per event handler**

In the V1, the following behavior _could_ occur: 

1. The **EventProcessor**  instance receives an **OrderPlaced** event
   from the **all** subscription in the service bus. 
2. The **EventProcessor** instance has two registered handlers, the
   **RegistrationProcessManagerRouter** and **OrderViewModelGenerator** handler
   classes, so it invokes the **Handle** method on each of them.
3. The **Handle** method in the **OrderViewModelGenerator** class
   completes successfully.
4. The **Handle** method in the **RegistrationProcessManagerRouter** class
   throws an exception.
5. The **EventProcessor** instances catches the exception and abandons
   the event message. The message is automatically put back into the
   subscription.
6. The **EventProcessor** instance receives the **OrderPlaced** event
   from the **all** subscription for a second time.
7. It invokes the two Handle methods, causing the
   **RegistrationProcessManagerRouter** class to retry the message, and the
   **OrderViewModelGenerator** class to process the message for a second
   time.
8. Every time the **RegistrationProcessManagerRouter** class throws an
   exception, the **OrderViewModelGenerator** class processes the event.

In the V2 model, if a handler class throws an exception, the 
**EventProcessor** instance puts the event message back on the 
subscription associated with that handler class. The retry logic now 
only causes the **EventProcessor** instance to retry the handler that 
raised the exception, so no other handlers re-processe the message. 

## Persisting integration events

One of the concerns raised with the V1 release was about the way that 
the system persists the integration events that are sent from the 
Conference Management bounded context to the Orders and Registrations 
bounded context. These events include information about conference 
creation and publishing, and details of seat types and quota changes. 

In the V1 release, the **ConferenceViewModelGenerator** class in the 
Orders and Registrations bounded context handles these events by 
updating its view model and sending commands to the 
**SeatsAvailability** aggregate to tell it to change its seat quota 
values. 

This approach means that the Orders and Registrations bounded context is 
not storing any history and this could potentially cause problems: for 
example, other views look up seat type descriptions from this projection 
that only contains the latest value of the seat type description, as a 
result replaying a set of events elsewhere may regenerate another 
read-model projection that contains incorrect seat type descriptions. 

The team considered the following five options:

* Save all of the events in the originating bounded context (the
  Conference Management bounded context) and use a shared event store
  that the Orders and Registrations bounded context can access to replay
  these events. The receiving bounded context could replay the event
  stream up to a point in time when it needed to see what the seat type
  description was previously.
* Save all of the events as soon as they arrive in the receiving bounded
  context (the Orders and Registrations bounded context).
* Let the command handler in the view model generator save the events,
  selecting only those that it needs.
* Let the command handler in the view model generator save different
  events, in effect using event sourcing for this view model.
* Store all command and event messages from all bounded contexts in a 
  message log.

The first option is not always viable. In this particular case it would 
work because the same team is implementing both bounded contexts and the 
infrastructure making it easy to use a shared event store. 

> **GaryPersona:** Although from a purist's perspective, the first
> option breaks the strict isolation between bounded contexts, in some
> scenarios it may be an acceptable and pragmatic solution.

A possible risk with the third option is that the set of events that are 
needed may change in the future. If we don't save events now, they are 
lost for good.

Although the fifth option stores all the commands and events, some of
which you might never need to refer to again, it does provide a complete
log of everything that happens in the system. This could be useful for
troubleshooting, and also helps you to meet requirements that have not
yet been identified. The team chose this option over option two because
it offers a more general purpose mechanism that may have future
benefits.

The purpose of persisting the events is to enable them to be played back 
when the the Orders and Registrations bounded context needs the 
information about current seat quotas in order to calculate the number 
of remaining seats. To calculate these numbers consistently, you must 
always play the events back in the same order. There are several choices 
for this ordering: 

* The order the events were sent by the Conference Management bounded
  context.
* The order the events were received by the Orders and Registrations
  bounded context.
* The order the events were processed by the Orders and Registrations
  bounded context.

Most of the time these orderings will be the same. There is no correct 
order, you just need to choose one to be consistent. Therefore, the 
choice is determined by simplicity: in this case the simplest approach 
is to persist the events in the order that the handler in the Orders and 
Registrations bounded context receives them (the second option). 

> **MarkusPersona:** This choice does not typically arise with event
> sourcing. Each aggregate create events in a fixed order, and that is the
> order that the system uses to persist the events. In this scenario,
> the integration events are not created by a single aggregate.

There is a similar issue with saving timestamps for these events. 
Timestamps may be useful in the future if these is a requirement to look 
at number of remaining seats at a particular time. The choice here is 
whether you should create a timestamp when the event is created in the 
Conference Management bounded context or when it is received by the 
Orders and Registrations bounded context. It's possible that the Orders 
and Registrations bounded context is offline for some reason when the 
Conference Management bounded context creates an event, therefore the 
team decided to create the timestamp when the Conference Management 
bounded context publishes the event. 

## Message ordering

The acceptance tests that the team created and ran to verify the V1 
release highlighted a potential issue with message ordering: the 
acceptance tests that exercised the Conference Management bounded 
context sent a sequence of commands to the Orders and Registrations 
bounded context that sometimes arrived out of order. 

> **MarkusPersona:** This effect was not noticed when a human user
> tested this part of the system because the time delay between the
> times that the commands were sent was much greater making it less
> likely that the messages would arrive out of order.

The team considered two alternatives for ensuring messages are 
guaranteed to arrive in the correct order. 

* The first option is to use message sessions, a feature of the Windows
  Azure Service Bus. If you use message sessions, this offers guarantees
  that messages within a session are delivered in the same order that
  they were sent.
* The second alternative is to modify the handlers within the
  application to detect out of order messages through the use of
  sequence numbers or timestamps added to the messages when they are
  sent. If the receiving handler detects an out of order message, it
  rejects the message and puts it back onto the queue or topic to be
  processed later, after it has processed the messages that were sent
  before the rejected message.

The preferred solution in this case is to use Windows Azure Service Bus 
message sessions because this requires less change to the existing code. 
Both approaches would introduce some additional latency into the message 
delivery, but the team does not anticipate that this will have a 
significant effect on the performance of the system. 

# Implementation details 

This section describes some of the significant features of the 
implementation of the Orders and Registrations bounded context. You may 
find it useful to have a copy of the code so you can follow along. You 
can download a copy of the code from the [Download center][downloadc], 
or check the evolution of the code in the repository on github: 
[mspnp/cqrs-journey-code][repourl]. You can download the code from the
V2 release from the [Tags][tags] page on Github.

> **Note:** Do not expect the code samples to exactly match the code in
> the reference implementation. This chapter describes a step in the
> CQRS journey, the implementation may well change as we learn more and
> refactor the code.

## Adding support for zero-cost orders

There were three specific goals in making this change, all of which are
related:

1. Modify the **RegistrationProcessManager** class and related aggregates to
   handle orders with a zero cost.
2. Modify the navigation in the UI to skip the payment step when the
   total cost of the order is zero.
3. Ensure that the system functions correctly after the upgrade to V2
   with the old events as well as the new.

### Changes to the RegistrationProcessManager class

Previously, the **RegistrationProcessManager** class sent a 
**ConfirmOrderPayment** command after it received notification from the 
UI that the Registrant had completed the payment. Now, if there is a 
zero-cost order, the UI sends a **ConfirmOrder** command directly to the 
**Order** aggregate. If the order requires a payment, the 
**RegistrationProcessManager** class sends a **ConfirmOrder** command to the 
**Order** aggregate after it receives notification of a successful 
payment from the UI. 

> **JanaPersona:** Notice that the name of the command has changed from
> **ConfirmOrderPayment** to **ConfirmOrder**. This reflects the fact
> that the order doesn't need to know anything about the payment, all it
> needs to know is that the order is confirmed. Similarly, there is a
> new **OrderConfirmed** event that is now used in place of the old
> **OrderPaymentConfirmed** event.

When the **Order** aggregate receives the **ConfirmOrder** command it 
raises an **OrderConfirmed** event. In addition to being peristed, this 
event is also handled by the following objects: 

* The **OrderViewModelGenerator** class where it updates the sate of the
  order in the read-model.
* The **SeatAssignments** aggregate where it initializes a new
  **SeatAssignments** instance.
* The **RegistrationProcessManager** class where it triggers a command to
  commit the seat reservation.

### Changes to the UI

The main change in the UI is in the **RegistrationController** MVC 
controller class in the **SpecifyRegistrantAndPaymentDetails** action. 
Previously, this action method returned an 
**InitiateRegistrationWithThirdPartyProcessorPayment** action result; 
now, if the new **IsFreeOfCharge** property of the **Order** object is 
true, it returns a **CompleteRegistrationWithoutPayment** action result, 
otherwise it returns a 
**CompleteRegistrationWithThirdPartyProcessorPayment** action result. 

```Cs
[HttpPost]
public ActionResult SpecifyRegistrantAndPaymentDetails(AssignRegistrantDetails command, string paymentType, int orderVersion)
{
    ...

    var pricedOrder = this.orderDao.FindPricedOrder(orderId);
    if (pricedOrder.IsFreeOfCharge)
    {
        return CompleteRegistrationWithoutPayment(command, orderId);
    }

    switch (paymentType)
    {
        case ThirdPartyProcessorPayment:

            return CompleteRegistrationWithThirdPartyProcessorPayment(command, pricedOrder, orderVersion);

        case InvoicePayment:
            break;

        default:
            break;
    }

    ...
}
```

The **CompleteRegistrationWithThirdPartyProcessorPayment** redirects the 
user to the **ThirdPartyProcessorPayment** action and the 
**CompleteRegistrationWithoutPayment** method redirects the user 
directly to the **ThankYou** action. 

### Data migration

The Conference Management bounded context stores order information from 
the Orders and Registrations bounded context in the **PricedOrders** 
table in its SQL database. Previously, the Conference Management bounded 
context received the **OrderPaymentConfirmed** event; now it receives 
the **OrderConfirmed** event that contains an additional 
**IsFreeOfCharge** property. This becomes a new column in the SQL 
database. 

> **Markus:** We didn't need to modify the existing data in this table
> during the migration because the default value for a boolean is
> **false**. All of the existing entries were created before the system
> supported zero-cost orders.

During the migration, any in-flight **ConfirmOrderPayment** commands 
could be lost because they are no longer handled by the **Order** 
aggregate. You should verify that none of these commands are currently 
on the command bus. 

> **PoePersona:** We need to plan carefully how to deploy the V2 release
> so that we can be sure that all the existing, in-flight
> **ConfirmOrderPayment** commands are processed by a worker role
> instance running the V1 release

The system persists the state of **RegistrationProcessManager** class 
instances to a SQL database table. There are no changes to the schema of 
this table. The only change you will see after the migration is an 
additional value in the **StateValue** column. This reflects the 
additional **PaymentConfirmationReceived** vlaue in the **ProcessState** 
enumeration in the **RegistrationProcessManager** class as shown in the 
following code sample: 

```Cs
public enum ProcessState
{
    NotStarted = 0,
    AwaitingReservationConfirmation = 1,
    ReservationConfirmationReceived = 2,
    PaymentConfirmationReceived = 3,
}
```

In the V1 release, the events that the event sourcing system persisted 
for the **Order** aggregate included the **OrderPaymentConfirmed** 
event. Therefore, the event store contains instances of this event type. 
In the V2 release, the **OrderPaymentConfirmed** is replaced with the 
**OrderConfirmed** event. 

The team decided for the V2 release not to introduce mapping and 
filtering events at the infrastructure level when events are 
deserialized. This means that the handlers must understand both the old 
and new events when the system replays these events from the event 
store. The following code sample shows this in the 
**SeatAssignmentsHandler** class: 

```Cs
static SeatAssignmentsHandler()
{
    Mapper.CreateMap<OrderPaymentConfirmed, OrderConfirmed>();
}

public SeatAssignmentsHandler(IEventSourcedRepository<Order> ordersRepo, IEventSourcedRepository<SeatAssignments> assignmentsRepo)
{
    this.ordersRepo = ordersRepo;
    this.assignmentsRepo = assignmentsRepo;
}

public void Handle(OrderPaymentConfirmed @event)
{
    this.Handle(Mapper.Map<OrderConfirmed>(@event));
}

public void Handle(OrderConfirmed @event)
{
    var order = this.ordersRepo.Get(@event.SourceId);
    var assignments = order.CreateSeatAssignments();
    assignmentsRepo.Save(assignments);
}
```

You can also see the same technique in use in the 
**OrderViewModelGenerator** class. 

The approach is slightly different in the **Order** class because this 
is one of the events that is persisted to the event store. The following 
code sample shows part of the **protected** constructor in the **Order** 
class: 

```Cs
protected Order(Guid id)
    : base(id)
{
    ...
    base.Handles<OrderPaymentConfirmed>(e => this.OnOrderConfirmed(Mapper.Map<OrderConfirmed>(e)));
    base.Handles<OrderConfirmed>(this.OnOrderConfirmed);
    ...
}
```

> **JanaPersona:** Handling the old events in this way was
> straightforward for this scenario because the only change was to the
> name of the event. It would be more complicated if the properties of
> the event changed as well. In the future, Contoso will consider doing
> the mapping in the infrastructure to avoid polluting the domain model
> with legacy events.

## Displaying remaining seats in the UI

There were three specific goals in making this change, all of which are
related:

1. Modify the system to include information about the
   number of remaining seats of each seat type in the conference
   read-model.
2. Modify the UI to display the number of remaining seats of each seat
   type.
3. Ensure that the system functions correctly after the upgrade to V2.

### Adding information about remaining seat quantities to the read-model

The information that the system needs to be able to display the number 
of remaining seats comes from two places. 

* The Conference Management bounded context raises the **SeatCreated**
  and **SeatUpdated** whenever the Business Customer creates new seat
  types or modifies seat quotas. 
* The **SeatsAvailability** aggregate in the Orders and Registrations
  bounded context raises the **SeatsReserved**,
  **SeatsReservationCancelled**, and **AvailableSeatsChanged** while a
  Registrant is creating an order.
  
> **Note:** The **ConferenceViewModelGenerator** class does not use the
> **SeatCreated** and **SeatUpdated**

The **ConferenceViewModelGenerator** class in the Orders and 
Registrations bounded context now handles these events and uses them to 
calculate and store the information about seat type quantities in the 
read-model. The following code sample shows the relevant handlers in the 
**ConferenceViewModelGenerator** class: 

```Cs
public void Handle(AvailableSeatsChanged @event)
{
	this.UpdateAvailableQuantity(@event, @event.Seats);
}

public void Handle(SeatsReserved @event)
{
	this.UpdateAvailableQuantity(@event, @event.AvailableSeatsChanged);
}

public void Handle(SeatsReservationCancelled @event)
{
	this.UpdateAvailableQuantity(@event, @event.AvailableSeatsChanged);
}

private void UpdateAvailableQuantity(IVersionedEvent @event, IEnumerable<SeatQuantity> seats)
{
	using (var repository = this.contextFactory.Invoke())
	{
		var dto = repository.Set<Conference>().Include(x => x.Seats).FirstOrDefault(x => x.Id == @event.SourceId);
		if (dto != null)
		{
			if (@event.Version > dto.SeatsAvailabilityVersion)
			{
				foreach (var seat in seats)
				{
					var seatDto = dto.Seats.FirstOrDefault(x => x.Id == seat.SeatType);
					if (seatDto != null)
					{
						seatDto.AvailableQuantity += seat.Quantity;
					}
					else
					{
						Trace.TraceError("Failed to locate Seat Type read model being updated with id {0}.", seat.SeatType);
					}
				}

				dto.SeatsAvailabilityVersion = @event.Version;

				repository.Save(dto);
			}
			else
			{
				Trace.TraceWarning ...
			}
		}
		else
		{
			Trace.TraceError ...
		}
	}
}
```

The **UpdateAvailableQuantity** method compares the version on the event 
to current version of the read-model to detect possible duplicate 
messages. 

> **MarkusPersona:** This check only detects duplicate messages, not out
> of sequence messages.

### Modifying the UI to display remaining seat quantities

Now, when the UI queries the conference read-model for a list of seat 
types, the list includes the currently available number of seats. The 
following code samples shows how the **RegistrationController** MVC 
controller uses the **AvailableQuantity** of the **SeatType** class: 

```Cs
private OrderViewModel CreateViewModel()
{
	var seatTypes = this.ConferenceDao.GetPublishedSeatTypes(this.ConferenceAlias.Id);
	var viewModel =
		new OrderViewModel
		{
			ConferenceId = this.ConferenceAlias.Id,
			ConferenceCode = this.ConferenceAlias.Code,
			ConferenceName = this.ConferenceAlias.Name,
			Items =
				seatTypes.Select(
					s =>
						new OrderItemViewModel
						{
							SeatType = s,
							OrderItem = new DraftOrderItem(s.Id, 0),
							AvailableQuantityForOrder = s.AvailableQuantity,
							MaxSelectionQuantity = Math.Min(s.AvailableQuantity, 20)
						}).ToList(),
		};

	return viewModel;
}
```

### Data migration

The SQL table that holds the conference read-model data now has a new 
column to hold the version number that is used to check for duplicate 
events, and the SQL table that holds the seat type read-model data now 
has a new column to hold the available quantity of seats. 

As part of the data migration it is necessary to replay all of the 
events in the event store for each of the **SeatsAvailability** 
aggregates in order to correctly calculate the available quantities. 

## De-duplicating command messages

The system currently uses the Windows Azure Service Bus to transport 
messages. When the system initializes the Windows Azure Service Bus from 
the start-up code in the **ConferenceProcessor** class, it configures 
the topics to detect duplicate messages as shown in the following code 
sample from the **ServiceBusConfig** class: 

```Cs
private void CreateTopicIfNotExists()
{
    var topicDescription =
        new TopicDescription(this.topic)
        {
            RequiresDuplicateDetection = true,
            DuplicateDetectionHistoryTimeWindow = topic.DuplicateDetectionHistoryTimeWindow,
        };
    try
    {
        this.namespaceManager.CreateTopic(topicDescription);
    }
    catch (MessagingEntityAlreadyExistsException) { }
}
```
> **Note:** You can configure the
> **DuplicateDetectionHistoryTimeWindow** in the **Settings.xml** file
> by adding an attribute to the **Topic** element. The default value is
> one hour.

However, for the duplicate detection to work you must ensure that every 
message has a unique id. The following code sample shows the 
**MarkSeatsAsReserved** command: 

```Cs
public class MarkSeatsAsReserved : ICommand
{
    public MarkSeatsAsReserved()
    {
        this.Id = Guid.NewGuid();
        this.Seats = new List<SeatQuantity>();
    }

    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public List<SeatQuantity> Seats { get; set; }

    public DateTime Expiration { get; set; }
}
```

The **BuildMessage** method in the **CommandBus** class uses the command 
Id to create a unique message Id that the Windows Azure Service Bus can 
use to detect duplicates: 

```Cs
private BrokeredMessage BuildMessage(Envelope<ICommand> command)
{
    var stream = new MemoryStream();
    ...

    var message = new BrokeredMessage(stream, true);
    if (!default(Guid).Equals(command.Body.Id))
    {
        message.MessageId = command.Body.Id.ToString();
    }

    ...

    return message;
}
```
## Guaranteeing message ordering

The team decided to use Windows Azure Service Bus Message Sessions to 
guarantee message ordering in the system.

The system configures the Windows Azure Service Bus topics and 
subscriptions from the **OnStart** method in the **ConferenceProcessor** 
class. The configuration in the **Settings.xml** file specifies whether 
a particular subscription should use sessions. The following code sample 
from the **ServiceBusConfig** class shows how the system creates and 
configures subscriptions. 

```Cs
private void CreateSubscriptionIfNotExists(NamespaceManager namespaceManager, TopicSettings topic, SubscriptionSettings subscription)
{
    var subscriptionDescription =
        new SubscriptionDescription(topic.Path, subscription.Name)
        {
            RequiresSession = subscription.RequiresSession
        };

    try
    {
        namespaceManager.CreateSubscription(subscriptionDescription);
    }
    catch (MessagingEntityAlreadyExistsException) { }
}
```

The following code sample from the **SessionSubscriptionReceiver** class 
shows how to use sessions to receive messages: 

```Cs
private void ReceiveMessages(CancellationToken cancellationToken)
{
	while (!cancellationToken.IsCancellationRequested)
	{
		MessageSession session;
		try
		{
			session = this.receiveRetryPolicy.ExecuteAction<MessageSession>(this.DoAcceptMessageSession);
		}
		catch (Exception e)
		{
			...
		}

		if (session == null)
		{
			Thread.Sleep(100);
			continue;
		}


		while (!cancellationToken.IsCancellationRequested)
		{
			BrokeredMessage message = null;
			try
			{
				try
				{
					message = this.receiveRetryPolicy.ExecuteAction(() => session.Receive(TimeSpan.Zero));
				}
				catch (Exception e)
				{
					...
				}

				if (message == null)
				{
					// If we have no more messages for this session, exit and try another.
					break;
				}

				this.MessageReceived(this, new BrokeredMessageEventArgs(message));
			}
			finally
			{
				if (message != null)
				{
					message.Dispose();
				}
			}
		}

		this.receiveRetryPolicy.ExecuteAction(() => session.Close());
	}
}

private MessageSession DoAcceptMessageSession()
{
	try
	{
		return this.client.AcceptMessageSession(TimeSpan.FromSeconds(45));
	}
	catch (TimeoutException)
	{
		return null;
	}
}
```

> **MarkusPersona:** You may find it useful to compare this version of
> the **ReceiveMessages** that uses message sessions with the original
> version in the **SubscriptionReceiver** class.

To be able to use message sessions when you receive a message, you must 
ensure that when you send a message you include a session id. The system 
uses the source id from the event as the session id as shown in the 
following code sample from the **BuildMessage** method in the 
**EventBus** class. 

```Cs
var message = new BrokeredMessage(stream, true);
message.SessionId = @event.SourceId.ToString();
```

In this way, you can guarantee that all of the messages from an 
individual source will be received in the correct order. 

> **PoePersona:** In the V2 release, the team changed the way that the
> system creates the Windows Azure Service Bus topics and subscriptions.
> Previously, the **SubscriptionReceiver** class created them if they
> didn't exist already. Now, the system creates them when the
> application starts up using configuration data. This happens early in
> the start-up process to avoid the risk of losing messages if one is
> sent to a topic before a the system initializes the subscriptions.

However, sessions can only guarantee to deliver messages in order if the messages are placed on the bus in the correct order. If the system sends messages asynchronously, then you must take special care to ensure that messages are placed on the bus in the correct order. In our system, it is important that the events from each individual aggregate instance arrive in order, but we don't care about the ordering of events from different aggregate instances. Therefore, although the system sends events asynchronously, the **EventStoreBusPublisher** instance waits for an acknowledgement that the previous event was sent before sending the next one. The following sample from the **TopicSender** class illustrates this:

```Cs
public void Send(Func<BrokeredMessage> messageFactory)
{
    var resetEvent = new ManualResetEvent(false);
    Exception exception = null;
    this.retryPolicy.ExecuteAction(
        ac =>
        {
            this.DoBeginSendMessage(messageFactory(), ac);
        },
        ar =>
        {
            this.DoEndSendMessage(ar);
        },
        () => resetEvent.Set(),
        ex =>
        {
            Trace.TraceError("An unrecoverable error occurred while trying to send a message:\r\n{0}", ex);
            exception = ex;
            resetEvent.Set();
        });

    resetEvent.WaitOne();
    if (exception != null)
    {
        throw exception;
    }
}
```

> **JanaPersona:** This code sample shows how the system uses the
> [Transient Fault Handling Block][tfhab] to make the asynchronous call
> reliably.

For additional information about message ordering and Windows Azure
Service Bus, see [Windows Azure Queues and Windows Azure Service Bus
Queues - Compared and Contrasted][queues].

For information about sending messages asynchronously and ordering, see 
the blog post [Windows Azure Service Bus Splitter and 
Aggregator][sessionseq]. 

## Persisting events from the Conference Management bounded context

The team decided to create a message log of all the commands and events 
that are sent. This will enable the Orders and Registrations bounded 
context to query this log for the events from the Conference Management 
bounded context that it requires to build its read-models. This is not event sourcing because we are not using these events to rebuild the state of our aggregates although we are using similar techniques to capture and persist these integration events.

> **GaryPersona:** This message log ensures that no messages are
> lost, so that in the future it will be possible to meet additional
> requirements.

### Adding additional metadata to the messages

The system now persists all messages to the message log. To make it 
easier to query the message log for specific commands or events, the 
system now adds more metadata to each message. Previously, the only 
metadata was the event type; now, the event metadata includes the event 
type type, namespace, assembly, and path. The system adds the metadata 
to the events in the **EventBus** class and to the commands in the 
**CommandBus** class. 

### Capturing and persisting messages to the message log

The system uses an additional subscription to the 
**conference/commands** and **conference/events** topics in Windows 
Azure Service Bus to receive copies of every message in the system. It 
then appends the message to a Windows Azure table storage table. The 
following code sample shows the entity that the 
**AzureMessageLogWriter** class uses to save the message to the table: 

```Cs
public class MessageLogEntity : TableServiceEntity
{
    public string Kind { get; set; }
    public string CorrelationId { get; set; }
    public string MessageId { get; set; }
    public string SourceId { get; set; }
    public string AssemblyName { get; set; }
    public string Namespace { get; set; }
    public string FullName { get; set; }
    public string TypeName { get; set; }
    public string SourceType { get; set; }
    public string CreationDate { get; set; }
    public string Payload { get; set; }
}
```
The **Kind** property specifies whether the message is either a command 
or an event. The **MessageId** and **CorrelationId** properties are set 
by the messaging infrastructure. The remaining properties are set from 
the message metadata. 

The following code sample shows the defintion of the partition and row 
keys for these messages: 

```Cs
PartitionKey = message.EnqueuedTimeUtc.ToString("yyyMM"),
RowKey = message.EnqueuedTimeUtc.Ticks.ToString("D20") + "_" + message.MessageId
```

Notice how the row key preserves the order in which the messages were 
originally sent and adds on the message id to guarantee uniqueness just 
in case two messages were enqueued at exactly the same time.

> **JanaPersona:** This is different from the event store where the
> partition key identifies the aggregate instance and the row key
> identifies the aggregate version number.

### Data migration

When Contoso migrates the system from V1 to V2, it will use the message 
log to rebuild the conference and priced-order read-models in the Orders 
and Registrations bounded context. 

> **GaryPersona:** Contoso can use the message log whenever it needs
> to rebuild the read-models that are built from events that are not
> associated with an aggregate such as the integration events from the
> Conference Management bounded context.

The conference read-model holds information about conferences and 
contains information from the **ConferenceCreated**, 
**ConferenceUpdated**, **ConferencePublished**, 
**ConferenceUnpublished**, **SeatCreated**, and **SeatUpdated** events 
that come from the Conference Management bounded context. 

The priced-order read model holds information from the **SeatCreated** 
and **SeatUpdated** events that come from the Conference Management 
bounded context. 

However, in V1 these event messages were not persisted, so the 
read-models cannot be re-populated in V2. To work around this problem, 
the team implemented a data migration utility that uses a best effort 
approach to generate events that contain the missing data to store in 
the message log. For example, after the migration to V2, the message log 
does not contain any **ConferenceCreated** events, so the migration 
utility finds this information in the SQL database used by the 
Conference Management bounded context and creates the missing events. 
You can see how this is done in the 
**GeneratePastEventLogMessagesForConferenceManagement** in the 
**Migrator** class in the **MigrationToV2** project. 

> **MarkusPersona:** You can see in this class that Contoso also copies
> all of the existing event sourced events into the message log.

The **RegenerateViewModels** method in the **Migrator** class shown
below rebuilds the read-models. It retrieves all the events from the
message log by invoking the **Query** method, and then uses the 
**ConferenceViewModelGenerator** and **PricedOrderViewModelUpdater** 
classes to handle the messages. 

```Cs
internal void RegenerateViewModels(AzureEventLogReader logReader, string dbConnectionString)
{
    var commandBus = new NullCommandBus();

    Database.SetInitializer<ConferenceRegistrationDbContext>(null);

    var handlers = new List<IEventHandler>();
    handlers.Add(new ConferenceViewModelGenerator(() => new ConferenceRegistrationDbContext(dbConnectionString), commandBus));
    handlers.Add(new PricedOrderViewModelUpdater(() => new ConferenceRegistrationDbContext(dbConnectionString)));

    using (var context = new ConferenceRegistrationMigrationDbContext(dbConnectionString))
    {
        context.UpdateTables();
    }

    try
    {
        var dispatcher = new MessageDispatcher(handlers);
        var events = logReader.Query(new QueryCriteria { });

        dispatcher.DispatchMessages(events);
    }
    catch
    {
        using (var context = new ConferenceRegistrationMigrationDbContext(dbConnectionString))
        {
            context.RollbackTablesMigration();
        }

        throw;
    }
}
```

> **JanaPersona:** They query may not be fast because it will retrieve
> entities from multiple partitions.

Notice how this method uses a **NullCommandBus** instance to swallow any 
commands from the **ConferenceViewModelGenerator** instance because we 
are only rebuilding the read-model here. 

Previously, the **PricedOrderViewModelGenerator** used the 
**ConferenceDao** class to obtain information about seats; now, it is 
autonomous and handles the **SeatCreated** and **SeatUpdated** events 
directly to maintain this information. As part of the migration, this 
information must be added to the read-model. In the previous code 
sample, the **PricedOrderViewModelUpdater** class only handles the 
**SeatCreated** and **SeatUpdated** events and adds the missing 
information to the priced-order read-model. 

## Migrating from V1 to V2

Migrating from V1 to V2 requires you to update the deployed application 
code and migrate the data. You should always rehearse the migration in a 
test environment before performing it in your production environment. 
These are the required steps: 

1. Deploy the V2 release to your Windows Azure staging environment. The
   V2 release has a **MaintenanceMode** property that is
   initially set to **true**. In this mode, the application displays a
   message to the user that site is currently undergoing maintenance and
   the worker role does not process messages.
2. When you are ready, swap the V2 release (still in maintenance mode)
   into your Windows Azure production environment.
3. Leave the V1 release (now running in the staging environment) to run
   for a few minutes to ensure that all in-flight messages complete
   their processing.
4. Run the migration program to migrate the data (see below).
5. After the data migration completes successfully, change the
   **MaintenanceMode** property of each role type to **false**.
6. The V2 release is now live in Windows Azure.

> **JanaPersona:** The team considered using a separate application to
> display to users that the site is undergoing maintenance during the
> upgrade process. However, using the **MaintenanceMode** property in
> the V2 release provides a simpler process, and adds a potentially
> useful new feature to the application.

> **PoePersona:** Because of the changes to the event store, it is not
> possible to perform a no downtime upgrade from V1 to V2. However, the
> changes that the team have made will ensure that the migration from V2
> to V3 will be possible with no downtime.

> **MarkusPersona:** The team applied various optimizations to the
> migration utility, such as batching the operations, in order to
> minimize the amount of downtime.

The following sections summarize the data migration from V1 to V2. Some 
of these steps were discussed previously in relation to a specific 
change or enhancement to the application. 

One of the changes the team introduced for V2 is to keep a copy of all 
command and event messages in a message log in order to future-proof the 
application by capturing everything that might be used in the future. 
The migration process takes this new feature into account.

Because the migration process copies large amounts of data 
around, you should run it in a Windows Azure worker role in order to 
minimize the cost . The migration utility is a console application so 
you can use the Windows Azure remote desktop feature. For information 
about how to run an application inside a Windows Azure role instance, 
see [Using Remote Desktop with Windows Azure Roles][azurerdp].

> **PoePersona:** For some organizations, the security poilicy will not
> allow you to use Windows Azure remote desktop in a production
> evironment. However, you only need the worker role that hosts the
> remote desktop session for the duration of the migration, you can
> delete it after the migration is complete. You could also run your
> migration code as a worker role instead of as a console application
> and ensure that it logs the status of the migration for you to verify.

### Generating past log messages for the Conference Management bounded context

Part of the migration process is to recreate, where possible, the 
messages that the V1 release discarded after processing and then add 
them to the message log. In the V1 release, all of the integration 
events sent from the Conference Management bounded context to the Orders 
and Registrations bounded context were lost in this way. The system 
cannot recreate all of the lost events, but it can create events that 
represent the state of system at the time of the migration. 

For more information, see the section "Persisting Events from the 
Conference Management Bounded Context" earlier in this chapter. 

### Migrating the event sourcing events

In the V2 release, the event store now stores additional metadata for 
each event in order to facilitate querying for for events. The migration 
process copies all of the events from the existing event store to a new 
event store with the new schema. 

> **JanaPersona:** The original events are not updated in any way and
> are treated as being immutable.

At the same time, the system adds a copy of all of these events to the 
message log that was introduced in the V2 release. 

For more information, see the 
**MigrateEventSourcedAndGeneratePastEventLogs** in the **Migrator** 
class in the **MigrationToV2** project. 

### Rebuilding the read-models

The V2 release includes several changes to the definitions of the 
read-models in the Orders and Registrations bounded context. The 
**MigrationToV2** project rebuilds the Conference read-model and 
Priced-order read-model in the Orders and Registrations bounded context. 

For more information, see the section "Persisting Events from the 
Conference Management Bounded Context" earlier in this chapter. 

# Impact on testing 

During this stage of the journey, the test team continuesd to expand the 
set of acceptance tests. They also created a set of tests to verify the 
data migration process. 

## SpecFlow revisited

Previously, the set of SpecFlow tests were implemented in two ways: 
either simulating user interaction by automating a web browser, or by 
operating directly on the MVC contollers. Both approaches had their 
advantages and disadvantages that are discussed in [Chapter 4, Extending 
and Enhancing the Orders and Registrations Bounded 
Contexts][j_chapter4]. 

After discussing these tests with another expert, the team also 
implemented a third approach. From the perspective of the DDD approach, 
the UI is not part of the domain-model, and the focus of the core team 
should be on understanding the domain with the help of the domain expert 
and implementing the business logic in the domain. The UI is just 
mechanics that is added to enable users to interact with the domain. 
Therefore acceptance testing should include verfying that the 
domain-model functions in the way that the domain expert expects. 
Therefore the team created a set of acceptance tests using SpecFlow that 
are designed to exercise the domain without the distraction of the UI 
parts of the system. 

The following code sample shows the 
**SelfRegistrationEndToEndWithDomain.feature** file in the 
**Features\Domain\Registration** folder in the 
**Conference.AcceptanceTests** Visual Studio solution. Notice how the 
**When** and **Then** clauses use commands and events. 

> **GaryPersona:** Typically, you would expect the **When** clauses
> to send commands and the **Then** clauses to see events or exceptions
> if your domain-model uses just aggregates. However in this example,
> the domain-model includes a process manager that responds to
> events by sending commands. The test is checking that all of the
> expected commands are sent and all of the expected events raised.

```
Feature: Self Registrant end to end scenario for making a Registration for a Conference site with Doamin Commands and Events
	In order to register for a conference
	As an Attendee
	I want to be able to register for the conference, pay for the Registration Order and associate myself with the paid Order automatically


Scenario: Make a reservation with the selected Order Items
Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |
And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
When the Registrant proceed to make the Reservation
	# command:RegisterToConference
Then the command to register the selected Order Items is received 
	# event: OrderPlaced
And the event for Order placed is emitted
	# command: MakeSeatReservation
And the command for reserving the selected Seats is received
	# event: SeatsReserved
And the event for reserving the selected Seats is emitted
	# command: MarkSeatsAsReserved
And the command for marking the selected Seats as reserved is received
	# event: OrderReservationCompleted 
And the event for completing the Order reservation is emitted
	# event: OrderTotalsCalculated
And the event for calculating the total of $249 is emitted
```

The following code sample shows some of the step implementations for the 
feature file. The steps use the command bus to send the commands. 

```Cs
[When(@"the Registrant proceed to make the Reservation")]
public void WhenTheRegistrantProceedToMakeTheReservation()
{
    registerToConference = ScenarioContext.Current.Get<RegisterToConference>();
    var conferenceAlias = ScenarioContext.Current.Get<ConferenceAlias>();

    registerToConference.ConferenceId = conferenceAlias.Id;
    orderId = registerToConference.OrderId;
    this.commandBus.Send(registerToConference);

    // Wait for event processing
    Thread.Sleep(Constants.WaitTimeout);
}

[Then(@"the command to register the selected Order Items is received")]
public void ThenTheCommandToRegisterTheSelectedOrderItemsIsReceived()
{
    var orderRepo = EventSourceHelper.GetRepository<Registration.Order>();
    Registration.Order order = orderRepo.Find(orderId);

    Assert.NotNull(order);
    Assert.Equal(orderId, order.Id);
}

[Then(@"the event for Order placed is emitted")]
public void ThenTheEventForOrderPlacedIsEmitted()
{
    var orderPlaced = MessageLogHelper.GetEvents<OrderPlaced>(orderId).SingleOrDefault();
            
    Assert.NotNull(orderPlaced);
    Assert.True(orderPlaced.Seats.All(
        os => registerToConference.Seats.Count(cs => cs.SeatType == os.SeatType && cs.Quantity == os.Quantity) == 1));
}
```

## Discovering a bug during the migration

When the test team ran their tests on the system after the migration, 
we discovered that the number of seat types in the Orders and 
Registrations bounded context was different from the number prior to the 
migration. The investigation revealed the following cause. 

The Conference Management bounded context allows a Business Customer to 
delete a seat type if the conference has never been published, but does 
not raise an integration event to report this fact to the Orders and 
Registrations bounded context. Therefore the Orders and Registrations 
bounded context receives an event from the Conference Management bounded 
context when a Business Customer creates a new seat type, but not when a 
Business Customer deletes a seat type. 

Part of the migration process creates a set of integration events to 
replace those that the V1 release discarded after processing. It creates 
these events by reading the SQL database used by the Conference 
Management bounded context. This process did not create integration 
events for the deleted seat types. 

In summary, in the V1 release deleted seat types incorrectly appeared in 
the read-models in the Orders and Registrations bounded context. After 
the migration to the V2 release, these deleted seat types did not appear 
in the read-models in the Orders and Registrations bounded context. 

> **PoePersona:** Testing the migration process not only verifies that
> the migration runs as expected, but potentially reveals bugs in the
> application itself.

[fig1]:              images/Journey_06_TopLevel.png?raw=true
[fig2]:              images/Journey_06_Subscriptions.png?raw=true

[j_chapter4]:        Journey_04_ExtendingEnhancing.markdown
[j_chapter7]:        Journey_07_V3Release.markdown
[r_chapter4]:        Reference_04_DeepDive.markdown

[messagesessions]:   http://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.messagesession.aspx
[repourl]:           https://github.com/mspnp/cqrs-journey-code
[queues]:            http://msdn.microsoft.com/en-us/library/windowsazure/hh767287(v=vs.103).aspx
[azurerdp]:          http://msdn.microsoft.com/en-us/library/windowsazure/gg443832.aspx
[downloadc]:         http://NEEDFWLINK
[tfhab]:             http://msdn.microsoft.com/en-us/library/hh680934(PandP.50).aspx
[sessionseq]:        http://geekswithblogs.net/asmith/archive/2012/04/10/149275.aspx
[tags]:              https://github.com/mspnp/cqrs-journey-code/tags
