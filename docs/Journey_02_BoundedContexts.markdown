### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Chapter 2: Decomposing the Domain 

_Planning the stops._

>"Without stones there is no arch," Marco Polo

In this chapter, we provide a high-level overview of the Contoso 
Conference Management System. The discussion will help you understand 
the structure of the application, the integration points, and how the 
parts of the application relate to each other. 

Here we describe this high-level structure in terms borrowed from the 
domain-driven design (DDD) approach that Eric Evans describes in his 
book, _Domain-Driven Design: Tackling Complexity in the Heart of 
Software_ (Addison-Wesley Professional, 2003). Although there is no 
universal consensus that DDD is a prerequisite for implementing the 
Command Query Responsibility Segregation (CQRS) pattern successfully, 
our team decided to use many of the concepts from the DDD approach, such 
as _domain_, _bounded context_, and _aggregate_, in line with common 
practice within the CQRS community. The chapter, "[CQRS in 
Context][r_chapter1]," in the Reference Guide discusses the relationship 
between the DDD approach and the CQRS pattern in more detail. 

# Definitions used in this chapter 

Throughout this chapter we use a number of terms, which we'll define in 
a moment. For more detail, and possible alternative definitions, see the 
chapter, "[CQRS in Context][r_chapter1]," in the Reference Guide. 

**Domain:** The _domain_ refers to the business domain for the Contoso 
Conference Management System (the reference implementation). Chapter 1, 
"[The Contoso Conference Management System][j_chapter1]," provides an 
overview of this domain. 

**Bounded context:** The term _bounded context_ comes from Eric Evans' 
book. In brief, Evans introduces this concept as a way to decompose a 
large, complex system into more manageable pieces; a large system is 
composed of multiple bounded contexts. Each bounded context is the 
context for its own self-contained domain model, and has its own 
ubiquitous language. You can also view a bounded context as an 
autonomous business component defining clear consistency boundaries: one 
bounded context typically communicates with another bounded context by 
raising events. 

> **GaryPersona:** When you use the CQRS pattern, you often use
> events to communicate between bounded contexts. There are alternative
> approaches to integration, such as sharing data at the database level.

**Context map:** According to Eric Evans, you should "Describe the 
points of contact between the models, outlining explicit translation for 
any communication and highlighting any sharing." This exercise results 
in what is called a _context map_, which serves several purposes that 
include providing an overview of the whole system and helping people to 
understand the details of how different bounded contexts interact with 
each other. 

# Bounded contexts in the conference management system 

**The orders and registrations bounded context:** Within the _orders and 
registrations bounded context_ are the reservations, payment, and 
registration items. When a registrant interacts with the system, the 
system creates an order to manage the reservations, payment, and 
registrations. An order contains one or more order items. 

A _reservation_ is a temporary reservation of one or more seats at a 
conference. When a registrant begins the ordering process to purchase a 
number of seats at a conference, the system creates reservations for 
that number of seats. Those seats are then unavailable for other 
registrants to reserve. The reservations are held for 15 minutes, during 
which time the registrant can complete the ordering process by making a 
payment for the seats. If the registrant does not pay for the tickets 
within 15 minutes, the system deletes the reservation and the seats 
become available for other registrants to reserve. 

> **CarlosPersona:** We discussed making the period of time that the
> system holds reservations a parameter that a Business Customer can
> adjust for each conference. This may be a feature that we add if we
> determine that there is a requirement for this level of control.

**The conference management bounded context:** Within this bounded 
context, a business customer can create new conferences and manage them. 
After a business customer creates a new conference, he can access the 
details of the conference by using his email address and conference 
locator access code. The system generates the access code when the 
business customer creates the conference. 

The business customer can specify the following information about a 
conference: 

* The name, description, and slug (part of the URL used to access the
  conference).
* The start and end dates of the conference.
* The different types and quotas of seats available at the conference.

Additionally, the Business Customer can control the visibility of the 
conference on the public website by either publishing or un-publishing 
the conference. 

The Business Customer can also use the conference management website to 
view a list of orders and Attendees. 

**The payments bounded context:** The _payments bounded context_ is responsible for managing the interactions between the conference management system and external payment systems. It forwards the necessary payment information to the external system and receives an acknowledgement that the payment was either accepted or rejected. It reports the success or failure of the payment back to the conference management system. 

Initially, the Payments bounded context will assume that the Business 
Customer has an account with the third-party payment system (although 
not necessarily a merchant account), or that the Business Customer will 
accept payment by invoice.

## Bounded contexts not included

Although they didn't make it into the final release of the Contoso 
Conference Management System, some work was done on three additional 
bounded contexts. Members of the community are working on these and 
other features, and any out-of-band releases and updates will be 
announced on the [Project "a CQRS Journey"][cqrsjourneysite]" website. 
If you would like to contribute to these bounded contexts or any other 
aspect of the system, visit the [Project "a CQRS 
Journey"][cqrsjourneysite] website or let us know at 
[cqrsjourney@microsoft.com][cqrsemail]. 

* **The Discounts Bounded Context:** This is a bounded context to handle
  the process of managing and applying discounts to the purchase of
  conference seats that would integrate with all three existing bounded
  contexts.
* **The Occasionally Disconnected Conference Management Client:** This
  is a bounded context to handle management of conferences on-site with
  functionality to handle label printing, recording attendee arrivals,
  and additional seat sales.
* **The Submissions and Schedule Management Bounded Context:** This is a
  bounded context to handle paper submissions and conference event
  scheduling written using Node.js.

> **Note:** Wait listing is not implemented in this release, but members
> of the community are working on this and other features. Any
> out-of-band releases and updates will be announced on the [Project "a
> CQRS Journey"][cqrsjourneysite] website.


## The Context Map for the Contoso Conference Management System

Figure 1 and the table that follows it represent a context map that 
shows the relationships between the different bounded contexts that make 
up the complete system, and as such it provides a high-level overview of 
how the system is put together. Even though this context map appears to 
be quite simple, the implementation of these bounded contexts, and more 
importantly the interactions between them, are relatively sophisticated; 
this enabled us to address a wide range of issues relating to the CQRS 
pattern and event sourcing (ES), and provided a rich source from which 
to capture many valuable lessons learned.

> **GaryPersona:** A frequent comment about CQRS projects is that it can
> be difficult to understand how all of the pieces fit together,
> especially if there a great many commands and events in the system.
> Often, you can perform some static analysis on the code to determine
> where events and commands are handled, but it is more difficult to
> automatically determine where they originate. At a high level, a
> context map can help you understand the integration between the
> different bounded contexts and the events involved. Maintaining
> up-to-date documentation about the commands and events can provide
> more detailed insight. Additionally, if you have tests that use
> commands as inputs and then check for events, you can examine the
> tests to understand the expected consequences of particular commands
> (see the section on testing in [Extending and Enhancing the Orders and
> Registrations Bounded Contexts][j_chapter4] for an example of this
> style of test).

Figure 1 shows the three bounded contexts that make up the Contoso 
Conference Management System. The arrows in the diagram indicate the 
flow of data as events between them. 

![Figure 1][fig1]

**Bounded contexts in the Contoso Conference Management System**

The following list provides more information about the arrows in figure 
1. You can find additional detail in the chapters that discuss the 
individual bounded contexts. 

1. Events that report when conferences have been created, updated, or
   published. Events that report when seat types have been created or
   updated.
2. Events that report when orders have been created or updated. Events
   that report when Attendees have been assigned to seats.
3. Requests for a payment to be made.
4. Acknowledgement of the success or failure of the payment.

> **GaryPersona:** Some of the events that the conference management
> bounded context raises are coarse-grained and contain multiple fields.
> Remember that conference management is a create, read, update and
> delete (CRUD)-style bounded context and does not raise fine-grained
> domain-style events. For more information, see Chapter 5, "[Preparing
> for the V1 Release][j_chapter5]."

# Why did we choose these bounded contexts?

During the planning stage of the journey, it became clear that these 
were the natural divisions in the domain that could each contain their 
own, independent domain models. Some of these divisions were easier to 
identify than others. For example, it was clear early on that the 
conference management bounded context is independent of the remainder of 
the domain. It has clearly defined responsibilities that relate to 
defining conferences and seat types and clearly defined points of 
integration with the rest of the application. 

On the other hand, it took some time to realize that the orders and 
registrations bounded context is separate from the 
payments bounded context. For example, it was not until the V2 release 
of the application that all concepts relating to payments disappeared 
from the Orders and Registrations bounded context when the 
**OrderPaymentConfirmed** event became the **OrderConfirmed** event. 

> **GaryPersona:** We continued to refine the domain models right
> through the journey as our understanding of the domain deepened.

More practically, from the perspective of the journey, we wanted a set 
of bounded contexts that would enable us to release a working 
application with some core functionality and that would enable us to 
explore a number of different implementation patterns: CQRS, CQRS/ES, as 
well as integration with a legacy, CRUD-style bounded context.

> **BethPersona:** Contoso wants to release a usable application as
> soon as possible, but be able to add both planned features and
> customer-requested features as they are developed and with no
> down time for the upgrades.

[j_chapter1]:     Journey_01_Introduction.markdown
[j_chapter4]:     Journey_04_ExtendingEnhancing.markdown
[j_chapter5]:     Journey_05_PaymentsBC.markdown
[r_chapter1]:     Reference_01_CQRSInContext.markdown
[cqrsemail]:      mailto:cqrsjourney@microsoft.com
[cqrsjourneysite]: http://cqrsjourney.github.com/
[fig1]:           images/Journey_02_BCs.png?raw=true