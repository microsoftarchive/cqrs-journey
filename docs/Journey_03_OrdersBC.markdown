### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Chapter 3: Orders and Registrations Bounded Context 

_The first stop on our CQRS journey._

> "The Allegator is the same, as the Crocodile, and differs only in Name," John Lawson

# A description of the bounded context

The orders and registrations bounded context is partially responsible 
for the booking process for attendees planning to come to a conference. 
In the orders and registrations bounded context, a person (the 
registrant) purchases seats at a particular conference. The registrant 
also assigns names of attendees to the purchased seats (this is 
described in chapter 5, [Preparing for the V1 Release][j_chapter5]). 

This was the first stop on our CQRS journey, so the team decided to 
implement a core, but self-contained part of the system &mdash; orders and 
registrations. The registration process must be as painless as possible 
for attendees. The process must enable the business customer to ensure 
that the maximum possible number of seats can be booked, and give them 
the flexibility set the prices for the different seat types at a 
conference. 

Because this was the first bounded context addressed by the team, we 
also implemented some infrastructure elements of the system to support 
the domain's functionality. These included command and event message 
buses and a persistence mechanism for aggregates. 

> **Note:** The Contoso Conference Management System described in this
> chapter is not the final version of the system. This guidance
> describes a journey, so some of the design decisions and
> implementation details change later in the journey. These
> changes are described in subsequent chapters.

Plans for enhancements to this bounded context in some future journey 
include support for wait listing, whereby requests for seats are placed 
on a wait list if there aren't sufficient seats available, and enabling 
the business customer to set various types of discounts for seat types. 

> **Note:** Wait listing is not implemented in this release, but members
> of the community are working on this and other features. Any
> out-of-band releases and updates will be announced on the [Project "A
> CQRS Journey"][cqrsjourneysite] website.

# Working definitions for this chapter

This chapter uses a number of terms that we will define in a moment. For 
more detail, and possible alternative definitions, see [A CQRS/ES Deep 
Dive][r_chapter4] in the Reference Guide. 

**Command.** A _command_ is a request for the system to perform an action that changes the state of the system. Commands are imperatives; **MakeSeatReservation** is one example. In this bounded context, commands originate either from the UI as a result of a user initiating a request, or from a process manager when the process manager is directing an aggregate to perform an action.

A single recipient processes a command. A command bus transports commands that command handlers then dispatch to aggregates. Sending a command is an asynchronous operation with no return value.

> **GaryPersona:** For a discussion of some possible optimizations
> that also involve a slightly different definition of a command, see
> Chapter 6, [Versioning our System][j_chapter6].

**Event.** An _event_, such as **OrderConfirmed**, describes something that has happened in the system, typically as a result of a command. Aggregates in the domain model raise events.

Multiple subscribers can handle a specific event. Aggregates publish events to an event bus; handlers register for specific types of events on the event bus and then deliver the event to the subscriber. In this bounded context, the only subscriber is a process manager.

**Process manager.** In this bounded context, a _process manager_ is a class that coordinates the behavior of the aggregates in the domain. A process manager subscribes to the events that the aggregates raise, and then follow a simple set of rules to determine which command or commands to send. The process manager does not contain any business logic; it simply contains logic to determine the next command to send. The process manager is implemented as a state machine, so when it responds to an event, it can change its internal state in addition to sending a new command.
Our process manager is an implementation of the Process Manager pattern defined on pages 312 to 321 of the book by Gregor Hohpe and Bobby Woolf,  entitled _Enterprise Integration Patterns: Designing, Building, and Deploying Messaging Solutions_ (Addison-Wesley Professional, 2003).

> **MarkusPersona:** It can be difficult for someone new to the code to follow the flow of commands and events through the system. For a discussion of a technique that can help, see the section "Impact on testing" in Chapter 4, "[Extending and Enhancing the Orders and Registrations Bounded Contexts][j_chapter4]."

The process manager in this bounded context can receive commands as well 
as subscribe to events. 

> **GaryPersona:** The team initially referred to the process manager class in the orders bounded context as a saga. To find out why we decided to change the terminology, see the section [Patterns and Concepts](#patternsandconcepts) later in this chapter.

The Reference Guide contains additional definitions and explanations of 
CQRS related terms.

# Domain definitions (ubiquitous language)

The following list defines the key domain-related terms that the team used during the development of this Orders and Registrations bounded contexts.

**Attendee.** An attendee is someone who is entitled to attend a conference. An Attendee can interact with the system to perform tasks such as manage his agenda, print his badge, and provide feedback after the conference. An attendee could also be a person who doesn't pay to attend a conference such as a volunteer, speaker, or someone with a 100% discount. An attendee may have multiple associated attendee types (speaker, student, volunteer, track chair, etc.)

**Registrant.** A registrant is a person who interacts with the system to place orders and to make payments for those orders. A registrant also creates the registrations associated with an order. A registrant may also be an attendee.

**User.** A user is a person such as an attendee, registrant, speaker, or volunteer who is associated with a conference. Each user has a unique record locator code that the user can use to access user-specific information in the system. For example, a registrant can use a record locator code to access her orders, and an attendee can use a record locator code to access his personalized conference agenda.

  
> **CarlosPersona:** We intentionally implemented a record locator mechanism to return to a previously submitted order via the mechanism. This eliminates an often annoying requirement for users to create an account in the system and sign in in order to evaluate its usefulness. Our customers were adamant about this.

**Seat assignment.** A seat assignment associates an attendee with a seat in a confirmed order. An order may have one or more seat assignments associated with it.

**Order.** When a registrant interacts with the system, the system creates an order to manage the reservations, payment, and registrations. An order is confirmed when the registrant has successfully paid for the order items. An order contains one or more order items.

**Order item.** An order item represents a seat type and quantity, and is associated with an order. An order item exists in one of three states: created, reserved, or rejected. An order item is initially in the created state. An order item is in the reserved state if the system has reserved the quantity of seats of the seat type requested by the registrant. An order item is in the rejected state if the system cannot reserve the quantity of seats of the seat type requested by the registrant.

**Seat.** A seat represents the right to be admitted to a conference or to access a specific session at the conference such as a cocktail party, a tutorial, or a workshop. The business customer may change the quota of seats for each conference. The business customer may also change the quota of seats for each session.
Reservation. A reservation is a temporary reservation of one or more seats. The ordering process creates reservations. When a registrant begins the ordering process, the system makes reservations for the number of seats requested by the registrant. These seats are then not available for other registrants to reserve. The reservations are held for n minutes during which the registrant can complete the ordering process by making a payment for those seats. If the registrant does not pay for the seats within n minutes, the system cancels the reservation and the seats become available to other registrants to reserve.

**Seat availability.** Every conference tracks seat availability for each type of seat. Initially, all of the seats are available to reserve and purchase. When a seat is reserved, the number of available seats of that type is decremented. If the system cancels the reservation, the number of available seats of that type is incremented. The business customer defines the initial number of each seat type to be made available; this is an attribute of a conference. A conference owner may adjust the numbers for the individual seat types.

**Conference site.** You can access every conference defined in the system by using a unique URL. Registrants can begin the ordering process from this site.

> Each of the terms defined here was formulated through active discussions between the development team and the domain experts. The following is a sample conversation between developers and domain experts that illustrates how the team arrived at a definition of the term _attendee_.
>
> Developer 1: Here's an initial stab at a definition for _attendee_. "An attendee is someone who has paid to attend a conference. An attendee can interact with the system to perform tasks such as manage his agenda, print his badge, and provide feedback after the conference."
>
> Domain Expert 1: Not all attendees will pay to attend the conference. For example, some conferences will have volunteer helpers, also speakers typically don't pay. And, there may be some cases where an attendee gets a 100% discount.
>
> Domain Expert 1: Don't forget that it's not the attendee who pays; that's done by the registrant.
>
> Developer 1: So we need to say that Attendees are people who are authorized to attend a conference?
>
> Developer 2: We need to be careful about the choice of words here. The term authorized will make some people think of security and authentication and authorization.
>
> Developer 1: How about entitled?
>
> Domain Expert 1: When the system performs tasks such as printing badges, it will need to know what type of attendee the badge is for. For example, speaker, volunteer, paid attendee, and so on.
>
> Developer 1: Now we have this as a definition that captures everything we've discussed. An attendee is someone who is entitled to attend a conference. An attendee can interact with the system to perform tasks such as manage his agenda, print his badge, and provide feedback after the conference. An attendee could also be a person who doesn't pay to attend a conference such as a volunteer, speaker, or someone with a 100% discount. An attendee may have multiple associated attendee types (speaker, student, volunteer, track chair, etc.)

# Requirements for creating orders

A registrant is the person who reserves and pays for (orders) seats at a conference. Ordering is a two-stage process: first, the registrant reserves a number of seats and then pays for the seats to confirm the reservation. If registrant does not complete the payment, the seat reservations expire after a fixed period and the system makes the seats available for other registrants to reserve.

Figure 1 shows some of the early UI mockups that the team used to explore the seat-ordering story. 

![Figure 1][fig1]

**Ordering UI mockups**

These UI mockups helped the team in several ways, allowing them to:

* Communicate the core team's vision for the system to the graphic designers who are on an independent team at a third-party company.
* Communicate the domain expert's knowledge to the developers.
* Refine the definition of terms in the ubiquitous language.
* Explore "what if" questions about alternative scenarios and approaches.
* Form the basis for the system's suite of acceptance tests.

# Architecture

The application is designed to deploy to Windows Azure. At this stage in the journey, the application consists of a web role that contains the ASP.NET MVC web application and a worker role that contains the message handlers and domain objects. The application uses SQL Database databases for data storage, both on the write-side and the read-side. The application uses the Windows Azure Service Bus to provide its messaging infrastructure.

While you are exploring and testing the solution, you can run it locally, either using the Windows Azure compute emulator or by running the MVC web application directly and running a console application that hosts the handlers and domain objects. When you run the application locally, you can use a local SQL Server Express database instead of SQL Database, and use a simple messaging infrastructure implemented in a SQL Server Express database.

For more information about the options for running the application, see 
[Appendix 1][appendix1].

> **GaryPersona:** A frequently cited advantage of the CQRS pattern is that it enables you to scale the read side and write side of the application independently to support the different usage patterns. In this bounded context, however, the number of read operations from the UI is not likely to hugely out-number the write operations: this bounded context focuses on registrants creating orders. Therefore, the read side and the write side are deployed to the same Windows Azure worker role rather than to two separate worker roles that could be scaled independently.

# Patterns and concepts <a name="patternsandconcepts"/>

The team decided to implement the first bounded context without using 
event sourcing in order to keep things simple. However, they did agree 
that if they later decided that event sourcing would bring specific 
benefits to this bounded context, then they would revisit this decision. 

> **Note** For a description of how event sourcing relates to the CQRS
> pattern, see [Introducing Event Sourcing][r_chapter3] in the Reference
> Guide.

One of the important discussions the team had concerned the choice of aggregates and entities that they would implement. The following images from the team's whiteboard illustrate some of their initial thoughts, and questions about the alternative approaches they could take with a simple conference seat reservation scenario to try and understand the pros and cons of alternative approaches.

> "A value I think developers would benefit greatly from recognizing is
> the de-emphasis on the means and methods for persistence of objects in
> terms of relational storage. Teach them to avoid modeling the domain
> as if it was a relational store, and I think it will be easier to
> introduce and understand both DDD and CQRS."  
> &mdash; Josh Elster, CQRS Advisors Mail List

> **GaryPersona:** These diagrams deliberately exclude details of how
> the system delivers commands and events through command and event
> handlers. The diagrams focus on the logical relationships between the
> aggregates in the domain.

This scenario considers what happens when a registrant tries to book
several seats at a conference. The system must:

- Check that sufficient seats are available.
- Record details of the registration.
- Update the total number of seats booked for the conference.

> **Note:** We deliberately kept the scenario simple to avoid
> distractions while the team examines the alternatives. These examples
> do not illustrate the final implementation of this bounded context. 

The first approach considered by the team, shown in Figure 2, uses two 
separate aggregates.

![Figure 2][fig2]

**Approach 1: Two separate aggregates**

The numbers in the diagram correspond to the following steps:

1. The UI sends a command to register Attendees X and Y for
   conference 157. The command is routed to a new **Order** aggregate.
2. The **Order** aggregate raises an event that reports that an order
   has been created. The event is routed to the **SeatsAvailability**
   aggregate.
3. The **SeatsAvailability** aggregate with an ID of 157 is
   re-hydrated from the data store.
4. The **SeatsAvailability** aggregate updates its total
   number of seats booked.
5. The updated version of the **SeatsAvailability**
   aggregate is persisted to the data store.
6. The new **Order** aggregate, with an ID of 4239, is persisted to the
   data store.
   
> **MarkusPersona:** The term rehydration refers to the process of
> deserializing the aggregate instance from a data store.

> **JanaPersona:** You could consider using the [Memento
> pattern][memento] to handle the persistence and rehydration.

The second approach considered by the team, shown in Figure 3, uses a 
single aggregate in place of two. 

![Figure 3][fig3]

**Approach 2: A single aggregate**

The numbers in the diagram correspond to the following steps:

1. The UI sends a command to register Attendees X and Y onto
   conference 157. The command is routed to the **Conference** aggregate
   with an ID of 157.
2. The **Conference** aggregate with an ID of 157 is rehydrated from
   the data store.
3. The **Order** entity validates the booking (it queries the 
   **SeatsAvailability** entity to see if there are enough
   seats left), and then invokes the method to update the number of
   seats booked on the conference entity.
4. The **SeatsAvailability** entity updates its total number
   of seats booked.
5. The updated version of the **Conference** aggregate is persisted to
   the data store.

The third approach considered by the team, shown in Figure 4, uses a 
process manager to coordinate the interaction between two aggregates. 

![Figure 4][fig4]

**Approach 3: Using a process manager**

The numbers in the diagram correspond to the following steps:

1. The UI sends a command to register Attendees X and Y for
   conference 157. The command is routed to a new **Order** aggregate.
2. The new **Order** aggregate, with an ID of 4239,  is persisted to
   the data store.
3. The **Order** aggregate raises an event that is handled by the
   **RegistrationProcessManager** class.
4. The **RegistrationProcessManager** class determines that a command should
   be sent to the **SeatsAvailability** aggregate with an ID of
   157.
5. The **SeatsAvailability** aggregate is rehydrated from the
   data store.
6. The total number of seats booked is updated in the
   **SeatsAvailability** aggregate and it is persisted to the
   data store.

> **GaryPersona:** Process manager or saga? Initially the team referred to
> the **RegistrationProcessManager** class as a saga. However, after they
> reviewed the original definition of a saga from the paper
> [Sagas][sagapaper] by Hector Garcia-Molina and Kenneth Salem, they
> revised their decision. The key reasons for this are that the reservation
> process does not include explicit compensation steps, and does not
> need to be represented as a long-lived transaction.

For more information about process managers and sagas, see chapter 6
[A Saga on Sagas][r_chapter6] in the Reference Guide.
   
The team identified the following questions about these approaches:

- Where does the validation that there are sufficient seats for the registration take place: in the **Order** or **SeatsAvailability** aggregate? 
- Where are the transaction boundaries? 
- How does this model deal with concurrency issues when multiple 
  registrants try to place orders simultaneously? 
- What are the aggregate roots?

The following sections discuss these questions in relation to the three
approaches considered by the team.

## Validation

Before a registrant can reserve a seat, the system must check that there 
are enough seats available. Although logic in the UI can attempt to 
verify that there are sufficient seats available before it sends a 
command, the business logic in the domain must also perform the check; 
this is because the state may change between the time the UI performs
the validation and the time that the system delivers the command to the
aggregate in the domain.

> **JanaPersona:** When we talk about UI validation here, we are talking
> about validation that the Model-View Controller (MVC) controller performs, not the browser.

In the first model, the validation must take place in either the 
**Order** or **SeatsAvailability** aggregate. If it is the 
former, the **Order** aggregate must discover the current seat 
availability from the **SeatsAvailability** aggregate before 
the reservation is made and before it raises the event. If it is the 
latter, the **SeatsAvailability** aggregate must somehow notify the
**Order** aggregate that it cannot reserve the seats, and that the
**Order** aggregate must undo (or compensate for) any work that it has
completed so far.

> **BethPersona:** Undo is just one of many compensating actions that
> occur in real life. The compensating actions could even be outside of
> the system implementation and involve human actors: for example, a
> Contoso clerk or the Business Customer calls the Registrant to tell
> them that an error was made and that they should ignore the last
> confirmation email they received from the Contoso system.

The second model behaves similarly, except that it is **Order** and 
**SeatsAvailability** entities cooperating within a 
**Conference** aggregate. 

In the third model, with the process manager, the aggregates exchange messages
through the process manager about whether the registrant can make the reservation
at the current time. 

All three models require entities to communicate about the validation 
process, but the third model with the process manager appears more complex than the 
other two. 

## Transaction boundaries

An aggregate, in the DDD approach, represents a consistency boundary. 
Therefore, the first model with two aggregates, and the third model with 
two aggregates and a process manager will involve two transactions: one when the 
system persists the new **Order** aggregate and one when the system 
persists the updated **SeatsAvailability** aggregate.

> **Note:** The term _consistency boundary_ refers to a boundary within
> which you can assume that all the elements remain consistent with each other all the time.

To ensure the consistency of the system when a registrant creates an 
order, both transactions must succeed. To guarantee this, we must take 
steps to ensure that the system is eventually consistent by ensuring 
that the infrastructure reliably delivers messages to aggregates. 

In the second approach, which uses a single aggregate, we will only have a 
single transaction when a registrant makes an order. This appears to be 
the simplest approach of the three. 

## Concurrency

The registration process takes place in a multi-user environment where many registrants could attempt to purchase seats simultaneously. The team decided to use the reservation pattern to address the concurrency issues in the registration process. In this scenario, this means that a registrant initially reserves seats (which are then unavailable to other registrants); if the registrant completes the payment within a timeout period, the system retains the reservation; otherwise the system cancels the reservation.

This reservation system introduces the need for additional message types; for example, an event to report that a registrant has made a payment, or report that a timeout has occurred.

This timeout also requires the system to incorporate a timer somewhere 
to track when reservations expire. 

Modeling this complex behavior with sequences of messages and the 
requirement for a timer is best done using a process manager. 

## Aggregates and aggregate roots

In the two models that have the **Order** aggregate and the **SeatsAvailability** aggregate, the team easily identified the entities that make up the aggregate, and the aggregate root. The choice is not so clear in the model with a single aggregate: it does not seem natural to access orders through a **SeatsAvailability** entity, or to access the seat availability through an **Order** entity. Creating a new entity to act as an aggregate root seems unnecessary.

The team decided on the model that incorporated a process manager because this 
offers the best way to handle the concurrency requirements in this 
bounded context. 

# Implementation details

This section describes some of the significant features of the orders 
and registrations bounded context implementation. You may find it useful 
to have a copy of the code so you can follow along. You can download it 
from from the [Download center][downloadc], or check the evolution of 
the code in the repository on github: 
[mspnp/cqrs-journey-code][repourl]. 

> **Note:** Do not expect the code samples to match exactly the code in
> the reference implementation. This chapter describes a step in the
> CQRS journey, the implementation may well change as we learn more and
> refactor the code.

## High-level architecture

As we described in the previous section, the team initially decided to 
implement the reservations story in the Conference Management System 
using the CQRS pattern but without using event sourcing. Figure 5 shows 
the key elements of the implementation: an MVC web application, a data 
store implemented using a Windows Azure SQL Database instance, the read and write models, and 
some infrastructure components. 

> **Note:** We'll describe what goes on inside the read and write models 
> later in this section. 

![Figure 5][fig5]

**High-level architecture of the registrations bounded context**

The following sections relate to the numbers in Figure 5 and provide 
more detail about these elements of the architecture. 

### 1. Querying the read model

The **ConferenceController** class includes an action named **Display** 
that creates a view that contains information about a particular 
conference. This controller class queries the read model using the 
following code: 


```Cs
public ActionResult Display(string conferenceCode)
{
	var conference = this.GetConference(conferenceCode);

	return View(conference);
}

private Conference.Web.Public.Models.Conference GetConference(string conferenceCode)
{
	var repo = this.repositoryFactory();
	using (repo as IDisposable)
	{
		var conference = repo.Query<Conference>().First(c => c.Code == conferenceCode);

		var conference =
			new Conference.Web.Public.Models.Conference { Code = conference.Code, Name = conference.Name, Description = conference.Description };

		return conference;
	}
}
```

The read model retrieves the information from the data store and returns 
it to the controller using a Data Transfer Object (DTO) class. 

### 2. Issuing Commands

The web application sends commands to the write model through a command 
bus. This command bus is an infrastructure element that provides 
reliable messaging. In this scenario, the bus delivers messages 
asynchronously and once only to a single recipient.

The **RegistrationController** class can send a **RegisterToConference** 
command to the write model in response to user interaction. This command 
sends a request to register one or more seats at the conference. The 
**RegistrationController** class then polls the read model to discover 
whether the registration request succeeded. See the section "6. Polling 
the Read Model" below for more details.

The following code sample shows how the **RegistrationController** sends
a **RegisterToConference** command:

```Cs
var viewModel = this.UpdateViewModel(conferenceCode, contentModel);

var command =
	new RegisterToConference
	{
		OrderId = viewModel.Id,
		ConferenceId = viewModel.ConferenceId,
		Seats = viewModel.Items.Select(x => new RegisterToConference.Seat { SeatTypeId = x.SeatTypeId, Quantity = x.Quantity }).ToList()
	};

this.commandBus.Send(command);
```

> **Note:** All of the commands are sent asynchronously and do not 
expect return values. 

### 3. Handling Commands

Command handlers register with the command bus; the command bus can then 
forward commands to the correct handler. 

The **OrderCommandHandler** class handles the **RegisterToConference** 
command sent from the UI. Typically, the handler is responsible for 
initiating any business logic in the domain and for persisting any state 
changes to the data store. 

The following code sample shows how the **OrderCommandHandler** 
class handles the **RegisterToConference** command: 

```Cs
public void Handle(RegisterToConference command)
{
    var repository = this.repositoryFactory();

    using (repository as IDisposable)
    {
        var seats = command.Seats.Select(t => new OrderItem(t.SeatTypeId, t.Quantity)).ToList();

        var order = new Order(command.OrderId, Guid.NewGuid(), command.ConferenceId, seats);

        repository.Save(order);
    }
}
```

### 4. Initiating business logic in the domain

In the previous code sample, the **OrderCommandHandler** class 
creates a new **Order** instance. The **Order** entity is an aggregate 
root, and its constructor contains code to initiate the domain logic. 
See the section "Inside the Write Model" below for more details of what 
actions this aggregate root performs. 

### 5. Persisting the changes

In the previous code sample, the handler persists the new **Order** 
aggregate by calling the **Save** method in the repository class. This 
**Save** method also publishes any events raised by the **Order** 
aggregate on the command bus. 

### 6. Polling the read model

To provide feedback to the user, the UI must have a way to check whether 
the **RegisterToConference** command succeeded. Like all commands in the 
system, this command executes asynchronously and does not return a 
result. The UI queries the read model to check whether the 
command succeeded. 

The following code sample shows the initial implementation where the 
**RegistrationController** class polls the read model until either the 
system creates the order or a timeout occurs. The **WaitUntilUpdated**
method polls the read-model until it finds either that the order has
been persisted or it times out.


```Cs
[HttpPost]
public ActionResult StartRegistration(string conferenceCode, OrderViewModel contentModel)
{
    ...
	
	this.commandBus.Send(command);

    var draftOrder = this.WaitUntilUpdated(viewModel.Id);

    if (draftOrder != null)
    {
        if (draftOrder.State == "Booked")
        {
            return RedirectToAction("SpecifyPaymentDetails", new { conferenceCode = conferenceCode, orderId = viewModel.Id });
        }
        else if (draftOrder.State == "Rejected")
        {
            return View("ReservationRejected", viewModel);
        }
    }

    return View("ReservationUnknown", viewModel);
}
```

The team later replaced this mechanism for checking whether the system 
saves the order with an implementation of the Post-Redirect-Get pattern. 
The following code sample shows the new version of the 
**StartRegistration** action method.

> **Note:** For more information about the Post-Redirect-Get pattern see
> the article [Post/Redirect/Get][prg] on Wikipedia.

```Cs
[HttpPost]
public ActionResult StartRegistration(string conferenceCode, OrderViewModel contentModel)
{
    ...

    this.commandBus.Send(command);

    return RedirectToAction("SpecifyRegistrantDetails", new { conferenceCode = conferenceCode, orderId = command.Id });
}
```

The action method now redirects to the **SpecifyRegistrantDetails** view 
immediately after it sends the command. The following code sample shows 
how the **SpecifyRegistrantDetails** action polls for the order in the 
repository before returning a view. 

```Cs
[HttpGet]
public ActionResult SpecifyRegistrantDetails(string conferenceCode, Guid orderId)
{
    var draftOrder = this.WaitUntilUpdated(orderId);
    
	...
}
```

The advantages of this second approach, using the Post-Redirect-Get 
pattern instead of in the **StartRegistration** post action are that it 
works better with the browser's forward and back navigation buttons, and 
that it gives the infrastructure more time to process the command before 
the MVC controller starts polling. 

## Inside the write model

### Aggregates

The following code sample shows the **Order** aggregate.

```Cs
public class Order : IAggregateRoot, IEventPublisher
{
    public static class States
    {
        public const int Created = 0;
        public const int Booked = 1;
        public const int Rejected = 2;
        public const int Confirmed = 3;
    }

    private List<IEvent> events = new List<IEvent>();

    ...

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ConferenceId { get; private set; }

    public virtual ObservableCollection<TicketOrderLine> Lines { get; private set; }

    public int State { get; private set; }

    public IEnumerable<IEvent> Events
    {
        get { return this.events; }
    }

    public void MarkAsBooked()
    {
        if (this.State != States.Created)
            throw new InvalidOperationException();

        this.State = States.Booked;
    }

    public void Reject()
    {
        if (this.State != States.Created)
            throw new InvalidOperationException();

        this.State = States.Rejected;
    }
}
```

Notice how the properties of the class are not virtual. In the original 
version of this class, the properties **Id**, **UserId**, 
**ConferenceId**, and **State** were all marked as virtual. The 
following conversation between two developers explores this decision. 

> *Developer 1:* I'm really convinced you should not make the 
> property virtual, except if required by the object-relational mapping (ORM) layer. If this is just for 
> testing purposes, entities and aggregate roots should never be tested 
> using mocking. If you need mocking to test your entities, this is a 
> clear smell that something is wrong in the design. 

> *Developer 2:* I prefer to be open and extensible by default. You 
> never know what needs may arise in the future, and making things 
> virtual is hardly a cost. This is certainly controversial and a bit 
> non-standard in .NET, but I think it's OK. We may only need virtuals 
> on lazy-loaded collections. 

> *Developer 1:* Since CQRS usually makes the need for lazy load 
> vanish, you should not need it either. This leads to even simpler code. 

> *Developer 2:* CQRS does not dictate usage of event sourcing (ES), so if you're 
> using an aggregate root that contains an object graph, you'd need that 
> anyway, right? 

> *Developer 1:* This is not about ES, it's about DDD. When your 
> aggregate boundaries are right, you don't need delay loading. 

> *Developer 2:* To be clear, the aggregate boundary is here to group 
> things that should change together for reasons of consistency. A lazy 
> load would indicate that things that have been grouped together don't 
> really need this grouping.

> *Developer 1:* I agree. I have found that lazy-loading in the 
> command side means I have it modeled wrong. If I don't need the value 
> in the command side, then it shouldn't be there. In addition, I 
> dislike virtuals unless they have an intended purpose (or some 
> artificial requirement from an object-relational mapping (ORM) tool). In my opinion, it violates the 
> Open-Closed principle: you have opened yourself up for modification in 
> a variety of ways that may or may not be intended and where the 
> repercussions might not be immediately discoverable, if at all. 

> *Developer 2:* Our **Order** aggregate in the model has a list of 
> **Order Items**. Surely we don't need to load the lines to mark it as 
> Booked? Do we have it modeled wrong there? 

> *Developer 1:* Is the list of **Order Items** that long? If it is, 
> the modeling may be wrong because you don't necessarily need 
> transactionality at that level. Often, doing a late round trip to get 
> and updated **Order Items** can be more costly that loading them 
> up front: you should evaluate the usual size of the collection and do 
> some performance measurement. Make it simple first, optimize if 
> needed. 

> &mdash; *Thanks to J&eacute;r&eacute;mie Chassaing and Craig Wilson*

### Aggregates and process managers

Figure 6 shows the entities that exist in the write-side model. There 
are two aggregates, **Order** and **SeatsAvailability**, each 
one containing multiple entity types. Also there is a 
**RegistrationProcessManager** class to manage the interaction between the
aggregates. 

The table in the Figure 6 shows how the process manager behaves given a current 
state and a particular type of incoming message. 

![Figure 6][fig6]

**Domain objects in the write model**

The process of registering for a conference begins when the UI sends a 
**RegisterToConference** command. The infrastructure delivers this 
command to the **Order** aggregate. The result of this command is that 
the system creates a new **Order** instance, and that the new **Order** 
instance raises an **OrderPlaced** event. The following code sample from 
the constructor in the **Order** class shows this happening. Notice how 
the system uses GUIDs to identify the different entities. 

```Cs
public Order(Guid id, Guid userId, Guid conferenceId, IEnumerable<OrderItem> lines)
{
    this.Id = id;
    this.UserId = userId;
    this.ConferenceId = conferenceId;
    this.Lines = new ObservableCollection<OrderItem>(items);

    this.events.Add(
        new OrderPlaced
        {
            OrderId = this.Id,
            ConferenceId = this.ConferenceId,
            UserId = this.UserId,
            Seats = this.Lines.Select(x => new OrderPlaced.Seat { SeatTypeId = x.SeatTypeId, Quantity = x.Quantity }).ToArray()
        });
}
```

> **Note:** To see how the infrastructure elements deliver commands and
  events, see Figure 7.

The system creates a new **RegistrationProcessManager** instance to manage the 
new order. The following code sample from the **RegistrationProcessManager** 
class shows how the process manager handles the event. 

```Cs
public void Handle(OrderPlaced message)
{
    if (this.State == ProcessState.NotStarted)
    {
        this.OrderId = message.OrderId;
        this.ReservationId = Guid.NewGuid();
        this.State = ProcessState.AwaitingReservationConfirmation;

        this.AddCommand(
            new MakeSeatReservation
            {
                ConferenceId = message.ConferenceId,
                ReservationId = this.ReservationId,
                NumberOfSeats = message.Items.Sum(x => x.Quantity)
            });
    }
    else
    {
        throw new InvalidOperationException();
    }
}
```

The code sample shows how the process manager changes its state and sends a new 
**MakeSeatReservation** command that the 
**SeatsAvailability** aggregate handles. The code sample also 
illustrates how the process manager is implemented as a state machine that receives 
messages, changes its state, and sends new messages. 

> **MarkusPersona:** Notice how we generate a new globally unique identifier (GUID) to identify the new reservation. We use these GUIDs to correlate messages to the correct process manager and aggregate instances.

When the **SeatsAvailability** aggregate receives a 
**MakeReservation** command, it makes a reservation if there are enough 
available seats. The following code sample shows how the 
**SeatsAvailability** class raises different events depending 
on whether or not there are sufficient seats. 

```Cs
public void MakeReservation(Guid reservationId, int numberOfSeats)
{
    if (numberOfSeats > this.RemainingSeats)
    {
        this.events.Add(new ReservationRejected { ReservationId = reservationId, ConferenceId = this.Id });
    }
    else
    {
        this.PendingReservations.Add(new Reservation(reservationId, numberOfSeats));
        this.RemainingSeats -= numberOfSeats;
        this.events.Add(new ReservationAccepted { ReservationId = reservationId, ConferenceId = this.Id });
    }
}
```

The **RegistrationProcessManager** class handles the 
**ReservationAccepted** and **ReservationRejected** events. This 
reservation is a temporary reservation for seats to give the user the 
opportunity to make a payment. The process manager is responsible for releasing 
the reservation when either the purchase is complete, or the reservation 
timeout period expires. The following code sample shows how the process manager 
handles these two messages. 

```Cs
public void Handle(ReservationAccepted message)
{
    if (this.State == ProcessState.AwaitingReservationConfirmation)
    {
        this.State = ProcessState.AwaitingPayment;

        this.AddCommand(new MarkOrderAsBooked { OrderId = this.OrderId });
        this.commands.Add(
            new Envelope<ICommand>(new ExpireOrder { OrderId = this.OrderId, ConferenceId = message.ConferenceId })
            {
                Delay = TimeSpan.FromMinutes(15),
            });
    }
    else
    {
        throw new InvalidOperationException();
    }
}

public void Handle(ReservationRejected message)
{
    if (this.State == ProcessState.AwaitingReservationConfirmation)
    {
        this.State = ProcessState.Completed;
        this.AddCommand(new RejectOrder { OrderId = this.OrderId });
    }
    else
    {
        throw new InvalidOperationException();
    }
}
```

If the reservation is accepted, the process manager starts a timer running by 
sending an **ExpireOrder** command to itself, and sends a 
**MarkOrderAsBooked** command to the **Order** aggregate. Otherwise, it 
sends a **ReservationRejected** message back to the **Order** aggregate. 

The previous code sample shows how the process manager sends the 
**ExpireOrder** command. The infrastructure is responsible for 
holding the message in a queue for the delay of fifteen minutes. 

You can examine the code in the **Order**, 
**SeatsAvailability**, and **RegistrationProcessManager** classes 
to see how the other message handlers are implemented. They all follow 
the same pattern: receive a message, perform some logic, and send a 
message. 

> **JanaPersona:** The code samples shown in this chapter are from an
> early version of the Conference Management System. The next chapter
> shows how the design and implementation evolved as the team explored
> the domain and learned more about the CQRS pattern.

### Infrastructure

The sequence diagram in Figure 7 shows how the infrastructure elements 
interact with the domain objects to deliver messages. 

![Figure 7][fig7]

**Infrastructure sequence diagram**

A typical interaction begins when an MVC controller in the UI sends a 
message using the command bus. The message sender invokes the **Send** 
method on the command bus asynchronously. The command bus then stores 
the message until the message recipient retrieves the message and 
forwards it to the appropriate handler. The system includes a number of 
command handlers that register with the command bus to handle specific 
types of commands. For example, the **OrderCommandHandler** class defines 
handler methods for the **RegisterToConference**, **MarkOrderAsBooked**, 
and **RejectOrder** commands. The following code sample shows the 
handler method for the **MarkOrderAsBooked** command. Handler methods 
are responsible for locating the correct aggregate instance, calling 
methods on that instance, and then saving that instance. 

```Cs
public void Handle(MarkOrderAsBooked command)
{
    var repository = this.repositoryFactory();

    using (repository as IDisposable)
    {
        var order = repository.Find<Order>(command.OrderId);

        if (order != null)
        {
            order.MarkAsBooked();
            repository.Save(order);
        }
    }
}
```

The class that implements the **IRepository** interface is responsible 
for persisting the aggregate and publishing any events raised by the 
aggregate on the event bus, all as part of a transaction. 

**CarlosPersona:** The team later discovered an issue with this when 
they tried to use Windows Azure Service Bus as the messaging 
infrastructure. Windows Azure Service Bus does not support distributed 
transactions with databases. For a discussion of this issue, see 
[Preparing for the V1 Release][j_chapter5] later in this guide. 

The only event subscriber in the reservations bounded context is the 
**RegistrationProcessManager** class. Its router subscribes to the event bus to 
handle specific events, as shown in the following code sample from the 
**RegistrationProcessManager** class. 

> **Note:** We use the term handler to refer to the classes that handle 
> commands and events and forward them to aggregate instances, and the
> term router to refer to the classes that handle events and commands 
> and forward them to process manager instances. 

```Cs
public void Handle(ReservationAccepted @event)
{
	var repo = this.repositoryFactory.Invoke();
	using (repo as IDisposable)
	{
        lock (lockObject)
        {
            var process = repo.Find<RegistrationProcessManager>(@event.ReservationId);
            process.Handle(@event);

            repo.Save(process);
        }
	}
}
```

Typically, an event handler method loads a process manager instance, passes the 
event to the process manager, and then persists the process manager instance. In this 
case, the **IRepository** instance is responsible for persisting the 
process manager instance and for sending any commands from the process manager 
instance to the command bus. 

## Using the Windows Azure Service Bus

To transport Command and Event messages, the team decided to use the 
Windows Azure Service Bus to provide the low-level messaging 
infrastructure. This section describes how the system uses the Windows 
Azure Service Bus and some of the alternatives and trade-offs the team 
considered during the design phase. 

> **JanaPersona:** The team at Contoso decided to use the Windows Azure
> Service Bus because it offers out-of-the-box support for the messaging
> scenarios in the Conference Management System. This minimizes the
> amount of code that the team needs to write, and provides for a
> robust, scalable messaging infrastructure. The team plans to use
> features such as duplicate message detection and guaranteed message
> ordering. For a summary of the differences between Windows Azure
> Service Bus and Windows Azure Queues, see [Windows Azure Queues and 
> Windows Azure Service Bus Queues - Compared and Contrasted][sbq] on
> MSDN.

Figure 8 shows how both command and event messages flow through the system.
MVC controllers in the UI and domain objects use **CommandBus** 
and **EventBus** instances to send **BrokeredMessage** messages to one 
of the two topics in the Windows Azure Service Bus. To receive messages, 
the handler classes register with the **CommandProcessor** and 
**EventProcessor** instances that retrieve messages from the topics by 
using the **SubscriptionReceiver** class. The **CommandProcessor** class 
determines which single handler should receive a command message; the 
**EventProcessor** class determines which handlers should receive an 
event message. The handler instances are responsible for invoking 
methods on the domain objects. 

> **Note:** A Windows Azure Service Bus topic can have multiple 
> subscribers. The Windows Azure Service Bus delivers messages sent to a 
> topic to all its subscribers. Therefore, one message can have multiple 
> recipients. 

![Figure 8][fig8]

**Message flows through a Windows Azure Service Bus topic**

In the initial implementation, the **CommandBus** and **EventBus** 
classes are very similar. The only difference between the **Send** 
method and the **Publish** method is that the **Send** method expects 
the message to be wrapped in an **Envelope** class. The **Envelope** 
class enables the sender to specify a time delay for the message 
delivery. 

Events can have multiple recipients. In the example shown 
in Figure 8, the **ReservationRejected** event is sent to the 
**RegistrationProcessManager**, the **WaitListProcessManager**, and one other 
destination. The **EventProcessor** class identifies the list of
handlers to receive the event by examining its list of registered
handlers.

A command has only one recipient. In Figure 8, the 
**MakeSeatReservation** is sent to the **SeatsAvaialbility** aggregate. 
There is just a single handler registered for this subscription. The
**CommandProcessor** class identifies the handler to receive the command
by examining its list of registered handlers.

This implementation gives rise to a number of questions:

1. How do you limit delivery of a command to a single recipient?
2. Why have separate **CommandBus** and **EventBus** classes if they are
   so similar?
3. How scalable is this approach?
4. How robust is this approach?
5. What is the granularity of a topic and a subscription?
6. How are commands and events serialized?

The following sections discuss these questions.

### Delivering a command to a single recipient

This discussion assumes you that you have a basic understanding of the 
differences between Windows Azure Service Bus queues and topics. For an 
introduction to Windows Azure Service Bus, see [Technologies Used in the 
Reference Implementation][r_chapter9] in the Reference Guide. 

With the implementation shown in Figure 8, two things are necessary to 
ensure that a single handler handles a command message. First, 
there should only be a single subscription to the 
**conference/commands** topic in Windows Azure Service Bus; remember 
that a Windows Azure Service Bus topic may have multiple subscribers. 
Second, the **CommandProcessor** should invoke a single handler for each 
command message that it receives. There is no way in Windows Azure
Service Bus to restrict a topic to a single subscription; therefore, the
developers must be careful to create just a single subscription on a
topic that is delivering commands. 

> **GaryPersona:** A separate issue is to ensure that the handler
> retrieves commands from the topic and processes them only once. You
> must ensure either that the command is idempotent, or that the system
> guarantees to process the command only once. The team will address
> this issue in a later stage of the journey. See the chapter
> [Adding Resilience and Optimizing Performance][j_chapter7] for more
> information.

> **Note:** It is possible to have multiple **SubscriptionReceiver** 
> instances running, perhaps in multiple worker role instances. If 
> multiple **SubscriptionReceiver** instances can receive messages from 
> the same topic subscription, then the first one to call the **Receive** 
> method on the **SubscriptionClient** object will get and handle the 
> command. 

An alternative approach is to use a Windows Azure Service Bus queue in 
place of a topic for delivering command messages. Windows Azure Service 
Bus queues differ from topics in that they are designed to deliver 
messages to a single recipient instead of to multiple recipients through 
multiple subscriptions. The developers plan to evaluate this option in 
more detail with the intention of implementing this approach later in the 
project. 

The following code sample from the **SubscriptionReceiver** class shows 
how it receives a message from the topic subscription. 

```Cs
private SubscriptionClient client;

...

private void ReceiveMessages(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        BrokeredMessage message = null;

        try
        {
            message = this.receiveRetryPolicy.ExecuteAction<BrokeredMessage>(this.DoReceiveMessage);
        }
        catch (Exception e)
        {
            Trace.TraceError("An unrecoverable error occurred while trying to receive a new message:\r\n{0}", e);

            throw;
        }

        try
        {
            if (message == null)
            {
                Thread.Sleep(100);
                continue;
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
}

protected virtual BrokeredMessage DoReceiveMessage()
{
    return this.client.Receive(TimeSpan.FromSeconds(10));
}
```

> **JanaPersona:** This code sample shows how the system uses the
> [Transient Fault Handling Application Block][tfab] to retrieve 
> messages reliably from the topic.

The Windows Azure Service Bus **SubscriptionClient** class uses a 
peek/lock technique to retrieve a message from a subscription. In the 
code sample, the **Receive** method locks the message on the 
subscription. While the message is locked, other clients cannot see it. 
The **Receive** method then tries to process the message. 
If the client processes the message successfully, it calls the 
**Complete** method; this deletes the message from the subscription. 
Otherwise, if the client fails to process the message successfully, it 
calls the **Abandon** method; this releases the lock on the message and 
the same, or a different client can then receive it. If the 
client does not call either the **Complete** or **Abandon** methods 
within a fixed time, the lock on the message is released. 

> **Note:** The **MessageReceived** event passes a reference to the 
> **SubscriptionReceiver** instance so that the handler can call either 
> the **Complete** or **Abandon** methods when it processes the message.

The following code sample from the **MessageProcessor** class shows how 
to call the **Complete** and **Abandon** methods using
the **BrokeredMessage** instance passed as a parameter to the 
**MessageReceived** event. 

```Cs
private void OnMessageReceived(object sender, BrokeredMessageEventArgs args)
{
    var message = args.Message;

    object payload;
    using (var stream = message.GetBody<Stream>())
    using (var reader = new StreamReader(stream))
    {
        payload = this.serializer.Deserialize(reader);
    }

    try
    {
        ...

        ProcessMessage(payload);

        ...
    }
    catch (Exception e)
    {
        if (args.Message.DeliveryCount > MaxProcessingRetries)
        {
            Trace.TraceWarning("An error occurred while processing a new message and will be dead-lettered:\r\n{0}", e);
            message.SafeDeadLetter(e.Message, e.ToString());
        }
        else
        {
            Trace.TraceWarning("An error occurred while processing a new message and will be abandoned:\r\n{0}", e);
            message.SafeAbandon();
        }

        return;
    }

    Trace.TraceInformation("The message has been processed and will be completed.");
    message.SafeComplete();
}
``` 

> **Note:** This example uses an extension method to invoke the
> **Complete** and **Abandon** methods of the **BrokeredMessage**
> reliably using the [Transient Fault Handling Application Block][tfab].

### Why have separate CommandBus and EventBus classes?

Although at this early stage in the development of the Conference 
Management system the implementations of the **CommandBus** and 
**EventBus** classes are very similar and you may wonder why we have both, the team anticipates that they 
will diverge in the future. 

> **DeveloperPersona:** There may be differences in how we invoke 
> handlers and what context we capture for them: commands may want to 
> capture additional runtime state, whereas events typically don't need 
> to. Because of these potential future differences, I didn't want to 
> unify the implementations. I've been there before and ended up 
> splitting them when further requirements came in. 

### How scalable is this approach?

With this approach, you can run multiple instances of the 
**SubscriptionReceiver** class and the various handlers in different 
Windows Azure worker role instances, which enables you to scale out your 
solution. You can also have multiple instances of the **CommandBus**, 
**EventBus**, and **TopicSender** classes in different Windows Azure 
worker role instances. 

For information about scaling the Windows Azure Service Bus 
infrastructure, see [Best Practices for Performance Improvements Using 
Service Bus Brokered Messaging][sbperf] on MSDN. 

### How robust is this approach?

This approach uses the brokered messaging option of the Windows Azure 
Service Bus to provide asynchronous messaging. The Service Bus reliably 
stores messages until consumers connect and retrieve their messages. 

Also, the peek/lock approach to retrieving messages from a queue or 
topic subscription adds reliability in the scenario in which a message 
consumer fails while it is processing the message. If a consumer fails 
before it calls the **Complete** method, the message is still available 
for processing when the consumer restarts. 

### What is the granularity of a topic and a subscription?

The current implementation uses a single topic (**conference/commands**) 
for all commands within the system, and a single topic 
(**conference/events**) for all events within the system. There is a 
single subscription for each topic, and each subscription receives all 
of the messages published to the topic. It is the responsibility of the 
**CommandProcessor** and **EventProcessor** classes to deliver the 
messages to the correct handlers. 

In the future, the team will examine the options of using multiple topics &mdash; for example, using a separate command topic for each bounded context; and multiple subscriptions &mdash; such as one per event type. These alternatives may simplify the code and facilitate scaling of the application across multiple worker roles. 

> **JanaPersona:** There are no costs associated with having 
> multiple topics, subscriptions, or queues. Windows Azure Service Bus 
> usage is billed based on the number of messages sent and the amount of 
> data transferred out of a Windows Azure sub-region. 

### How are commands and events serialized?

The Contoso Conference Management System uses the [Json.NET][jsonnet] 
serializer. For details on how the application uses this serializer,
see [Technologies Used in the Reference Implementation][r_chapter9] in
the Reference Guide. 

> "You should consider whether you always need to use the Windows Azure
> Service Bus for commands. Commands are typically used within a bounded
> context and you may not need to send them across a process boundary
> (on the write-side you may not need additional tiers), in which case
> you could use an in memory queue to deliver your commands."
  
> &mdash; Greg Young, conversation with the patterns and practices team

# Impact on testing

Because this was the first bounded context the team tackled, one of the key concerns was how to approach testing given that the team wanted to adopt a test-driven development approach. The following conversation between two developers about how to do TDD when they are implementing the CQRS pattern without event sourcing summarizes their thoughts: 


> *Developer 1*: If we were using event sourcing, it would be easy to use 
> a TDD approach when we were creating our domain objects. The input to the 
> test would be a command (that perhaps originated in the UI), and we 
> could then test that the domain object fires the expected events. 
> However if we're not using event sourcing, we don't have any events: the 
> behavior of the domain object is to persist its changes in data store 
> through an ORM layer. 

> *Developer 2*: So why don't we raise events anyway? Just because we're 
> not using event sourcing doesn't mean that our domain objects can't 
> raise events. We can then design our tests in the usual way to check for 
> the correct events firing in response to a command. 

> *Developer 1*: Isn't that just making things more complicated than they 
> need to be? One of the motivations for using CQRS is to simplify things! 
> We now have domain objects that need to persist their state using an ORM 
> layer and raise events that report on what they have persisted just 
> so we can run our unit tests. 

> *Developer 2*: I see what you mean. 

> *Developer 1*: Perhaps we're getting stuck on how we're doing the 
> tests. Maybe instead of designing our tests based on the expected 
> *behavior* of the domain objects, we should think about testing the 
> *state* of the domain objects after they've processed a command. 

> *Developer 2*: That should be easy to do; after all, the domain objects 
> will have all of the data we want to check stored in properties so that 
> the ORM can persist the right information to the store. 

> *Developer 1*: So we really just need to think about a different style 
> of testing in this scenario. 

> *Developer 2*: There is another aspect of this we'll need to consider: 
> we might have a set of tests that we can use to test our domain objects, 
> and all of those tests might be passing. We might also have a set of 
> tests to verify that our ORM layer can save and retrieve objects 
> successfully. However, we will also have to test that our domain objects 
> function correctly when we run them against the ORM layer. It's possible 
> that a domain object performs the correct business logic, but can't 
> properly persist its state, perhaps because of a problem related to how 
> the ORM handles specific data types. 

For more information about the two approaches to testing discussed here, 
see Martin Fowler's article [Mocks Aren't Stubs][tddstyle] and
[Point/Counterpoint][point] by Steve Freeman, Nat Pryce, and Joshua
Kerievsky.

> **Note:** The tests included in the solution are written using
> xUnit.net.

The following code sample shows two examples of tests written using the 
behavioral approach discussed above. 

> **MarkusPersona:** These are the tests we started with, but we then
  replaced them with state-based tests.

```Cs
public SeatsAvailability given_available_seats()
{
	var sut = new SeatsAvailability(SeatTypeId);
	sut.AddSeats(10);
	return sut;
}
		
[TestMethod]
public void when_reserving_less_seats_than_total_then_succeeds()
{
	var sut = this.given_available_seats();
	sut.MakeReservation(Guid.NewGuid(), 4);
}

[TestMethod]
[ExpectedException(typeof(ArgumentOutOfRangeException))]
public void when_reserving_more_seats_than_total_then_fails()
{
	var sut = this.given_available_seats();
	sut.MakeReservation(Guid.NewGuid(), 11);
}
```

These two tests work together to verify the behavior of the 
**SeatsAvailability** aggregate. In the first test, the 
expected behavior is that the **MakeReservation** method succeeds and 
does not throw an exception. In the second test, the expected behavior 
is for the **MakeReservation** method to throw an exception because 
there are not enough free seats available to complete the reservation. 

It is difficult to test the behavior in any other way without the 
aggregate raising events. For example, if you tried to test the behavior 
by checking that the correct call is made to persist the aggregate to 
the data store, the test becomes coupled to the data store 
implementation (which is a smell); if you want to change the data store
implementation, you will need to change the tests on the aggregates in
the domain model. 

The following code sample shows an example of a test written using the 
state of the objects under test. This style of test is the one used
in the project.

```Cs
public class given_available_seats
{
	private static readonly Guid SeatTypeId = Guid.NewGuid();

	private SeatsAvailability sut;
	private IPersistenceProvider sutProvider;

	protected given_available_seats(IPersistenceProvider sutProvider)
	{
		this.sutProvider = sutProvider;
		this.sut = new SeatsAvailability(SeatTypeId);
		this.sut.AddSeats(10);

		this.sut = this.sutProvider.PersistReload(this.sut);
	}

	public given_available_seats()
		: this(new NoPersistenceProvider())
	{
	}

	[Fact]
	public void when_reserving_less_seats_than_total_then_seats_become_unavailable()
	{
		this.sut.MakeReservation(Guid.NewGuid(), 4);
		this.sut = this.sutProvider.PersistReload(this.sut);

		Assert.Equal(6, this.sut.RemainingSeats);
	}

	[Fact]
	public void when_reserving_more_seats_than_total_then_rejects()
	{
        var id = Guid.NewGuid();
        sut.MakeReservation(id, 11);

        Assert.Equal(1, sut.Events.Count());
        Assert.Equal(id, ((ReservationRejected)sut.Events.Single()).ReservationId);
	}
}
```

The two tests shown here test the state of the **SeatsAvailability** 
aggregate after invoking the **MakeReservation** method. The first test 
tests the scenario in which there are enough seats available. The second 
test tests the scenario in which there are not enough seats available. 
This second test can make use of the behavior of the **SeatsAvailability** 
aggregate because the aggregate does raise an event if it rejects a 
reservation. 

[j_chapter4]:     Journey_04_ExtendingEnhancing.markdown
[j_chapter5]:     Journey_05_PaymentsBC.markdown
[j_chapter6]:     Journey_06_V2Release.markdown
[j_chapter7]:     Journey_07_V3Release.markdown
[r_chapter3]:     Reference_03_ESIntroduction.markdown
[r_chapter4]:     Reference_04_DeepDive.markdown
[r_chapter6]:     Reference_06_Sagas.markdown
[r_chapter9]:     Reference_09_Technologies.markdown
[appendix1]:      Appendix1_Running.markdown

[tddstyle]:       http://martinfowler.com/articles/mocksArentStubs.html
[repourl]:        https://github.com/mspnp/cqrs-journey-code
[res-pat]:        http://www.rgoarchitects.com/nblog/2009/09/08/SOAPatternsReservations.aspx
[sbperf]:         http://msdn.microsoft.com/en-us/library/hh528527.aspx
[jsonnet]:        http://james.newtonking.com/pages/json-net.aspx
[sagapaper]:      http://www.amundsen.com/downloads/sagas.pdf
[tfab]:           http://msdn.microsoft.com/en-us/library/hh680934(PandP.50).aspx
[cqrsjourneysite]: http://cqrsjourney.github.com/
[memento]:        http://www.oodesign.com/memento-pattern.html
[downloadc]:      http://NEEDFWLINK
[prg]:            http://en.wikipedia.org/wiki/Post/Redirect/Get
[sbq]:            http://msdn.microsoft.com/en-us/library/windowsazure/hh767287.aspx
[point]:          http://doi.ieeecomputersociety.org/10.1109/MS.2007.84

[fig1]:           images/OrderMockup.png?raw=true
[fig2]:           images/Journey_03_Aggregates_01.png?raw=true
[fig3]:           images/Journey_03_Aggregates_02.png?raw=true
[fig4]:           images/Journey_03_Aggregates_03.png?raw=true
[fig5]:           images/Journey_03_Architecture_01.png?raw=true
[fig6]:           images/Journey_03_Architecture_02.png?raw=true
[fig7]:           images/Journey_03_Sequence_01.png?raw=true
[fig8]:           images/Journey_03_ServiceBus_01.png?raw=true