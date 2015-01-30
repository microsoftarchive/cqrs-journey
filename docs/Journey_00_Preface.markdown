### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Preface

_Why are we embarking on this journey?_

> "The best way to observe a fish is to become a fish," Jacques Cousteau


# Why we created this guidance now

The **Command Query Responsibility Segregation (CQRS)** pattern and 
**Event Sourcing** (ES) are currently generating a great deal of interest 
from developers and architects who are designing and building 
large-scale, distributed systems. There are conference sessions, blogs, 
articles, and frameworks all dedicated to the CQRS pattern and to event 
sourcing, and all explaining how they can help you to improve the 
maintainability, testability, scalability, and flexibility of your 
systems. 

However, like anything new, it takes some time before a pattern, 
approach, or methodology is fully understood and consistently defined by 
the community and has useful, practical guidance to help you to apply or
implement it. 

This guidance is designed to help you get started with the CQRS pattern 
and event sourcing. It is not intended to be **the** guide to the CQRS 
pattern and event sourcing, but **a** guide that describes the 
experiences of a development team in implementing the CQRS pattern and 
event sourcing in a real-world application. The development team did not 
work in isolation; they actively sought input from industry experts and 
from a wider group of advisors to ensure that the guidance is both 
detailed and practical. 

The CQRS pattern and event sourcing are not mere simplistic solutions to 
the problems associated with large-scale, distributed systems. By 
providing you with both a working application and written guidance, we 
expect you'll be well prepared to embark on your own CQRS journey. 

# How is this guidance structured?

There are two closely related parts to this guidance: 

* A working reference implementation (RI) sample, which is intended to
  illustrate many of the concepts related to the CQRS pattern and event
  sourcing approaches to developing complex enterprise
  applications. 

* This written guidance, which is intended to complement the RI by 
  describing how it works, what decisions were made during its 
  development, and what trade-offs were considered. 

This written guidance is itself split into three distinct sections that you can read independently: a description of the journey we took as we learned about CQRS, and a collection of CQRS reference materials, and a collection of case studies that describe the experiences other teams have had with the CQRS pattern. The map in Figure 1 illustrates the relationship between the first two sections: a journey with some defined stopping points that enables us to explore a space. 

![Figure 1][fig1]

**A CQRS journey**

## A CQRS journey

This section is closely related to the RI and the chapters follow the 
chronology of the project to develop the RI. Each chapter describes 
relevant features of the domain model, infrastructure elements, 
architecture, and user interface (UI) that the team was concerned with 
during that phase of the project. Some parts of the system are discussed 
in several chapters, and this reflects the fact that the team revisited 
certain areas during later stages. Each of these chapters discuss how 
and why particular CQRS patterns and concepts apply to the design and 
development of particular bounded contexts, describe the implementation, 
and highlight any implications for testing. 

Other chapters look at the big picture. For example, there 
is a chapter that explains the rationale for splitting the RI into the 
bounded contexts we chose, another chapter analyzes the implications of 
our approach for versioning the system, and other chapters look at how 
the different bounded contexts in the RI communicate with each other. 

This section describes our journey as we learned about CQRS, and how we 
applied that learning to the design and implementation of the RI. It is 
not prescriptive guidance and is not intended to illustrate the only way 
to apply the CQRS approach to our RI. We have tried wherever possible to 
capture alternative viewpoints through consultation with our advisors 
and to explain why we made particular decisions. You may disagree with 
some of those decisions; please let us know at 
[cqrsjourney@microsoft.com][cqrsemail]. 

This section of the written guidance makes frequent cross-references to 
the material in the second section for readers who wish to explore any 
of the concepts or patterns in more detail. 

## CQRS reference

The second section of the written guidance is a collection of reference 
material collated from many sources. It is not the definitive 
collection, but should contain enough material to help you to understand 
the core patterns, concepts, and language of CQRS.

## Tales from the trenches

This section of the written guidance is a collection of case studies from other teams that describe their experiences of implementing the CQRS pattern and event sourcing in the real world. These case studies are not as detailed as the journey section of the guidance and are intended to give an overview of these projects and to summarize some of the key lessons learned.

The following is a list of the chapters that comprise both sections of 
the written guidance: 

### A CQRS journey

* Chapter 1, "The Contoso Conference Management System," introduces our
  sample application and our team of (fictional) experts.
* Chapter 2, "Decomposing the Domain," provides a high-level view of the
  sample application and describes the bounded contexts that make up the
  application.
* Chapter 3, "Orders and Registrations Bounded Context," introduces our
  first bounded context, explores some CQRS concepts, and describes some
  elements of our infrastructure.
* Chapter 4, "Extending and Enhancing the Orders and Registrations
  Bounded Context," describes adding new features to the bounded context
  and discusses our testing approach.
* Chapter 5, "Preparing for the V1 Release," describes adding two new
  bounded contexts and handling integration issues between them, and
  introduces our event-sourcing implementation. This is our first
  pseudo-production release.
* Chapter 6, "Versioning Our System," discusses how to version the
  system and handle upgrades with minimal down time.
* Chapter 7, "Adding Resilience and Optimizing Performance," describes
  what we did to make the system more resilient to failure scenarios and
  how we optimized the performance of the system. This was the last
  release of the system in our journey.
* Chapter 8, "Lessons Learned," collects the key lessons we learned from
  our journey and suggests how you might continue the journey.

### CQRS Reference

* Chapter 1, "CQRS in Context," provides some context for CQRS,
  especially in relation to the domain-driven design approach.
* Chapter 2, "Introducing the Command Query Responsibility Segregation
  Pattern," provides a conceptual overview of the CQRS pattern.
* Chapter 3, "Introducing Event Sourcing," provides a conceptual
  overview of event sourcing.
* Chapter 4, "A CQRS and ES Deep Dive," describes the CQRS pattern and
  event sourcing in more depth.
* Chapter 5, "Communicating between Bounded Contexts," describes some
  options for communicating between bounded contexts.
* Chapter 6, "A Saga on Sagas," explains our choice of terminology:
  process manager instead of saga. It also describes the role of process
  managers.
* Chapter 7, "Technologies Used in the Reference Implementation,"
  provides a brief overview of some of the other technologies we used,
  such as the Windows Azure Service Bus.
* Appendix 1, "Building and Running the Sample Code," contains detailed
  instructions for downloading, building, and running the sample
  application and test suites.
* Appendix 2, "Migrations," contains instructions for performing the code and data migrations between the pseudo-production releases of the Contoso Conference Management System.

### Tales from the trenches

* Chapter 1, "Twilio," describes a highly available, cloud-hosted, communications platform. Although the team who developed this product did not explicitly use CQRS, many of the architectural concepts they adopted are very closely related to the CQRS pattern.
* Chapter 2, "Lokad Hub," describes a project that made full use of domain-driven design, CQRS, and event sourcing in an application designed to run on multiple cloud platforms.
* Chapter 3, "DDD/CQRS for large financial company," describes a project that made full use of domain-driven design and CQRS to build a reference application for a large financial company. It used CQRS to specifically address the issues of performance, scalability, and reliability.
* Chapter 4, "Digital Marketing," describes how an existing application was refactored over time while delivering new features. This project adopted the CQRS pattern for one of its pieces as the project progressed.
* Chapter 5, "TOPAZ Technologies," describes a project that used the CQRS pattern and event sourcing to simplify the development of an off-the-shelf enterprise application.
* Chapter 6, "eMoney Nexus," describes a project to migrate an application that used legacy three-tier architecture to an architecture that used the CQRS pattern and event sourcing. Many of the conclusions drawn in this project are similar to our own experiences in our CQRS journey.


## Selecting the domain for the RI

Before embarking on our journey, we needed to have an outline of the 
route we planned to take and an idea of what the final destination 
should be. We needed to select an appropriate domain for the RI. 

We engaged with the community and our advisory board to help us choose a 
domain that would enable us to highlight as many of the features and 
concepts of CQRS as possible. To help us select between our candidate 
domains, we used the criteria in the following list. The domain selected 
should be: 

* **Non-trivial.** The domain must be complex enough to exhibit real 
problems, but at the same time simple enough for most people to 
understand without weeks of study. The problems should involve dealing 
with temporal data, stale data, receiving out-of-order events, and 
versioning. The domain should enable us to illustrate solutions using 
event sourcing, sagas, and event merging. 

* **Collaborative.** The domain must contain collaborative elements where 
multiple actors can operate simultaneously on shared data. 

* **End to end.** We wanted to be able illustrate the concepts and 
patterns in action from the back-end data store through to the user 
interface. This might include disconnected mobile and smart 
clients. 

* **Cloud friendly.** We wanted to have the option of hosting parts of the 
RI on Windows Azure and be able to illustrate how you can use CQRS for 
cloud-hosted applications. 

* **Large.** We wanted to be able to show how our domain can be broken 
down into multiple bounded contexts to highlight when to use and when 
not use CQRS. We also wanted to illustrate how multiple architectural 
approaches (CQRS, CQRS/ES, and CRUD) and legacy systems can co-exist 
within the same domain. We also wanted to show how multiple 
development teams could carry out work in parallel. 

* **Easily deployable.** The RI needed to be easily deployable so that you 
can install it and experiment with it as you read this guidance. 

As a result, we chose to implement the conference management system that 
Chapter 1, "[The Contoso Conference Management System][j_chapter1]" introduces. 

[fig1]:           images/Map.png?raw=true
[cqrsemail]:      mailto:cqrsjourney@microsoft.com
[j_chapter1]:     Journey_01_Introduction.markdown