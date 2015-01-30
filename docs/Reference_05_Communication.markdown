### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Reference 5: Communicating Between Bounded Contexts (Chapter Title)

# Introduction

Bounded contexts are autonomous components, with their own domain models 
and their own ubiquitous language. They should not have any dependencies 
on each other at run-time and should be capable of running in isolation. 
However they are a part of the same overall system and do need to 
exchange data with one another. If you are implementing the CQRS pattern 
in a bounded context, you should use events for this type of 
communication: your bounded context can respond to events that are 
raised outside of the bounded context, and your bounded context can 
publish events that other bounded contexts may subscribe to. Events 
(one-way, asynchronous messages that publish information about something 
that has already happened), enable you to maintain the loose coupling 
between your bounded contexts. This guidance uses the term *integration 
event* to refer to an event that crosses bounded contexts. 

# Context Maps 

A large system, with dozens of bounded contexts, and hundreds of 
different integration event types, can be difficult to understand. A 
valuable piece of documentation records which bounded contexts publish 
which integration events, and which bounded contexts subscribe to which 
integration events. 

# The anti-corruption layer

Bounded contexts are independent of each other and may be modified or 
updated independently of each other. Such modifications may result in 
changes to the events that a bounded context publishes. These changes 
might include, introducing a new event, dropping the use of an event, 
renaming an event, or changing the definition of event by adding or 
removing information in the payload. A bounded context must be robust in 
the face of changes that might be made to another bounded context. 

A solution to this problem is to introduce an anti-corruption layer to 
your bounded context. The anti-corruption layer is responsible for 
verifying that incoming integration events make sense. For example, by 
verifying that the payload contains the expected types of data for the 
type of event. 

You can also use the anti-corruption layer to translate incoming 
integration events. This translation might include the following 
operations: 

* Mapping to a different event type when the publishing bounded context
  has changed the type of an event to one that the receiving bounded
  context does not recognize.
* Converting to a different version of the event when the publishing
  bounded context uses a different version to the receiving bounded
  context.

# Integration With legacy systems

Bounded contexts that implement the CQRS pattern will already have much 
of the infrastructure necessary to publish and receive integration 
events: a bounded context that contains a legacy system may not. How you 
choose to implement with a bounded context that uses a legacy 
implementation depends largely on on whether you can modify that legacy 
system. It may be that it is a black-box with fixed interfaces, or you 
may have access to the source code and be able to modify it to work with 
events. 

The following sections outline some common approaches to getting data 
from a legacy system to a bounded context that implements the CQRS 
pattern: 

## Reading the database

Many legacy systems use a relational database to store their data. A 
simple way to get data from the legacy system to your bounded context 
that implements the CQRS pattern, is to have your bounded context read 
the data that it needs directly from the database. This approach may be 
useful if the legacy system has no APIs that you can use or if you 
cannot make any changes to the legacy system. However, it does mean that 
your bounded context is tightly coupled to the database schema in the 
legacy system. 

## Generating events from the database

As an alternative, you can implement a mechanism that monitors the 
database in the legacy system, and then publishes integration events 
that describe those changes. This approach decouples the two bounded 
contexts and can still be done without changing the existing legacy code 
because you are creating an additional process to monitor the database. 
However, you now have another program to maintain that is tightly 
coupled to the legacy system. 

## Modifying the legacy systems

If you are able to modify the legacy system, you could modify it to 
publish integration events directly. With this approach, unless you are 
careful, you still have a potential consistency problem. You must ensure 
that the legacy system always saves its data and publishes the event. To 
ensure consistency, you either need to use a distributed transaction or 
introduce another mechanism to ensure that both operations complete 
successfully. 

## Implications for Event Sourcing

If the bounded context that implements the CQRS pattern also uses event 
sourcing, then all of the events published by aggregates in that domain 
are persisted to the event store. If you have modified your legacy 
system to publish events, you should consider whether you should persist 
these integration events as well. For example, you may be using theses 
events to populate a read-model. If you need to be able to re-build the 
read-model, you will need a copy of all these integration events. 

If you determine that you need to persist your integration events from a 
legacy bounded context, you also need to decide where to store those 
events: in the legacy publishing bounded context, or the receiving 
bounded context. Because you use the integration events in the receiving 
bounded context, you should probably store them in the receiving bounded 
context. 

You event store must have a way to store events that are not associated 
with an aggregate. 

> **Note:** As a practical solution, you could also consider allowing
> the legacy bounded context to persist events directly into the event
> store that your CQRS bounded context uses.
