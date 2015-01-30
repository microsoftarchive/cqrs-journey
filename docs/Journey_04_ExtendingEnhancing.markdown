### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Chapter 4: Extending and Enhancing the Orders and Registrations Bounded Context

_Further exploration of the Orders and Registrations bounded context._

_"I see that it is by no means useless to travel, if a man wants to see something new." Jules Verne, Around the World in Eighty Days_


# Changes to the bounded context

The previous chapter describes the **Orders and Registrations** bounded context in some 
detail. This chapter describes some changes that 
the team made in this bounded context during the second stage of their 
CQRS journey. 

The specific topics described in this chapter include:

* Improvements to the way that message correlation works with the 
  **RegistrationProcessManager** class. This illustrates how aggregate
  instances within the bounded context can interact in a complex manner.
* Implementing a record locator to enable a Registrant to retrieve an 
  order that she saved during a previous session. This illustrates
  adding some additional logic to the write-side that enables you to
  locate an aggregate instance without knowing its unique Id.
* Adding a countdown timer to the UI to enable a Registrant to track how 
  much longer they have to complete an order. This illustrates
  enhancements to the write-side to support displaying rich information
  in the UI.
* Supporting orders for multiple seat types simultaneously. For example,
  a Registrant requests five seats for pre-conference event and eight
  seats for the full conference. This adds more complex business logic
  into the write-side.
* CQRS command validation using MVC. This illustrates how to make use
  of the model validation feature in MVC to validate your CQRS commands
  before you send them to the domain.

> **Note:** The Contoso Conference Management System described in this
> chapter is not the final version of the system. This guidance
> describes a journey, so some of the design decisions and
> implementation details change in later steps in the journey. These
> changes are described in subsequent chapters.

## Working definitions for this chapter 

The remainder of this chapter uses the following definitions. 
For more detail, and possible alternative definitions, see [A CQRS/ES 
Deep Dive][r_chapter4] in the Reference Guide. 

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
in the domain model raise events. 

Multiple subscribers can handle a specific event. Aggregates publish 
events to an event bus; handlers register for specific types of event on 
the event bus and then deliver the events to the subscriber. In this 
bounded context, the only subscriber is a process manager. 

### Process manager

In this bounded context, a process manager is a class that coordinates 
the behavior of the aggregates in the domain. A process manager 
subscribes to the events that the aggregates raise, and then follow a 
simple set of rules to determine which command or commands to send. The 
process manager does not contain any business logic, simply logic to 
determine the next command to send. The process manager is implemented 
as a state machine, so when the process manager responds to an event, it 
can change its internal state in addition to sending a new command. 

The process manager in this bounded context can receive commands as well 
as subscribe to events. 

Our process manager is an implementation of the Process Manager pattern 
defined on pages 312 to 321 in the book "Enterprise Integration 
Patterns: Designing, Building, and Deploying Messaging Solutions" by 
Gregor Hohpe and Bobby Woolf. 

## User stories 

This chapter discusses the implementation of two user stories in 
addition to describing some changes and enhancements to the **Orders and 
Registrations** bounded context. 

### Implement a login using a Record Locator

When a Registrant creates an order for seats at a conference, the system 
generates a five-character **order access code** and sends it to the 
Registrant by email. The Registrant can use her email address and the 
**order access code** on the conference web site to retrieve the order 
from the system at a later date. The Registrant may wish to retrieve the 
order to review it, or to complete the registration process by assigning 
Attendees to seats. 

> **CarlosPersona:** From the business perspective it was important for
> us to be as user-friendly as possible: we don't want to block or
> unnecessarily burden anyone who is trying to register for a
> conference. Therefore, we have no requirement for a user to create an
> account in the system prior to registration, especially since users
> must enter most of their information in a standard checkout process
> anyway.

### Inform the registrant how much time remains to complete an order

When a Registrant creates an order, the system reserves the seats 
requested by the Registrant until the order is complete or the 
reservations expire. To complete an order, the Registrant must submit 
her details, such as name and email address, and make a successful 
payment. 

To help the Registrant, the system displays a countdown timer to inform 
the Registrant how much time remains to complete the order before the 
seat reservations expire. 

### Enabling a registrant to create an order that includes multiple seat types

When a Registrant creates an order, the Registrant may request different 
numbers of different seat types. For example, a Registrant may request 
five seats for the full conference and three seats for the 
pre-conference workshop. 

## Architecture 

The application is designed to deploy to Windows Azure. At this stage in 
the journey, the application consists of a web role that contains the 
ASP.NET MVC web application and a worker role that contains the message 
handlers and domain objects. The application uses SQL Database databases 
for data storage, both on the write-side and the read-side. The 
application uses the Windows Azure Service Bus to provide its messaging 
infrastructure. Figure 1 shows this high-level architecture.

![Figure 1][fig1]

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

This section describes some of the key areas of the application that the 
team visited during this stage of the journey and introduces some of the 
challenges met by the team when we addressed these areas. 

## Record Locators

The system uses **Access Codes** instead of passwords to avoid the 
overhead for the Registrant of setting up an account with the system. 
Many Registrants may use the system only once, so there is no need to 
create a permanent account with a user ID and a password. 

The system needs to be able to retrieve order information quickly based 
on the Registrant's email address and access code. To provide a minimum 
level of security, the access codes that the system generates should not 
be predictable, and the order information that Registrants can retrieve 
should not contain any sensitive information. 

## Querying the read-side

The previous chapter focused on the write-side model and implementation; 
in this chapter we'll explore the read-side implementation in more 
detail. In particular, you'll see how the team implemented the read model 
and the querying mechanism from the MVC controllers. 

In this initial exploration of the CQRS pattern, the team decided to use 
SQL views in the database as the underlying source of the data queried 
by the MVC controllers on the read-side. To minimize the work that the 
queries on the read side must perform, these SQL views provide a 
denormalized version of the data. These views currently exist in the 
same database as the normalized tables that the write model uses. 

> **JanaPersona:** The team will split the database into two and explore
> options for pushing changes from the normalized write-side to the
> de-normalized read-side in a later stage of the journey. For an 
> example of using Windows Azure blob storage instead of SQL tables for
> storing the read-side data, see the 
> **SeatAssignmentsViewModelGenerator** class.

### Storing denormalized views in a database

One common option for storing the read-side data is to use a set of 
relational database tables to hold the de-normalized views. You should
optimize the read-side for fast reads, so there is typically no 
benefit in storing normalized data because this will require complex 
queries to construct the data for the client. This implies that goals 
for the read-side should be to keep the queries as simple as possible, 
and to structure the tables in the database in such a way that they can 
be read quickly and efficiently.

> **GaryPersona:** Application scalability and a responsive UI are
> often explicit goals when people choose to implement the CQRS pattern.
> Optimizing the read-side to provide fast responses with low
> resource utilization to queries will help you to achieve these goals.

> **JanaPersona:** A normalized database schema can fail to provide
> adequate response times because of the excessive table JOIN
> operations. Despite advances in relational database technology, a JOIN
> operation is still very expensive compared to a single-table read.

An important area for consideration is the interface whereby a client 
such as an MVC controller action submits a query to the read-side model. 

![Figure 2][fig2]

**The Read-side storing data in a relational database**

In figure 2, a client such as an MVC controller action invokes a method 
on a **ViewRepository** class to request the data that it needs. The 
**ViewRepository** class in turn runs a query against the de-normalized 
data in the database. 

> **JanaPersona:** The Repository pattern mediates between the domain
> and data mapping layers using a collection-like interface for
> accessing domain objects. For more info see Martin Fowler, Catalog of
> Patterns of Enterprise Application Architecture,
> [Repository][repopattern]. 

The team at Contoso evaluated two approaches to implementing the 
**ViewRepository** class: using the **IQueryable** interface and using 
non-generic data access objects (DAOs). 

#### Using the **IQueryable** interface

One approach to consider for the **ViewRepository** class is to have it 
return an **IQueryable** instance that enables the client to use LINQ to 
specify its query. It is very easy to return an **IQueryable** instance 
from many ORMs such as Entity Framework or NHibernate. The following 
code snippet illustrates how the client can submit such queries. 

```Cs
var ordersummary = repository.Query<OrderSummary>().Where(LINQ query to retrieve order summary);
var orderdetails = repository.Query<OrderDetails>().Where(LINQ query to retrieve order details);
```

This approach has a number of advantages:

* **Simplicity #1.** This approach uses a thin abstraction layer over 
  the underlying database. Many ORMs suport this approach and it
  minimizes the amount of code that you must write. 
* **Simplicity #2.** You only need to define a single repository and a 
  single **Query** method. 
* **Simplicity #3.** You don't need a separate query object. On the 
  read-side, the queries should be simple because you have already 
  de-normalized the data from the write-side to support the read-side
  clients. 
* **Simplicity #4.** You can make use of LINQ to provide support for 
  features such as filtering, paging, and sorting in the client. 
* **Testability.** You can use LINQ to Objects for mocking. 

> **MarkusPersona:** In the RI, using Entity Framework, we didn't need
> to write any code at all to expose the **IQueryable** instance. We
> also had just a single **ViewRepository** class. 

Possible objections to this approach include:

* It is not easy to replace the data store with a non-relational 
  database (that does not expose an **IQueryable** object. However, you
  can choose to implement the read-model differently in each bounded
  context using an approach that is appropriate to that bounded context. 
* The client might abuse the **IQueryable** interface be performing 
  operations that can be done more efficiently as a part of the 
  de-normalization process. You should ensure that the de-normalized
  data fully meets the requirements of the clients. 
* Using the **IQueryable** interface hides the queries away. However, 
  since you de-normalize the data from the write-side, the queries 
  against the relational database tables are unlikely to be complex. 
* It's hard to know if your integration tests cover all the different
  uses of the **Query** method.

#### Using non-generic DAOs

An alternative approach is to have the **ViewRepository** expose custom 
**Find** and **Get** methods as shown in the following code snippets. 

```Cs
var ordersummary = dao.FindAllSummarizedOrders(userId);
var orderdetails = dao.GetOrderDetails(orderId);
```

You could also choose to use different DAO classes. This would make it 
easier to access different data sources. 

```Cs
var ordersummary = OrderSummaryDAO.FindAll(userId);
var orderdetails = OrderDetailsDAO.Get(orderId);
```

This approach has a number of advantages:

* **Simplicity #1.** Dependencies are clearer for the client. For 
  example, the client references an explicit **IOrderSummaryDAO**
  instance rather than a generic **IViewRepository** instance. 
* **Simplicity #2.** For the majority of queries, there are only one or 
  two predefined ways to access the object. Different queries typically 
  return different projections. 
* **Flexibility #1.** The **Get** and **Find** methods hide details such 
  as the partitioning of the data store and the data access methods such 
  as an ORM or executing SQL code explicitly. This makes it easier to 
  change these choices in the future. 
* **Flexibility #2.** The **Get** and **Find** methods could use an ORM, 
  LINQ, and the **IQueryable** interface behind the scenes to get the
  data from the data store. This is a choice that you could make on a
  method-by-method basis. 
* **Performance #1.** You can easily optimize the queries that the 
  **Find** and **Get** methods run. 
* **Performance #2.** The data access layer executes all queries. There 
  is no risk that the client MVC controller action tries to run complex 
  and inefficient LINQ queries against the data source. 
* **Testability.** It is easier to specify unit tests for the **Find** 
  and **Get** methods than to create suitable unit tests for the range
  of possible LINQ queries that a client could specify. 
* **Maintainability.** All of the queries are defined in the same
  location, the DAO classes, making it easier to modify the system
  consistently.

Possible objections to this approach include:

* Using the **IQueryable** interface makes it much easier to use grids 
  that support features such as paging, filtering, and sorting in the
  UI. However, if the developers are aware of this downside and are
  committed to delivering a task-based UI, then this should not be an
  issue.
  
The team decided to adopt the second approach because of the clarity it 
brings to the code; in this context, we did not see any significant 
advantage in the flexibility of the approach that uses the 
**IQueryable** interface. For examples, see the **ConferenceDao** and 
**OrderDao** classes in the **Registration** project. 

## Making information about partially fulfilled orders available to the read-side

The UI displays data about orders that it obtains by querying the model 
on the read-side. Part of the data that the UI displays to the 
Registrant is information about partially fulfilled orders: for each 
seat type in the order, the number of seats requested and the number of 
seats that are available. This is temporary data that the system only
uses while the Registrant is creating the order using the UI; the 
business only needs to store information about seats that were actually
purchased, not the difference between what the Registrant requested and 
what the Registrant purchased. 

The consequence of this is that the information about how many seats 
the Registrant requested only needs to exist in the model on the 
read-side. 

> **JanaPersona:** You can't store this information in an HTTP session
> because the Registrant may leave the site in between requesting the
> seats and completing the order.

A further consequence is that the underlying storage on the read-side 
cannot be simple SQL views because it includes data that is not stored 
in the underlying table storage on the write-side. Therefore you must
pass this information to the read-side using events. 

Figure 3 shows all the commands and events that the **Order** and 
**SeatsAvailability** aggregates use and how the **Order** aggregate 
pushes changes to the read-side by raising events. 

![Figure 3][fig3]

**The new architecture of the reservation process**

The **OrderViewModelGenerator** class handles the **OrderPlaced**, 
**OrderUpdated**, **OrderPartiallyReserved**, 
**OrderRegistrantAssigned**, and **OrderReservationCompleted** events 
and uses **DraftOrder** and **DraftOrderItem** instances to persist 
changes to the view tables. 

> **GaryPersona:** If you look ahead to the next chapter, [Preparing
> for the V1 Release][j_chapter5], you'll see that the team extended the
> use of events and migrated the **Orders and Registrations** bounded
> context to use event sourcing. 

## CQRS command validation

When you implement the write-model, you should try to ensure that 
commands very rarely fail. This gives the best user experience, and 
makes it much easier to implement the asynchronous behavior in your 
application. 

One approach, adopted by the team, is to use the model validation 
features in ASP.NET MVC. 

You should be careful to distinguish between errors and business
failures. Examples of errors include: 

* A message is not delivered due to a failure in the messaging
  infrastructure.
* Data is not persisted due to a connectivity problem with the
  database.

In many cases, especially in the cloud, you can handle these errors by
retrying the operation.

> **MarkusPersona:** The Transient Fault Handling Application Block from
> Microsoft patterns & practices is designed to make it easier to
> implement consistent retry behavior for any transient faults. It comes
> with a set of built-in detection strategies for SQL Database, Windows
> Azure storage, Windows Azure Caching, and Windows Azure Service Bus,
> and it also allows you to define your own strategies. Similarly, it
> comes with a set of handy built-in retry policies and supports custom
> ones. For more information, see [The Transient Fault Handling
> Application Block][tfhab].

A business failure should have a predetermined business response. For
 example:

* If the system cannot reserve a seat because there are no seats left, 
  then it should add the request to a wait-list.
* If a credit card payment fails, the user should be given the chance
  either to try a different card, or to set up payment by invoice.

> **GaryPersona:** Your domain experts should help you to identify
> possible business failures and determine the way that you handle
> them: either using an automated process or manually.

## The countdown timer and the read-model

The countdown timer that displays how much time remains to complete the 
order to the Registrant is part of the business data in the system, and 
not just a part of the infrastructure. When a Registrant creates an 
order and reserves seats, the countdown begins. The countdown 
continues, even if the Registrant leaves the conference web site. The UI 
must be able to display the correct countdown value if the Registrant 
returns to the site, therefore the reservation expiry time is a part of 
the data that is available from the read-model. 

# Implementation details 

This section describes some of the significant features of the 
implementation of the Orders and Registrations bounded context. You may 
find it useful to have a copy of the code so you can follow along. You 
can download a copy of the code from the [Download center][downloadc], 
or check the evolution of the code in the repository on github: 
[mspnp/cqrs-journey-code][repourl]. 

> **Note:** Do not expect the code samples to match exactly the code in
> the reference implementation. This chapter describes a step in the
> CQRS journey, the implementation may well change as we learn more and
> refactor the code.

## The Order Access Code record locator 

A Registrant may need to retrieve an Order, either to view it, or to 
complete assigning Attendees to seats. This may happen in a different 
web session, so the Registrant must supply some information to locate 
the previously saved order. 

The following code sample shows how the **Order** class generates an new 
five character order access code that is persisted as part of the 
**Order** instance. 

```Cs
public string AccessCode { get; set; }

protected Order()
{
	...
	this.AccessCode = HandleGenerator.Generate(5);
}
```

To retrieve an **Order** instance, a Registrant must provide her email 
address and the order access code. The system will use these two items 
to locate the correct order. This logic is part of the read-side. 

The following code sample from the **OrderController** class in the web 
application shows how the MVC controller submits the query to the read 
side using the **LocateOrder** method to discover the unique **OrderId** 
value. This **Find** action passes the **OrderId** value to a 
**Display** action that displays the order information to the 
Registrant. 

```Cs
[HttpPost]
public ActionResult Find(string email, string accessCode)
{
    var orderId = orderDao.LocateOrder(email, accessCode);

    if (!orderId.HasValue)
    {
        return RedirectToAction("Find", new { conferenceCode = this.ConferenceCode });
    }

    return RedirectToAction("Display", new { conferenceCode = this.ConferenceCode, orderId = orderId.Value });
}
```

## The countdown timer

When a Registrant creates an order and makes a seat reservation, those 
seats are reserved for a fixed period of time. The 
**RegistrationProcessManager** instance, which forwards the reservation from 
the **SeatsAvailability** aggregate, passes the time that the 
reservation expires to the **Order** aggregate. The following code 
sample shows how the **Order** aggregate receives and stores the 
reservation expiry time. 

```Cs
public DateTime? ReservationExpirationDate { get; private set; }

public void MarkAsReserved(DateTime expirationDate, IEnumerable<SeatQuantity> seats)
{
    ...

    this.ReservationExpirationDate = expirationDate;
    this.Items.Clear();
    this.Items.AddRange(seats.Select(seat => new OrderItem(seat.SeatType, seat.Quantity)));
}

```

> **MarkusPersona:** The **ReservationExpirationDate** is intially set
> in the **Order** constructor to a time 15 minutes after the **Order**
> is instantiated. The **RegistrationProcessManager** class may revise this
> time based on when the reservations are actually made. It is this time
> the process manager sends to the **Order** aggregate in the
> **MarkSeatsAsReserved** command.

When the **RegistrationProcessManager** sends the **MarkSeatsAsReserved** 
command to the **Order** aggregate with the expiry time that the UI will 
display, it also sends a command to itself to initate the 
process of releasing the reserved seats. This 
**ExpireRegistrationProcess** command is held for the expiry duration 
plus a buffer of five minutes. This buffer ensures that time differences 
between the servers don't cause the **RegistrationProcessManager** class to 
release the reserved seats before the timer in UI counts down to zero. 
In the following code sample from the **RegistrationProcessManager** class, 
the UI uses the **Expiration** property in the **MarkSeatsAsReserved**
to display the countdown timer, and the **Delay** 
property in the **ExpireRegistrationProcess** command determines when 
the reserved seats are released. 

```Cs
public void Handle(SeatsReserved message)
{
    if (this.State == ProcessState.AwaitingReservationConfirmation)
    {
        var expirationTime = this.ReservationAutoExpiration.Value;
        this.State = ProcessState.ReservationConfirmationReceived;

        if (this.ExpirationCommandId == Guid.Empty)
        {
            var bufferTime = TimeSpan.FromMinutes(5);

            var expirationCommand = new ExpireRegistrationProcess { ProcessId = this.Id };
            this.ExpirationCommandId = expirationCommand.Id;

            this.AddCommand(new Envelope<ICommand>(expirationCommand)
            {
                Delay = expirationTime.Subtract(DateTime.UtcNow).Add(bufferTime),
            });
        }


        this.AddCommand(new MarkSeatsAsReserved
        {
            OrderId = this.OrderId,
            Seats = message.ReservationDetails.ToList(),
            Expiration = expirationTime,
        });
    }
	
    ...
}
```

The MVC **RegistrationController** class retrieves the order information 
on the read-side. The **DraftOrder** class includes the reservation 
expiry time that the controller passes to the view using the **ViewBag** 
class, as shown in the following code sample. 

```Cs
[HttpGet]
public ActionResult SpecifyRegistrantDetails(string conferenceCode, Guid orderId)
{
	var repo = this.repositoryFactory();
	using (repo as IDisposable)
	{
		var draftOrder = repo.Find<DraftOrder>(orderId);
		var conference = repo.Query<Conference>()
			.Where(c => c.Code == conferenceCode)
			.FirstOrDefault();

		this.ViewBag.ConferenceName = conference.Name;
		this.ViewBag.ConferenceCode = conference.Code;
		this.ViewBag.ExpirationDateUTCMilliseconds = draftOrder.BookingExpirationDate.HasValue ? ((draftOrder.BookingExpirationDate.Value.Ticks - EpochTicks) / 10000L) : 0L;
		this.ViewBag.OrderId = orderId;

		return View(new AssignRegistrantDetails { OrderId = orderId });
	}
}
```

The MVC view then uses Javascript to display an animated countdown 
timer. 

## Using ASP.NET MVC validation for commands

You should try to ensure that any commands that the MVC controllers in 
your application send to the write-model will succeed. You can use the 
features in MVC to validate the commands both client-side and 
server-side before sending them to the write-model. 

> **MarkusPersona:** Client-side validation is primarily a convenience
> to the user that avoids the need to for round trips to the server to
> help the user complete a form correctly. You still need server-side
> validation to ensure that the data is validated before it is forwarded
> to the write-model.

The following code sample shows the **AssignRegistrantDetails** command 
class that uses **DataAnnotations** to specify the validation 
requirements; in this example, that the **FirstName**, **LastName**, and 
**Email** fields are not empty. 

```Cs
using System;
using System.ComponentModel.DataAnnotations;
using Common;

public class AssignRegistrantDetails : ICommand
{
	public AssignRegistrantDetails()
	{
		this.Id = Guid.NewGuid();
	}

	public Guid Id { get; private set; }

	public Guid OrderId { get; set; }

	[Required(AllowEmptyStrings = false)]
	public string FirstName { get; set; }

	[Required(AllowEmptyStrings = false)]
	public string LastName { get; set; }

	[Required(AllowEmptyStrings = false)]
	public string Email { get; set; }
}
```

The MVC view uses this command class as its model class. The following 
code sample from the **SpecifyRegistrantDetails.cshtml** file shows how 
the model is populated. 

```HTML
@model Registration.Commands.AssignRegistrantDetails

...

<div class="editor-label">@Html.LabelFor(model => model.FirstName)</div><div class="editor-field">@Html.EditorFor(model => model.FirstName)</div>
<div class="editor-label">@Html.LabelFor(model => model.LastName)</div><div class="editor-field">@Html.EditorFor(model => model.LastName)</div>
<div class="editor-label">@Html.LabelFor(model => model.Email)</div><div class="editor-field">@Html.EditorFor(model => model.Email)</div>

```

The **Web.config** file configures the client-side validation based on 
the **DataAnnotations** attributes as shown in the following snippet. 

```XML
<appSettings>
    ...
    <add key="ClientValidationEnabled" value="true" />
    <add key="UnobtrusiveJavaScriptEnabled" value="true" />
</appSettings>
```

The server-side validation occurs in the controller before it sends the
command. The following code sample from the **RegistrationController** 
class shows how the controller uses the **IsValid** property to validate 
the command. Remember that this example uses an instance of the command 
as the model. 

```Cs
[HttpPost]
public ActionResult SpecifyRegistrantDetails(string conferenceCode, Guid orderId, AssignRegistrantDetails command)
{
    if (!ModelState.IsValid)
    {
        return SpecifyRegistrantDetails(conferenceCode, orderId);
    }

    this.commandBus.Send(command);

    return RedirectToAction("SpecifyPaymentDetails", new { conferenceCode = conferenceCode, orderId = orderId });
}
```

For an additional example, see the **RegisterToConference** command and 
the **StartRegistration** action in the **RegistrationController** 
class. 

For more information, see [Models and Validation in ASP.NET 
MVC][modelvalidation] on MSDN. 

## Pushing changes to the read-side

Some information about orders only needs to exist on the read-side. In 
particular, the information about partially fulfilled orders is only 
used in the UI and is not part of the business information persisted by 
the domain model on the write-side. 

This means that the system can't use SQL views as the underlying storage 
mechanism on the read-side because views cannot contain data that does 
not exist in the tables that they are based on. 

The system stores the de-normalized order data in two tables in a SQL 
database: the **OrdersView** and **OrderItemsView** tables. The 
**OrderItemsView** table includes the **RequestedSeats** column that 
contains data that only exists on the read-side. 

<table border="1">
	<tr><th>Column</th><th>Description</th></tr>
	<tr><td>OrderId</td><td>A unique identifier for the Order</td></tr>
	<tr><td>ReservationExpirationDate</td><td>The time when the seat reservations expire</td></tr>
	<tr><td>StateValue</td><td>The state of the Order: Created, PartiallyReserved, ReservationCompleted, Rejected, Confirmed</td></tr>
	<tr><td>RegistrantEmail</td><td>The email address of the Registrant</td></tr>
	<tr><td>AccessCode</td><td>The Access Code that the Registrant can use to access the Order</td></tr>
</table>

**OrdersView Table**

<table border="1">
	<tr><th>Column</th><th>Description</th></tr>
	<tr><td>OrderItemId</td><td>A unique identifier for the Order Item</td></tr>
	<tr><td>SeatType</td><td>The type of Seat requested</td></tr>
	<tr><td>RequestedSeats</td><td>The number of seats requested</td></tr>
	<tr><td>ReservedSeats</td><td>The number of seats reserved</td></tr>
	<tr><td>OrderID</td><td>The OrderId in the parent OrdersView table</td></tr>
</table>

**OrderItemsView Table**

To populate these tables in the read-model, the read-side handles events 
raised by the write-side and uses them to write to these tables. See
Figure 3 above for more details.

The **OrderViewModelGenerator** class handles these events and updates
the read-side repository.

```Cs
public class OrderViewModelGenerator :
    IEventHandler<OrderPlaced>, IEventHandler<OrderUpdated>,
    IEventHandler<OrderPartiallyReserved>, IEventHandler<OrderReservationCompleted>,
    IEventHandler<OrderRegistrantAssigned>
{
    private readonly Func<ConferenceRegistrationDbContext> contextFactory;

    public OrderViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public void Handle(OrderPlaced @event)
    {
        using (var context = this.contextFactory.Invoke())
        {
            var dto = new DraftOrder(@event.SourceId, DraftOrder.States.Created)
            {
                AccessCode = @event.AccessCode,
            };
            dto.Lines.AddRange(@event.Seats.Select(seat => new DraftOrderItem(seat.SeatType, seat.Quantity)));

            context.Save(dto);
        }
    }

    public void Handle(OrderRegistrantAssigned @event)
    {
        ...
    }

    public void Handle(OrderUpdated @event)
    {
        ...
    }

    public void Handle(OrderPartiallyReserved @event)
    {
        ...
    }

    public void Handle(OrderReservationCompleted @event)
    {
        ...
    }

    ...
}
```

The following code sample shows the **ConferenceRegistrationDbContext** 
class. 

```Cs
public class ConferenceRegistrationDbContext : DbContext
{
    ...

    public T Find<T>(Guid id) where T : class
    {
        return this.Set<T>().Find(id);
    }

    public IQueryable<T> Query<T>() where T : class
    {
        return this.Set<T>();
    }

    public void Save<T>(T entity) where T : class
    {
        var entry = this.Entry(entity);

        if (entry.State == System.Data.EntityState.Detached)
            this.Set<T>().Add(entity);

        this.SaveChanges();
    }
}
```

> **JanaPersona:** Notice that this **ConferenceRegistrationDbContext**
> in the read-side includes a **Save** method to persist the changes
> sent from the write-side and handled by the
> **OrderViewModelGenerator** handler class. 

## Querying the read-side

The following code sample shows a non-generic DAO class that the MVC controllers use to query for conference information on the read-side. It wraps the **ConferenceRegistrationDbContext** class shown previously.

```Cs
public class ConferenceDao : IConferenceDao
{
    private readonly Func<ConferenceRegistrationDbContext> contextFactory;

    public ConferenceDao(Func<ConferenceRegistrationDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public ConferenceDetails GetConferenceDetails(string conferenceCode)
    {
        using (var context = this.contextFactory.Invoke())
        {
            return context
                .Query<Conference>()
                .Where(dto => dto.Code == conferenceCode)
                .Select(x => new ConferenceDetails { Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description, StartDate = x.StartDate })
                .FirstOrDefault();
        }
    }

    public ConferenceAlias GetConferenceAlias(string conferenceCode)
    {
        ...
    }

    public IList<SeatType> GetPublishedSeatTypes(Guid conferenceId)
    {
        ...
    }
}
```

> **JanaPersona:** Notice how this **ConferenceDao** class only contains
> methods that return data. It is is used by the MVC controllers to
> retrieve data to display in the UI.

## Refactoring the SeatsAvailability aggregate

In the first stage of our CQRS, the domain included a 
**ConferenceSeatsAvailabilty** aggregate root class that modeled the 
number of seats remaining for a conference. In this stage of the 
journey, the team replaced the **ConferenceSeatsAvailabilty** aggregate 
with a **SeatsAvailability** aggregate to reflect the fact that there 
may be multiple seat types available at a particular conference; for 
example, full conference seats, pre-conference workshop seats, and 
cocktail party seats. Figure 4 shows the new **SeatsAvailability** 
aggregate and its constituent classes. 

![Figure 4][fig4]

**The SeatsAvailability aggregate and its associated commands and events.**

This aggregate now models the following facts:

* There may be multiple seat types at a conference.
* There may be different numbers of seats available for each seat type.

The domain now includes a **SeatQuantity** value type that you can use 
to represent a quantity of a particular seat type. 

Previously, the aggregate raised either a **ReservationAccepted** or 
a **ReservationRejected** event depending on whether there were sufficient 
seats. Now the aggregate raises a **SeatsReserved** event that reports 
how many seats of a particular type it could reserve. This means that 
the number of seats reserved may not match the number of seats 
requested; this information is passed back to the UI for the Registrant 
to make a decision on how to proceed with the registration. 

### The AddSeats method

You may have noticed in Figure 3 that the **SeatsAvailability** 
aggregate includes an **AddSeats** method with no corresponding command. 
The **AddSeats** method adjusts the total number of available seats of a 
given type. The Business Customer is responsible for making any such 
adjustments, and does this in the Conference Management bounded context. 
The Conference Management bounded context raises an event whenever the 
total number of available seats changes, the **SeatsAvailability** class 
then handles the event when its handler invokes the **AddSeats** method. 

# Impact on testing

This section discusses some of the testing issues addressed during this 
stage of the journey. 

## Acceptance tests and the domain expert

In [Chapter 3, Orders and Registrations Bounded Context][j_chapter3], 
you saw some of the UI mockups that the developers and the domain expert 
worked on together to refine some of the functional requirements for the 
system. One of the planned uses for these UI mockups was to form the 
basis of a set of acceptance tests for the system. 

The team had the following goals for their acceptance testing approach:

* That the acceptance tests should be expressed clearly and
  unambiguously in a format that the domain expert could understand.
* That it should be possible to execute the acceptance tests
  automatically.

To achieve these goals the domain expert paired with a member of the 
test team and used [SpecFlow][specflow] to specify the core acceptance 
tests. 

### Defining acceptance tests using SpecFlow features

The first step is to define the acceptance tests using the SpecFlow 
notation. These tests are saved as feature files in a Visual Studio 
project. The following code sample from the 
**ConferenceConfiguration.feature** file in the 
Features\UserInterface\Views\Management folder shows an acceptance test 
for the Conference Management bounded context. A typical SpecFlow test 
scenario consists of a collection of **Given**, **When**, and **Then** 
statements. Some of these statements include the data that the test is 
uses.

> **MarkusPersona:** SpecFlow feature files in fact use the Gherkin
> language; a domain specific language (DSL) created especially for
> behavior descriptions.

```
Feature:  Conference configuration scenarios for creating and editing Conference settings
    In order to create or update a Conference configuration
    As a Business Customer
    I want to be able to create or update a Conference and set its properties


Background: 
Given the Business Customer selected the Create Conference option

Scenario: An existing unpublished Conference is selected and published
Given this conference information
| Owner         | Email                    | Name      | Description                             | Slug   | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012P | CQRS summit 2012 conference (Published) | random | 05/02/2012 | 05/12/2012 |
And the Business Customer proceed to create the Conference
When the Business Customer proceed to publish the Conference
Then the state of the Conference change to Published

Scenario: An existing Conference is edited and updated
Given an existing published conference with this information
| Owner         | Email                    | Name      | Description                            | Slug   | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012U | CQRS summit 2012 conference (Original) | random | 05/02/2012 | 05/12/2012 |
And the Business Customer proceed to edit the existing settigns with this information
| Description                           |
| CQRS summit 2012 conference (Updated) |
When the Business Customer proceed to save the changes
Then this information is show up in the Conference settings
| Description                           |
| CQRS summit 2012 conference (Updated) |

...

```

> **CarlosPersona:** I found these acceptance tests were a great way for
> me to clarify my definitions of the expected behavior of the system to
> the developers.

For additional examples, see the **Conference.AcceptanceTests** Visual
Studio solution file included with the downloadable source. 

### Making the tests executable

An acceptance test in a feature file is not directly executable: you 
must provide some plumbing code to bridge the gap between the SpecFlow 
feature file and your application. 

For examples of implementations, see the classes in the **Steps** 
folder in the **Conference.Specflow** project in the 
**Conference.AcceptanceTests** solution. 

These step implementations use two different approaches.

The first approach runs the test by simulating a user of the system. It 
does this by driving a web browser directly using the [WatiN][watin] 
open source library. The advantages of this approach are that it 
exercises the system in exactly the same way that a real user would 
interact with the system and that it is simple implement initially. 
However, these tests are fragile and will require a considerable 
maintenance effort to keep them up to date as the UI and system change. The 
following code sample shows an example of this approach, defining some 
of the **Given**, **When**, and **Then** steps from the feature file 
shown previously. SpecFlow uses the **Given**, **When**, and **Then** 
attributes to link the steps to the clauses in the feature file and to 
pass parameter values to step methods: 

```Cs
public class ConferenceConfigurationSteps : StepDefinition
{
    ...

    [Given(@"the Business Customer proceed to edit the existing settigns with this information")]
    public void GivenTheBusinessCustomerProceedToEditTheExistingSettignsWithThisInformation(Table table)
    {
        Browser.Click(Constants.UI.EditConferenceId);
        PopulateConferenceInformation(table);
    }

    [Given(@"an existing published conference with this information")]
    public void GivenAnExistingPublishedConferenceWithThisInformation(Table table)
    {
        ExistingConferenceWithThisInformation(table, true);
    }

    private void ExistingConferenceWithThisInformation(Table table, bool publish)
    {
        NavigateToCreateConferenceOption();
        PopulateConferenceInformation(table, true);
        CreateTheConference();
        if(publish) PublishTheConference();

        ScenarioContext.Current.Set(table.Rows[0]["Email"], Constants.EmailSessionKey);
        ScenarioContext.Current.Set(Browser.FindText(Slug.FindBy), Constants.AccessCodeSessionKey);
    }

    ...

    [When(@"the Business Customer proceed to save the changes")]
    public void WhenTheBusinessCustomerProceedToSaveTheChanges()
    {
        Browser.Click(Constants.UI.UpdateConferenceId);
    }

    ...

    [Then(@"this information is show up in the Conference settings")]
    public void ThenThisInformationIsShowUpInTheConferenceSettings(Table table)
    {
        Assert.True(Browser.SafeContainsText(table.Rows[0][0]),
                        string.Format("The following text was not found on the page: {0}", table.Rows[0][0]));
    }

    private void PublishTheConference()
    {
        Browser.Click(Constants.UI.PublishConferenceId);
    }

    private void CreateTheConference()
    {
        ScenarioContext.Current.Browser().Click(Constants.UI.CreateConferenceId);
    }
        
    private void NavigateToCreateConferenceOption()
    {
        // Navigate to Registration page
        Browser.GoTo(Constants.ConferenceManagementCreatePage);
    }

    private void PopulateConferenceInformation(Table table, bool create = false)
    {
        var row = table.Rows[0];

        if (create)
        {
            Browser.SetInput("OwnerName", row["Owner"]);
            Browser.SetInput("OwnerEmail", row["Email"]);
            Browser.SetInput("name", row["Email"], "ConfirmEmail");
            Browser.SetInput("Slug", Slug.CreateNew().Value);
        }

        Browser.SetInput("Tagline", Constants.UI.TagLine);
        Browser.SetInput("Location", Constants.UI.Location);
        Browser.SetInput("TwitterSearch", Constants.UI.TwitterSearch);

        if (row.ContainsKey("Name")) Browser.SetInput("Name", row["Name"]);
        if (row.ContainsKey("Description")) Browser.SetInput("Description", row["Description"]);
        if (row.ContainsKey("Start")) Browser.SetInput("StartDate", row["Start"]);
        if (row.ContainsKey("End")) Browser.SetInput("EndDate", row["End"]);
    }
}
```

You can see how this approach simulates clicking on, and entering text 
into, UI elements in the web browser. 

The second approach is to implement the tests by interacting with the 
MVC controller classes. In the longer-term, this approach will be less 
fragile at the cost of an initially more complex implementation that 
requires some knowledge of the internal implementation of the system. 
The following code samples show an example of this approach.

First, an example scenario from the 
**SelfRegistrationEndToEndWithControllers.feature** file in the 
Features\UserInterface\Controllers\Registration project folder: 

```
Scenario: End to end Registration implemented using controllers
	Given the Registrant proceed to make the Reservation
	And these Order Items should be reserved
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
	And these Order Items should not be reserved
	| seat type     |
	| CQRS Workshop |
	And the Registrant enter these details
	| first name | last name | email address            |
	| Gregory    | Weber     | gregoryweber@contoso.com |
	And the Registrant proceed to Checkout:Payment
	When the Registrant proceed to confirm the payment
	Then the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
	And the Registrant assign these seats
	| seat type                 | first name | last name | email address       |
	| General admission         | William    | Weber     | William@Weber.com   |
	| Additional cocktail party | Jim        | Gregory   | Jim@Gregory.com     |
	And these seats are assigned
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
```

Second, some of the step implementations from the 
**SelfRegistrationEndToEndWithControllersSteps** class: 

```Cs
[Given(@"the Registrant proceed to make the Reservation")]
public void GivenTheRegistrantProceedToMakeTheReservation()
{
    var redirect = registrationController.StartRegistration(
        registration, registrationController.ViewBag.OrderVersion) as RedirectToRouteResult;

    Assert.NotNull(redirect);

    // Perform external redirection
    var timeout =  DateTime.Now.Add(Constants.UI.WaitTimeout);

    while (DateTime.Now < timeout && registrationViewModel == null)
    {
        //ReservationUnknown
        var result = registrationController.SpecifyRegistrantAndPaymentDetails(
            (Guid)redirect.RouteValues["orderId"], registrationController.ViewBag.OrderVersion);

        Assert.IsNotType<RedirectToRouteResult>(result);
        registrationViewModel = RegistrationHelper.GetModel<RegistrationViewModel>(result);
    }

    Assert.False(registrationViewModel == null, "Could not make the reservation and get the RegistrationViewModel");
}

...

[When(@"the Registrant proceed to confirm the payment")]
public void WhenTheRegistrantProceedToConfirmThePayment()
{
    using (var paymentController = RegistrationHelper.GetPaymentController())
    {
        paymentController.ThirdPartyProcessorPaymentAccepted(
            conferenceInfo.Slug, (Guid) routeValues["paymentId"], " ");
    }
}

...

[Then(@"the Order should be created with the following Order Items")]
public void ThenTheOrderShouldBeCreatedWithTheFollowingOrderItems(Table table)
{
    draftOrder = RegistrationHelper.GetModel<DraftOrder>(registrationController.ThankYou(registrationViewModel.Order.OrderId));
    Assert.NotNull(draftOrder);

    foreach (var row in table.Rows)
    {
        var orderItem = draftOrder.Lines.FirstOrDefault(
            l => l.SeatType == conferenceInfo.Seats.First(s => s.Description == row["seat type"]).Id);

        Assert.NotNull(orderItem);
        Assert.Equal(Int32.Parse(row["quantity"]), orderItem.ReservedSeats);
    }
}
```

You can see how this approach uses the **RegistrationController** MVC 
class directly. 

> **Note:** In these code samples, you can see how the values in
> the attributes link the step implementation to the statements in the
> related SpecFlow feature files.

The team chose to implement these steps as [xUnit.net][xunit] tests. To 
run these tests within Visual Studio, you can use any of the test 
runners supported by xUnit.net such as ReSharper, CodeRush, and 
TestDriven.NET. 

> **JanaPersona:** Remember that these acceptance tests are not the only
> tests performed on the system. The main solution includes
> comprehensive unit and integration tests, and the test team also
> performed exploratory and performance testing on the application.

## Using tests to help developers understand message flows

A common comment about implementations that use the CQRS pattern or that 
use messaging extensively is the difficulty in understanding how all of 
the different pieces of the application fit together through sending and 
receiving commands and events. You can help someone to understand your 
code base through appropriately designed unit tests. 

Consider this first example of a unit test for the **Order** aggregate:

```Cs
public class given_placed_order
{
    ...

    private Order sut;

    public given_placed_order()
    {
        this.sut = new Order(
            OrderId, new[] 
            {
                new OrderPlaced 
                { 
                    ConferenceId = ConferenceId,
                    Seats = new[] { new SeatQuantity(SeatTypeId, 5) },
                    ReservationAutoExpiration = DateTime.UtcNow
                }
            });
    }

    [Fact]
    public void when_updating_seats_then_updates_order_with_new_seats()
    {
        this.sut.UpdateSeats(new[] { new OrderItem(SeatTypeId, 20) });

        var @event = (OrderUpdated)sut.Events.Single();
        Assert.Equal(OrderId, @event.SourceId);
        Assert.Equal(1, @event.Seats.Count());
        Assert.Equal(20, @event.Seats.ElementAt(0).Quantity);
    }

    ...
}
``` 

This unit test creates an **Order** instance and directly invokes the 
**UpdateSeats** method. It does not provide any information to the 
person reading the test code about the command or event that causes this 
method to be invoked. 

Now consider this second example that performs the same test, but in 
this case it does so by sending a command: 

```Cs
public class given_placed_order
{
    ...
    
    private EventSourcingTestHelper<Order> sut;

    public given_placed_order()
    {
        this.sut = new EventSourcingTestHelper<Order>();
        this.sut.Setup(new OrderCommandHandler(sut.Repository, pricingService.Object));

        this.sut.Given(
                new OrderPlaced 
                { 
                    SourceId = OrderId,
                    ConferenceId = ConferenceId,
                    Seats = new[] { new SeatQuantity(SeatTypeId, 5) },
                    ReservationAutoExpiration = DateTime.UtcNow
                });
    }

    [Fact]
    public void when_updating_seats_then_updates_order_with_new_seats()
    {
        this.sut.When(new RegisterToConference { ConferenceId = ConferenceId, OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 20) }});

        var @event = sut.ThenHasSingle<OrderUpdated>();
        Assert.Equal(OrderId, @event.SourceId);
        Assert.Equal(1, @event.Seats.Count());
        Assert.Equal(20, @event.Seats.ElementAt(0).Quantity);
    }
    
    ...
}
```

This example uses a helper class that enables you to send a command to 
the **Order** instance. Now someone reading the test can see that when 
you send a **RegisterToConference** command you expect to see an 
**OrderUpdated** event. 

# A journey into code comprehension

*A tale of pain, relief, and learning*

This section describes the journey taken by Josh Elster, a member of the 
CQRS Advisory Board, as he explored the source code of the Contoso 
Conference Management System. 

## Testing is important

I've once believed that well-factored applications are easy to 
comprehend, no matter how large or broad the code base. Any time I had a 
problem understanding how some feature of an application behaved, the 
fault would lie with the code and not in me. 

Never let your ego get in the way of common sense. 

Truth was, up until a certain point in my career, I simply hadn't had 
exposure to a large, well-factored code base. I wouldn't have known what 
one looked like if it walked up and hit me in the face. Thankfully, as I 
got more experienced reading code, I learned to recognize the 
difference. 

> **Note:** In any well-organized project, tests are a cornerstone of
> comprehension for developers seeking to understanding of the project.
> Topics ranging from naming conventions and coding styles to design
> approaches and usage patterns are baked into test suites, providing an
> excellent starting spot for integrating into a codebase. It's also
> good practice in code literacy - and practice makes perfect!

My first action after cloning the Conference code was to skim the tests. 
After a perusal of the integration and unit test suites in the 
Conference Visual Studio solution, I focused my attention on the 
Conference.AcceptanceTests Visual Studio solution that contains the 
[SpecFlow][specflow] acceptance tests. Other members of the project team 
had done some initial work on the '.feature' files, which worked out 
nicely for me since I wasn't intimiate with the business rules. 
Implementing step bindings for these features would be an excellent way 
to both contribute to the project and to learn about how the system 
worked. 

## Domain tests

My goal then was to take a feature file looking something like this:

```
    Feature: Self Registrant scenarios for making a Reservation for a Conference site with all Order Items initially available
	In order to reserve Seats for a conference
	As an Attendee
	I want to be able to select an Order Item from one or many of the available Order Items and make a Reservation

    Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference with the slug code SelfRegFull
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |
	And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |

    Scenario: All the Order Items are available and all get reserved
	When the Registrant proceed to make the Reservation		
	Then the Reservation is confirmed for all the selected Order Items
	And these Order Items should be reserved
		| seat type                 |
		| General admission         |
		| CQRS Workshop             |
		| Additional cocktail party |
	And the total should read $749
	And the countdown started
```
 
And bind it to code that either performs an action, creates expectations, or makes assertions:

```Cs
    [Given(@"the '(.*)' site conference")]
    public void GivenAConferenceNamed(string conference)
    {
        ...
    }
```

All at a level just below the UI, but above (and beyond) infrastructure 
concerns. Testing is tightly focused on the behavior of the overall 
solution domain, which is why I'll call these types of tests Domain 
Tests. Other terms such as BDD can be used to describe this style of 
testing.

> **JanaPersona:** These "below the UI" tests are also known as
> _subcutaneous tests_, (see [Meszaros, G., Melnik, G., Acceptance Test
> Engineering Guide][testingguide]).

It may seem a little redundant to re-write application logic already 
implemented in the web site, but there are a number of reasons why it is 
worth the time: 

1. You aren't interested (for these purposes) in testing how the website
   or any other piece of infrastructure behaves, only the domain. Unit
   and Integration -level tests will validate the correct functioning of
   that code, so there's no need to duplicate those tests.
2. When iterating stories with product owners, spending time on pure UI
   concerns can slow down the feedback cycle, reducing the quality and
   usefulness of feedback.
3. Discussing a feature in more abstract terms can lead to better
   understanding of the problem that the business is trying to solve,
   given the sometimes large impedence mismatch between people of
   varying lexicons for technological concerns.
4. Obstacles encountered in implementing the testing logic can help
   improve the system's overall design quality. Difficulty in separating
   infrastructure code from application logic is generally regarded as a
   smell. 

> **Note:** There are a whole lot more reasons why these types of tests
> are a good idea than are listed here, but these are the important ones
> for this example.

The architecture for the [Contoso Conference Management System](repourl) 
is loosely-coupled, utilizing messages to transfer commands and events 
to interested parties. Commands are routed to a single handler via a 
Command Bus, while Events are routed to their *0...N* handlers via an 
Event Bus. A bus isn't tied to any specific technology as far as 
consuming applications are concerned, allowing arbitrary implementations 
to be created and used throughout the system transparent to users. 

Another bonus when it comes to behavioral testing of a loosely-coupled 
message architecture is related to the fact that BDD (or 
similarly-styled) tests do not involve themselves with the inner 
workings of application code. They only care about the observable 
*behavior* of the application under test. This means that for the 
SpecFlow tests, we need only concern ourselves with publishing some 
commands to the bus and examining the outward results by asserting 
expected message traffic and payloads against the actual traffic/data. 

> **Note**: It's OK to use mocks and stubs with these types of tests
> where appropriate. An appropriate example would be in using a mock
> ICommandBus object instead of the AzureCommandBus type. Not
> appropriate? Mocking a domain service *in totum*. Stick to mocking
> minimally, limiting yourself to infrastructure concerns and you'll
> make your life - and your tests - a lot less stressful.

## The other side of the coin

With all of the pixels I just spent on talking how awesome and easy 
things are, where's the pain? The pain is in comprehending what goes on 
in a system. The loose coupling of the architecture has a wicked back 
edge; techniques such as Inversion of Control and Dependency Injection 
hinder code readability by their very nature, since one can never be 
sure what concrete class is being injected at a particular point without 
examing the container's initialization closely. In the journey code, 
**IProcess** marks classes representing long-running business processes 
(also known as Sagas or Process Managers) responsible for coordinating 
business logic between different Aggregates. In order to maintain the 
integrity, idempotency, and transactionality of the system's data and 
state, Processes leave the actual publishing of their issued commands to 
the individual persistence repository's implementation. Since IoC and DI 
containers hide these types of details from consumers, it and other 
properties of the system create a bit of difficulty when it comes to 
answering seemingly trivial questions such as: 

- Who issue(s)d a particular command or event?
- What class handles a particular command or event?
- Where are processes or Aggregates created/persisted?
- When is a command sent in relation to other commands/events?
- Why does the system behave the way it does?
- How does the application's state change as a result of a particuler
  command?

Because the application's dependencies are so loose, many traditional 
tools and methods of code analysis become either less useful or even 
completely useless. 

Let's take an example of this and work out some heuristics involved in 
answering these questions. We'll use as an example the 
**RegistrationProcessManager**. 

1. Open the RegistrationProcessManager.cs file, noting that, like many
   process managers it has a 
   **ProcessState** enumeration. We take note of the beginning state for
   the process, **NotStarted**. Next, we want to find code that does one
   of the following:
       - A new instance of the process is created (where are processes
	     created/persisted?)
       - The initial state is changed to a different state (how does
	     state change?)

2. Locate the first location in source code where either or both of the
   above occur. In this case, it's the **Handle** method in the 
   **RegistrationProcessManagerRouter** class. **Important:** this does NOT
   necessarily mean that the Process is a Command Handler! Process
   managers are responsible for creating/retrieving Aggregate Roots (AR)
   from storage for the purpose of routing messages to the AR, so while
   they have methods similar in naming and signature to an
   **ICommandHandler<T>** implementation, they do not implement a
   command's logic. 

3. Take note of the message type that is received as a parameter to the
   method where the state change occurs, since we now need to figure out
   where that message originated. 
       - We also note that a new command, **MakeSeatReservation**, is
         being issued by the **RegistrationProcessManager**. 
       - As mentioned above, this command isn't actually published by
         the Process issuing it; rather, publication occurs when the
         Process is saved to disk. 
       - These heuristics will need to be repeated to some degree or
         another on any commands issued as side-effects of a Process
         handling a command.

4. Do a find references on the **OrderPlaced** symbol to locate the
   (or a) top-most (external facing) component which publishes a message
   of that type via **Send** method on the ICommandBus interface.
       - Since internally-issued commands are indirectly published (by a
         Repository) on save, it may be safe to assume that any
         non-infrastructure logic which directly calls the **Send**
         method is an external point of entry. 

While there is certainly more to these heuristics than what is given, I 
think that what is there is sufficient to demonstrate the point that 
even discussing the interactions is a rather lengthy, cumbersome 
process. That makes it prone to misunderstanding without expending 
considerable effort. Comprehension of the various command/event 
messaging interactions is possible in this way, but it is not gained 
very efficiently. 

> **Note:** As a rule, a person can really only maintain between 4-8
> distinct thoughts in their head at any given time. To illustrate this
> concept, let's take a conservative count of the number of simultaneous
> items you'll need to maintain in your short-term memory while
> following the above heuristics:  
> Process type + Process state property + Initial State (NotStarted) +
> new() location + message type + intermediary routing class types + 2
> *N^n Commands issued (location, type, steps) + discrimination rules
> (logic is data too!) > 8

When infrastructure requirements get mixed into the equation, the issue 
of information saturation becomes even more apparent. Being the 
competent, capable, developers that we all are (right?), we can start 
looking for ways to optimize these steps and increase the signal to 
noise ratio of relevant information. 

To summarize, we have two problems:

1. The number of items we are forced to keep in our head is too many to
   make for efficient comprehension
2. Discussion and documentation for messaging interactions is verbose,
   error-prone, and complicated

Fortunately, it is quite possible to kill two birds with a single stone,
with MIL (Messaging Intermediate Language). 

MIL began as a series of LINQPad scripts and snippets that I created to 
help juggle all these facts while answering questions. Initially, all 
that these scripts accomplished was to reflect through one or more 
project assemblies and output the various types of messages and 
handlers. In discussions with members of the team it became apparent 
that others were experiencing the same types of problems I was. A few 
chats and brainstorming sessions with members of the patterns and 
practices team later, we came up with the idea of introducing a small 
DSL that would encapsulate the interactions being discussed. The 
tentatively-named SawMIL toolbox, located here 
[http://jelster.github.com/CqrsMessagingTools/][mil] provides utilities, 
scripts, and examples that enable you to use MIL as part of your 
development and analysis process managers. 

In MIL, messaging components and interactions are represented in a 
specific manner: commands, since they are requests for the system to 
perform some action, are denoted by '?', as in 'DoSomething?'. Events 
represent something definite that happened in the system, and hence gain 
a '!' suffix, as in 'SomethingHappened!'. 

Another important element of MIL is message publication and reception. 
Messages received from a messaging source (such as Windows Azure Service 
Bus, nServiceBus, etc) are always preceded by the '->' symbol, while 
messages that are being sent have the symbol following it. To keep the 
examples simple for now, the optional nil element, '.', is used to 
indicate explicitly a no-op (in other words, nothing is receiving the 
message). The following snippet shows an example of the nil element 
sysntax: 

```
SendCustomerInvoice? -> .  
CustomerInvoiceSent! -> .
```

Once a Command or Event has been published, something needs to do 
something with it. Commands have one and only one handler, while events 
can have multiple handlers. MIL represents this relationship between 
message and handler by placing the name of the handler on the other side 
of the messaging operation as shown in the following snippet: 

```
SendCustomerInvoice? -> CustomerInvoiceHandler  
CustomerInvoiceSent! ->  
    -> CustomerNotificationHandler  
    -> AccountsAgeingViewModelGenerator
```

Notice how the command handler is on the same line as the command while 
the event is separated from its handlers? That's because in CQRS, there 
is a 1:1 correlation between commands and command handlers. Putting them 
together helps reinforce that concept, while keeping events separate 
from event handlers helps reinforce the idea that a given event can have 
0...*N* handlers. 

Aggregate Roots are prefixed with the '@' sign, a convention that should 
be familiar to anyone who has ever used twitter. Aggregate Roots never 
handle commands, but occasionally may handle events. Aggregate Roots are 
most frequently event sources, raising events in response to business 
operations invoked on the aggregate. Something that should be made clear 
about these events however, is that in most systems there are other 
elements that decide upon and actually perform the publication of domain 
events. This is an interesting case where business and technical 
requirements blur boundaries, with the requirements being met by 
infrastructure logic rather than application or business logic. An 
example of this lies in the Journey code: in order to ensure consistency 
between event sources and event subscribers, the implementation of the 
repository which persists the Aggregate Root is the element responsible 
for actually publishing the events to a bus. The following snippet shows 
an example of the AggregateRoot syntax: 

```
SendCustomerInvoice? -> CustomerInvoiceHandler  
@Invoice::CustomerInvoiceSent! -> .
```

In the above example, a new language element called the scope context 
operator appears alongside the '@AggregateRoot'. Denoted by double 
colons - '::' - the scope context element may or may not have whitespace 
between its two characters, and is used to identify relationships 
between two objects. Above, the AR '@Invoice' is generating the 
'CustomerSent!' event in response to logic invoked by the 
'CustomerInvoiceHandler'. The next example demonstrates use of the scope 
element on an AR which generates multiple events in response to a single 
command: 

```
SendCustomerInvoice? -> CustomerInvoiceHandler  
@Invoice:  
    :CustomerInvoiceSent! -> .  
    :InvoiceAged! -> .
```

Scope context is also used to signify intra-element routing that does
not involve infrastructure messaging apparatus:

```
SendCustomerInvoice? -> CustomerInvoiceHandler  
@Invoice::CustomerInvoiceSent! ->  
    -> InvoiceAgeingProcessRouter::InvoiceAgeingProcess
```

The last element that I'll introduce is the State Change element. State 
changes are one of the best ways to track what is happening within a 
system, and thus MIL treats them as 1st class citizens. These statements 
must appear on their own line of text, and are prefixed with the '*' 
character. It's the only time in MIL that there is any mention or 
appearance of assignment because it's just that important! The following
snippet shows an example of the State Change element: 

```
SendCustomerInvoice? -> CustomerInvoiceHandler  
@Invoice::CustomerInvoiceSent! ->  
    -> InvoiceAgegingProcessRouter::InvoiceAgeingProcess  
        *InvoiceAgeingProcess.ProcessState = Unpaid
```

## Summary

We've just walked through the basic steps used when describing messaging 
interactions in a loosely-coupled application. Although the interactions 
described are only a subset of possible interactions, MIL is evolving 
into way to compactly describe the interactions of a 
message-based system. Different nouns and verbs (elements and actions) 
are represented by distinct, mnemonically significant symbols. This 
provides a cross-substrate (squishy human brains < - > silicon CPU) 
means of communicating meaningful information about systems as a whole. 
Although the language describes some types of messaging interactions 
very well, it is very much a work in progress with many elements of the 
language and tooling that have need of development and/or improvement. 
This presents some great opportunities for people looking to contribute 
to OSS, so if you've been on the fence about contributing or are 
wondering about OSS participation, there's no time like the present to 
head over to [http://jelster.github.com/CqrsMessagingTools/][mil], fork 
the repos, and get started! 

[j_chapter3]:       Journey_03_OrdersBC.markdown
[j_chapter5]:       Journey_05_PaymentsBC.markdown
[r_chapter4]:       Reference_04_DeepDive.markdown
[appendix1]:        Appendix1_Running.markdown

[fig1]:             images/Journey_04_TopLevel.png?raw=true
[fig2]:             images/Journey_04_ViewRepository.png?raw=true
[fig3]:             images/Journey_04_Architecture.png?raw=true
[fig4]:             images/Journey_04_SeatsAvailability.png?raw=true

[repourl]:          https://github.com/mspnp/cqrs-journey-code
[modelvalidation]:  http://msdn.microsoft.com/en-us/library/dd410405(VS.98).aspx
[specflow]:         http://www.specflow.org/
[watin]:            http://watin.org
[xUnit]:            http://xunit.codeplex.com/
[mil]:              http://jelster.github.com/CqrsMessagingTools/
[downloadc]:        http://NEEDFWLINK
[repopattern]:      http://martinfowler.com/eaaCatalog/repository.html
[tfhab]:            http://msdn.microsoft.com/en-us/library/hh680934(v=pandp.50)
[testingguide]:     http://testingguidance.codeplex.com
