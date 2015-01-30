### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Reference 6: A Saga on Sagas (Chapter Title)

**Process Managers, Coordinating Workflows, and Sagas**

# Clarifying the terminology

The term **Saga** is commonly used in discussions of CQRS to refer to a 
piece of code that coordinates and routes messages between bounded 
contexts and aggregates. However, for the purposes of this guidance we 
prefer to use the term **Process Manager** to 
refer to this type of code artefact. There are two reasons for this: 

1. There is a well-known, pre-existing definition of the term **Saga**
   that has a different meaning from the one generally understood in
   relation to CQRS.
2. The term **Process Manager** is a better description of the
   role performed by this type of code artefact.
   
> **Make this a sidebar**
> Although the term Saga is often used in the context of the CQRS
> pattern, it has a pre-existing definition. We have chosen to use the
> term process manager in this guidance to avoid confusion with this
> pre-existing definition. 
> 
> The term saga, in relation to distributed systems, was originally
> defined in the paper [Sagas](sagapaper) by Hector Garcia-Molina and
> Kenneth Salem. This paper proposes a mechanism that it calls a saga as
> an alternative to using a distributed transaction for managing a
> long-running business process. The paper recognizes that business
> processes are often comprised of multiple steps, each one of which
> involves a transaction, and that overall consistency can be achieved
> by grouping these individual transactions into a distributed
> transaction. However, in long-running business processes, using
> distributed transactions can impact on the performance and concurrency
> of the system because of the locks that must be held for the duration
> of the distributed transaction. 
> 
> The saga concept removes the need for a distributed transaction by
> ensuring that the transaction at each step of the business process has
> a defined compensating transaction. In this way, if the business
> process encounters an error condition and is unable to continue, it
> can execute the compensating transactions for the steps that have
> already completed. This undoes the work completed so far in the
> business process and maintains the consistency of the system. 

Although we have chosen to use the term process manager, Sagas (as 
defined in the [paper][sagapaper] by Hector Garcia-Molina and Kenneth 
Salem) may still have a part to play in a system that implements the 
CQRS pattern in some of its bounded contexts. Typically, you would 
expect to see a process manager routing messages between aggregates 
within a bounded context, and you would expect to see a saga managing a 
long-running business process that spans multiple bounded contexts. 

The following section describes what we mean by the term **Process 
Manager**. This is the working definition we used during our CQRS 
journey project. 

> **Note:** For a time the team developing the Reference Implementation
> used the term **Coordinating Workflow** before settling on the term
> **Process Manager**. This pattern is described in the book "Enterprise
> Integration Patterns" by Gregor Hohpe and Bobby Woolf.

# Process Manager

This section outlines our definition of the term **Process Manager**. 
Before describing the **Process Manager** there is a 
brief recap of how CQRS typically uses messages to communicate between 
aggregates and bounded contexts. 

## Messages and CQRS

When you implement the CQRS pattern, you typically think about two types 
of message to exchange information within your system: commands and 
events. 

Commands are imperatives; they are requests for the system to 
perform a task or action. For example, "book two places on conference X" 
or "allocate speaker Y to room Z." Commands are usually processed just 
once, by a single recipient.

Events are notifications; they inform interested parties that something 
has happened. For example, "the payment was rejected" or "seat type X 
was created." Notice how they use the past tense. Events are published 
and may have multiple subscribers. 

Typically, commands are sent within a bounded context. Events may have 
subscribers in the same bounded context as where they are published, or 
in other bounded contexts. 

The chapter [A CQRS and ES Deep Dive][r_chapter4] in this Reference Guide 
describes the differences between these two message types in detail. 

## What is a Process Manager?

In a complex system that you have modelled using aggregates and bounded 
contexts, there may be some business processes that involve multiple 
aggregates, or multiple aggregates in multiple bounded contexts. In 
these business processes multiple messages of different types are 
exchanged by the participating aggregates. For example, in a conference 
management system, the business process of purchasing seats at a 
conference might involve an order aggregate, a reservation aggregate, 
and a payment aggregate. They must all cooperate to enable a customer to 
complete a purchase. 

Figure 1 shows a simplified view of the messages that these aggregates 
might exchange to complete an order. The numbers identify the message 
sequence. 

> **Note:** This does not illustrate how the Reference Implementation
> processes orders.

![Figure 1][fig1]

**Order processing without using a Process Manager**

In the example shown in Figure 1, each aggregate sends the appropriate 
command to the aggregate that performs the next step in the process. The 
**Order** aggregate first sends a **MakeReservation** command to the 
**Reservation** aggregate to reserve the seats requested by the 
customer. After the seats have been reserved, the **Reservation** 
aggregate raises a **SeatsReserved** event to notify the **Order** 
aggregate, and the **Order** aggregate sends a **MakePayment** command 
to the **Payment** aggregate. If the payment is successful, the 
**Order** aggregate raises an **OrderConfirmed** event to notify the 
**Reservation** aggregate that it can confirm the seat reservation, and 
the customer that the order is now complete. 

![Figure 2][fig2]

**Order processing with a Process Manager**

The example shown in Figure 2 illustrates the same business process as 
that shown in Figure 1, but this time using a Process Manager. 
Now, instead of each aggregate sending messages directly to other 
aggregates, the messages are mediated by the Process Manager. 

This appears to complicate the process: there is an additional object 
(the Process Manager) and a few more messages. However, there are 
benefits to this approach. 

Firstly, the aggregates no longer need to know what is the next step in 
the process. Originally, the **Order** aggregate needed to know that 
after making a reservation it should try to make a payment by sending a 
message to the **Payment** aggregate. Now, it simply needs to report 
that an order has been created. 

Secondly, the definition of the message flow is now located in a single 
place, the Process Manager, rather than being scattered throughout 
the aggregates. 

In a simple business process such as the one shown in Figure 1 and 
Figure 2, these benefits are marginal. However, if you have a business 
process that involves six aggregates and tens of messages, the benefits 
become more apparent. This is espcially true if this is a volatile part 
of the system where there are frequent changes to the business process: 
in this scenario, the changes are likely to be localized to a limited 
numbe of objects. 

In Figure 3, to illustrate this point, we introduce wait-listing to the 
process. If some of the seats requested by the customer cannot be 
reserved, the system adds these seat requests to a wait-list. To make 
this change, we modify the **Reservation** aggregate to raise a 
**SeatsNotReserved** event to report how many seats could not be 
reserved in addition to the **SeatsReserved** event that reports how 
many seats could be reserved. The Process Manager can then send a 
command to the **WaitList** aggregate to wait-list the unfulfilled part 
of the request. 

![Figure 3][fig3]

It's important to note that the Process Manager does not perform 
any business logic. It only routes messages, and in some cases 
translates between message types. For example, when it receives a 
**SeatsNotReserved** event, it sends an **AddToWaitList** command. 

## When should I use a Process Manager?

Process Manager route commands and events between aggregate 
instances, however they don't implement any business logic. There are 
two key reasons to use a Process Manager: 

* When your bounded context uses a large number of events and commands
  that would be difficult to manage as a collection point-to-point
  interactions between aggregates.
* When you want to make it easier to modify message routing in the
  bounded context. A Process Manager gives a single place where
  the routing is defined.

## When should I not use a Process Manager?

The following list identifies reasons not to use a Process Manager:

* You should not use a Process Manager if your bounded context
  contains a small number of aggregate types that use a limited number
  of messages. 
* You should not use a Process Manager to implement any business
  logic in your domain. Business logic belongs in the aggregate types.
 

## Sagas and CQRS

Although we have chosen to use the term process manager as defined 
earlier in this chapter, Sagas (as defined in the paper by Hector 
Garcia-Molina and Kenneth Salem) may still have a part to play in a 
system that implements the CQRS pattern in some of its bounded contexts. 
Typically, you would expect to see a process manager routing 
messages between aggregates within a bounded context, and you would 
expect to see a saga managing a long-running business process that spans 
multiple bounded contexts. 

[r_chapter4]:     Reference_04_DeepDive.markdown
[sagapaper]:      http://www.amundsen.com/downloads/sagas.pdf

[fig1]:           images/Reference_06_Naive.png?raw=true
[fig2]:           images/Reference_06_Workflow.png?raw=true
[fig3]:           images/Reference_06_WorkflowExtended.png?raw=true