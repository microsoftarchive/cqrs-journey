### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Reference 3: Introducing Event Sourcing

Event sourcing (ES) and Command Query Responsibility Segregation (CQRS) are 
frequently mentioned together. Although neither one necessarily implies 
the other, you will see that they do complement each other. This chapter 
introduces the key concepts that underlie event sourcing, with some 
pointers to the potential relationship with the CQRS pattern. This 
chapter is an introduction; the chapter "[A CQRS/ES Deep 
Dive][r_chapter4]" explores event sourcing and its relationship with 
CQRS in more depth. 

Before going any further, we should have a basic definition of events 
that captures their essential characteristics: 


- Events happen in the past. For example, "the speaker was booked,"
  "the seat was reserved," "the cash was dispensed." Notice how we
  describe these events using the past tense. 
- Events are immutable. Because events happen in the past, they cannot
  be changed or undone. Subsequent events may alter or negate the
  effects of earlier events. For example, "the reservation was
  cancelled." 
- Events are one-way messages. Events have a single source (publisher) 
  that publishes the event. One or more recipients (subscribers) may receive
  events. 
- Typically, events include parameters that provide additional
  information about the event. For example, "Seat E23 was booked by
  Alice." 
- In the context of event sourcing, events should describe business
  intent. For example, "Seat E23 was booked by Alice" describes in
  business terms what has happened and is more descriptive in business
  terms than, "In the bookings table, the row with key E23 had the name
  field updated with the value Alice." 

We will also assume that the events being discussed in this chapter are 
associated with aggregates; see the chapter "[CQRS in 
Context][r_chapter1]" for a descriptions of the DDD terms: aggregates, 
aggregate roots, and entities. The features of aggregates that are 
relevant to events and event sourcing are: 

- Aggregates define consistency boundaries for groups of related
  entities; therefore, you can use an event raised by an aggregate to
  notify interested parties that a transaction (consistent set of
  updates) has taken place on that group of entities. 
- Every aggregate has a unique ID; therefore, you can use that ID to
  record which aggregate in the system was the source of a particular
  event. 

For the remainder of this chapter, we will use the term aggregate to 
refer to a cluster of associated objects that are treated as a unit for 
the purposes of data changes. This does not mean that event sourcing is 
directly related to the DDD approach; we are simply using the 
terminology from DDD to try to maintain some consistency in our 
terminology in this guide. 

# What is Event Sourcing? 

Event sourcing is a way of persisting your application's state by storing 
the history that determines the current state of your application. For 
example, a conference management system needs to track the number of 
completed bookings for a conference so it can check whether there are 
still seats available when someone tries to make a new booking. The 
system could store the total number of bookings for a conference in two 
ways: 

- It could store the total number of bookings for a particular
  conference and adjust this number whenever someone makes or cancels a
  booking. You can think of the number of bookings as being an integer
  value stored in a specific column of a table that has a row for each
  conference in the system. 
- It could store all the booking and cancellation events for each
  conference and then calculate the current number of bookings by
  replaying the events associated with the conference for which you
  wanted to check the current total number of bookings. 
  
## Comparing using an ORM layer and Event Sourcing

![Figure 1][fig1]

**Using an object-relational mapping layer**

Figure 1 illustrates the first approach to storing the total number of 
reservations. The following list of steps corresponds to the numbers in 
the diagram: 

1. A process manager or a UI issues a command to reserve seats 
   for two attendees on the conference with an ID of 157. The command is 
   handled by the command handler for the **SeatsAvailability** 
   aggregate type. 
2. If necessary, the object-relational mapping (ORM) layer populates an aggregate instance with 
   data. The ORM layer retrieves the data by issuing a query against the table 
   (or tables) in the data store. This includes the existing number of 
   reservations for the conference. 
3. The command handler invokes the business method on the aggregate
   instance to make the reservations. 
4. The **SeatsAvailability** aggregate performs its domain logic. In 
   this example, this includes calculating the new number of
   reservations for the conference. 
5. The ORM persists the information in the aggregate instance to 
   the data store. The ORM layer constructs the necessary update (or 
   updates) that must be executed. 
   
**Note:** For a definition of _process manager_, see Chapter 6, "[A Saga on Sagas][r_chapter6]."

This diagram provides a deliberately simplified view of the process. In 
practice, the mapping performed by the ORM layer will be significantly more 
complex. You will also need to consider exactly when the load and save 
operations must happen to balance the demands of consistency, 
reliability, scalability, and performance. 


![Figure 2][fig2]

**Using event sourcing**

Figure 2 illustrates the second approach, using event sourcing in place 
of an ORM layer and an RDMS. 


> **Note:** You might decide to implement the event store using an 
RDBMS. The relational schema will be much simpler than the schema used by the ORM layer in the first 
approach. You can also use a custom event store. 

**JanaPersona:** CQRS/ES makes it easy to change your technologies. For example, you could start with a file-based event store for proto-typing and development, and later switch to a Windows Azure table-based store for production.

The following list of steps corresponds to the numbers in the diagram. 
Note that steps one, two, and four are the same. 


1. A process manager or a UI issues a command to reserve seats  
   for two attendees on a conference with an ID of 157. The command is 
   handled by the command handler for the **SeatsAvailability** 
   aggregate type. 
2. An aggregate instance is populated by querying for all of the
   events that belong to **SeatsAvailability** aggregate 157. 
3. The command handler invokes the business method on the aggregate
   instance to make the reservations. 
4. The **SeatsAvailability** aggregate performs its domain logic. In 
   this example, this includes calculating the new number of
   reservations for the conference. The aggregate creates an event to
   record the effects of the command.
5. The system appends the event that records making two new reservations
   to the list of events associated with the aggregate in the event
   store. 

This second approach is simpler because it dispenses with the ORM layer, 
and replaces a complex relational schema in the data store with a much 
simpler one. The data store only needs to support querying for events by 
aggregate ID and appending new events. You will still need to consider 
performance and scalability optimizations for reading from and writing 
to the store, but the impact of these optimizations on reliability and 
consistency should be much easier to understand. 

> **Note:** Some optimizations to consider are using snapshots so you 
  don't need to query and replay the full list of events to obtain the current
  state of the aggregate, and maintaining cached copies of aggregates in
  memory. 

You must also ensure that you have a mechanism that enables an aggregate 
to rebuild its state by querying for its list of historical events. 

What you have also gained with the second approach is a complete 
history, or audit trail, of the bookings and cancellations for a 
conference. Therefore, the event stream becomes your only source of 
truth. There's no need to persist aggregates in any other form or shape 
since you can easily replay the events and restore the state of the 
system to any point in time. 

In some domains, such as accounting, event sourcing is the natural, 
well-established approach: accounting systems store individual 
transactions from which it always possible to reconstruct the current 
state of the system. Event sourcing can bring similar benefits to 
other domains. 

> **GaryPersona:** For additional insights into using events as a storage mechanism see [Events as a Storage Mechanism][eventstorage] by Greg Young.

# Why should I use Event Sourcing? 

So far, the only justification we have offered for event sourcing is the 
fact that it stores a complete history of the events associated with the 
aggregates in your domain. This is a vital feature in some domains such 
as accounting where you need a complete audit trail of the financial 
transactions, and where events must be immutable. Once a transaction has 
happened, you cannot delete or change it, although you can create a new 
corrective or reversing transaction if necessary.

> "The primary benefit of using event sourcing is a built-in audit
> mechanism that ensures consistency of transactional data and audit
> data because these are the same data. Representation via events allows
> you to reconstruct the state of any object at any moment in time."
> Pawel Wilkosz (Customer Advisory Council)

The following list describes some of the additional benefits that you 
can derive from using event sourcing. The significance of the individual 
benefits will vary depending on the domain you are working in. 

- **Performance.** Because events are immutable, you can use an
  append-only when you save them. Events are also simple, stand-alone
  objects. Both these factors can lead to better performance and
  scalability for the system than approaches that use complex relational
  storage models. 
- **Simplification.** Events are simple objects that describe what has
  happened in the system. By simply saving events, you are avoiding the
  complications associated with saving complex domain objects to a
  relational store: the object-relational impedance mismatch.
  
> "Another problem with the having of two models is that it is necessarily more work. One must create the code to save the current state of the objects and one must write the code to generate and publish the events. No matter how go about doing these things it cannot possibly be easier than only publishing events, even if you had something that made storing current state completely trivial to say a document storage, there is still the effort of bringing that into the project."  
> Greg Young [Why use Event Sourcing?][whyevent]

- **Audit Trail.** Events are immutable and store the full history of
  the state of the system. As such, they can provide a detailed audit
  trail of what has taken place within the system. 
- **Integration with other sub-systems.** Events provide a useful way of
  communicating with other sub-systems. Your event store can publish
  events to notify other interested sub-systems of changes to the
  application's state. Again, the event store provides a complete
  record of all the events that it published to other systems. 
- **Deriving additional business value from event history.** By storing
  events, you have the ability to determine what the state of the system
  was at any previous point in time by querying the events associated
  with a domain object up to that point in time. This enables you to
  answer historical questions from the business about the system. In
  addition, you cannot predict what questions the business might want to
  ask about the information stored in a system. If you store your
  events, you are not discarding information that may prove to be
  valuable in the future.  
- **Production troubleshooting.** You can use the event store to
  troubleshoot problems in a production system by taking a copy of the
  production event store and replaying it in a test environment. If you
  know the time that an issue occurred in the production system then you
  can easily replay the event stream up to that point to observe exactly
  what was happening.  
- **Fixing errors.** You might discover a coding error that results in
  the system calculating an incorrect value. Rather than fixing the
  coding error and performing a risky manual adjustment on a stored item
  of data, you can fix the coding error and replay the event stream so
  that the system calculates the value correctly based on the new
  version of the code. 
- **Testing.** All of the state changes in your aggregates are recorded
  as events. Therefore, you can test that a command had the expected
  effect on an aggregate by simply checking for the event.
  
> "Event sourcing can also help with complex testing scenarios where you
> need to verify that a given action triggered a specific result. This
> is especially relevant for negative results, where you need to verify
> that an action did _not_ trigger a result; this is frequently not
> verified when writing tests, but can easily be instrumented when the
> changes are being recorded through events."  
> Alberto Poblacion  (Customer Advisory Council)

- **Flexibility.** A sequence of events can be projected to any desired
  structural representation.
  
> "As long as you have a steam of events, you can project it to any form,
> even a conventional SQL database. For instance, my favorite approach
> is to project event streams into JSON documents stored in a cloud
> storage."  
> Rinat Abdullin [Why Event Sourcing?][rinatwhy]





The chapter "[A CQRS and ES Deep Dive][r_chapter4]" discusses these benefits 
in more detail. There are also many illustrations of these benefits in 
these reference implementation described in the guide "A CQRS Journey." 

> From experience, ORMs lead you down the path of a structural model
> while ES leads you down the path of a behavioral model. Sometimes one 
> just makes more sense than the other. For example, in my own domain 
> (not model) I get to integrate with other parties that send a lot of 
> really non-interesting information that I need to send out again later 
> on when something interesting happens on my end. It's inherently 
> structural. Putting those things into events would be a waste of time, 
> effort, space. Contrast this with another part of the domain that 
> benefits a lot from knowing what happened, why it happened, when it 
> did or didn't happen, where time and historical data are important to 
> make the next business decision. Putting that into a structural model 
> is asking for a world of pain. It depends, get over it, choose wisely, 
> and above all: make your own mistakes. 
> Yves Reynhout (CQRS Advisors Mail List)

# Event Sourcing concerns 

The previous section described some of the benefits that you might 
obtain if you decide to use event sourcing in your system. However, 
there may be some concerns that you will need to address if you decide 
to use event sourcing in your system: 

- **Performance.** Although event sourcing typically improves the
  performance of updates, you may need to consider the time it takes to
  load domain object state by querying the event store for all of the
  events that relate to the state of an aggregate. Using snapshots may
  enable you limit the amount of data that you need to load: you can go
  back to the latest snapshot and replay the events from that point
  forward. See the chapter "A CQRS and ES Deep Dive" for more information
  about snapshots. 
- **Versioning.** You may find it necessary to change the definition of
  a particular event type or aggregate at some point in the future. You
  must consider how your system will be able to handle multiple versions
  of an event type and aggregates. 
- **Querying.** Although it is easy to load the current state of an
  object by replaying its event stream (or its state at some point in
  the past), it is difficult or expensive to run a query such as: find
  all my orders where the total value is greater than $250. However, if you are implementing the CQRS pattern, you
  should remember that such queries will typically be executed on the
  read-side where you can ensure that you can build data projections that
  are specifically designed to answer such questions.
  
# CQRS/ES

The CQRS pattern and event sourcing are frequently combined; each adding benefit to the other.

The chapter "[Introducing Command Query Responsibility Segregation][r_chapter2]" 
suggested that events can form the basis of the push synchronization of 
the application's state from the data store on the write-side to the data 
store on the read-side. Remember that typically the read-side data-store 
contains denormalized data that is optimized for the queries that are 
run against your data, for example to display information in your 
application's UI. 

> ES is a great pattern to use to implement the link between the thing 
> that writes and the thing that reads. It's by no means the only possible 
> way to create that link, but it's a reasonable one and there's plenty of 
> prior art with various forms of logs and log shipping. The major tipping 
> point for whether the link is "ES" seem to be whether the log is 
> ephemeral or a permanent source of truth. The CQRS pattern itself merely 
> mandates a split between the write and the read thing, so ES is strictly 
> complementary.  
> Clemens Vasters  (CQRS Advisors Mail List)

> Event sourcing is about the state of the domain model being persisted as 
> a stream of events rather than as a single snapshot, not about how the 
> command and query sides are kept in sync (usually with a 
> publish/subscribe message-based approach).  
> Udi Dahan  (CQRS Advisors Mail List)

You can use the events that you persist in your event store to propagate 
all the updates made on the write-side to the read-side. The read-side 
can use the information contained in the events to maintain whatever 
de-normalized data you require on the read-side to support your queries. 

![Figure 3][fig3] 

**CQRS and Event Sourcing**

Notice how the write-side publishes events after it persists them to the 
event store. This avoids the need to use a two-phase commit, which would 
be the case if the aggregate were responsible for saving the event to 
the even store and publishing the event to the read-side. 

Normally, these events will enable you to keep the data on the read-side 
up to date in close to real-time; there will be some delay due to the 
transport mechanism, and the chapter "[A CQRS and ES Deep Dive][r_chapter4]" 
discusses the possible consequences of this delay. 

You can also rebuild the data on the read-side from scratch at any time 
by replaying the events from your event-store on the write-side. You 
might need to do this if the read-side data store got out of 
synchronization for some reason, or because you needed to modify the 
structure of the read-side data store to support a new query. 

You need to be careful replaying the events from the event store to 
rebuild the read-side data store if other bounded contexts also 
subscribe to the same events. It might be easy to empty the read-side 
data store before replaying the events, it might not be so easy to 
ensure the consistency of another bounded context if it sees a duplicate 
stream of events. 

Remember that the CQRS pattern does not mandate you to use different 
stores on the read-side and the write-side. You could decide to use a 
single relational store with a schema in third normal form and a set of 
denormalized views over that schema. However, replaying events is a very 
convenient mechanism for re-synchronizing the read-side data store with 
the write-side data store. 

# Standalone Event Sourcing 

You can use event sourcing without also applying the CQRS pattern. The 
ability to rebuild the application state, to mine the event history for 
new business data, and to simplify the data storage part of the 
application are all valuable in some scenarios. However, this guide 
focuses on using event sourcing in the context of the CQRS pattern. 

# Event stores 

If you are using event sourcing, you will need a mechanism to store your 
events and to return the stream of events associated with an aggregate 
instance so that you can replay the events to re-create the state of the 
aggregate. This storage mechanism is typically referred to as an Event 
Store. 

You may choose to implement your own event store, or use a third-party 
event store such as Jonathan Oliver's [EventStore][jolivereventstore]. 
Although you can implement a small-scale event store relatively easily, 
a production quality, scalable event store is more of a challenge. 

Chapter 8, "[Epilogue: Lessons Learned][j_chapter40]," summarizes the 
experiences that our team had implementing our own event store. 

## Basic requirements

Typically, when you implement the CQRS pattern, aggregates raise events 
to publish information to other interested parties, such as other 
aggregates, process managers, read-models, or other bounded 
contexts. When you use event sourcing, you persist these same events to 
an event store. This enables you to use those events to load the state 
of an aggregate by replaying the sequence of events associated with that 
aggregate. 

Therefore, whenever an aggregate instance raises an event, two things 
must happen. The system must persist the event to the event store, and 
the system must publish the event. 

> **Note:** In practice not all events in a system necessarily have
> subscribers. You may raise some events solely as a way to persist some
> properties of an aggregate.

Whenever the system needs to load the current state of an aggregate, it 
must query the event store for the list of past events associated with 
that aggregate instance. 

## Underlying storage

Events are not complex data structures; typically they have some 
standard metadata that includes the Id of the aggregate instance they 
are associated with and a version number, and a payload with the details 
of the event itself. You do not need to use a relational database to 
store your events: you could use a NoSQL store, a document database, or 
a file system. 

## Performance, scalability, and consistency

Stored events should be immutable and are always read in the order that 
they were saved; so saving an event should be a simple, fast append 
operation on the underlying store. 

When you load the persisted events, you will load the events in the 
order that were originally saved. If you are using a relational 
database, the records should be keyed using the aggregate Id and a field 
that defines the ordering the events. 

If an aggregate instance has a large number of events, this may affect 
the time that it takes to replay all of the events to reload the state 
of the aggregate. One option to consider in this scenario is to use a 
snapshot mechanism: in addition to the full stream of events in the 
event store, you can store a snapshot of the state of the aggregate at 
some recent point in time. To reload the state of the aggregate, you 
first load the most recent snapshot, and then replay all of the 
subsequent events. You could generate the snapshot during the write 
process, for example by creating a snapshot every 100 events. 

> **Note:** Determining how frequently you should take snapshots depends
> on the performance characteristics of your underlying storage. You
> will need to measure how long it takes to replay different lengths of
> event stream to determine the optimum time to create your snapshots.

As an alternative, you could cache heavily used aggregate instances in 
memory to avoid needing to repeatedly replay the event stream. 

When an event store persists an event, it must also publish that event. 
To preserve the consistency of the system, both operations must succeed 
or fail together. The traditional approach to this type of scenario is 
to use a distributed, two-phase commit transaction that wraps the append 
operation to the data-store and the publish operation to the messaging 
infrastructure together. In practice, you may find that support for 
two-phase commit transactions is limited in many data stores and 
messaging platforms. Using two-phase commit transactions may also limit 
the performance and scalability of the system.

> **Note:** For a discussion of two-phase commit transactions and the
> impact on scalability, see the article [Your Coffee Shop Doesn’t Use
> Two-Phase Commit][coffee] by Gregor Hohpe.

One of the key problems you must solve if you choose to implement your 
own event store is how to achieve this consistency. For example, an 
event store built on top of Windows Azure table storage could take the 
following approach to maintain consistency between persisting and 
publishing events: use a transaction to write copies of the event to two 
entities in the same partition in the same table; one entity stores an 
immutable event that constitutes part of the event stream of the 
aggregate; the other entity stores an event that is part of a list of 
events pending publication. You can then have a process that reads the 
list of events pending publication, that guarantees to publish those 
events at least once, and that after publication removes the event from 
the pending list. 

An additional set of problems related to consistency occur if you plan 
to scale out your event store across multiple storage nodes, or use 
multiple writers to write to the store. In this scenario, you must take 
steps to ensure the consistency of your data. The data on the write-side 
should be fully consistent, not eventually consistent. For more 
information about the CAP theorem and maintaining consistency in 
distributed systems, see the next chapter "[A CQRS/ES Deep 
Dive][r_chapter4]". 

[jolivereventstore]: https://github.com/joliver/EventStore
[coffee]:            http://eaipatterns.com/docs/IEEE_Software_Design_2PC.pdf
[eventstorage]:      http://cqrs.wordpress.com/documents/events-as-storage-mechanism/
[whyevent]:          http://codebetter.com/gregyoung/2010/02/20/why-use-event-sourcing/
[rinatwhy]:          http://bliki.abdullin.com/event-sourcing/why

[r_chapter1]:     Reference_01_CQRSContext.markdown
[r_chapter2]:     Reference_02_CQRSIntroduction.markdown
[r_chapter4]:     Reference_04_DeepDive.markdown
[r_chapter6]:     Reference_06_Sagas.markdown
[j_chapter40]:    Journey_40_Conclusions.markdown

[fig1]:           images/Reference_03_ORM.png?raw=true
[fig2]:           images/Reference_03_ES.png?raw=true
[fig3]:           images/Reference_03_CQRS.png?raw=true