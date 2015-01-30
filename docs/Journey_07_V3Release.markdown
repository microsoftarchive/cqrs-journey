### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Chapter 7: Adding Resilience and Optimizing Performance 

_Reaching the end of our journey: the final tasks_

> "You cannot fly like an eagle with the wings of a wren," Henry Hudson

The three primary goals for this last stage in our journey are to make the 
system more resilient to failures, to improve the responsiveness of 
the UI, and to ensure that our design is scalable. The effort to harden the system focuses on the 
**RegistrationProcessManager** class in the Orders and Registrations 
bounded context. The focus on performance is on the way the UI interacts 
with the domain-model during the order creation process. 

# Working definitions for this chapter 

The following definitions are used for the remainder of this chapter. 
For more detail, and possible alternative definitions, see [A CQRS/ES 
Deep Dive][r_chapter4] in the Reference Guide. 

## Command

A command is a request for the system to perform an action that changes 
the state of the system. Commands are imperatives, for example 
**MakeSeatReservation**. In this bounded context, commands originate 
either from the UI as a result of a user initiating a request, or from 
a process manager when the process manager is directing an aggregate to 
perform an action. 

Commands are processed once by a single recipient. Commands are either 
transported to their recipients by a command bus, or delivered directly 
in-process. If a command is delivered through a command bus, then then the 
command is sent asynchronously. If the comand can be delivered directly 
in-process, then the command is sent synchronously. 

## Event

An event, such as **OrderConfirmed**, describes something that has 
happened in the system, typically as a result of a command. Aggregates 
in the domain model raise events. Events can also come from other
bounded contexts.

Multiple subscribers can handle a specific event. Aggregates publish 
events to an event bus; handlers register for specific types of event on 
the event bus and then deliver the events to the subscriber. In the 
orders and registrations bounded context bounded context, the 
subscribers are a process manager and the read model generators. 

## Snapshots

Snapshots are an optimization that you can apply to event sourcing: 
instead of replaying all of the persisted events associated with an 
aggregate when it is re-hydrated, you load a recent copy of the state of 
the aggregate and then replay only the events that were persisted after 
saving the snapshot. In this way you can reduce the amount of data that 
you must load from the event store.

## Idempotency

_Idempotency_ is a characteristic of an operation that means the operation can be applied multiple times without changing the result. For example, the operation "set the value x to ten" is idempotent, while the operation "add one to the value of x" is not. In a messaging environment, a message is idempotent if it can be delivered multiple times without changing the result: either because of the nature of the message itself, or because of the way the system handles the message.

## Eventual consistency

_Eventual consistency_ is a consistency model that does not guarantee immediate access to updated values. After an update to a data object, the storage system does not guarantee that subsequent accesses to that object will return the updated value. However, the storage system does guarantee that if no new updates are made to the object during a sufficiently long period of time, then eventually all accesses can be expected to return the last updated value.

# Architecture 

The application is designed to deploy to Windows Azure. At this stage in the journey, the application consists of web roles that contain the ASP.NET MVC web applications and a worker role that contains the message handlers and domain objects. The application uses Windows Azure SQL Database (SQL Database) instances for data storage, both on the write side and the read side. The application also uses Windows Azure table storage on the write side and blob storage on the read side in some places. The application uses the Windows Azure Service Bus to provide its messaging infrastructure. Figure 1 shows this high-level architecture.

![Figure 1][fig1]

**The top-level architecture in the V3 release**

While you are exploring and testing the solution, you can run it 
locally, either using the Windows Azure compute emulator or by running 
the MVC web application directly and running a console application that 
hosts the handlers and domain objects. When you run the application 
locally, you can use a local SQL Express database instead of SQL Database, 
and use a simple messaging infrastructure implemented in a SQL Express 
database. 

For more information about the options for running the application, see 
[Appendix 1][appendix1]. 

# Adding resilience

During this stage of the journey the team looked at options for 
hardening the **RegistrationProcessManager** class. This part of the 
Orders and Registrations bounded context is responsible for managing the 
interactions between the aggregates in the Orders and Registrations 
bounded context and for ensuring that they are all consistent with each 
other. It is important that this process manager is resilient to a wide 
range of failure conditions if the bounded context as a whole is to 
maintain its consistent state. 

Typically, a process manager receives incoming events and then, based on 
the state of the process manager, sends out one or more commands to 
aggregates within the bounded context. When a process manager sends out 
commands, it typically changes its own state. 

The Orders and Registrations bounded context contains the 
**RegistrationProcessManager** class. This process manager is 
responsible for coordinating the activities of the aggregates in both 
this bounded context and the Payments bounded context by routing events 
and commands between them. The process manager is therefore responsible 
for ensuring that the aggregates in these bounded contexts are correctly 
synchronized with each other. 

> **GaryPersona:** An aggregate determines the consistency boundaries
> within the write-model with respect to the consistency of the data
> that the system persists to storage. The process manager manages
> the relationship between different aggregates, possibly in different
> bounded contexts, and ensures that the aggregates are eventually
> consistent with each other.

A failure in the registration process could have adverse consequences 
for the system: the aggregates could get out of synchronization with 
each other which may cause unpredicatable behavior in the system; some 
processes might end up as zombie processes continuing to run and use 
resources but never completing. The team identified the following 
specific failure scenarios related to the **RegistrationProcessManager** 
process manager. The process manager could: 

* Crash or be unable to persist its state after it receives an event but
  before it sends any commands. The message processor may not be able to
  mark the event as complete, so after a timeout the event is placed
  back in the topic subscription and re-processed.
* Crash after it persists its state but before it sends any commands.
  This puts the system into an inconsistent state because the process manager
  saves its new state without sending out the expected commands. The
  original event is put back in the topic subscription and 
  re-processed.
* Fail to mark that an event has been processed. The process manager will
  process the event a second time because after a timeout, the system
  will put the event back onto the service bus topic subscription.
* Timeout while it waits for a specific event that it is expecting. The
  process manager cannot continue processing and reach an expected end-state.
* Receive an event that it does not expect to receive while the process manager
  is in a particular state. This may indicate a problem elsewhere that
  implies that it is unsafe for the process manager to continue.
  
These scenarios can be summarized to identify two specific issues to
address:

1. The **RegistrationProcessManager** handles an event successfully but fails
   to mark the message as complete. The **RegistrationProcessManager** will
   then process the event again after it is automatically returned to
   the subscription to the Windows Azure Service Bus topic.
2. The **RegistrationProcessManager** handles an event successfully, marks it
   as complete, but then fails to send out the commands.

## Making the system resilient when an event is reprocessed

If the behavior of the process manager itself is idempotent, then if it 
receives and processes an event a second time then this does not result 
in any inconsistencies within the system. This approach would handle the 
first three failure conditions. After a crash, you can restart the 
process manager and reprocess the incoming event a second time. 

Instead of making the process manager idempotent, you could ensure that 
all the commands that the process manager sends are idempotent. 
Restarting the process manager may result in sending commands a second 
time, but if those commands are idempotent this will have no adverse 
affect on the process or the system. For this approach to work, you 
still need to modify the process manager to guarantee that it sends all 
commands at least once. If the commands are idempotent, it doesn't 
matter if they are sent multiple times, but it does matter if a command 
is never sent at all. 

In the V1 release, most message handling is already either idempotent, 
or the system detects duplicate messages and sends them to a dead-letter 
queue. The exceptions are the **OrderPlaced** event and the 
**SeatsReserved** event, so the team modified the way that the V3
release of the system processes these two events in order to address
this issue.

## Ensuring that commands are always sent

To ensure that the system always sends commands when the 
**RegistrationProcessManager** class saves its state requires 
transactional behavior. This requires the team to implement a 
pseudo-transaction because it is neither advisable nor possible to enlist the Windows 
Azure Service Bus and a SQL Database table together in a distributed
transcation. 

The solution adopted by the team for the V3 release ensures that the 
system persists all commands that the **RegistrationProcessManager** 
generates at the same time that it persists the state of the 
**RegistrationProcessManager** instance. Then the system tries to send 
the commands, removing them from storage after they have been sent 
successfully. The system also checks for un-dispatched messages whenever
it loads a **RegistrationProcessManager** instance from storage.

# Optimizing performance

During this stage of the journey we ran performance and stress tests using
[Visual Studio 2010][loadtest] to analyze response 
times and identify bottlenecks. The team used Visual Studio Load Test to 
simulate different numbers of users accessing the application, and added 
additonal tracing into the code to record timing information for 
detailed analysis. As a result of this exercise, the team made a number 
of changes to the system to optimize its performance. 

The team created the performance test environment in Windows Azure, 
running the test controller and test agents in Windows Azure VM role 
instances. This enabled us to test how the Contoso Conference Management 
System performed under different loads by using the test agents to 
simulate different numbers of virtual users. 

> **GaryPersona:** Although in this journey the team did their
> performance testing and optimization work at the end of the project,
> it typically makes sense to do this work as you go, addressing
> scalability issues and adding hardening as soon as possible. This is
> especially true if you are building your own infrastructure and need to be able to handle high volumes of throughput.

> **MarkusPersona:** Because implementing the CQRS pattern leads to a
> very clear separation of responsibities for the many different parts
> that make up the system, we found it relatively easy to add
> optimizations and hardening because many of the necessary changes were
> very localized within the system.

The initial optimization effort focused on how the UI interacts with 
the domain, and we identified ways to streamline this aspect of the 
system. The detailed performance tests we ran using Visual Studio Load 
Test after this uncovered unacceptable response times for Registrants 
creating orders when the system was under load and we then looked at 
ways to optimize the infrastructure. 

## UI flow before optimization

When a Registrant creates an order, she visits the following sequence of 
screens in the UI. 

1. The Register screen. This screen displays the ticket types for the
   conference, the number of seats currently available according to the eventually consistent read model. The Registrant
   selects the quantities of each seat type that she would like to
   purchase.
2. The Checkout screen. This screen displays a summary of the order
   that includes a total price and a countdown timer that tells the
   Registrant how long the seats will remain reserved. The Registrant
   enters her details and preferred payment method.
3. The Payment screen. This simulates a third-party payment processor.
4. The Registration success screen. This displays if the payment
   succeeded. It displays to the Registrant an order locator code and
   link to a screen that enables the Registrant to assign Attendees to
   seats.
   
See the section Task-based UI in chapter 5, [Preparing for the V1
Release][j_chapter5] for more information about the screens and flow in
the UI.
   
In the V2 release, the system must process the following commands and 
events between the Register screen and the Checkout screen: 

* RegisterToConference
* OrderPlaced
* MakeSeatReservation
* SeatsReserved
* MarkSeatsAsReserved
* OrderReservationCompleted
* OrderTotalsCalculated

In addition, the MVC controller is also validating that there are 
sufficient seats available by querying the read model to fulfill the order before it sends the 
initial **RegisterToConference** command. 

The team load tested the application using Visual Studio Load Test with 
different user load patterns. We noticed that with higher loads, the UI often has to wait for the domain to 
to complete its processing and for the read-models to receive data from 
the write-model, before it can display the next screen to the 
Registrant. In particular, with the V2 release deployed to medium-sized 
web and worker role instances we found that: 

* With a constant load pattern of less than five orders per second,
  all orders are processed within a five second window.
* With a constant load pattern of between eight and ten orders per
  second, many orders are not processed within the five second window.
* With a constant load pattern of between eight and ten orders per
  second, the role instances are used sub-optimally (for
  example CPU usage is low).

> **Note:** The five second window is the maximum duration that we want
> to see between the time that the UI sends the initial command on the
> service bus and the time when the priced order becomes visible in the
> read-model enabling the UI to display the next screen to the user.

To address this issue, the team identified two possible sets of 
optimizations: optimizing the interaction between the UI and the domain, 
and optimizing the infrastructure. We decided to address the interaction 
between the UI and the domain first; when this did not improve 
performance sufficiently we added the infrastructure optimizations as 
well. 

## Optimizing the UI

The team discussed with the domain expert whether or not is always 
necessary to validate the seats availability before the UI sends the 
**RegisterToConference** command to the domain. 

> **GaryPersona:** This scenario illustrates some practical issues in
> relation to eventual consistency. The read-side, in this case the
> priced order view model, is eventually consistent with the write-side.
> Typically, when you implement the CQRS pattern you should be able
> embrace eventual consistency and not need to wait in the UI for
> changes to propagate to the read-side. However in this case, the UI
> must wait for the write-model to propagate information that relates to
> a specific order to the read-side. This perhaps indicates a problem
> with the original analysis and design of this part of the system.

The domain expert was clear that the system should confirm that seats are 
available before taking payment. Contoso does not want to sell seats and 
then have to explain to a Registrant that those seats are not in fact 
available. Therefore, the team looked for ways to streamline getting as 
far as the Payment screen in the UI. 

> **BethPersona:** This cautious strategy is not appropriate in all
> scenarios. In some cases the business may prefer to take the money
> even if it cannot immediately fulfill the order: the business may know
> that the stock will be replenished soon, or that the customer will be
> happy to wait. In our scenario, although Contoso could refund the
> money to a Registrant if tickets turned out not to be available, a 
> Registrant may decide to purchase flight tickets that are not
> refundable in the belief that the conference registration is
> confirmed. This type of decision is clearly one for the business and
> the domain expert.

### UI optimization 1

Most of the time, there are plenty of seats available for a conference 
and Registrants do not have to compete with each other to reserve seats. 
It is only for a brief time, as the conference beomes close to selling 
out, that Registrants do end up competing for the last few available 
seats. 

If there are plenty of available seats for the conference then there is 
a minimal risk that a Registrant will get as far as the Payment screen 
only to find that the system could not reserve the seats. In this case, 
some of the processing that the V2 release performs before getting to 
the Checkout screen can be allowed to happen asynchronously while the 
Registrant is entering information on the Checkout screen. This 
reduces the chance that the Checkout experiences a delay before seeing 
the Registrant screen. 

> **JanaPersona:** Essentially we are relying on the fact that a
> reservation is likely to succeed, avoiding a time-consuming check. We
> still perform the check before the registrant makes a payment.

However, if the controller checks and finds that there are not enough 
seats available to fulfill the order _before_ it sends the 
**RegisterToConference** command, it can re-display the Register screen 
to enable the Registrant to update her order based on current 
availability. 

> **JanaPersona:** A possible enhancement to this strategy is to look at
> whether there are _likely to be_ enough seats available before sending
> the **RegisterToConference** command. This could reduce the number of
> occassions that a Registrant has to adjust her order as the last few
> seats are sold. However, this scenario will occur infrequently enough
> that it is probably not worth implementing.

### UI optimization 2

In the V2 release, the MVC controller cannot display the Checkout 
screen until the domain publishes the **OrderTotalsCalculated** event 
and the system updates the priced order view model. This event is the 
last event that occurs before the controller can display the screen. 

If the system calculates the total and updates the priced order view 
model earlier, the controller can display the Checkout screen sooner. 
The team determined that the **Order** aggregate could calculate the 
total when the order is placed instead of when the reservation is 
complete. This will enable the UI flow to move more quickly to the 
Checkout screen than in the V2 release.

# Optimizing the infrastructure

The second set of optimizations that the team added in this stage of the 
journey related to the infrastructure of the system. These changes 
addressed both the performance and the scalability of the system. The 
following sections describe the most significant changes we made here. 

## Sending and receiving commands and events asynchronously

As part of the optimization process, the team updated the system to 
ensure that all messages sent on the Service Bus are sent 
asynchronously. This optimization is intended to improve the overall 
responsiveness of the application and improve the throughput of 
messages. As part of this change, the team also used the [Transient 
Fault Handling Application Block][tfhab] to handle any transient errors 
encountered when using the Service Bus.

> **MarkusPersona:** This optimization resulted in major changes to the
> infrastructure code. Combining asynchronous calls with the Transient
> Fault Handling Block is complex: we would benefit from some of the new
> simplifying syntax in C# 4.5!

> **JanaPersona:** For other proven practices to help you optimize
> performance when using the Windows Azure Service Bus, see this guide:
> [Best Practices for Performance Improvements Using Service Bus Brokered
> Messaging][sbscale].

## Optimizing command processing 

The V2 release used the same messaging infrastructure, the Windows Azure Service Bus, for both commands and events. The team evaluated whether the Contoso Conference Management System needs to send all its command messages using the same infrastructure.

There are a number of factors that we considered when we 
determined whether to continue using the Windows Azure Service Bus for 
transporting all command messages. 

* Which commands, if any, can be handled in-process?
* Will the system lose any resilience if it handles some commands in-process?
* Will there be any significant performance gains if it handles some commands in-process?

We identified a set of commands that the system can send synchronously 
and in-process from the public conference web application. To implement 
this optimization we had to add some infrastructure elements (the event 
store repositories, the event bus, and the event publishers) to the 
public conference web application; previously, these infrastructure 
elements were only in the system's worker role. 

> An asynchronous command doesn't exist, it's actually another event. If
> I must accept what you send me and raise an event if I disagree, it's
> no longer you telling me to do something, it's you telling me
> something has been done. This seems like a slight difference at first,
> but it has many implications.  
> Greg Young - Why lot's of developers use one-way command messaging 
> (async handling) when it's not needed? - DDD/CQRS Google Groups

## Using snapshots with event sourcing

The performance tests also uncovered a bottleneck in the use of the 
**SeatsAvailability** aggregate that we addressed by using a form of 
snapshot. 

> **JanaPersona:** Once the team identified this bottleneck, it was easy
> to implement and test this solution. One of the advantages of the
> approach we followed implementing the CQRS pattern is that we can make
> small localized changes in the system. Updates don't require us to
> make complex changes across multiple parts of the system.

When the system re-hydrates an aggregate instance from the event store, 
it must load and replay all of the events associated with that aggregate 
instance. A possible optimization here is to store a rolling snapshot of 
the state of the aggregate at some recent point in time so that the 
system only needs to load the snapshot and the subsequent events, 
thereby reducing the number of events that it must reload and replay. 
The only aggregate in the Contoso Conference Management System that is 
likely to accumulate a significant number of events over time is the 
**SeatsAvailability** aggregate. We decided to use the 
[Memento][memento] pattern as the basis of the snapshot solution to use 
with the **SeatAvailability** aggregate. The solution we implmented uses 
a memento to capture the state of the **SeatAvailability** aggregate, 
and then keeps a copy of the memento in a cache. The system then tries 
to work with the cached data instead of always reloading the aggregate 
from the event store.

> **GaryPersona:** Often, in the context of event sourcing, snapshots
> are persisent, not transient local caches as we have implemented in
> our project.

## Publishing events in parallel

This optimization proved to be one of the most significant in terms of improving the throughput of event messages in the system. The team went through several interations to obtain the best results:

* Iteration 1: This approach used the [Parallel.ForEach][pforeach] method with a custom partitioning scheme to assign messages to partitions and to set an upper bound on the degree of parallelism. This approach used synchronous Windows Azure Service Bus API calls to publish the messages.
* Iteration 2: This approach used some asynchronous API calls. This approach required the use of custom semaphore-based throttling to handle the asynchronous callbacks correctly.
* Iteration 3: This approach uses dynamic throttling that takes into account the transient failures that indicate too many messages are being sent to a specific topic. This approach uses more asynchronous Windows Azure Service Bus API calls.

> **JanaPersona:** We adopted the same dynamic throttling approach in
> the SubscriptionReceiver and SessionSubscriptionReceiver classes when
> the system retrieves messages from the service bus.


## Filtering messages in subscriptions

This optimization adds filters to the Windows Azure Service Bus topic subscriptions to avoid reading messages that would later be ignored by the handlers associated with the subscription.

**MarkusPersona:** Here we are taking advantage of a feature provided by Windows Azure Service Bus.

## Creating a dedicated receiver for the **SeatsAvailability** aggregate

This enables the receiver for the **SeatsAvailability** aggregate to use a subscription that supports sessions. This is to guarantee that we have a single writer per aggregate instance because the **SeatsAvailability** aggregate is a high-contention aggregate. This prevents us from receiving a large number of concurrency exceptions when we scale out.

> **JanaPersona:** Elsewhere, we use subscriptions with sessions to guarantee the ordering of events. In this case we are using sessions for a different reason &mdash; to guarantee that we have a single writer for each aggregate instance.

## Caching conference information

This optimization caches several read models that the public conference web site uses extensively. It includes logic to determine how to keep the data in the cache based on the number of available seats for a particular conference: if there are plenty of seats available the system can cache the data for a long period of time, but if there are very few seats available the data is not cached.

## Partitioning the Service Bus

The team also partitioned the Service Bus to make the application more scalable and to avoid throttling when the volume of messages that the system sends approaches the maximum throughput that the Service Bus can handle. Each Service Bus topic may be handled by a different node in Windows Azure, so by using multiple topics we can increase our potential throughput. We considered the following partitioning schemes:

* Use separate topics for different message types.
* Use multiple, similar topics and listen to them all on a round-robin to spread the load.

For a detailed discussion of these partitioning schemes, see Chapter 11, "Asynchronous Communication and Message Buses" in "Scalability Rules: 50 Principles for Scaling Web Sites" by Martin L. Abbott and Michael T. Fisher (Addison-Wesley, 2011).

We decided to use separate topics for the events published by the **Order** aggregates and the **SeatAvailability** aggregates because these aggregates are responsible for the majority of events flowing through the service bus.

> **GaryPersona:** Not all messages have the same importance. You could also use separate, prioritized message buses to handle different message types or even consider not using a message bus for some messages.

> **JanaPersona:** Treat the Service Bus just like any other critical
> component of your system. This means you should ensure that your
> service bus can be scaled. Also, remember that not all data has the same
> value to your business. Just because you have a Service Bus, doesn't
> mean everything has to go through it. It's prudent to eliminate
> low-value, high-cost traffic.



## Other optimizations

The team performed some additional optimizations that are listed in the 
"Implementation details" section below. The primary goal of the team 
during this stage of the journey was to optimize the system to ensure 
that the UI appears sufficiently responsive to the user. There are 
additional optimizations that we could perform that would help to 
further improve performance and to optimize the way that the system uses 
resources. For example: a further optimization that the team considered 
was to scale out the view model generators that populate the various 
read-models in the system. Every web-role that hosts a view model 
generator instance must handle the events published by the write-side by 
creating a subscription the the Windows Azure Service Bus topics.

## Further changes that would improve performance

In addition to the changes we made during this last stage of the journey to improve the performance of the application, the team identified a number of other changes that would result in further improvements. However, the available time for this journey was limited so it was not possible to make these changes in the V3 release.

* We added asynchronous behavior to many areas of the application, especially in the calls the application makes to the Windows Azure Service Bus. However, there are other areas where the application still makes blocking, synchronous calls that we could make asynchronous: for example, when the system accesses the data stores. In addition, we would make use of new language features such as **async** and **await** that will be available in Visual Studio 2012 (the application is currently implemented using .Net 4.0 and Visual Studio 2010).
* There are opportunities to process messages in batches and to reduce the number of round-trips the the data store by adopting a [store and forward][storeforward] design. For example, taking advantage of Windows Azure Service Bus sessions would enable us to accept a session from the Service Bus, read multiple items from the data store, process multiple messages, save once to the data store, and then complete all the messages. 

> **MarkusPersona:** By accepting a Service Bus session you have a single writer and listener for that session for as long as you keep the lock: this reduces the chances of an optimistic concurrency exception. This design would fit particularly well in the **SeatsAvailability** read and write models. For the read-models associated with the **Order** aggregates, which have very small partitions, you could acquire multiple small sessions from the Service Bus and use the store and forward approach on each session. Although both the read and write models in the system could benefit from this approach, it's easier to implement in the read-models where we expect the data to be eventually consistent and not fully consistent.

* The website already caches some frequently accessed read-model data, but we could extend the use of caching to other areas of the system. The CQRS pattern means that we can regard a cache as part of the eventually consistent read-model, and if necessary provide  access to read-model data from different parts of the system using different caches or no caching at all.
* The application currently listens for all messages on all Service Bus subscriptions using the same priority. In practice, some messages are more important then others; therefore, when the application is under stress we should prioritize some message processing to minimize the impact on core application functionality. For example, we could identify certain read-models where we are willing to accept more latency.

> **PoePersona:** We could also use autoscaling to scale out the application when load increases (for example by using the [Autoscaling Application Block][aab]), but adding new instances takes time. By prioritizing certain message types, we can continue to deliver performance in key areas of the application while the autoscaler adds resources.

* As part of our optimizations to the system, we now process some commands in-process instead of sending them through the Service Bus. We could extend this to other commands and potentially the process manager.
* In the current implementation, the process manager processes incoming messages and then the repository tries to send the outgoing messages synchronously (it uses the [Transient Fault Handling Application Block][tfhab] to retry sending commands if the Service Bus throws any exceptions due to throttling behavior). We could instead use a mechanism similar to that used by the **EventStoreBusPublisher** class so that the process manager saves a list of messages that must be sent along with its state in a single transaction, and then notifies a separate part of the system, that is responsible for sending the messages, that there are some new messages ready to send.

> **MarkusPersona:** The part of the system that is reponsible for
> sending the messages can do so asynchronously. It could also implement
> dynamic throttling for sending the messages and dynamically control
> how many parallel senders to use.

* Our current event store implementation publishes a single, small message on the service bus for every event that's saved in the event store. We could group some of these messages together to reduce the total number of I/O operations on the service bus. For example, a **SeatsAvailability** aggregate instance for a large conference publishes a large number of events, and the **Order** aggregate publishes events in bursts (when an **Order** aggregate is created it publishes both an **OrderPlaced** event and an **OrderTotalsCalculated** event). This will also help to reduce the latency in the system because currently, in those scenarios in which ordering is important, we must wait for a confirmation that one event has been sent before sending the next one. Grouping sequences of events in a single message would mean that we don't need to wait for the confirmation between publishing individual events.

## Further changes that would enhance scalability

The Contoso Conference Management System is designed to allow you to deploy multiple instances of the web and worker roles to scale out the application to handle larger loads. However, the design is not fully scalable because some of the other elements of the system such as the messages buses and data stores place constraints on the maximum achievable throughput. This section outlines some changes that we could make to the system to remove some of these constraints and significantly enhance the scalability of the system. The available time for this journey was limited so it was not possible to make these changes in the V3 release.

* **Partition the data:** The system stores different types of data in different partitions. You can see in the bootstrapping code how the different bounded contexts use different connection strings to connect to the SQL Database instance. However, each bounded context currently uses a single SQL Database instance and we could change this to use multiple different instances, each holding a specific set of data that the system uses. For example the the orders and registrations bounded context could use different SQL Database instances for the different read-models. We could also consider using the Federations feature to use sharding to scale out some of the SQL Database instances.

> "Data persistence is the hardest technical problem most scalable SaaS
> businesses face."  
> Evan Cooke, CTO, Twilio

> **JanaPersona:** Where the system stores data in Windows Azure table
> storage, we chose keys to partition the data for scalability. As an
> alternative to using SQL Database federations to shard the data we
> could move some of the read-model data currently in the SQL Database
> instance to either Windows Azure table storage or blob storage.

* **Further partition the Service Bus:** We already partition the Service Bus, by using different topics for different event publishers, to avoid throttling when the volume of messages that the system is sending approaches the maximum throughput that the Service Bus can handle. We could further partition the topics by using multiple, similar topics and listening to them all on a round-robin to spread the load. For a detailed description of this approach, see Chapter 11, "Asynchronous Communication and Message Buses" in _Scalability Rules: 50 Principles for Scaling Web Sites_, by Abbott and Fisher (Addison-Wesley, 2011).

* **Store and forward:** We introduced the store and forward design in the previous section that discuss performance improvement. By batching multiple operations, you not only reduce the number of round-trips to the data store and reduce the latency in the system, you also enhace the scalability of the system because fewer requests reduces the stress on the data store.

* **Listen for and react to throttling indicators:** Currently, the system uses the [Transient Fault Handling Application Block][tfhab] to detect transient error conditions such as throttling indicators from the Windows Azure Service Bus, the SQL Database instance, and Windows Azure table storage. The system uses the block to implement retries in these scenarios, typically by using an exponential back-off strategy. At present, we use dynamic throttling at the level of an individual subscription; however, we'd like to modify this to perform the dynamic throttling for all of the subscriptions to a specific topic. Similarly, we'd like to implement dynamic throttling at the level of the SQL Database instance, and at the level of the Windows Azure storage account.

> **JanaPersona:** For an example of implementing dynamic throttling within the application to avoid throttling from the service, see how the **EventStoreBusPublisher**, **SubscriptionReceiver**, and **SessionSubscriptionReceiver** classes use the **DynamicThrottling** class to manage the degree of parallelism they use to send or receive messages.

> **PoePersona:** Each service (Windows Azure Service Bus, SQL Database,
> Windows Azure storage) has its own particular way of implementing
> throttling behavior and notifying you when it is placed under heavy
> load. For example, see [SQL Azure Throttling][sqlthrottle]. It's
> important to be aware of all the throttling that your application may
> be subject in different services that your application uses.

> **PoePersona:** The team also considered using the Windows Azure SQL Database Business
> edition instead of the Windows Azure SQL Database Web edition but, upon investigation, we
> determined that at present, the only difference between the editions
> is the maximum database size. The different editions are not tuned to
> support different types of workload, and both editions implement the
> same throttling behavior.

For some additional information relating to scalability, see:

* [Windows Azure Storage Abstractions and their Scalability Targets][wascale]
* [Best Practices for Performance Improvements Using Service Bus Brokered Messaging][sbscale]

It's important not to get a false sense of optimism when it comes to scalability and high availability. While with many of the suggested practices the applications tend to scale more efficiently and become more resilient to failure, they are still prone to high-demand bottlenecks. Make sure to allocate sufficient time for performance testing and for meeting your performance goals.

# No downtime migration

_"Preparation, I have often said, is rightly two-thirds of any venture." Amelia Earhart_

The team planned to have a no-downtime migration from the V2 to the V3 release in Windows Azure. To achieve this, 
the migration process uses an ad-hoc processor running in a Windows Azure worker role to perform some of the migration steps. 

The migration process still requires you to complete a configuration step to switch off the V2 processor and switch on the V3 processor. In retrospect, we would have used a different mechanism to streamline the transition from the V2 to the V3 processor based on feedback from the handlers themselves to indicate when they have finished their processing.

For details of these steps, see Appendix 1,
"[Building and Running the Sample Code][appendix]."

> **PoePersona:** You should always rehearse the migration in a test
> environment before performing it in your production environment.

## Rebuilding the read models

During the migration from V2 to V3, one of the steps we must perform is to rebuild the **DraftOrder** and **PricedOrder** view models by replaying events from the event log to populate the new V3 read-model tables. We can do this asynchronously. However, at some point in time, we need to start sending events from the live application to these read models. Furthermore, we need to keep both the V2 and V3 versions of these read models up to date until the migration process is complete because the V2 front-end web role will need the V2 read-model data to be available until we switch to the V3 front-end web role. At the point at which we switch to the V3 front end, we must ensure that the V3 read models are completely up to date.

To keep these read models up to date, we created an ad-hoc processor as a Windows Azure worker role that runs just while the migration is taking place. See the MigrationToV3 project in the Conference solution for more details. The steps that this processor performs are to:

* Create a new set of topic subscriptions that will receive the live
  events that will be used to populate the new V3 read models. These
  subscriptions will start accumulating the events that will be handled
  when the V3 application is deployed.
* Replay the events from the event log to populate the new V3 read
  models with historical data.
* Handle the live events and keep the V2 read models up to date until
  the V3 front end is live, at which point we no longer need the V2 read
  models.

The migration process first replays the events from the event store to populate the new V3 read models. When this is complete, we stop the V2 processor that contains the event handlers, and start the new handlers in their V3 processor. While these are running and catching up on the events that were accumulated in the new topic subscriptions, the ad-hoc processor is also keeping the V2 read models up to date because at this point we still have the V2 front end. When the V3 worker roles are ready, we can perform a VIP switch to bring the new V3 front end into use. After the V3 front end is running, we no longer have any need for the V2 read models.

One of the issues to address with this approach is how to determine when the new V3 processor should switch from processing archived events in the event log to the live stream of events. There is some latency in the process that writes events to the event log, so an instantaneous switch could result in the loss of some events. The team decided to allow the V3 processor to temporarily handle both archived events and the live stream which means there is a possibility that there will be duplicate events; the same event exists in the event store and in the list of events accumulated by the new subscription. However, we can detect these duplicates and handle them accordingly.

> **MarkusPersona:** Typically, we rely on the infrastructure to detect duplicate messages. In this particular scenario where duplicate events may come from different sources, we cannot rely on the infrastructure and must add the duplicate detection logic into our code explicitly.

An alternative approach that we considered was to include both V2 and V3 handling in the V3 processor. With this approach there is no need for an ad-hoc worker role to process the V2 events during the migration. However, we decided to keep the migration-specific code in a separate project to avoid bloating the V3 release with functionality that is only needed during the migration.

> **JanaPersona:** The migration process would be slightly easier if we included both V2 and V3 handling in the V3 processor. We decided that the benefit of such an approach was outweighed by the benefit of not having to maintain duplicate functionality in the V3 processor.

> **Note:** TThe intervals between each step of the migration take some time to complete, so the migration achieves no downtime, but the user does experience delays. We would have benefited from some faster mechanisms to deal with the toggle switches, such as stopping the V2 processor and starting the V3 processor.

# Implementation details 

This section describes some of the significant features of the 
implementation of the Orders and Registrations bounded context. You may 
find it useful to have a copy of the code so you can follow along. You 
can download a copy of the code from the [Download center][downloadc], 
or check the evolution of the code in the repository on github: 
[mspnp/cqrs-journey-code][repourl]. You can download the code from the
V3 release from the [Tags][tags] page on Github.

> **Note:** Do not expect the code samples to exactly match the code in
> the reference implementation. This chapter describes a step in the
> CQRS journey, the implementation may well change as we learn more and
> refactor the code.

## Hardening the RegistrationProcessManager class

This section describes how the team hardened the **RegistrationProcessManager** 
process manager by checking for duplicate instances of the **SeatsReserved** 
and **OrderPlaced** messages. 

### Detecting out of order SeatsReserved events

Typically, the **RegistrationProcessManager** class sends a 
**MakeSeatReservation** command to the **SeatAvailability** aggregate, 
the **SeatAvailability** aggregate publishes a **SeatsReserved** event 
when it has made the reservation, and the **RegistrationProcessManager** 
receives this notification. The **RegistrationProcessManager** sends a 
**MakeSeatReservation** command both when the order is created and when 
it is updated. It is possible that the **SeatsReserved** events could 
arrive out of order, however the system should honor the event 
related to the last command that was sent. The solution described in 
this section enables the **RegistrationProcessManager** to identify the 
most recent **SeatsReserved** message and then ignore any earlier 
messages instead of re-processing them. 

Before the **RegistrationProcessManager** class sends the 
**MakeSeatReservation** command, it saves the **Id** of the command in 
the **SeatReservationCommandId** variable as shown in the following code 
sample: 

```Cs
public void Handle(OrderPlaced message)
{
    if (this.State == ProcessState.NotStarted)
    {
        this.ConferenceId = message.ConferenceId;
        this.OrderId = message.SourceId;
        // use the order id as the opaque reservation id for the seat reservation
        this.ReservationId = message.SourceId;
        this.ReservationAutoExpiration = message.ReservationAutoExpiration;
        this.State = ProcessState.AwaitingReservationConfirmation;

        var seatReservationCommand =
            new MakeSeatReservation
            {
                ConferenceId = this.ConferenceId,
                ReservationId = this.ReservationId,
                Seats = message.Seats.ToList()
            };
        this.SeatReservationCommandId = seatReservationCommand.Id;
        this.AddCommand(seatReservationCommand);

        ...
}
```

Then, when it handles the **SeatsReserved** event, it checks that the 
**CorrelationId** property of the event matches the most recent value of 
the **SeatReservationCommandId** variable as shown in the following code 
sample: 

```Cs
public void Handle(Envelope<SeatsReserved> envelope)
{
    if (this.State == ProcessState.AwaitingReservationConfirmation)
    {
        if (envelope.CorrelationId != null)
        {
            if (string.CompareOrdinal(this.SeatReservationCommandId.ToString(), envelope.CorrelationId) != 0)
            {
                // skip this event
                Trace.TraceWarning("Seat reservation response for reservation id {0} does not match the expected correlation id.", envelope.Body.ReservationId);
                return;
            }
        }

        ...
}
```

Notice how this **Handle** method handles an **Envelope** instance 
instead of a **SeatsReserved** instance. As a part of the V3 release, 
events are wrapped in an **Envelope** instance that includes the 
**CorrelationId** property. The **DoDispatchMessage** method in the 
**EventDispatcher** assigns the value of the correlation Id. 

> **MarkusPersona:** As a side-effect of adding this feature, the
> **EventProcessor** class can no longer use the **dynamic** keyword
> when it forwards events to handlers. Now in V3 it uses the new
> **EventDispatcher** class: this class uses reflection to identify the
> correct handlers for a given message type.

During performance testing, the team identified a further issue with 
this specific **SeatsReserved** event. Because of a delay elsewhere in 
the system when it was under load, a second copy of the 
**SeatsReserved** event was being published. This **Handle** method was 
then throwing an exception that caused the system to retry processing 
the message several times before sending it to a dead-letter queue. To 
address this specific issue, the team modified this method by adding the 
**else if** clause as shown in the following code sample: 

```Cs
public void Handle(Envelope<SeatsReserved> envelope)
{
    if (this.State == ProcessState.AwaitingReservationConfirmation)
    {
        ...
    }
    else if (string.CompareOrdinal(this.SeatReservationCommandId.ToString(), envelope.CorrelationId) == 0)
    {
        Trace.TraceInformation("Seat reservation response for request {1} for reservation id {0} was already handled. Skipping event.", envelope.Body.ReservationId, envelope.CorrelationId);
    }
    else
    {
        throw new InvalidOperationException("Cannot handle seat reservation at this stage.");
    }
}
```

> **MarkusPersona:** This optimization was only applied for this
> specific message. Notice that it makes use of the value of
> **SeatReservationCommandId** property that was previously saved in the
> instance. If you want to perform this kind of check on other messages
> you'll need to store more information in the process manager.

### Detecting duplicate OrderPlaced events

To achieve this, the **RegistrationProcessManagerRouter** class now performs a 
check to see of the event is has already been processed. The new V3 
version of the code is shown in the following code sample: 

```Cs
public void Handle(OrderPlaced @event)
{
    using (var context = this.contextFactory.Invoke())
    {
        var pm = context.Find(x => x.OrderId == @event.SourceId);
        if (pm == null)
        {
            pm = new RegistrationProcessManager();
        }

        pm.Handle(@event);
        context.Save(pm);
    }
}
```

### Creating a pseudo transaction when the RegistrationProcessManager class saves its state and sends a command

It is not possible to have a transaction in Windows Azure that spans 
persisting the **RegistrationProcessManager** to storage and sending the 
command. Therefore the team decided to save all the commands that the 
process manager generates so that if the process crashes the commands 
are not lost and can be sent later. We use another process to handle 
sending the commands reliably. 

> **MarkusPersona:** The migration utility for moving to the V3 release
> updates the database schema to accomodate the new storage requirement.

The following code sample from the **SqlProcessDataContext** class shows 
how the system persists all the commands along with the state of the 
process manager: 

```Cs
public void Save(T process)
{
    var entry = this.context.Entry(process);

    if (entry.State == System.Data.EntityState.Detached)
        this.context.Set<T>().Add(process);

    var commands = process.Commands.ToList();
    UndispatchedMessages undispatched = null;
    if (commands.Count > 0)
    {
        // if there are pending commands to send, we store them as undispatched.
        undispatched = new UndispatchedMessages(process.Id)
                            {
                                Commands = this.serializer.Serialize(commands)
                            };
        this.context.Set<UndispatchedMessages>().Add(undispatched);
    }

    try
    {
        this.context.SaveChanges();
    }
    catch (DbUpdateConcurrencyException e)
    {
        throw new ConcurrencyException(e.Message, e);
    }

    this.DispatchMessages(undispatched, commands);
}
```

The following code sample from the **SqlProcessDataContext** class shows 
how the system tries to send the command messages: 

```Cs
private void DispatchMessages(UndispatchedMessages undispatched, List<Envelope<ICommand>> deserializedCommands = null)
{
	if (undispatched != null)
	{
		if (deserializedCommands == null)
		{
			deserializedCommands = this.serializer.Deserialize<IEnumerable<Envelope<ICommand>>>(undispatched.Commands).ToList();
		}

		var originalCommandsCount = deserializedCommands.Count;
		try
		{
			while (deserializedCommands.Count > 0)
			{
				this.commandBus.Send(deserializedCommands.First());
				deserializedCommands.RemoveAt(0);
			}
		}
		catch (Exception)
		{
			// We catch a generic exception as we don't know what implementation of ICommandBus we might be using.
			if (originalCommandsCount != deserializedCommands.Count)
			{
				// if we were able to send some commands, then updates the undispatched messages.
				undispatched.Commands = this.serializer.Serialize(deserializedCommands);
				try
				{
					this.context.SaveChanges();
				}
				catch (DbUpdateConcurrencyException)
				{
					// if another thread already dispatched the messages, ignore and surface original exception instead
				}
			}

			throw;
		}

		// we remove all the undispatched messages for this process manager.
		this.context.Set<UndispatchedMessages>().Remove(undispatched);
		this.retryPolicy.ExecuteAction(() => this.context.SaveChanges());
	}
}
```

The **DispatchMessages** method is also invoked from the **Find** method 
in the **SqlProcessDataContext** class so that it tries to send any 
un-dispatched messages whenever the system rehydrates a 
**RegistrationProcessManager** instance. 

## Optimizing the UI flow

The first optimization is to allow the UI to navigate directly to the 
Registrant screen provided that there are plenty of seats still 
available for the conference. This change is introduced in the 
**StartRegistration** method in the **RegistrationController** class 
that now performs an additional check to verify that there are enough 
remaining seats to stand a good chance of making the reservation before 
it sends the **RegisterToConference** command as shown in the following 
code sample: 

```Cs
[HttpPost]
public ActionResult StartRegistration(RegisterToConference command, int orderVersion)
{
    var existingOrder = orderVersion != 0 ? this.orderDao.FindDraftOrder(command.OrderId) : null;
    var viewModel = existingOrder == null ? this.CreateViewModel() : this.CreateViewModel(existingOrder);
    viewModel.OrderId = command.OrderId;
    
    if (!ModelState.IsValid)
    {
        return View(viewModel);
    }

    // checks that there are still enough available seats, and the seat type IDs submitted ar valid.
    ModelState.Clear();
    bool needsExtraValidation = false;
    foreach (var seat in command.Seats)
    {
        var modelItem = viewModel.Items.FirstOrDefault(x => x.SeatType.Id == seat.SeatType);
        if (modelItem != null)
        {
            if (seat.Quantity > modelItem.MaxSelectionQuantity)
            {
                modelItem.PartiallyFulfilled = needsExtraValidation = true;
                modelItem.OrderItem.ReservedSeats = modelItem.MaxSelectionQuantity;
            }
        }
        else
        {
            // seat type no longer exists for conference.
            needsExtraValidation = true;
        }
    }

    if (needsExtraValidation)
    {
        return View(viewModel);
    }

    command.ConferenceId = this.ConferenceAlias.Id;
    this.commandBus.Send(command);

    return RedirectToAction(
        "SpecifyRegistrantAndPaymentDetails",
        new { conferenceCode = this.ConferenceCode, orderId = command.OrderId, orderVersion = orderVersion });
}
```

If there are not enough available seats, the controller redisplays the 
current screen, displaying the currently available seat quantities to 
enable the Registrant to revise her order. 

This remaining part of the change is in the 
**SpecifyRegistrantAndPaymentDetails** method in the 
**RegistrationController** class. The following code sample from the V2 
release shows how before the optimization the controller calls the 
**WaitUntilSeatsAreConfirmed** method before continuing to the 
Registrant screen: 

```Cs
[HttpGet]
[OutputCache(Duration = 0, NoStore = true)]
public ActionResult SpecifyRegistrantAndPaymentDetails(Guid orderId, int orderVersion)
{
    var order = this.WaitUntilSeatsAreConfirmed(orderId, orderVersion);
    if (order == null)
    {
        return View("ReservationUnknown");
    }

    if (order.State == DraftOrder.States.PartiallyReserved)
    {
        return this.RedirectToAction("StartRegistration", new { conferenceCode = this.ConferenceCode, orderId, orderVersion = order.OrderVersion });
    }

    if (order.State == DraftOrder.States.Confirmed)
    {
        return View("ShowCompletedOrder");
    }

    if (order.ReservationExpirationDate.HasValue && order.ReservationExpirationDate < DateTime.UtcNow)
    {
        return RedirectToAction("ShowExpiredOrder", new { conferenceCode = this.ConferenceAlias.Code, orderId = orderId });
    }

    var pricedOrder = this.WaitUntilOrderIsPriced(orderId, orderVersion);
    if (pricedOrder == null)
    {
        return View("ReservationUnknown");
    }

    this.ViewBag.ExpirationDateUTC = order.ReservationExpirationDate;

    return View(
        new RegistrationViewModel
        {
            RegistrantDetails = new AssignRegistrantDetails { OrderId = orderId },
            Order = pricedOrder
        });
}
```

The following code sample shows the V3 version of this method that no 
longer waits for the reservation to be confirmed: 

```Cs
[HttpGet]
[OutputCache(Duration = 0, NoStore = true)]
public ActionResult SpecifyRegistrantAndPaymentDetails(Guid orderId, int orderVersion)
{
    var pricedOrder = this.WaitUntilOrderIsPriced(orderId, orderVersion);
    if (pricedOrder == null)
    {
        return View("PricedOrderUnknown");
    }

    if (!pricedOrder.ReservationExpirationDate.HasValue)
    {
        return View("ShowCompletedOrder");
    }

    if (pricedOrder.ReservationExpirationDate < DateTime.UtcNow)
    {
        return RedirectToAction("ShowExpiredOrder", new { conferenceCode = this.ConferenceAlias.Code, orderId = orderId });
    }

    return View(
        new RegistrationViewModel
        {
            RegistrantDetails = new AssignRegistrantDetails { OrderId = orderId },
            Order = pricedOrder
        });
}
```

> **Note:** We made this method asynchronous later on during this stage
> of the journey.

The second optimization in the UI flow is to perform the calculation of the order total 
earlier in the process. In the previous code sample, the 
**SpecifyRegistrantAndPaymentDetails** method still calls the 
**WaitUntilOrderIsPriced** method which pauses the UI flow until the 
system calculates an order total and makes it available to the 
controller by saving it in the priced order view model on the read-side. 

The key change to implement this is in the **Order** aggregate. The 
constructor in the **Order** class now invokes the **CalculateTotal** 
method and raises an **OrderTotalsCalculated** event as shown in the 
following code sample: 

```Cs
public Order(Guid id, Guid conferenceId, IEnumerable<OrderItem> items, IPricingService pricingService)
    : this(id)
{
    var all = ConvertItems(items);
    var totals = pricingService.CalculateTotal(conferenceId, all.AsReadOnly());

    this.Update(new OrderPlaced
    {
        ConferenceId = conferenceId,
        Seats = all,
        ReservationAutoExpiration = DateTime.UtcNow.Add(ReservationAutoExpiration),
        AccessCode = HandleGenerator.Generate(6)
    });
    this.Update(new OrderTotalsCalculated { Total = totals.Total, Lines = totals.Lines != null ? totals.Lines.ToArray() : null, IsFreeOfCharge = totals.Total == 0m });
}
```

Previously, in the V2 release the **Order** aggregate waited until it 
received a **MarkAsReserved** command before it called the 
**CalculateTotal** method. 

## Receiving, completing, and sending messages asynchronously

This section outlines how the system now performs all IO on the Windows 
Azure Service Bus asynchronously. 

### Receiving messages asynchronously

The **SubscriptionReceiver** and **SessionSubscriptionReceiver** classes 
now receive messages asynchronously instead of synchronously in the loop 
in the **ReceiveMessages** method. 

For details see either the **ReceiveMessages** method in the 
**SubscriptionReceiver** class or the **ReceiveMessagesAndCloseSession** 
method in the **SessionSubscriptionReceiver** class. 

> **MarkusPersona:** This code sample also shows how to use the
> [Transient Fault Handling Application Block][tfhab] to reliably
> receive messages asynchronously from the Service Bus topic. The
> asynchronous loops make the code much harder to read, but much more
> efficient. This is recommended best practice. This code would benefit
> from the new **async** keywords in C# 4.

### Completing messages asynchronously

The system uses the peek/lock mechanism to retrieve messages from the 
Service Bus topic subscriptions. To learn how the system performs these
operations asynchronously, see the **ReceiveMessages** methods in the
**SubscriptionReceiver** and **SessionSubscriptionReceiver** classes. This provides one example of how the system uses asynchronous APIs.

### Sending messages asynchronously

The application now sends all messages on the Service Bus 
asynchronously. For more details see the **TopicSender** class. 

## Handling commands synchronously and in-process

In the V2 release, the system used the Windows Azure Service Bus to 
deliver all commands to their recipients. This meant that the system 
delivered the commands asynchronously. In the v3 release, the MVC 
controllers now send their commands synchronously and in-process in order to 
improve the response times in the UI by bypassing the command bus and 
delivering commands directly to their handlers. In addition, in the 
**ConferenceProcessor** worker role, commands sent to **Order** 
aggregates are sent synchronously in-process using the same mechanism. 

> **MarkusPersona:** We still continue to send commands to the
> **SeatsAvailability** aggregate asynchronously because with multiple
> instances of the **RegistrationProcessManager** running in parallel,
> there will contention as multiple threads all try to access the same
> instance of the **SeatsAvailability** aggregate.

The team implemeted this behavior by adding the 
**SynchronousCommandBusDecorator** and **CommandDispatcher** classes to 
the infrastructure and registering them during the start up of the web 
role as shown in the following code sample from the 
**OnCreateContainer** method in the Global.asax.Azure.cs file: 

```Cs
var commandBus = new CommandBus(new TopicSender(settings.ServiceBus, "conference/commands"), metadata, serializer);
var synchronousCommandBus = new SynchronousCommandBusDecorator(commandBus);

container.RegisterInstance<ICommandBus>(synchronousCommandBus);
container.RegisterInstance<ICommandHandlerRegistry>(synchronousCommandBus);


container.RegisterType<ICommandHandler, OrderCommandHandler>("OrderCommandHandler");
container.RegisterType<ICommandHandler, ThirdPartyProcessorPaymentCommandHandler>("ThirdPartyProcessorPaymentCommandHandler");
container.RegisterType<ICommandHandler, SeatAssignmentsHandler>("SeatAssignmentsHandler");
```

> **Note:** There is similar code in the Conference.Azure.cs file to
> configure the worker role to send some commands in-process.


The following code sample shows how the 
**SynchronousCommandBusDecorator** class implements sending a command 
message: 

```Cs
public class SynchronousCommandBusDecorator : ICommandBus, ICommandHandlerRegistry
{
    private readonly ICommandBus commandBus;
    private readonly CommandDispatcher commandDispatcher;

    public SynchronousCommandBusDecorator(ICommandBus commandBus)
    {
        this.commandBus = commandBus;
        this.commandDispatcher = new CommandDispatcher();
    }

    ...

    public void Send(Envelope<ICommand> command)
    {
        if (!this.DoSend(command))
        {
            Trace.TraceInformation("Command with id {0} was not handled locally. Sending it through the bus.", command.Body.Id);
            this.commandBus.Send(command);
        }
    }

    ...

    private bool DoSend(Envelope<ICommand> command)
    {
        bool handled = false;

        try
        {
            var traceIdentifier = string.Format(CultureInfo.CurrentCulture, " (local handling of command with id {0})", command.Body.Id);
            handled = this.commandDispatcher.ProcessMessage(traceIdentifier, command.Body, command.MessageId, command.CorrelationId);

        }
        catch (Exception e)
        {
            Trace.TraceWarning("Exception handling command with id {0} synchronously: {1}", command.Body.Id, e.Message);
        }

        return handled;
    }
}
```

Notice how this class tries to send the command synchronously without 
using the service bus, but if it cannot find a handler for the command, 
it reverts to using the service bus. The following code sample shows how 
the **CommandDispatcher** class tries to locate a handler and deliver a 
command message: 

```Cs
public bool ProcessMessage(string traceIdentifier, ICommand payload, string messageId, string correlationId)
{
    var commandType = payload.GetType();
    ICommandHandler handler = null;

    if (this.handlers.TryGetValue(commandType, out handler))
    {
        Trace.WriteLine("-- Handled by " + handler.GetType().FullName + traceIdentifier);
        ((dynamic)handler).Handle((dynamic)payload);
        return true;
    }
    else
    {
        return false;
    }
}
```

## Implementing snapshots with the memento pattern

In the Contoso Conference Management System, the only event-sourced 
aggregate that is likely to have a significant number of events per instance and 
benefit from snapshots is the **SeatAvailability** aggregate. 

> **MarkusPersona:** Because we chose to use the memento pattern, the
> snapshot of the aggregate state is stored in the memento.

The following code sample from the **Save** method in the 
**AzureEventSourcedRepository** class shows how the system creates a 
cached memento object if there is a cache and the aggregate implements 
the **IMementoOriginator** interface. 

```Cs
public void Save(T eventSourced, string correlationId)
{
    ...

    this.cacheMementoIfApplicable.Invoke(eventSourced);
}
```

Then, when the system loads an aggregate by invoking the **Find** method 
in the **AzureEventSourcedRepository** class, it checks to see of there 
is a cached memento containing A snapshot of the state of the object to 
use: 

```Cs

private readonly Func<Guid, Tuple<IMemento, DateTime?>> getMementoFromCache;

...

public T Find(Guid id)
{
	var cachedMemento = this.getMementoFromCache(id);
	if (cachedMemento != null && cachedMemento.Item1 != null)
	{
		IEnumerable<IVersionedEvent> deserialized;
		if (!cachedMemento.Item2.HasValue || cachedMemento.Item2.Value < DateTime.UtcNow.AddSeconds(-1))
		{
			deserialized = this.eventStore.Load(GetPartitionKey(id), cachedMemento.Item1.Version + 1).Select(this.Deserialize);
		}
		else
		{
			deserialized = Enumerable.Empty<IVersionedEvent>();
		}

		return this.originatorEntityFactory.Invoke(id, cachedMemento.Item1, deserialized);
	}
	else
	{
		var deserialized = this.eventStore.Load(GetPartitionKey(id), 0)
			.Select(this.Deserialize)
			.AsCachedAnyEnumerable();

		if (deserialized.Any())
		{
			return this.entityFactory.Invoke(id, deserialized);
		}
	}

	return null;
}
```

If the cache entry was updated in the last few seconds, there is a high probability that it is not stale because we have a single writer for high-contention aggregates. Therefore, we optimistically avoid checking for new events in the event store since the memento was created. Otherwise, we check in the event store for events that arrived after the memento was created. 

 

The following code sample shows how the **SeatsAvailability** class adds 
a snapshot of its state data to the memento object to be cached: 

```Cs
public IMemento SaveToMemento()
{
    return new Memento
    {
        Version = this.Version,
        RemainingSeats = this.remainingSeats.ToArray(),
        PendingReservations = this.pendingReservations.ToArray(),
    };
}
```

## Publishing Events in parallel

In chapter 5, [Preparing for the V1 Release][j_chapter5], you saw how 
the system publishes events whenever it saves them to the event store. 
This optimization enables the system to publish some of these events in 
parallel insteand of publishing them sequentially. It is important that 
the events associated with a specific aggregate instance are sent in the 
correct order, so the system only creates new tasks for different 
partition keys. The following code sample from the **Start** method in 
the **EventStoreBusPublisher** class shows how the parallel tasks are 
defined: 

```Cs
Task.Factory.StartNew(
    () =>
    {
        try
        {
            foreach (var key in GetThrottlingEnumerable(this.enqueuedKeys.GetConsumingEnumerable(cancellationToken), this.throttlingSemaphore, cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    ProcessPartition(key);
                }
                else
                {
                    this.enqueuedKeys.Add(key);
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    },
    TaskCreationOptions.LongRunning);
```

The **SubscriptionReceiver** and **SessionSubscriptionReceiver** classes use the same **DynamicThrottling** class to dynamically throttle the retrieval of messages from the service bus.

## Filtering messages in subscriptions

The team added filters to the Windows Azure Service Bus subscriptions to restrict the messages that each each subscription receives to those messages that the subscription is intended to handle. You can see the definitions of these filters in the Settings.Template.xml file as shown in the following snippet:

```Xml
<Topic Path="conference/events" IsEventBus="true">
  <Subscription Name="log" RequiresSession="false"/>
  <Subscription Name="Registration.RegistrationPMOrderPlaced" RequiresSession="false" SqlFilter="TypeName IN ('OrderPlaced')"/>
  <Subscription Name="Registration.RegistrationPMNextSteps" RequiresSession="false" SqlFilter="TypeName IN ('OrderUpdated','SeatsReserved','PaymentCompleted','OrderConfirmed')"/>
  <Subscription Name="Registration.OrderViewModelGenerator" RequiresSession="true" SqlFilter="TypeName IN ('OrderPlaced','OrderUpdated','OrderPartiallyReserved','OrderReservationCompleted','OrderRegistrantAssigned','OrderConfirmed','OrderPaymentConfirmed')"/>
  <Subscription Name="Registration.PricedOrderViewModelGenerator" RequiresSession="true" SqlFilter="TypeName IN ('OrderPlaced','OrderTotalsCalculated','OrderConfirmed','OrderExpired','SeatAssignmentsCreated','SeatCreated','SeatUpdated')"/>
  <Subscription Name="Registration.ConferenceViewModelGenerator" RequiresSession="true" SqlFilter="TypeName IN ('ConferenceCreated','ConferenceUpdated','ConferencePublished','ConferenceUnpublished','SeatCreated','SeatUpdated','AvailableSeatsChanged','SeatsReserved','SeatsReservationCancelled')"/>
  <Subscription Name="Registration.SeatAssignmentsViewModelGenerator" RequiresSession="true" SqlFilter="TypeName IN ('SeatAssignmentsCreated','SeatAssigned','SeatUnassigned','SeatAssignmentUpdated')"/>
  <Subscription Name="Registration.SeatAssignmentsHandler" RequiresSession="true" SqlFilter="TypeName IN ('OrderConfirmed','OrderPaymentConfirmed')"/>
  <Subscription Name="Conference.OrderEventHandler" RequiresSession="true" SqlFilter="TypeName IN ('OrderPlaced','OrderRegistrantAssigned','OrderTotalsCalculated','OrderConfirmed','OrderExpired','SeatAssignmentsCreated','SeatAssigned','SeatAssignmentUpdated','SeatUnassigned')"/>

  ...
</Topic>

```

## Creating a dedicated **SessionSubscriptionReciever** instance for the **SeatsAvailability** aggregate

In the V2 release, the system did not use sessions for commands because we do not require ordering guarantees for commands. However, we now want to use sessions for commands to guarantee a single listener for each **SeatsAvailability** aggregate instance, which will help us to scale out without getting a large number of concurrency exceptions from this high-contention aggregate.

The following code sample from the Conference.Processor.Azure.cs file shows how the system creates a dedicated **SessionSubscriptionReceiver** instance to receive messages destined for the **SeatsAvailability** aggregate:

```Cs
var seatsAvailabilityCommandProcessor =
    new CommandProcessor(new SessionSubscriptionReceiver(azureSettings.ServiceBus, Topics.Commands.Path, Topics.Commands.Subscriptions.SeatsAvailability, false), serializer);

...

container.RegisterInstance<IProcessor>("SeatsAvailabilityCommandProcessor", seatsAvailabilityCommandProcessor);
```

The following code sample shows the new abstract **SeatsAvailabilityCommand** class that includes a session Id based on the conference that the command is associated with:

```Cs
public abstract class SeatsAvailabilityCommand : ICommand, IMessageSessionProvider
{
    public SeatsAvailabilityCommand()
    {
        this.Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
    public Guid ConferenceId { get; set; }

    string IMessageSessionProvider.SessionId
    {
        get { return "SeatsAvailability_" + this.ConferenceId.ToString(); }
    }
}
```

The command bus now uses a separate subscription for commands destined for the **SeatsAvailability** aggregate.

**MarkusPersona:** The team applied a similar technique to the  RegistrationProcessManager process manager by creating a separate subscription for OrderPlaced events to handle new orders. A separate subscription receives all the other events destined for the process manager.

## Caching read-model data

As part of the performance optimizations in the V3 release, the team 
added caching behavior for the conference information stored in the 
Orders and Registrations bounded context read model. This reduces the 
time taken to read this commonly used data.

The following code sample from the **GetPublishedSeatTypes** method in the **CachingConferenceDao** class shows how the system determines whether to cache the data for a conference based on the number of available seats:

```Cs
TimeSpan timeToCache;
if (seatTypes.All(x => x.AvailableQuantity > 200 || x.AvailableQuantity <= 0))
{
    timeToCache = TimeSpan.FromMinutes(5);
}
else if (seatTypes.Any(x => x.AvailableQuantity < 30 && x.AvailableQuantity > 0))
{
    // there are just a few seats remaining. Do not cache.
    timeToCache = TimeSpan.Zero;
}
else if (seatTypes.Any(x => x.AvailableQuantity < 100 && x.AvailableQuantity > 0))
{
    timeToCache = TimeSpan.FromSeconds(20);
}
else
{
    timeToCache = TimeSpan.FromMinutes(1);
}

if (timeToCache > TimeSpan.Zero)
{
    this.cache.Set(key, seatTypes, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(timeToCache) });
}
```

> **JanaPersona:** You can see how we manage the risks associated with displaying stale data by adjusting the caching duration, or even deciding not to cache the data at all.

The system now also uses a cache to hold seat type descriptions in the **PricedOrderViewModelGenerator** class.

## Using multiple topics to partition the service bus

To reduce the number of messages flowing through the service bus topics, we partitioned the service bus by creating two additional topics to transport events published by the **Order** and **SeatAvailability** aggregates. This helps us to avoid being throttled by the service bus when the application is experiencing very high loads. The following snippet from the Settings.xml file shows the definitions of these new topics:

```XML
<Topic Path="conference/orderevents" IsEventBus="true">
  <Subscription Name="logOrders" RequiresSession="false"/>
  <Subscription Name="Registration.RegistrationPMOrderPlacedOrders" RequiresSession="false"
    SqlFilter="TypeName IN ('OrderPlaced')"/>
  <Subscription Name="Registration.RegistrationPMNextStepsOrders" RequiresSession="false"
    SqlFilter="TypeName IN ('OrderUpdated','SeatsReserved','PaymentCompleted','OrderConfirmed')"/>
  <Subscription Name="Registration.OrderViewModelGeneratorOrders" RequiresSession="true"
    SqlFilter="TypeName IN ('OrderPlaced','OrderUpdated','OrderPartiallyReserved','OrderReservationCompleted',
    'OrderRegistrantAssigned','OrderConfirmed','OrderPaymentConfirmed')"/>
  <Subscription Name="Registration.PricedOrderViewModelOrders" RequiresSession="true"
    SqlFilter="TypeName IN ('OrderPlaced','OrderTotalsCalculated','OrderConfirmed',
    'OrderExpired','SeatAssignmentsCreated','SeatCreated','SeatUpdated')"/>
  <Subscription Name="Registration.SeatAssignmentsViewModelOrders" RequiresSession="true"
    SqlFilter="TypeName IN ('SeatAssignmentsCreated','SeatAssigned','SeatUnassigned','SeatAssignmentUpdated')"/>
  <Subscription Name="Registration.SeatAssignmentsHandlerOrders" RequiresSession="true"
    SqlFilter="TypeName IN ('OrderConfirmed','OrderPaymentConfirmed')"/>
  <Subscription Name="Conference.OrderEventHandlerOrders" RequiresSession="true"
    SqlFilter="TypeName IN ('OrderPlaced','OrderRegistrantAssigned','OrderTotalsCalculated',
    'OrderConfirmed','OrderExpired','SeatAssignmentsCreated','SeatAssigned','SeatAssignmentUpdated','SeatUnassigned')"/>
</Topic>
<Topic Path="conference/availabilityevents" IsEventBus="true">
  <Subscription Name="logAvail" RequiresSession="false"/>
  <Subscription Name="Registration.RegistrationPMNextStepsAvail" RequiresSession="false"
    SqlFilter="TypeName IN ('OrderUpdated','SeatsReserved','PaymentCompleted','OrderConfirmed')"/>
  <Subscription Name="Registration.PricedOrderViewModelAvail" RequiresSession="true"
    SqlFilter="TypeName IN ('OrderPlaced','OrderTotalsCalculated','OrderConfirmed',
    'OrderExpired','SeatAssignmentsCreated','SeatCreated','SeatUpdated')"/>
  <Subscription Name="Registration.ConferenceViewModelAvail" RequiresSession="true"
    SqlFilter="TypeName IN ('ConferenceCreated','ConferenceUpdated','ConferencePublished',
    'ConferenceUnpublished','SeatCreated','SeatUpdated','AvailableSeatsChanged',
    'SeatsReserved','SeatsReservationCancelled')"/>
</Topic>
```

## Other optimizing and hardening changes

This section outlines some of the additional ways that the team optimized the performance of the application and improved its resilience: 

* Using sequential GUIDs
* Using asynchronous ASP.NET MVC controllers.
* Using prefetch to retrieve multiple messages from the Service Bus.
* Accepting multiple Windows Azure Service Bus sessions in parallel.
* Expiring seat reservation commands.

### Sequential GUIDs

Previously, the system generated the GUIDs that it used for the IDs of aggregates such as orders and reservations using the **Guid.NewGuid** method, which generates random GUIDs. If these GUIDs are used as primary key values in a SQL Database instance, this causes frequent page splits in the indexes, which has a negative impact on the performance of the database. In the V3 release, the team added a utility class that generates sequential GUIDs. This ensures that new entries in the SQL Database tables are always appends; this improves the overall performance of the database. The following code sample shows the new **GuidUtil** class:

```Cs
public static class GuidUtil
{
	private static readonly long EpochMilliseconds = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000L;

	/// <summary>
	/// Creates a sequential GUID according to SQL Server's ordering rules.
	/// </summary>
	public static Guid NewSequentialId()
	{
		// This code was not reviewed to guarantee uniqueness under most conditions, nor completely optimize for avoiding
		// page splits in SQL Server when doing inserts from multiple hosts, so do not re-use in production systems.
		var guidBytes = Guid.NewGuid().ToByteArray();

		// get the milliseconds since Jan 1 1970
		byte[] sequential = BitConverter.GetBytes((DateTime.Now.Ticks / 10000L) - EpochMilliseconds);

		// discard the 2 most significant bytes, as we only care about the milliseconds increasing, but the highest ones should be 0 for several thousand years to come.
		if (BitConverter.IsLittleEndian)
		{
			guidBytes[10] = sequential[5];
			guidBytes[11] = sequential[4];
			guidBytes[12] = sequential[3];
			guidBytes[13] = sequential[2];
			guidBytes[14] = sequential[1];
			guidBytes[15] = sequential[0];
		}
		else
		{
			Buffer.BlockCopy(sequential, 2, guidBytes, 10, 6);
		}

		return new Guid(guidBytes);
	}
}
```

For further information, see [The Cost of GUIDs as Primary Keys][combguids] and [Good Page Splits and Sequential GUID Key Generation][seqguids].

### Asynchronous ASP.NET MVC controllers.

The team converted some of the MVC controllers in the public conference web application to be asynchronous contollers. This avoids blocking some ASP.NET threads and enabled us to use the support for the **Task** class in ASP.NET MVC 4.

For example, the team modified the way that the controller polls for updates in the read models to use timers. 

### Using Prefetch with Windows Azure Service Bus

The team enabled the prefetch option whem the system retrieves messages 
from the Windows Azure Service Bus. This option enables the system to 
retrieve multiple messages in a single round-trip to the server and 
helpes to reduce the latency in retrieving existing messages from the 
Service Bus topics. 

The following code sample from the **SubscriptionReceiver** class ahows 
how to enable this option. 

```Cs
protected SubscriptionReceiver(ServiceBusSettings settings, string topic, string subscription, bool processInParallel, ISubscriptionReceiverInstrumentation instrumentation, RetryStrategy backgroundRetryStrategy)
{
    this.settings = settings;
    this.topic = topic;
    this.subscription = subscription;
    this.processInParallel = processInParallel;

    this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
    this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);

    var messagingFactory = MessagingFactory.Create(this.serviceUri, tokenProvider);
    this.client = messagingFactory.CreateSubscriptionClient(topic, subscription);
    if (this.processInParallel)
    {
        this.client.PrefetchCount = 18;
    }
    else
    {
        this.client.PrefetchCount = 14;
    }

    ...
}
```

### Accepting multiple sessions in parallel

In the V2 release, the **SessionSubscriptionReceiver** creates sessions 
to receive messages from the Windows Azure Service Bus in sequence. 
However if you are using a session, you can only handle messages from 
that session, other messages are ignored until you switch to a different 
session. In the V3 release, the **SessionSubscriptionReceiver** creates 
multiple sessions in parallel, this enables the system to receive 
messages from multiple sessions simultaneously. 

For details, see the **AcceptSession** method in the 
**SessionSubscriptionReceiver** class. 

> **MarkusPersona:** The **AcceptSession** method uses the Transient
> Fault Handling Application Block to reliably accept sessions.

### Adding an optimistic concurrency check

The team also added an optimistic concurrency 
check when the system saves the **RegistrationProcessManager** class by 
adding a timestamp property to the **RegistrationProcessManager** class 
as shown in the following code sample: 

```Cs
[ConcurrencyCheck]
[Timestamp]
public byte[] TimeStamp { get; private set; }
```

For more information, see [Code First Data Annotations][codefirst] on
the MSDN website.

With the optimistic concurrency check in place, we also removed the C# 
lock in the **SessionSubscriptionReceiver** class that was a potential 
bottleneck in the system. 

### Adding a time-to-live value to the MakeSeatReservation command

Windows Azure Service Bus brokered messages can have a value assigned to 
the TimeToLive property: when the time-to-live expires, the message is 
automatically sent to a dead-letter queue. The application uses this 
feature of the Service Bus to avoid processing **MakeSeatReservation** 
commands if the order they are associated with has already expired. 

### Reducing the number of round-trips to the database

We identified a number of locations in the 
**PricedOrderViewModelGenerator** class where we could optimize the 
code. Previously, the system made two calls to the SQL Database instance 
when this class handled an order being placed or expired; now the system 
only makes a single call. 

# Impact on testing 

During this stage of the journey the team re-organized the 
**Conference.Specflow** project in the **Conference.AcceptanceTests** 
Visual Studio solution to better reflect the purpose of the tests. 

## Integration tests

The tests in the **Features\Integration** folder in the 
**Conference.Specflow** project are designed to test the behavior of the 
domain directly, verifying the behavior of the domain by looking at the 
commands and events that are sent and received. These tests are designed 
to be understood by "programmers" rather than "domain experts" and are 
formulated using a more technical vocabulary than the ubiquitous 
language. In addition to verifying the behavior of the domain and 
helping developers to understand the flow of commands and events in the 
system, these tests proved to be useful in testing the behavior of the 
domain in scenarios where events are lost or are received out of order. 

The **Conference** folder contains integration tests for the Conference 
Management bounded context, and the **Registration** folder contains 
tests for the Orders and Registrations bounded context.

> **MarkusPersona:** These integration tests make the assumption that
> the command handlers trust the sender of the commands to send valid
> command messages. This may not be appropriate for other systems that
> you may be designing tests for.

## User interface tests

The **UserInterface** folder contains the acceptance tests. These tests 
are described in more detail in [Chapter 4, Extending and Enhancing the 
Orders and Registrations Bounded Context][j_chapter4]. The 
**Controllers** folder contains the tests that use the MVC controllers 
as the point of entry, and the **Views** folder contains the tests that 
use [WatiN][watin] to drive the system through its UI. 


[fig1]:              images/Journey_07_TopLevel.png?raw=true

[r_chapter4]:        Reference_04_DeepDive.markdown
[j_chapter4]:        Journey_04_ExtendingEnhancing.markdown
[j_chapter5]:        Journey_05_PaymentsBC.markdown
[appendix]:          Appendix1_Running.markdown

[pforeach]:          http://msdn.microsoft.com/en-us/library/dd460720.aspx
[repourl]:           https://github.com/mspnp/cqrs-journey-code
[watin]:             http://watin.org
[codefirst]:         http://msdn.microsoft.com/en-us/library/gg197525(VS.103).aspx
[downloadc]:         http://NEEDFWLINK
[parallelext]:       http://blogs.msdn.com/b/pfxteam/archive/2010/04/06/9990420.aspx
[tags]:              https://github.com/mspnp/cqrs-journey-code/tags
[memento]:           http://www.oodesign.com/memento-pattern.html
[loadtest]:          http://msdn.microsoft.com/en-us/library/dd293540.aspx
[tfhab]:             http://msdn.microsoft.com/en-us/library/hh680934(PandP.50).aspx
[storeforward]:      http://social.technet.microsoft.com/wiki/contents/articles/sql-azure-performance-and-elasticity-guide.aspx#SQL_Azure_Performance_Checklist
[combguids]:         http://www.informit.com/articles/article.aspx?p=25862
[sqlthrottle]:       http://social.technet.microsoft.com/wiki/contents/articles/sql-azure-performance-and-elasticity-guide.aspx#SQL_Azure_Throttling
[wascale]:           http://blogs.msdn.com/b/windowsazurestorage/archive/2010/05/10/windows-azure-storage-abstractions-and-their-scalability-targets.aspx
[sbscale]:           http://aka.ms/SBperf
[aab]:               http://aka.ms/autoscaling
[seqguids]:          http://blogs.msdn.com/b/dbrowne/archive/2012/06/26/good-page-splits-and-sequential-guid-key-generation.aspx