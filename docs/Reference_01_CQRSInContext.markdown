### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Reference 1: CQRS in Context

This chapter is intended to provide some context for the main subject of 
this guide: a discussion of the Command Query Responsibility Segregation 
(CQRS) pattern. It is useful to understand some of the origins of the 
CQRS pattern and some of the terminology you will encounter both in this 
guide and in other material that discusses the CQRS pattern. It is 
particularly important to understand that the CQRS pattern is not 
intended for use as the top-level architecture of your system; rather it 
should be applied to those sub-systems that will gain specific benefits 
from the application of the pattern. 

Before we look at the issues surrounding the use of different 
architectures within a complex application, we need to introduce some of 
the terminology that we will use in this, and subsequent chapters of 
this reference guide. Much of this terminology comes from an approach to 
developing software systems known as domain-driven design (DDD). There 
are a few important points to note about our use of this terminology: 

- There are other approaches that tackle the same problems that DDD 
  tackles, with similar concepts, but with their own specific
  terminologies.  

- We are using the DDD terminology because many CQRS practitioners also 
  use this terminology, and it is used in much of the existing CQRS 
  literature. 

- Using a DDD approach can lead naturally 
  to an adoption of the CQRS pattern. However the DDD
  approach does not always lead to the use of the CQRS pattern, nor is the DDD 
  approach a prerequisite for using the CQRS pattern. 

- You may question our interpretation of some of the concepts of DDD. 
  The intention of this guide is to take what is useful from DDD to help
  us explain the CQRS pattern and related concepts, not to provide
  guidance on how to use the DDD approach. 

To learn more about the foundational principles of DDD, you should read 
the book "Domain-Driven Design: Tackling Complexity in the Heart of 
Software" by Eric Evans (2003) and to see how these principles apply to 
a concrete development project on the .NET platform, along with insights 
and experimentation, you should read the book "Applying Domain Driven 
Design and Patterns" by Jimmy Nillson (2006). 

In addition, to see how Evans's understanding of what works and what 
doesn't in DDD, and for his view on how it all changed over the previous 
five years, we recommend his talk at [QCon London 2009][evansqcon]. 

For a summary of the key points in Eric Evans' book, you should read the
free book, "[Domain-Driven Design Quickly][dddquickly]" by Abel Avram
and Floyd Marinescu. 

# What is Domain-Driven Design? 

As previously stated, DDD is an approach to developing software systems, 
and in particular systems that are complex, that have ever-changing 
business rules, and that you expect to last for the long-term within the 
enterprise. 

The core of the DDD approach uses a set of techniques to analyze your 
domain and to construct a conceptual model that captures the results of 
that analysis: you can then use that model as the basis of your 
solution. The analysis and model in the DDD approach are especially well 
suited to designing solutions for large and complex domains. DDD also 
extends its influence to other aspects of the software development 
process as a part of the attempt to help you manage complexity: 

> "Every effective DDD person is a Hands-on Modeller, including me."  
> Eric Evans

- In focusing on the domain, DDD focuses on the area where the business
  and the development team must be able to communicate with each other
  clearly, but where in practice they often misunderstand each other. 
  The domain models that DDD uses should capture detailed, rich business
  knowledge, but should also be very close to the code that is actually
  written. 
- Domain models are also useful in the longer term if they are
  kept up to date. By capturing valuable domain knowledge, they facilitate
  maintaining and enhancing the system in the future. 
- DDD offers guidance on how large problem domains can be effectively
  divided up, enabling multiple teams to work in parallel, and enabling
  you to target appropriate resources to those parts of the system with
  the greatest business value. 
  
> "Focus relentlessly on the core domain! Find the differentiator in
> your software - something significant!"  
> Eric Evans

The DDD approach is appropriate for large, complex systems that are 
expected to have a long life span. You are unlikely to see a return on 
your investment in DDD if you use it on small, simple, or short-term 
projects. 

# Domain-Driven Design: Concepts and terminology 

This guide is not intended to provide guidance on 
using the DDD approach. However, it is useful to understand some of 
the concepts and terminology from DDD because they are useful when we 
describe some aspects of CQRS pattern implementations. These are not 
official or rigorous definitions; they are intended to be useful, 
working definitions for the purposes of this guide. 

## Domain model 

At the heart of DDD lies the concept of the _domain model_. This model is 
built by the team responsible for developing the system in question, and 
that team consists of both domain experts from the business and software 
developers. The domain model serves several functions: 

- It captures all of the relevant domain knowledge from the domain
  experts. 
- It enables the team to determine the scope and verify the consistency
  of that knowledge. 
- The model is expressed in code by the developers. 
- It is constantly maintained to reflect evolutionary changes in the
  domain. 


DDD focuses on the domain because it contains business value. An 
enterprise derives its competitive advantage and generates business 
value from its core domains. The role of the domain model is to capture 
what is valuable or unique to the business. 

Much of the DDD approach focuses on how to create, maintain, and use 
these domain models. Domain models are typically composed of elements 
such as _Entities_, _Value Objects_, _Aggregates_, and described using terms 
from a _Ubiquitous Language_. 

## Ubiquitous language 

The concept of a ubiquitous language is very closely related to that of 
the domain model. One of the functions of the domain model is to foster 
a common understanding of the domain between the domain experts and the 
developers. If both the domain experts and the developers use the same 
terms for things and actions within the domain (for example, conference, 
chair, attendee, reserve, waitlist), there is less possibility for 
confusion or misunderstanding. More specifically, if everyone uses the 
same language there are less likely to be misunderstandings that result 
from translations between languages. For example, if a developer has to 
think, "if the domain expert talks about a delegate, he is really 
talking about an attendee in the software," then eventually something 
will go wrong as a result of a misunderstanding or mistranslation. 

> **JanaPersona:** In our journey, we used SpecFlow to express business
> rules as acceptance tests. They helped us to communicate information
> about our domain with clarity and brevity, and formulate a ubiquitous
> language in the process. For more information, see Chapter 4,
> "[Extending and Enhancing the Orders and Registrations Bounded
> Context][j_chapter4]," in the journey.

## Entities, Value Objects, and Services 

DDD uses these terms to identify some of the internal artifacts (or "building blocks" as Evans calls them) that 
will make up the domain model. 

### Entities 

Entities are objects that are defined by their identity, and that 
identity continues through time. For example, a conference in a 
conference management system will be an entity; many of its attributes 
could change over time (such as its status, size, and even name), but 
within the system each particular conference will have its own unique 
identity. The object that represents the conference may not always exist 
within the system's memory; at times it may be persisted to a database, 
only to be re-instantiated at some point in the future. 

### Value Objects 

Not all objects are defined by their identity: for some objects what is 
important are the values of their attributes. For example, within our 
conference management system we do not need to assign an identity to 
every attendee's address (for a start all of the attendees from a 
particular organization may share the same address). All we are 
concerned with are the values of the attributes of an address: street, 
city, state, etc. Value objects are usually immutable.

> **JanaPersona:** The following video is a good refresher on using
> value objects properly, especially if you are confusing them with
> DTOs: [Power Use of Value Objects in DDD][valueobjects].

### Services 

You cannot always model everything as an object. For example, in the 
conference management system it may make sense to model a third-party 
payment processing system as a service: the system can pass the 
parameters required by the payment processing service and then receive a 
result back from the service. Notice that a common characteristic of a 
service is that it is stateless (unlike entities and value objects). 

> **Note:** Services are usually implemented as regular class libraries
  that expose a collection of stateless methods. A service in a DDD
  domain model is not a Web service; these are two different concepts. 

##Aggregates and Aggregate Roots 

Whereas entities, value objects, and services are terms for the elements 
that DDD uses to describe things in the domain model, the terms 
aggregate and aggregate root relate specifically to the life cycle and 
grouping of those elements.

> An aggregate is like grapes - in the sense that you have something you
> think of as a conceptual whole, which is also made up of smaller
> parts. You have rules that apply to the whole thing. So every one of
> those grapes is part of a grape bunch. Aggregates are super important.
> It is one of those things that helps you to enforce the real rules.  
> Eric Evans, [What I've learned about DDD since the book][evans_2009_1]

When you design a system that allows multiple users to work on shared 
data, you will have to evaluate the trade-off between consistency and 
usability. At one extreme, when a user needs to make a change to some 
data, you could lock the entire database to ensure that the change is 
consistent. However, the system would be unusable for all other users 
for the duration of the update. At the other extreme, you could decide 
not enforce any locks at all, allowing any user to edit any piece of 
data at any time, but you would soon end up with inconsistent or corrupt 
data within the system. Choosing the correct granularity for locking to 
balance the demands of consistency and usability requires detailed 
knowledge of the domain: 

- You need to know which set of entities and value objects each
  transaction can potentially affect. For example, are there
  transactions in the system that can update attendee, conference, and
  room objects? 
- You need to know how far the relationships from one object extend
  through other entities and value objects within the domain, and where
  you must define the consistency boundaries. For example, if you delete
  a room object, should you also delete a projector object, or a set of
  attendee objects? 

DDD uses the term aggregate to define a cluster of related entities and 
value objects that form a consistency boundary within the system. That 
consistency boundary is usually based on transactional consistency. 

An aggregate root (also known as a Root Entity) is the gatekeeper object 
for the aggregate: all access to the objects within the aggregate must 
be done through the aggregate root, external entities are only permitted 
to hold a reference to the aggregate root, and all invariants should be 
checked by the aggregate root. 

In summary, aggregates are the mechanism that DDD uses to manage the 
complex set of relationships that typically exist between the many 
entities and value objects that exist within a typical domain model. 

# Bounded Contexts 

So far, the DDD concepts and terminology that we have briefly introduced 
are related to creating, maintaining, and using a domain model. For a 
large system, it may not be practical to maintain a single domain model; 
it is too large and complex to make it feasible to keep it coherent and 
consistent. To manage this scenario, DDD introduces the concepts of 
bounded contexts and multiple models. Within a system, you might choose 
to use multiple smaller models rather than a single large model, each 
one focusing on some aspect or grouping of functionality within the 
overall system. A bounded context is the context for one, particular 
domain model. Similarly, each bounded context (if implemented following 
the DDD approach) has its own ubiquitous language, or at least its own 
dialect of the domain's ubiquitous language. 

![Figure 1][fig1]

**Bounded contexts within a large, complex system**

Figure 1 shows an example, drawn from a conference management system, of 
a system that is divided into multiple bounded contexts. In practice, 
there are likely to be more bounded contexts than the three shown in the 
diagram. 

There are no hard and fast rules that specify how big a bounded context 
should be. Ultimately it's a pragmatic issue that is determined by your 
requirements and the constraints on your project: 

> Favoring Larger Bounded Contexts 
>
> - Flow between user tasks is smoother when more is handled with a
>   unified model. 
> - It is easier to understand one coherent model than two distinct
>   ones plus mappings. 
> - Translation between two models can be difficult (sometimes
>   impossible). 
> - Shared language fosters clear team communication. 
>
> Favoring Smaller Bounded Contexts
>
> - Communication overhead between developers is reduced. 
> - Continuous Integration is easier with smaller teams and code bases. 
> - Larger contexts may call for more versatile abstract models,
>   requiring skills that are in short supply. 
> - Different models can cater to special needs or encompass the jargon
>   of specialized groups of users, along with specialized dialects of
>   the Ubiquitous Language. 
>
>   Taken from Eric Evans, "Domain-Driven Design," p383. 

You decide which patterns and approaches to apply (for example, whether 
to use the CQRS pattern or not) within a bounded context, not for the 
entire system. 

> **JanaPersona:** BC is often used as an acronym for bounded contexts
> (in DDD) and business components (in SOA). Do not confuse them. In our
> guidance, BC means "bounded context."

> A given Bounded Context should be divided into Business Components,
> where these Business Components have full UI through DB code, and are
> ultimately put together in composite UI’s and other physical pipelines
> to fulfill the system’s functionality. A Business Component can exist
> in only one Bounded Context.  
> Udi Dahan, [Udi & Greg Reach CQRS Agreement][udigreg]
> 
> For me a bounded context is an abstract concept (and it's still an
> important one!) but it comes to technical details, the business
> component is far more important than the bounded context.  
> Greg Young, conversation with the patterns &amp; practices team

## Anti-corruption layers

Different bounded contexts have different domain models. When your 
bounded contexts communicate with each other, you need to ensure that 
concepts specific to one domain model do not leak into another domain 
model. An _anti-corruption layer_ functions as a gatekeeper between 
bounded contexts and helps you to keep your domain models clean. 

## Context Maps

A large complex system can have multiple bounded contexts that interact 
with one another in various ways. A **Context Map** is the documentation 
that describes the relationships between these bounded contexts. It 
might might be in the form of diagrams or tables or text.

> "I think context mapping is perhaps one thing in there that should be
> done on every project. The context map helps you keep track of all the
> models you are using."  
> Eric Evans

> "Sometimes the process of gathering information to draw the context map
> is more important than the map itself."  
> Alberto Brandolini

A **Context Map** helps to visualize the system at a high level, showing 
how some of the key parts relate to each other. It also helps to clarify 
the boundaries between the bounded contexts: it shows where and how the 
bounded contexts exchange data and share data, and also where you must 
translate data as it moves from one domain model to another.

A business entity such as a customer might exist in several 
bounded contexts. However, it may need to expose different facets or 
properties that are relevant to a particular bounded context. As a 
customer entity moves from one bounded context to another you may need 
to translate it so that it exposes the relevant facets or properties for 
its current context. 

## Bounded contexts and multiple architectures 

A bounded context typically represents a slice of the overall system 
with clearly defined boundaries separating it from other bounded 
contexts within the system. If a bounded context is implemented by 
following the DDD approach, the bounded context will have its own domain 
model and its own ubiquitous language. Bounded contexts are also 
typically vertical slices through the system, so the implementation of a 
bounded context will include everything from the data store, right up to 
the UI. 

The same domain concept can exist in multiple bounded contexts: for 
example, the concept of an attendee in a conference management system 
might exist in the bounded context that deals with bookings, in the 
bounded context that deals with badge printing, and in the bounded 
context that deals with hotel reservations. From the perspective of the 
domain expert, these different versions of the attendee may require 
different behaviors and attributes. For example, in the bookings bounded 
context the attendee is associated with a registrant who makes the 
bookings and payments. Information about the registrant is not relevant 
in the hotel reservations bounded context where information such as 
dietary requirements or smoking preferences is important. 

One important consequence of this split is that you can use different 
implementation architectures in different bounded contexts. For example, 
one bounded context might be implemented using a DDD layered 
architecture, another might use a two-tier CRUD architecture, and 
another might use an architecture derived from the CQRS pattern. Figure 
2 illustrates a system with multiple bounded contexts each using a 
different architectural style. It also highlights that each bounded 
context is typically end-to-end, from the persistence store through to 
the UI. 

![Figure 2][fig2]

**Multiple architectural styles within a large, complex application**

This highlights another benefit, in addition to managing complexity, of 
dividing the system into bounded contexts. You can use an appropriate 
technical architecture for different parts of the system that addresses 
the specific characteristics of that part: is it a complex part of the 
system; does it contain core domain functionality; what is its expected 
lifetime? 

## Bounded contexts and multiple development teams 

Clearly separating out the different bounded contexts, working with 
separate domain models and ubiquitous languages also makes it possible 
to parallelize the development work by using separate teams for each 
bounded context. This relates to the idea of using different technical 
architectures for different bounded contexts because the different 
development teams might have different skill sets and skill levels. 

## Maintaining multiple bounded contexts 

Although bounded contexts help to manage the complexity of large systems 
by dividing them up into more manageable pieces, it is unlikely that 
each bounded context will exist in isolation. Bounded contexts will need 
to exchange data with each other, and this exchange of data will be 
complicated if you need to translate between the different definitions 
of the same thing that exist in the different domain models. In our 
conference management system, we may need to move information about 
attendees between the bounded contexts that deal with conference 
bookings, badge printing, and hotel reservations. The DDD approach 
offers a number of approaches for handling the interactions between 
multiple models in multiple bounded contexts such as using 
anti-corruption layers, or using shared kernels. 

> **Note:** At the technical implementation level, communication between
  bounded contexts is often handled asynchronously using events and a
  messaging infrastructure. 

For more information about how DDD deals with large systems and complex 
models, you should read "Part IV: Strategic Design" in the book 
"Domain-Driven Design: Tackling Complexity in the Heart of Software" by 
Eric Evans. 

# CQRS and DDD 

As stated in the introduction to this chapter, it is useful to 
understand a little of the terminology and concepts from DDD when you 
are learning about the CQRS pattern. 

Many of the ideas that informed the CQRS pattern arose from issues that 
DDD practitioners faced when applying the DDD approach to real-world 
problems. As such, if you decide to use the DDD approach, you may find 
that the CQRS pattern is a very natural fit for some of the bounded 
contexts that you identify within your system, and that it is relatively 
straightforward to move from your domain model to the physical 
implementation of the CQRS pattern.

Some experts consider the DDD approach to be an essential pre-requisite
for implementing the CQRS pattern.

> "It is essential to write the whole Domain Model, ubiquitous language,
> including use cases, domain and user intention specifications, and to
> identify both context boundaries and autonomous components. In my
> experience, those are absolutely mandatory." 
> Jose Miguel Torres (Customer Advisory Council)

However, many people can point to projects where they have seen real 
benefits from implementing the CQRS pattern, but where they have not 
used the DDD approach for the domain analysis and model design. 

> "It is something of a tradition to connect both paradigms because using
> DDD can lead naturally into CQRS, and also the available literature
> about CQRS tends to use DDD terminology. However, DDD is mostly
> appropriate for very large and complex projects. On the other hand,
> there is no reason why a small and simple project can not benefit from
> CQRS. For example, a relatively small project that would otherwise use
> distributed transactions could be split into a "write side" and a
> "read side" with CQRS to avoid the distributed transaction, but it may
> be simple enough that applying DDD would be overkill."  
> Alberto Poblacion (Customer Advisory Council)

In summary, the DDD approach is not a pre-requisite for implementing the 
CQRS pattern, but in practice they do often go together. 

[j_chapter4]:     Journey_04_ExtendingEnhancing.markdown

[dddquickly]:      http://www.infoq.com/minibooks/domain-driven-design-quickly 
[evansqcon]:       http://domaindrivendesign.org/library/evans_2009_1
[valueobjects]:    http://www.infoq.com/presentations/Value-Objects-Dan-Bergh-Johnsson
[udigreg]:         http://www.udidahan.com/2012/02/10/udi-greg-reach-cqrs-agreement

[fig1]:           images/Reference_01_BCs.png?raw=true
[fig2]:           images/Reference_01_Archs.png?raw=true