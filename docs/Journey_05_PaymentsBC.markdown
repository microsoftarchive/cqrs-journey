### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Chapter 5: Preparing for the V1 Release  

*Adding functionality and refactoring in preparation for the V1 release.*

_"Most people, after accomplishing something, use it over and over again like a gramophone record till it cracks, forgetting that the past is just the stuff with which to make more future." Freya Stark_

# A description of the Contoso Conference Management V1 release

This chapter describes the changes made by the team to prepare for the 
first production release of the Contoso Conference Management System. 
This work includes some refactoring and additions to the **Orders and 
Registrations** bounded context that the previous two chapters introduce
as well as a new **Conference Management** bounded context and
a new **Payments** bounded context. 

One of the key refactorings undertaken by the team during this phase of 
the journey was to introduce event sourcing into the Orders and 
Registrations bounded context. 

One of the anticipated benefits from implementing the CQRS pattern is 
that it will help to manage change in a complex system. Having a V1 
release during the CQRS journey will help the team to evaluate how the 
CQRS pattern and event sourcing deliver these benefits when we move 
forward from the V1 release to the next production release of the 
system. The following chapters will describe what happens after the V1 
release. 

This chapter also describes the Metro-inspired UI that the team added to 
the public website during this phase and includes a discussion of 
task-based UIs. 

## Working definitions for this chapter 

The remainder of this chapter uses the following definitions. 
For more detail, and possible alternative definitions, see the chapter 
[A CQRS and ES Deep Dive][r_chapter4] in the Reference Guide. 

### Access Code

When a Business Customer creates a new Conference, the system generates 
a five character Access Code and sends it by email to the Business 
Customer. The Business Customer can use his email address and the Access 
Code on the Conference Management Web Site to retrieve the conference 
details from the system at a later date. The system uses access codes 
instead of passwords to avoid the overhead for the Business Customer of 
setting up an account with the system.

### Event Sourcing

Event Sourcing is a way of persisting and reloading the state of 
aggregates within the system. Whenever the the state of an aggregate 
changes, the aggregate raises an event detailing the state change. The 
system then saves this event in an event store. The system can recreate 
the state of an aggregate by replaying all of the previously saved 
events associated with that aggregate instance. The event store becomes 
the book of record for the data stored by the system. 

In addition, you can use event sourcing as a source of audit data, as a 
way to query historic state, gain new business insights from past data, 
and to replay events for debugging and problem analysis.

### Eventual consistency

Eventual consistency is a consistency model whereby after an update to a 
data object, the storage system does not guarantee that subsequent 
accesses to that object will return the updated value. However, the 
storage system does guarantee that if no new updates are made to the 
object during a sufficiently long period of time, then eventually all 
accesses can be expected to return the last updated value. 

## User stories 

The team implemented the user stories listed below during this phase of 
the project. 

### Ubiquitous language definitions

The **Business Customer** represents the organization that is using the 
conference management system to run its conference. 

A **Seat** represents a space at a conference or access to a specific 
session at the conference such as a cocktail party, a tutorial, or a 
workshop. 

A **Registrant** is a person who interacts with the system to make 
Orders and to make payments for those Orders. A Registrant also creates 
the Registrations associated with an Order. 

### Conference Management bounded context user stories

A Business Customer can create new conferences and manage them. After a 
Business Customer creates a new conference, he can access the details of 
the conference by using his email address and conference locator access 
code. The system generates the access code when the Business Customer 
creates the conference. 

The Business Customer can specify the following information about a 
conference: 

* The name, description, and slug (part of the URL used to access the
  conference).
* The start and end dates of the conference.
* The different types and quotas of seats available at the conference.

Additionally, the Business Customer can control the visibility of the 
conference on the public website by either publishing or un-publishing 
the conference. 

The Business Customer can use the conference management website to view 
a list of orders and Attendees. 

### Ordering and Registration bounded context user stories

When a Registrant creates an order, it may not be possible to fulfill 
the order completely. For example, a Registrant may request five seats 
for the full conference, five seats for the welcome reception, and three 
seats for the pre-conference workshop. There may only be three seats 
available and one seat for the welcome reception, but more than three 
seats available for the pre-conference workshop. The system displays 
this information to the Registrant and gives the Registrant the 
opportunity to adjust the number of each type of seat in the order 
before continuing to the payment process. 

After a Registrant has selected the quantity of each seat type, the 
system calculates the total to pay for the order, and the Registrant can 
then pay for those seats using an online payment service. Contoso does
not handle payments on behalf of its customers: each Business Customer
must have a mechanism for accepting payments through an online payments
service. In a later stage of the project, Contoso will add support for
Business Customers to integrate their invoicing systems with the
Conference Management System. At some future time, Contoso may offer a
service to collect payments on behalf of customers.

> **Note:** In this version of the system, the actual payment is
> simulated.

After a Registrant has purchased seats at a conference, she can assign 
Attendees to those seats. The system stores the name and contact details 
for each Attendee.

## Architecture 

Figure 1 illustrates the key architectural elements of the Contoso 
Conference Management System in the V1 release. The application consists 
of two web sites and three bounded contexts. The infrastructure includes 
SQL databases, an event store, and messaging infrastructure. 

The first table that follows figure 1 lists all of the messages that the 
artifacts (aggregates, MVC controllers, read-model generators, and data
access objects) shown on the diagram exchange with each other. 

> **Note:** For reasons of clarity, the handlers (such as the
> **OrderCommandHandler** class) that deliver the messages to the domain
> objects are not shown.

![Figure 1][fig1]

**Architecture of the V1 release**


<table border="1">
  <thead>
    <tr>
      <th align="left">Element</th>
      <th align="left">Type</th>
      <th align="left">Sends</th>
      <th align="left">Recieves</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td align="left">ConferenceController</td>
      <td align="left">MVC Controller</td>
      <td align="left">N/A</td>
      <td align="left">ConferenceDetails</td>
    </tr>
    <tr>
      <td align="left">OrderController</td>
      <td align="left">MVC Controller</td>
      <td align="left">AssignSeat<br/>
                       UnassignSeat</td>
      <td align="left">DraftOrder<br/>
                       OrderSeats<br/>
                       PricedOrder</td>
    </tr>
    <tr>
      <td align="left">RegistrationController</td>
      <td align="left">MVC Controller</td>
      <td align="left">RegisterToConference<br/>
                       AssignRegistrantDetails<br/>
                       InitiateThirdPartyProcessorPayment</td>
      <td align="left">DraftOrder<br/>
                       PricedOrder<br/>
                       SeatType</td>
    </tr>
    <tr>
      <td align="left">PaymentController</td>
      <td align="left">MVC Controller</td>
      <td align="left">CompleteThirdPartyProcessorPayment<br/>
                       CancelThirdPartyProcessorPayment</td>
      <td align="left">ThirdPartyProcessorPaymentDetails</td>
    </tr>
    <tr>
      <td align="left">Conference Management</td>
      <td align="left">CRUD Bounded Context</td>
      <td align="left">ConferenceCreated<br/>
                       ConferenceUpdated<br/>
                       ConferencePublished<br/>
                       ConferenceUnpublished<br/>
                       SeatCreated<br/>
                       SeatUpdated<br/></td>
      <td align="left">OrderPlaced<br/>
                       OrderRegistrantAssigned<br/>
                       OrderTotalsCalculated<br/>
                       OrderPaymentConfirmed<br/>
                       SeatAssigned<br/>
                       SeatAssignmentUpdated<br/>
                       SeatUnassigned</td>
    </tr>
    <tr>
      <td align="left">Order</td>
      <td align="left">Aggregate</td>
      <td align="left">OrderPlaced<br/>
                      *OrderExpired<br/>
                      *OrderUpdated<br/>
                      *OrderPartiallyReserved<br/>
                      *OrderReservationCompleted<br/>
                      *OrderPaymentConfirmed<br/>
                      *OrderRegistrantAssigned</td>
      <td align="left">RegisterToConference<br/>
                       MarkSeatsAsReserved<br/>
                       RejectOrder<br/>
                       AssignRegistrantDetails<br>
                       ConfirmOrderPayment</td>
    </tr>
    <tr>
      <td align="left">SeatsAvailability</td>
      <td align="left">Aggregate</td>
      <td align="left">SeatsReserved<br/>
                       *AvailableSeatsChanged<br/>
                       *SeatsReservationCommitted<br/>
                       *SeatsReservationCancelled</td>
      <td align="left">MakeSeatReservation<br/>
                       CancelSeatReservation<br/>
                       CommitSeatReservation<br/>
                       AddSeats<br/>
                       RemoveSeats</td>
    </tr>
    <tr>
      <td align="left">SeatAssignments</td>
      <td align="left">Aggregate</td>
      <td align="left">*SeatAssignmentsCreated<br/>
                       *SeatAssigned<br/>
                       *SeatUnassigned<br/>
                       *SeatAssignmentUpdated</td>
      <td align="left">AssignSeat<br/>
                       UnassignSeat</td>
    </tr>
    <tr>
      <td align="left">RegistrationProcessManager</td>
      <td align="left">Process manager</td>
      <td align="left">MakeSeatReservation<br/>
                       ExpireRegistrationProcess<br/>
                       MarkSeatsAsReserved<br/>
                       CancelSeatReservation<br/>
                       RejectOrder<br/>
                       CommitSeatReservation<br/>
                       ConfirmOrderPayment</td>
      <td align="left">OrderPlaced<br/>
                       PaymentCompleted<br/>
                       SeatsReserved<br/>
                       ExpireRegistrationProcess</td>
    </tr>
    <tr>
      <td align="left">OrderViewModelGenerator</td>
      <td align="left">Handler</td>
      <td align="left">DraftOrder</td>
      <td align="left">OrderPlaced<br/>
                       OrderUpdated<br/>
                       OrderPartiallyReserved<br/>
                       OrderReservationCompleted<br/>
                       OrderRegistrantAssigned</td>
    </tr>
    <tr>
      <td align="left">PricedOrderViewModelGenerator</td>
      <td align="left">Handler</td>
      <td align="left">N/A</td>
      <td align="left">SeatTypeName</td>
    </tr>
    <tr>
      <td align="left">ConferenceViewModelGenerator</td>
      <td align="left">Handler</td>
      <td align="left">Conference<br/>
                       AddSeats<br/>
                       RemoveSeats<br/></td>
      <td align="left">ConferenceCreated<br/>
                       ConferenceUpdated<br/>
                       ConferencePublished<br/>
                       ConferenceUnpublished<br/>
                       **SeatCreated<br/>
                       **SeatUpdated</td>
    </tr>
    <tr>
      <td align="left">ThirdPartyProcessorPayment</td>
      <td align="left">Aggregate</td>
      <td align="left">PaymentCompleted<br/>
                       PaymentRejected<br/>
                       PaymentInitiated</td>
      <td align="left">InitiateThirdPartyProcessorPayment<br/>
                       CompleteThirdPartyProcessorPayment<br/>
                       CancelThirdPartyProcessorPayment</td>
    </tr>
  </tbody>
</table>

> \* These events are only used for persisting aggregate state using
> event sourcing.  
> \*\* The **ConferenceViewModelGenerator** creates these commands from
> the **SeatCreated** and **SeatUpdated** events that it handles from
> the Conference Management bounded context.

The following list outlines the message naming conventions in the Contoso Conference Management System
* All events use the past tense the naming convention.
* All commands use the imperative naming convention.
* All DTOs are nouns.

The application is designed to deploy to Windows Azure. At this stage in 
the journey, the application consists of two web roles that contain the 
MVC web applications and a worker role that contains the message 
handlers and domain objects. The application uses SQL Database databases 
for data storage, both on the write-side and the read-side. The Orders 
and Registrations bounded context now uses an event store to persist the 
state from the write-side. This event store is implemented using Windows 
Azure table storage to store the events. The application uses the 
Windows Azure Service Bus to provide its messaging infrastructure. 

While you are exploring and testing the solution, you can run it 
locally, either using the Windows Azure compute emulator or by running 
the MVC web application directly and running a console application that 
hosts the handlers and domain objects. When you run the application 
locally, you can use a local SQL Express database instead of SQL Database, 
use a simple messaging infrastructure implemented in a SQL Express 
database, and a simple event store also implemented using a SQL Express 
database. 

> **Note:** The SQL-based implementations of the event store and the
> messaging infrastructure are only intended to facilitate running the
> application locally for understanding and testing. They are not
> intended to illustrate a production-ready approach.

For more information about the options for running the application, see 
[Appendix 1][appendix1].

### Conference Management bounded context

The Conference Management bounded context is a simple two-tier, 
CRUD-style web application. It is implemented using MVC 4 and Entity 
Framework. 

> **JanaPersona:** The team implemented this bounded context after it
> implemented the public Conference Management website that uses MVC 3.
> In a later stage of the journey, as part of the V3 release, the
> Conference Management site will be upgraded to MVC 4.

This bounded context must integrate with other bounded contexts that 
implement the CQRS pattern. 

# Patterns and concepts 

This section describes some of the key areas of the application that the 
team visited during this stage of the journey and introduces some of the 
challenges met by the team when we addressed these areas. 

## Event Sourcing

The team at Contoso originally implemented the Orders and Registrations 
bounded context without using event sourcing. However, during the 
implementation it became clear that using event sourcing would help to 
simplify this bounded context. 

In the previous chapter, [Extending and Enhancing the Orders and 
Registrations Bounded Contexts][j_chapter4] the team found that we 
needed to use events to push changes from the write-side to the 
read-side. On the read-side the **OrderViewModelGenerator** class 
subscribed to the events published by the **Order** aggregate, and used 
those events to update the views in the database that were queried by 
the read-model. 

This was already half-way to an event sourcing implementation, so it 
made sense to use a single persistence mechanism based on events for the 
whole bounded context. 

The event sourcing infrastructure is reusable in other bounded contexts, 
and the implementation of the Orders and Registrations becomes simpler. 

> **PoePersona:** As a practical problem, the team had limited time
> before the V1 release to implement a production quality event store.
> They created a simple, basic event store based on Windows Azure tables
> as an interim solution. However, they will potentially face the
> problem in the future of migrating from one event store to another.


> Evolution is key here; for example, one could show how implementing
> event sourcing allows you to get rid of those tedious data migrations,
> and even allows you to build reports from the past.  
> Tom Janssens - CQRS Advisors Mail List

The team implemented the basic event store using Windows Azure table 
storage. If you are hosting your application in Windows Azure, you could 
also consider using Windows Azure blobs or SQL Database to store your 
events.

When choosing the underlying technology for your event store, you should 
ensure that your choice can deliver the required level of availability, 
consistency, reliability, scale, and performance for your application. 

> **JanaPersona:** One of the issues to consider when choosing between
> storage mechanisms in Windows Azure is cost. If you use SQL Database you
> are billed based on the size of the database, if you use Windows Azure
> table or blob storage you are billed based on the amount of storage
> you use and the number of storage transactions. You need to carefully
> evaluate the usage patterns on the different aggregates in your system
> to determine which storage mechanism is the most cost effective. It
> may turn out that different storage mechanisms make sense for
> different aggregate types. You may be able to introduce optimizations
> that lower your costs, for example by using caching to reduce the
> number of storage transactions.


> My rule of thumb is that if you're doing greenfield development, you
> need very good arguments in order to choose SQL Database. Windows Azure
> Storage Services should be the default choice. However, if you already
> have an existing SQL Server database that you want to move to the
> cloud, it's a different case...  
> Mark Seeman - CQRS Advisors Mail List

### Identifying aggregates

In the Windows Azure table storage based implementation of the event 
store that the team created for the V1 release, we used the aggregate 
id as the partition key. This makes it efficient to locate the partition 
that holds the events for any particular aggregate. 

In some cases, the system must locate related aggregates. For example,
an order aggregate may have a related registrations aggregate that holds 
details of the Attendees assigned to specific seats. In this scenario, 
the team decided to reuse the same aggregate id for the related pair of 
aggregates (the order and registration aggregates) in order to
facilitate look-ups. 

> **GaryPersona:** You want to consider in this case whether you
> should have two aggregates. You could model the registrations as an
> entity inside the order aggregate.

A more common scenario is to have a one-to-many relationship between 
aggregates instead of a one-to-one. In this case, it is not possible to 
share aggregate ids: instead the aggregate on the one-side can store a 
list of the ids of the aggregates on the many-side, and each aggregate 
on the many-side can store the id of the aggregate on the one-side. 

> Sharing aggregate ids is common when the aggregates exist in different
> bounded contexts. If you have aggregates in different bounded contexts
> that model different facets of the same real-world entity, it makes
> sense for them to share the same id. This makes it easier to follow a
> real-world entity as different bounded contexts in your system process 
> it.  
> Greg Young - Conversation with the PnP team.

## Task-based UI

The design of UIs has improved greatly over the last decade: 
applications are easier to use, more intuitive, and simpler to navigate 
than they were before. Examples of guidelines for UI designers are the 
[Microsoft Inductive User Interface Guidelines][inductiveui] and the [UX 
guidelines for Metro style apps][metroux]. 

Another factor that affects the design and usability of the UI is how 
the UI communicates with the rest of the application. If the application 
is based on a CRUD-style architecture, this can leak through to the UI. 
If the developers focus on CRUD-style operations, this can result in a 
UI as shown in the first screen design in Figure 2. 

![Figure 2][fig2]

**Example UIs for conference registration**

On the first screen, the labels on the buttons reflect the underlying 
CRUD operations that the system will perform when the user clicks the 
**Submit** button. The first screen also requires the user to apply some 
deductive knowledge about how the screen and the application function. 
For example, the function of the **Add** button is not immediately 
apparent. 

A typical implementation behind the first screen will use a data 
transfer object (DTO) to exchange data between the back-end and the UI. 
The UI will request data from the back-end that will arrive encapsulated 
in a DTO, it will modify the data in the DTO, and then return the DTO 
to the back-end. The back-end will use the DTO to figure out what CRUD 
operations it must perform on the underlying data store. 

The second screen is more explicit about what is happening in terms of 
the business process: the user is selecting quantities of seat types as 
a part of the conference registration task. Thinking about the UI in 
terms of the task that the user is performing makes it easier to relate 
the UI to the write-model in your implementation of the CQRS pattern. 
The UI can send commands to the write-side, and those commands are a 
part of the domain model on the write-side. In a bounded context that 
implements the CQRS pattern, the UI typically queries the read-side and 
receives a DTO, and sends commands to the write-side. 

![Figure 3][fig3]

**Task-based UI flow**

Figure 3 shows a sequence of pages that enable the Registrant to 
complete the "purchase seats at a conference" task. On the first page, 
the Registrant selects the type and quantity of seats. On the second 
page, the Registrant can review the seats she has reserved, enter her 
contact details, and complete the necessary payment details. The system 
then redirects the Registrant to a payment provider, and if the payment 
completes successfully, the system displays the third page. The third 
page shows a summary of the order and provides a link to pages where the 
Registrant can start additional tasks. 

The sequence shown in Figure 3 is deliberately simplified in order to 
highlight the roles of the commands and queries in a task-based UI. For 
example, the real flow includes pages that the system will display based 
on the payment type selected by the Registrant, and error pages that the 
system displays if the payment fails. 

> **GaryPersona:** You don't always need to use task-based UIs. In
> some scenarios, simple CRUD-style UIs work well. You must evaluate
> whether benefits of task-based UIs outweigh the additional 
> implementation effort of a task-based UI. Very often, the bounded
> contexts where you choose to implement the CQRS pattern are also the
> bounded contexts that benefit from task-based UIs because of the 
> more complex business logic and more complex user interactions. 

> I would like to state once and for all that CQRS does not require a
> task based UI. We could apply CQRS to a CRUD based interface (though
> things like creating separated data models would be much harder).  
> There is however one thing that does really require a task based UI…
> That is Domain Driven Design.  
> Greg Young, [CQRS, Task Based UIs, Event Sourcing agh!][gregtask].

For more information, see the chapter 
[A CQRS and ES Deep Dive][r_chapter4] in the Reference Guide. 

## CRUD

You should not use the CQRS pattern as part of your top-level 
architecture: you should implement the pattern only in those bounded 
contexts where it brings clear benefits. In the Contoso Conference 
Management System, the conference management bounded context is a 
relatively simple, stable, and low volume part of the overall system. 
Therefore, the team decided that we would implement this bounded 
context using a traditional two-tier, CRUD-style architecture. 

For a discussion about when CRUD-style architecture is, or is not, 
appropriate see the blog post, [Why CRUD might be what they want, but 
may not be what they need][crudpost]. 

## Integration between bounded contexts

The Conference Management bounded context needs to integrate with the 
Orders and Registrations bounded context. For example, if the business 
customer changes the quota for a seat type in the Conference Management 
bounded context, this change must be propagated to the Orders and 
Registrations bounded context. Also, if a Registrant adds a new Attendee 
to a conference, the Business Customer must be able to view details of 
the Attendee in the list in the Conference Management website. 

### Pushing changes from the Conference Management bounded context

The following conversation between several developers and the domain 
expert highlights some of the key issues that the team needed to address 
in planning how to implement this integration. 

> *Developer #1*: I want to talk about how we should implement two
> pieces of the integration story associated with our CRUD-style,
> Conference Management bounded context. First of all, when a Business
> Customer creates a new conference or defines new seat types for an
> existing conference in this bounded context, other bounded contexts
> such as the Orders and Registrations bounded context will need to know
> about the change. Secondly, when a Business Customer changes the quota
> for a seat type other bounded contexts will need to know about this
> change as well.

> *Developer #2*: So in both cases you are pushing changes from the
> Conference Management bounded context. It's one-way.

> *Developer #1*: Correct.

> *Developer #2*: What are the significant differences between the
> scenarios that you outlined?

> *Developer #1*: In the first scenario, these changes are relatively
> infrequent and typically happen when the Business Customer creates the
> conference. Also, these are append only changes. We don't allow a
> Business Customer to delete a conference or a seat type after the
> conference has been published for the first time. In the second
> scenario, the changes might be more frequent and a Business Customer
> might increase or decrease a seat quota.

> *Developer #2*: What implementation approaches are you considering for
> these integration scenarios?

> *Developer #1*: Because we have a two-tier CRUD-style bounded context,
> for the first scenario I was planning on exposing the conference and
> seat type information directly from the database as a simple read-only
> service. For the second scenario, I was planning to publish events
> whenever the Business Customer updates the seat quotas.

> *Developer #2*: Why use two different approaches here, it would be
> simpler to use a single approach. Using events is more flexible in the
> long run. If additional bounded contexts need this information, they
> can easily subscribe to the event. Using events provides for more
> decoupling between the bounded contexts.

> *Developer #1*: I can see that it would be easier to adapt to changing
> requirements in the future if we used events. For example, if a new
> bounded context required information about who changed the quota, we
> add this information to the event. For existing bounded contexts, we
> could add an adapter that converted the new event format to the old.

> *Developer #2*: You implied that the events that notify subscribers of
> quota changes would send the change in the quota. For example, the
> Business Customer increased a seat quota by 50. What happens if a
> subscriber wasn't there at the beginning and so doesn't receive the
> full history of updates?

> *Developer #1*: We may have to include some synchronization mechanism
> that uses snapshots of the current state. However, in this case the
> event could simply report the new value of the quota. If necessary,
> the event could report both the delta and the absolute value of the
> seat quota.

> *Developer #2*: How are you going to ensure consistency? You need to
> guarantee that your bounded context persists its data to storage and
> publishes the events on a message queue.

> *Developer #1*: We can wrap the database write and the add-to-queue
> operations in a transaction.

> *Developer #2*: There are two reasons that's going to be problematic
> in the long-run when the size of the network increases, response times
> get longer, and the probablility of failure increases.
> Firstly, our infrastructure uses the Windows Azure Service Bus for
> messages. You can't use a transaction to combine sending a message on
> the Service Bus and write to a database. Secondly, we're trying to 
> avoid two-phase commits because they always cause problems in the
> long-run.

> *Domain Expert*: We have a similar scenario with another bounded
> context that we'll be looking at later. In this case, we can't make any
> changes to the bounded context, we no longer have an up to date copy
> of the source code.

> *Developer #1*: What can we do to avoid using a two-phase commit? And
> what can we do if we don't have access to the source code and so can't
> make any changes?

> *Developer #2*: In both cases, we use the same technique to solve the
> problem. Instead of publishing the events from within the application
> code, we can use another process that monitors the database and sends
> the events when it detects a change in the database. This solution may
> introduce a small amount of latency, but it does avoid the need for a
> two-phase commit and you can implement it without making any changes
> to the application code.

A further issue relates to when and where to persist integration events. 
In the example discussed above, the Conference Management bounded 
context publishes the events and the Orders and Registrations bounded 
context handles them and uses them to populate its read-model. If a 
failure occurs that causes the system to lose the read-model data, then 
without saving the events there is no way to re-create this read-model 
data. 

Whether you need to persist these integration events will depend on the 
specific requirements and implementation of your application. For 
example: 

* The write-side may handle the integration, not the
  read-side as in the current example. The events will then result in
  changes on the write-side it persists as other events.
* Integration events may represent transient data that does not need to
  be persisted.
* Integration events from a CRUD-style bounded context may contain state
  data so that only the last event is needed. For example if the event
  from the Conference Management bounded context includes the current
  seat quota, you may not be interested in previous values.

> Another approach to consider is to use an event-store that multiple
> bounded contexts share. In this way, the originating bounded
> context (for example the CRUD-style Conference Management bounded
> context) could be responsible for persisting the integration events.  
> Greg Young - Conversation with the PnP team.

#### Some comments on Windows Azure Service Bus

The previous discussion suggested a way to avoid using a distributed 
two-phase commit in the Conference Management bounded context. However, 
there are some alternative approaches. 

Although the Windows Azure Service Bus does not support distributed 
transactions with databases, you can use the 
**RequiresDuplicateDetection** property when you send messages, 
and the **PeekLock** mode when you receive messages to create the 
desired level of robustness without using a distributed transaction.

As another approach, you can use a distributed transaction to update the 
database and send a message using a local MSMQ queue. You can then use a 
bridge to connect the MSMQ queue to a Windows Azure Service Bus queue. 

For example of implementing a bridge from MSMQ to Windows Azure Service 
Bus, see the sample in the [Windows Azure AppFabric SDK][appfabsdk]. 

For more information about the Windows Azure Service Bus, see 
[Technologies Used in the Reference Implementation][r_chapter9] in the 
Reference Guide. 

### Pushing changes to the Conference Management bounded context

Pushing information about completed orders and registrations from the 
Orders and Registrations bounded context to the Conference Management 
bounded context raised a different set of issues. 

The Orders and Registrations bounded context typically raises many of 
the following events during the creation of an order: **OrderPlaced**, 
**OrderRegistrantAssigned**, **OrderTotalsCalculated**, 
**OrderPaymentConfirmed**, **SeatAssignmentsCreated**, 
**SeatAssignmentUpdated**, **SeatAssigned**, and **SeatUnassigned**. The 
bounded context uses these events to communicate between aggregates and 
for event sourcing. 

For the Conference Management bounded context to capture the information 
that it requires to display information about registrations and 
Attendees, it must handle all of these events. It can use the 
information that these events contain to create a de-normalized SQL 
table of the data, that the Business Customer can then view in the UI. 

The issue with this approach is that the Conference Management bounded 
context needs to understand a complex set of events from another bounded 
context. It is a brittle solution because a change in the Orders and 
Registrations bounded context may break this feature in the Conference 
Management bounded context. 

Contoso plans to keep this solution for the V1 release of the system, 
but will evaluate alternatives during the next stage of the journey. 
These alternative approaches will include: 

1. Modify the Orders and Registrations bounded context to generate more
   useful events designed explicitly for integration.
2. Generate the de-normalized data in the Orders and Registrations
   bounded context and notify the Conference Management bounded context
   when the data is ready. The Conference Management bounded context can
   then request the information through a service call.
   
> **Note:** To see how the current approach works, look at the
> **OrderEventHandler** class in the **Conference** project.

### Choosing when to update the read-side data

In the Conference Management bounded context, the Business Customer can 
change the description of a seat type. This results in a **SeatUpdated** 
event that the **ConferenceViewModelGenerator** class in the Orders and 
Registrations bounded context handles; This class updates the read-model 
data to reflect the new information about the seat type. The UI displays 
the new seat description when a Registrant is making an order. 

However, if a Registrant views a previously created order (for example 
to assign Attendees to seats), the Registrant sees the original seat 
description. 

> **CarlosPersona:** This is a deliberate business decision: we don't
> want to confuse Registrants by changing the seat description after
> they create an order.


> **GaryPersona:** If we did want to update the seat description on
> existing orders, we would need to modify the
> **PricedOrderViewModelGenerator** class to handle the **SeatUpdated**
> and adjust its view model.

## Distributed transactions and Event Sourcing

The previous section that discussed the integration options for the 
**Conference Management** bounded context raised the issue of using a 
distributed, two-phase commit transaction to ensure consistency between 
the database that stores the conference management data and the 
messaging infrastructure that publishes changes to other bounded 
contexts. 

The same problem arises when you implement event sourcing: you must 
ensure consistency between the event store in the bounded context that 
stores all the events and the messaging infrastructure that publishes 
those events to other bounded contexts. 

A key feature of an event store implementation should be that it offers 
a way to guarantee consistency between the events that it stores and the 
events that the bounded context publishes to other bounded contexts.

> **CarlosPersona:** This is a key challenge you should address if you
> decide to implement an event store yourself. If you are designing a
> scalable event store that you plan to deploy in a distributed
> environment such as Windows Azure, you must be very careful to ensure
> that you meet this requirement. 

## Autonomy versus authority

The **Orders and Registrations** bounded context is responsible for 
creating and managing orders on behalf of Registrants. The **Payments** 
bounded context is responsible for managing the interaction with an 
external payments system so that Registrants can pay for the seats that 
they have ordered. 

When the team was examining the domain models for these two bounded 
contexts, it discovered that neither of them knew anything about 
pricing. The **Orders and Registrations** bounded context created an 
order that listed the quantities of the different seat types that the 
Registrant requested. The **Payments** bounded context simply passed a 
total to the external payments system. At some point, the system needed 
to calculate the total from the order before invoking the payment 
process. 

The team considered two different approaches to solve this problem: 
favoring autonomy and favoring authority. 

### Favoring Autonomy

This approach assigns the responsibility for calculating the order total
to the **Orders and Registrations** bounded context. The 
**Orders and Registrations** bounded context is not dependent on another 
bounded context at the time that it needs to perform the calculation 
because it already has the necessary data. At some point in the past, it 
will have collected the pricing information it needs from other bounded 
contexts (such as the **Conference Management** bounded context) and 
cached it. 

The advantage of this approach is that the **Orders and Registrations** 
bounded context is autonomous. It doesn't rely on the availability of 
another bounded context or service at the point in time that it needs to 
perform the calculation. 

The disadvantage is that the pricing information could be out of date. 
The Business Customer might have changed the pricing information in the 
**Conference Management** bounded context, but that change might not yet 
have reached the **Orders and Registrations** bounded context. 

### Favoring Authority

In this approach, the part of the system that calculates the order total 
obtains the pricing information from the bounded contexts (such as the 
**Conference Management** bounded context) at the point in time that it 
performs the calculation. The **Orders and Registrations** bounded
context could still perform the calculation, or it could delegate the 
calculation to another bounded context or service within the system. 

The advantage of this approach is that the system always uses the latest 
pricing information whenever it is calculating an order total. 

The disadvantage is that the **Orders and Registrations** bounded 
context is dependent on another bounded context when it needs to 
determine the total for the order. It either needs to query the 
**Conference Management** bounded context for the up to date pricing 
information, or call another service that performs the calculation. 

### Choosing between Autonomy and Authority

The choice between the two alternatives is a business decision. The 
specific business requirements of your scenario should determine which 
approach to take. Autonomy is often the preference for large, online 
systems.

> **JanaPersona:** This choice may change depending on the state of your
> system. Consider an overbooking scenario. The autonomy strategy may
> optimize for the normal case when lots of conference seats are still
> available, but as a particular conference fills up, the system may
> need to become more conservative and favour authority using the latest
> information on seat availability.

The way that the Conference Management System calculates the total for 
an order provides an example of choosing autonomy over authority. 

> **CarlosPersona:** For Contoso, the clear choice is for autonomy.
> It's a serious problem if Registrants can't purchase seats because
> some other bounded context is down. However, we don't really care if
> there's a short lag between the Business Customer modifying the
> pricing information, and that new pricing information being used to
> calculate order totals.

The section [Calculating Totals](#totals) below describes how the system
performs this calculation.

## Approaches to implementing the read-side

In the discussions of the read-side in the previous chapters, you saw 
how the team used a SQL-based store for the denormalized projections of 
the data from the write-side. 

You can use other storage mechanisms for the read-model data, for 
example the file system or in Windows Azure table or blob storage. In 
the Orders and Registrations bounded context, the system uses Windows 
Azure blobs to store information about the seat assignments. 

> **GaryPersona:** When you are choosing the underlying storage
> mechanism for the read-side, you should consider the costs associated
> with the storage (especially in the cloud) in addition to the
> requirement that the read-side data should be easy and efficient to
> access using the queries on the read-side.

> **Note:** See the **SeatAssignmentsViewModelGenerator** class to
> understand how the data is persisted to blob storage and the
> **SeatAssignmentsDao** class to understand how the UI retrieves the
> data for display.

## Eventual consistency

During testing, the team discovered a scenario where the Registrant 
might see evidence of eventual consistency in action. If the Registrant 
assigns Attendees to seats on an order and then quickly navigates to 
view the assignments, then sometimes this view shows only some of the 
updates. However, refreshing the page displays the correct information. 
This happens because it takes time for the events that record the seat 
assignments to propagate to the read-model, and sometimes the tester 
viewed the information queried from the read-model too quickly. 

The team decided to add a note to the view page warning users about this 
possibility, although a production system is likely to update the 
read-model faster than a debug version of the application running 
locally. 

> **CarlosPersona:** So long as the Registrant knows that the changes
> have been persisted, and that what the UI displays could be a
> few seconds out of date, they are not going to be concerned.

# Implementation details 

This section describes some of the significant features of the 
implementation of the Orders and Registrations bounded context. You may 
find it useful to have a copy of the code so you can follow along. You 
can download a copy of the code from the [Download center][downloadc], 
or check the evolution of the code in the repository on github: 
[mspnp/cqrs-journey-code][repourl]. You can download the code from the
V1 release from the [Tags][tags] page on Github.

> **Note:** Do not expect the code samples to match exactly the code in
> the reference implementation. This chapter describes a step in the
> CQRS journey, the implementation may well change as we learn more and
> refactor the code.

## The Conference Management bounded context

The Conference Management Bounded Context that enables a Business 
Customer to define and manage conferences is a simple two-tier, 
CRUD-style application that uses MVC 4. 

In the Visual Studio solution, the **Conference** project contains the 
model code, and the **Conference.Web** project contains the MVC views 
and controllers. 

### Integration with the Orders and Registration bounded context

The Conference Management bounded context pushes notifications of 
changes to conferences by publishing the following events. 

* **ConferenceCreated:** The bounded context publishes this event
  whenever a Business Customer creates a new conference.
* **ConferenceUpdated:** The bounded context publishes this event
  whenever a Business Customer updates an existing conference.
* **ConferencePublished:** The bounded context publishes this event
  whenever a Business Customer publishes a conference.
* **ConferenceUnpublished:** The bounded context publishes this event
  whenever a Business Customer un-publishes a new conference.
* **SeatCreated:** The bounded context publishes this event whenever a
  Business Customer defines a new seat type.
* **SeatsAdded:** The bounded context publishes this event whenever a
  Business Customer increases the quota of a seat type.
  
The **ConferenceService** class in the Conference project publishes 
these events to the event bus. 

> **MarkusPersona:** At the moment, there is no distributed transaction
> to wrap the database update and the message publishing.

## The Payments bounded context

The Payments bounded context is responsible for handling the interaction 
with the external systems that validate and process payments. In the V1 
release, payments can be processed either by a fake, external, 
third-party payments processor (that mimics the behavior of systems such 
as PayPal) or by an invoicing system. The external systems can report 
either that a payment was successful or that a payment failed. 

The sequence diagram in figure 4 illustrates how the key elements that 
are involved in the payments process interact with each other. The 
diagram is shows a simplified view, for example by ignoring the handler 
classes to better describe the process. 

![Figure 4][fig4]

**Overview of the payment process**

The diagram shows how the Orders and Registrations bounded context, the 
Payments bounded context, and the external payments service all interact 
with each other. In the future, Registrants will also be able to pay by
invoice instead of using a third-party payments processing service. 

The Registrant makes a payment as a part of the overall flow in the UI 
as shown in figure 3. The **PaymentController** controller class does 
not display a view unless it has to wait for the system to create the 
**ThirdPartyProcessorPayment** aggregate instance. Its role is to 
forward to payment information collected from the Registrant to the 
third-party payments processor. 

Typically, when you implement the CQRS pattern, you use events as the 
mechanism for communicating between bounded contexts. However in this 
case, the **RegistrationController** and **PaymentController** 
controller classes send commands to the Payments bounded context. The 
Payments bounded context does use events to communicate back with the 
**RegistrationProcessManager** instance in the Orders and 
Registrations bounded context. 

The implementation of the Payments bounded context uses the CQRS pattern 
without event sourcing. 

The write-side model contains an aggregate called 
**ThirdPartyProcessorPayment** that consists of two classes: 
**ThirdPartyProcessorPayment** and **ThirdPartyProcessorPaymentItem**. 
Instances of these classes are persisted to a SQL database by using 
Entity Framework. The **PaymentsDbContext** class implements an Entity 
Framework context. 

The **ThirdPartyProcessorPaymentCommandHandler** implements a command 
handler for the write-side. 

The read-side model is also implemented using Entity Framework. The 
**PaymentDao** class exposes the payment data on the read-side. For an 
example, see the **GetThirdPartyProcessorPaymentDetails** method. 

Figure 5 illustrates the different parts that make up the read-side and 
the write-side of the Payments bounded context. 

![Figure 5][fig5]

**The read-side and the write-side in the Payments bounded context**

### Integration with online payment services, eventual consistency, and command validation

Typically, online payment services offer two levels of integration with
your site:

* The simple approach, for which you don't need a merchant account with
  the payments provider, works through a simple redirect mechanism. You
  redirect your customer to the payment service. The payment service
  takes the payment, and then redirects the customer back to a page on
  your site along with an acknowledgement code. 
* The more sophisticated approach, for which you do need a merchant
  account, is based on an API. It typically uses two steps. First, the
  payment service verifies that your customer can pay the required
  amount, and sends you a token. Second, you can use the token within a
  fixed time to complete the payment by sending the token back to the
  payment service. 

Contoso assumes that its Business Customers do not have a merchant 
account and must use the simple approach. One consequence of this is 
that a seat reservation could expire while the customer is completing 
the payment. If this happens, the system tries to re-acquire the 
seats after the customer makes the payment. In the event that the seats 
cannot be re-acquired, the system notifies the Business Customer of the 
problem and the Business Customer must resolve the situation manually.

> **Note:** The system allows a little extra time over and above the
> time shown in the countdown clock to allow payment processing to
> complete.

This specific scenario, where the system cannot make itself fully 
consistent without a manual intervention by a user (in this case the 
business owner must initiate a refund or override the seat quota) 
illustrates a more general point in relation to eventual consistency and 
command validation. 

A key benefit of embracing eventual consistency is to remove the 
requirement for using distributed transactions that have a significant, 
negative impact on the scalability and performance of large systems 
because of the number and duration of locks that they must hold in 
the system. In this specific scenario, you could take steps to avoid the 
potential problem of accepting payment without seats being available in 
two ways: 

* Change the system to re-check the seat availability just before
  completing the payment. This is not possible because of the way that
  the integration with the payments system works without a merchant
  account.
* Keep the seats reserved (locked) until the payment is complete. This
  is difficult because you do not know how long the payment process will
  take: you must reserve (lock) the seats for an indeterminate period
  while you wait for the Registrant to complete the payment.

The team chose to allow for the possibility that a Registrant could pay 
for seats only to find that they are no longer available; in addition to 
being very unlikely to happen in practice (a timeout occurring while a 
Registrant is paying for the very last seats), this approach has the 
least impact on the system because it doesn't require a long-term 
reservation (lock) on any seats. 

> **MarkusPersona:** To minimize further the chance of this scenario
> occurring, the team decided to increase the buffer time for releasing
> reserved seats from five minutes to fourteen minutes. The original
> value of five minutes was chosen to account for any possible clock
> skew between the servers so that reservations were not released before
> the fifteen-minute countdown timer in the UI expired.

In more general terms, you could re-state the two options above as:

* Validate commands just before they execute to try to ensure that the
  command will succeed.
* Lock all the resources until the command completes.

If the command only affects a single aggregate and does not need to 
reference anything outside of the consistency boundary defined by the 
aggregate, then there is no problem because all of the information 
required to validate the command is within the aggregate. This is not 
the case in the current scenario: if you could validate whether the 
seats were still available just before you made the payment, this check 
would involve checking information from outside the current aggregate. 

If, in order to validate the command you need to look at data outside 
of the aggregate, for example, by querying a read model, or by looking 
in a cache, this is going to affect the scalability of the system. Also, 
if you are querying a read-model remember that read-models are 
eventually consistent. In the current scenario, you would need to query 
an eventually consistent read-model to check on the seats availability. 

If you decide to lock all of the relevant resources until the command 
completes, be aware of the impact this will have on the scalability of 
your system. 

> It is far better to handle such a problem from a business perspective
> than to make large architectural constraints upon our system.  
> Greg Young - [Q/A Greg Young's Blog][gregyoungqa]

For a detailed discussion of this issue, see
[Q/A Greg Young's Blog][gregyoungqa].

## Event Sourcing

The initial implementation of the event sourcing infrastructure is 
extremely basic: the team intends to replace it with a production 
quality event store in the near future. This section describes the 
initial, basic, implementation and lists the various ways to improve it. 

The core elements of this basic event sourcing solution are:

* Whenever the state of an aggregate instance changes, the instance
  raises an event that fully describes the state change.
* The system persists these events in an event store.
* An aggregate can rebuild its state by replaying its past stream of
  events.
* Other aggregates and process managers (possibly in different bounded
  contexts) can subscribe to these events.
  
### Raising events when the state of an aggregate changes

The following two methods from the **Order** aggregate are examples of 
methods that the **OrderCommandHandler** class invokes when it receives 
a command for the order. Neither of these methods updates the state of 
the **Order** aggregate, instead they raise an event that will be 
handled by the **Order** aggregate. In the **MarkAsReserved** method, 
there is some minimal logic to determine which of two events to raise. 


```Cs
public void MarkAsReserved(DateTime expirationDate, IEnumerable<SeatQuantity> reservedSeats)
{
    if (this.isConfirmed)
        throw new InvalidOperationException("Cannot modify a confirmed order.");

    var reserved = reservedSeats.ToList();

    // Is there an order item which didn't get an exact reservation?
    if (this.seats.Any(item => !reserved.Any(seat => seat.SeatType == item.SeatType && seat.Quantity == item.Quantity)))
    {
        this.Update(new OrderPartiallyReserved { ReservationExpiration = expirationDate, Seats = reserved.ToArray() });
    }
    else
    {
        this.Update(new OrderReservationCompleted { ReservationExpiration = expirationDate, Seats = reserved.ToArray() });
    }
}

public void ConfirmPayment()
{
    this.Update(new OrderPaymentConfirmed());
}
```

The abstract base class of the **Order** class defines the **Update** 
method. The following code sample shows this method and the **Id** and 
**Version** properties in the **EventSourced** class. 

```Cs
private readonly Guid id;
private int version = -1;

protected EventSourced(Guid id)
{
    this.id = id;
}

public int Version { get { return this.version; } }

protected void Update(VersionedEvent e)
{
    e.SourceId = this.Id;
    e.Version = this.version + 1;
    this.handlers[e.GetType()].Invoke(e);
    this.version = e.Version;
    this.pendingEvents.Add(e);
}
```

The **Update** method sets the **Id** and increments the version of the 
aggregate. It also determines which of the event handlers in the 
aggregate it should invoke to handle the event type. 

> **MarkusPersona:** Every time the system updates the state of an
> aggregate, it increments the version number of the aggregate.

The following code sample shows the event handler methods in the 
**Order** class that are invoked when the command methods shown above 
are called. 


```Cs
private void OnOrderPartiallyReserved(OrderPartiallyReserved e)
{
    this.seats = e.Seats.ToList();
}

private void OnOrderReservationCompleted(OrderReservationCompleted e)
{
    this.seats = e.Seats.ToList();
}

private void OnOrderExpired(OrderExpired e)
{
}

private void OnOrderPaymentConfirmed(OrderPaymentConfirmed e)
{
    this.isConfirmed = true;
}
```

These methods update the state of the aggregate.

An aggregate must be able to handle both events from other aggregates 
and events that it raises itself. The protected constructor in the 
**Order** class lists all the events that the **Order** aggregate can 
handle. 

```Cs
protected Order()
{
    base.Handles<OrderPlaced>(this.OnOrderPlaced);
    base.Handles<OrderUpdated>(this.OnOrderUpdated);
    base.Handles<OrderPartiallyReserved>(this.OnOrderPartiallyReserved);
    base.Handles<OrderReservationCompleted>(this.OnOrderReservationCompleted);
    base.Handles<OrderExpired>(this.OnOrderExpired);
    base.Handles<OrderPaymentConfirmed>(this.OnOrderPaymentConfirmed);
    base.Handles<OrderRegistrantAssigned>(this.OnOrderRegistrantAssigned);
}
```

### Persisting events to the event store

When the aggregate processes an event in the **Update** method in the 
**EventSourcedAggregateRoot** class, it adds the event to a private list 
of pending events. This list is exposed as a public, **IEnumerable** 
property of the abstract **EventSourced** class called **Events**. 

The following code sample from the **OrderCommandHandler** class shows 
how the handler invokes a method in the **Order** class to handle a 
command, and then uses a repository to persist the current state of the 
**Order** aggregate by appending all pending events to the store. 


```Cs
public void Handle(MarkSeatsAsReserved command)
{
    var order = repository.Find(command.OrderId);

    if (order != null)
    {
        order.MarkAsReserved(command.Expiration, command.Seats);
        repository.Save(order);
    }
}
```

The following code sample shows the initial, simple implementation of 
the **Save** method in the **SqlEventSourcedRepository** class. 

> **Note:** These examples refer to a SQL-based event store. This was
> the initial approach that was later replaced with an implementation
> based on Windows Azure table storage. The SQL-based event store
> remains in the solution as a convenience: you can run the application
> locally and use this implementation to avoid any dependencies on
> Windows Azure.


```Cs
public void Save(T eventSourced)
{
    // TODO: guarantee that only incremental versions of the event are stored
    var events = eventSourced.Events.ToArray();
    using (var context = this.contextFactory.Invoke())
    {
        foreach (var e in events)
        {
            using (var stream = new MemoryStream())
            {
                this.serializer.Serialize(stream, e);
                var serialized = new Event { AggregateId = e.SourceId, Version = e.Version, Payload = stream.ToArray() };
                context.Set<Event>().Add(serialized);
            }
        }

        context.SaveChanges();
    }

    // TODO: guarantee delivery or roll back, or have a way to resume after a system crash
    this.eventBus.Publish(events);
}
```

### Replaying events to re-build state

When a handler class loads an aggregate instance from storage, it loads 
the state of the instance by replaying the stored event stream.

> **PoePersona:** We later found that using event sourcing and being
> able to replay events was invaluable as a technique for analyzing bugs
> in the production system running in the cloud. We could make a local
> copy of the event store, then replay the event stream locally, and
> debug the application in Visual Studio to understand exactly what
> happened in the production system.

The following code sample from the **OrderCommandHandler** class shows 
how calling the **Find** method in the repository initiates this
process. 

```Cs
public void Handle(MarkSeatsAsReserved command)
{
    var order = repository.Find(command.OrderId);

    ...
}
```

The following code sample shows how the **SqlEventSourcedRepository**
class loads the event stream associated with the aggregate.

> **JanaPersona:** The team later developed a simple event store using
> Windows Azure tables instead of the **SqlEventSourcedRepository**. The
> next section describes this Windows Azure table storage based
> implementation.

```Cs
public T Find(Guid id)
{
    using (var context = this.contextFactory.Invoke())
    {
        var deserialized = context.Set<Event>()
            .Where(x => x.AggregateId == id)
            .OrderBy(x => x.Version)
            .AsEnumerable()
            .Select(x => this.serializer.Deserialize(new MemoryStream(x.Payload)))
            .Cast<IVersionedEvent>()
            .AsCachedAnyEnumerable();

        if (deserialized.Any())
        {
            return entityFactory.Invoke(id, deserialized);
        }

        return null;
    }
}
```

The following code sample shows the constructor in the **Order** class 
that rebuilds the state of the order from its event stream when it is 
invoked by the **Invoke** method in the previous code sample. 


```Cs
public Order(Guid id, IEnumerable<IVersionedEvent> history) : this(id)
{
    this.LoadFrom(history);
}
```

The **LoadFrom** method is defined in the **EventSourced** class as
shown in the following code sample. 

```Cs
protected void LoadFrom(IEnumerable<IVersionedEvent> pastEvents)
{
    foreach (var e in pastEvents)
    {
        this.handlers[e.GetType()].Invoke(e);
        this.version = e.Version;
    }
}
```

For each stored event in the history, it determines the appropriate 
handler method to invoke in the **Order** class and updates the version
number of the aggregate instance.

### Issues with the simple event store implementation

The simple implementation of event sourcing and an event store outlined 
in the previous sections has a number of shortcomings. The following 
list identifies some of these shortcomings that should be overcome in a 
production quality implementation. 

1. There is no guarantee in the **Save** method in the
   **SqlEventRepository** class that the event is persisted to storage
   and published to the messaging infrastructure. A failure could result
   in an event being saved to storage but not being published.
2. There is no check that when the system persists an event, that it is
   a later event than the previous one. Potentially, events could be
   stored out of sequence.
3. There are no optimizations in place for aggregate instances that have
   a large number of events in their event stream. This could result in
   performance problems when replaying events.
   
## Windows Azure Table Storage based event store

The Windows Azure table storage based event store addresses some of the 
shortcomings of the simple SQL-based event store. However, at this 
point on time, it is still _not_ a production quality implementation. 

The team designed this implementation to guarantee that events are both 
persisted to storage and published on the message bus. To achieve this, 
it uses the transactional capabilities of Windows Azure tables. 

> **MarkusPersona:** Windows Azure table storage supports transactions
> across records that share the same partition key.

The **EventStore** class initially saves two copies of every event to be 
persisted. One copy is the permanent record of that event, and the other 
copy becomes part of a virtual queue of events that must be published on 
the Windows Azure Service Bus. The following code sample shows the 
**Save** method in the **EventStore** class. The prefix "Unpublished" 
identifies the copy of the event that is part of the virtual queue of 
unpublished events. 

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

> **Note:** This code sample also illustrates how a duplicate key error
> is used to identify a concurrency error.

The **Save** method in the repository class is shown below. This method 
is invoked by the event handler classes, invokes the **Save** method 
shown in the previous code sample, and invokes the **SendAsync** 
method of the **EventStoreBusPublisher** class. 

```Cs
public void Save(T eventSourced)
{
    var events = eventSourced.Events.ToArray();
    var serialized = events.Select(this.Serialize);

    var partitionKey = this.GetPartitionKey(eventSourced.Id);
    this.eventStore.Save(partitionKey, serialized);

    this.publisher.SendAsync(partitionKey);
}
```

The **EventStoreBusPublisher** class is responsible for reading the 
unpublished events for the aggregate from the virtual queue in the 
Windows Azure table store, publishing the event on the Windows Azure 
Service Bus, and then deleting the unpublished event from the virtual 
queue. 

If the system fails between publishing the event on the Windows Azure 
Service Bus and deleting the event from the virtual queue then, when the 
application restarts, the event is published a second time. To avoid 
problems caused by duplicate events, the Windows Azure Service Bus is 
configured to detect duplicate messages and ignore them. 

> **MarkusPersona:** In the case of a failure, the system must include a
> mechanism for scanning all of the partitions in table storage for
> aggregates with unpublished events and then publishing those events.
> This process will take some time to run, but will only need to run
> when the application restarts.

## Calculating totals

<a name="totals" />

To ensure the autonomy of the Orders and Registrations bounded context 
it calculates order totals without accessing the Conference Management 
bounded context. The Conference Management bounded context is 
responsible for maintaining the prices of seats for conferences. 

Whenever a Business Customer adds a new seat type or changes the price 
of a seat, the Conference Management bounded context raises an event. 
The Orders and Registrations bounded context handles these events and 
persists the information in as part of its read-model (see the 
**ConferenceViewModelGenerator** class for details). 

When the **Order** aggregate calculates the order total, it uses the 
data provided by the read-model. See the **MarkAsReserved** method in 
the **Order** aggregate and the **PricingService** class for details. 

> **JanaPersona:** The UI also displays a dynamically calculated total
> as the Registrant adds seats to an order. The application calculates
> this value using Javascript. When the Registrant makes a payment, the
> system uses the total that the **Order** aggregate calculates.

# Impact on testing

> **MarkusPersona:** Don't let your passing unit tests lull you into a
> false sense of security. There are lots of moving parts when you
> implement the CQRS pattern. You need to test that they all work
> correctly together.

> **MarkusPersona:** Don't forget to create unit tests for your
> read-models. A unit test on the read model generator uncovered a bug
> just prior to the V1 release where the system removed order items when
> it updated an order.

## Timing issues

One of the acceptance tests verifies the behavior of the system when a 
Business Customer creates new seat types. The key steps in the test 
create a conference, create a new seat type for the conference, and then 
publish the conference. This raises the corresponding sequence of 
events: **ConferenceCreated**, **SeatCreated**, and 
**ConferencePublished**. 

The Orders and Registrations bounded context handles these are 
integration events. The test determined that the the Orders and 
Registrations bounded context received these events in a different 
order. 

The Windows Azure Service Bus only offers best-effort FIFO, therefore it 
may not deliver events in the order that they were sent, it is also 
possible in this scenario that the issue occurs because of the different 
times it takes for the steps in the test to create the messages and 
deliver them to the Windows Azure Service Bus. The introduction of an 
artificial delay between the steps in the test provided a temporary 
solution to this problem. 

In the V2 release, the team plans to address the general issue of 
messaging ordering and either modify the infrastructure to guarantee 
ordering or make the system more robust if messages do arrive out of 
order. 

## Involving the domain expert

In [Chapter 4, Extending and Enhancing the Orders and Registrations 
Bounded Contexts][j_chapter4] you saw how the domain expert was involved 
with designing the acceptance tests and the benefits that involvement 
brought to the project in terms of clarifying domain knowledge. 

You should also ensure that the domain expert attends bug triage 
meetings. The domain expert can help to clarify the expected behavior of 
the system, and during the discussion may uncover new user stories. For 
example, during the triage of a bug related to un-publishing a 
conferences in the Conference Management bounded context, the domain 
expert identified a requirement for providing the Business Customer with 
the ability to add a redirect link for the un-published conference to a 
new conference or alternate page. 

[j_chapter3]:       Journey_03_OrdersBC.markdown
[j_chapter4]:       Journey_04_ExtendingEnhancing.markdown
[r_chapter4]:       Reference_04_DeepDive.markdown
[r_chapter9]:       Reference_09_Technologies.markdown
[appendix1]:        Appendix1_Running.markdown

[fig1]:             images/Journey_05_Architecture.png?raw=true
[fig2]:             images/Journey_05_UIs.png?raw=true
[fig3]:             images/Journey_05_Tasks.png?raw=true
[fig4]:             images/Journey_05_Payments.png?raw=true
[fig5]:             images/Journey_05_PaymentBC.png?raw=true

[inductiveui]:      http://msdn.microsoft.com/en-us/library/ms997506.aspx
[metroux]:          http://msdn.microsoft.com/en-us/library/windows/apps/hh465424.aspx
[unity]:            http://msdn.microsoft.com/en-us/library/ff647202.aspx
[appfabsdk]:        http://www.microsoft.com/download/en/details.aspx?displaylang=en&id=27421
[gregyoungqa]:      http://goodenoughsoftware.net/2012/05/08/qa/
[repourl]:          https://github.com/mspnp/cqrs-journey-code
[downloadc]:        http://NEEDFWLINK
[tags]:             https://github.com/mspnp/cqrs-journey-code/tags
[gregtask]:         http://codebetter.com/gregyoung/2010/02/16/cqrs-task-based-uis-event-sourcing-agh/
[crudpost]:         http://codebetter.com/iancooper/2011/07/15/why-crud-might-be-what-they-want-but-may-not-be-what-they-need/