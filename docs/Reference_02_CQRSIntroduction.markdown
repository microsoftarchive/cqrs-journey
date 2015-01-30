### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Reference 2: Introducing the Command Query Responsibility Segregation Pattern

In this chapter, we describe the Command Query Responsibility 
Segregation (CQRS) pattern, which is at the heart of almost everything 
discussed in this guidance. Here we will show you how applying this 
pattern affects the architecture of your enterprise application. It is 
important to understand that CQRS is not a silver bullet for all the 
problems you encounter when you design and build enterprise 
applications, and that it is not a top-level architectural approach. The 
chapter [Decomposing the Domain][j_chapter2] in "A CQRS Journey" 
describes how Contoso divided up the Contoso Conference Management 
System into bounded contexts and identified which bounded contexts would 
benefit from using the CQRS pattern. 

Subsequent chapters in A CQRS Journey provide more in-depth guidance on 
how to apply the CQRS pattern and other related patterns when building 
your implementation.

# What is CQRS?

In his book "[Object Oriented Software Construction][meyerbook]," 
Betrand Meyer introduced the term "[Command Query Separation][cqsterm]" 
to describe the principle that an object's methods should be either 
commands, or queries. A query returns data and does not alter the state 
of the object; a command changes the state of an object but does not 
return any data. The benefit is that you have a better understanding 
what does, and what does not, change the state in your system. 

CQRS takes this principle a step further to define a simple pattern.

> "CQRS is simply the creation of two objects where there was previously 
>  only one. The separation occurs based upon whether the methods are a 
>  command or a query (the same definition that is used by Meyer in
>  Command and Query Separation: a command is any method that mutates 
>  state and a query is any method that returns a value)."  
>  Greg Young,
>  [CQRS, Task Based UIs, Event Sourcing agh!][gregyoungcqrs] 

What is important and interesting about this simple pattern is how, 
where, and why you use it when you build enterprise systems. Using this 
simple pattern enables you to meet a wide range of architectural 
challenges, such as achieving scalability, managing complexity, and 
managing changing business rules in some portions of your system. 

> CQRS is a simple pattern that strictly segregates the responsibility 
> of handling command input into an autonomous system from the 
> responsibility of handling side-effect-free query/read access on the 
> same system. Consequently, the decoupling allows for any number of 
> homogeneous or heterogeneous query/read modules to be paired with a 
> command processor. This principle presents a very suitable 
> foundation for event sourcing, eventual-consistency state 
> replication/fan-out and, thus, high-scale read access. In simple 
> terms, you don't service queries via the same module of a service 
> that you process commands through. In REST terminology, GET requests 
> wire up to a different thing from what PUT, POST, and DELETE requests wire 
> up to.  
> Clemens Vasters (CQRS Advisors Mail List)

The following conversation between Greg Young and Udi Dahan highlights some of the important aspects of the CQRS pattern:

> **Udi Dahan:** If you are going to be looking at applying CQRS, it
> should be done within a specific bounded context, rather than at the
> whole system level, unless you are in a special case, when your entire
> system is just one single bounded context. 
> 
> **Greg Young:** I would absolutely agree with that statement. CQRS is
> not a top-level architecture. CQRS is something that happens at a much
> lower level, where your top level architecture is probably going to
> look more like SOA and EDA [service-oriented or event-driven architectures]. 
> 
> **Udi Dahan:** That's an important distinction. And that's something
> that a lot of people who are looking to apply CQRS don't give enough
> attention to: just how important on the one hand, and how difficult on
> the other, it is to identify the correct bounded contexts or services,
> or whatever you call that top-level decomposition and the event-based
> synchronization between them. A lot of times, when discussing CQRS
> with clients, when I tell them "You don't need CQRS for that," their
> interpretation of that statement is that, in essence, they think 
> I'm telling them that they need to go back to an N-tier type of
> architecture, when primarily I mean that a two-tier style of architecture is
> sufficient. And even when I say two-tier, I don't necessarily mean that
> the second tier needs to be a relational database. To a large extent,
> for a lot of systems, a NoSQL, document-style database would probably
> be sufficient with a single data-management type tier operated on the
> client side. As an alternative to CQRS, it's important to lay out a
> bunch of other design styles or approaches, rather than thinking
> either you are doing N-tier object relational mapping or CQRS. 
> 
> **Question:** Do you consider CQRS to be an approach or a pattern? If
> it's a pattern, what problem does it specifically solve? 
> 
> **Greg Young:** If we were to go by the definition that we have set up
> for CQRS a number of years ago, it's going to be a very simple
> low-level pattern. It's not even that interesting as a pattern; it's
> more just pretty conceptual stuff, you just separate. What's more
> interesting about it is what it enables. It's the enabling that the
> pattern provides that's interesting. Everybody gets really caught up
> in systems and they talk about how complicated CQRS is with service
> bus and all the other stuff they are doing, and in actuality, none of
> that is necessary. If you go with the simplest possible definition, it
> would be a pattern. But it's more what happens once you apply that
> pattern; the opportunities that you get.

## Read and write sides

Figure 1 shows a typical application of the CQRS pattern to a portion of 
an enterprise system. This approach shows how, when you apply the CQRS 
pattern, you can separate the read and write sides in this portion of 
the system. 

![Figure 1][fig1]

**A possible architectural implementation of the CQRS pattern**

In Figure 1, you can see how this portion of the system is split into a 
read side and a write side. The object or objects or the read side 
contain only query methods, and the objects on the write side contain 
only command methods. 

There are several motivations for this segregation including:

- In many business systems, there is a large imbalance between the 
  number of reads and the number of writes. A system may process 
  thousands of reads for every write. Segregating the two sides enables 
  you to optimize them independently. For example, you can 
  scale out the read side to support the larger number of read
  operations independently of the write side. 
- Typically, commands involve complex business logic to ensure that the 
  system writes correct and consistent data to the data store. Read 
  operations are often much simpler than write operations. A single 
  conceptual model that tries to encapsulate both read and write 
  operations may do neither well. Segregating the two sides ultimately 
  results in simpler, more maintainable, and more flexible models. 
- Segregation can also occur in the data store. The write side may use a 
  database schema that is close to third normal form (3NF) and optimized 
  for data modifications, while the read side uses a denormalized 
  database that is optimized for fast query operations. 

> **Note:** Although figure 1 shows two data stores, applying the CQRS 
  pattern does not mandate that you split the data store, or that you use any 
  particular persistence technology such as a relational database, NoSQL store, or 
  event store (which in turn could be implemented on top of a
  relational database, NoSQL store, file storage, blob storage and so forth.).
  You should view CQRS as a pattern that facilitates 
  splitting the data store and enabling you to select from a range of 
  storage mechanisms.
  
Figure 1 might also suggest a one-to-one relationship between the write 
side and the read side. However, this is not necessarily the case. It 
can be useful to consolidate the data from multiple write models into a 
single read model if your user interface (UI) needs to display 
consolidated data. The point of the read-side model is to simplify what 
happens on the read side, and you may be able to simplify the 
implementation of your UI if the data you need to display has already 
been combined. 

There are some questions that might occur to you about the 
practicalities of adopting architecture such as the one shown in 
figure 1.

- Although the individual models on the read side and write side might 
  be simpler than a single compound model, the overall architecture is 
  more complex than a traditional approach with a single model and a 
  single data store. So, haven't we just shifted the complexity?
- How should we manage the propagation of changes in the data store on the 
  write-side to the read-side?
- What if there is a delay while the updates on the write-side are 
  propagated to the read-side?
- What exactly do we mean when we talk about models?

The remainder of this chapter will begin to address these questions and 
to explore the motivations for using the CQRS pattern. 
Later chapters will explore these issues in more depth.

# CQRS and Domain-Driven Design

The previous chapter, "[CQRS in Context][r_chapter1]," introduced some 
of the concepts and terminology from the Domain-Driven Design (DDD) approach that 
are relevant to the implementation of the CQRS pattern. Two areas are 
particularly significant to the CQRS pattern.

> **Note:** "CQRS is an architectural style that is often enabling of 
  DDD."  
  Eric Evans, tweet February 2012.

The first is the concept of the model:

> "The model is a set of concepts built up in the heads of people on 
  the project, with terms and relationships that reflect domain 
  insight. These terms and interrelationships provide the semantics of 
  a language that is tailored to the domain while being precise enough 
  for technical development."  
  Eric Evans, "Domain-Driven Design: Tackling Complexity in the Heart of Software," p23.


Eric Evans in his book "_Domain-Driven Design: Tackling Complexity in the 
Heart of Software_," (Addison-Wesley Professional, 2003) provides the 
following list of ingredients for effective modeling. This list helps to 
capture the idea of a model, but is no substitute for reading the book 
to gain a deeper understanding of the concept: 

- Models should be bound to the implementation.
- You should cultivate a language based on the model.
- Models should be knowledge rich.
- You should brainstorm and experiment to develop the model.

In figure 1, the implementation of the model exists on the write side; 
it encapsulates the complex business logic in this portion of the 
system. The read side is a simpler, read-only view of the system state 
accessed through queries.

The second important concept is the way that DDD divides large, 
complex systems into more manageable units known as bounded contexts. 
A bounded context defines the context for a model:

> "Explicitly define the context within which a model applies. 
  Explicitly set boundaries in terms of team organization, usage 
  within specific parts of the application, and physical manifestations 
  such as code bases and database schemas. Keep the model strictly 
  consistent within these bounds, but don't be distracted or confused 
  by issues outside."  
  Eric Evans, "Domain-Driven Design," p335.

> **Note:** Other design approaches may use different terminology; for example, in event-driven service-oriented architecture (SOA), the service concept is similar to the bounded context concept in DDD.

When we say that you should apply the CQRS pattern to a portion of a 
system, we mean that you should implement the CQRS pattern within a 
bounded context.

The reasons for identifying context boundaries for your domain models 
are not necessarily the same reasons for choosing the portions of the 
system that should use the CQRS pattern. In DDD, a bounded context 
defines the context for a model and the scope of a ubiquitous language. 
You should implement the CQRS pattern to gain certain benefits for your 
application such as scalability, simplicity, and maintainability. 
Because of these differences, it may make sense to think about applying 
the CQRS pattern to business components rather than bounded contexts.

> "A given Bounded Context should be divided into Business Components, 
  where these Business Components have full UI through DB code, and are 
  ultimately put together in composite UIs and other physical 
  pipelines to fulfill the system's functionality.  
  A Business Component can exist in only one Bounded Context.  
  CQRS, if it is to be used at all, should be used within a Business 
  Component."  
  Udi Dahan, [Udi & Greg Reach CQRS Agreement][udigreg].

It is quite possible that your bounded contexts map exactly onto your 
business components.

> **Note:** Throughout this guide, we use the term bounded context in preference to the term business component to refer to the context within which we are implementing the CQRS pattern.

In summary, you should **not** apply the CQRS pattern to the top-level 
of your system. You should clearly identify the different portions of 
your system that you can design and implement largely independently of 
each other, and then only apply the CQRS pattern to those portions 
where there are clear business benefits in doing so.

# Introducing Commands, Events, and Messages

DDD is an analysis and design approach that encourages you to use models 
and a ubiquitous language to bridge the gap between the business and the 
development team by fostering a common understanding of the domain. Of 
necessity, the DDD approach is oriented towards analyzing behavior 
rather than just data in the business domain, and this leads to a focus 
on modeling and implementing behavior in the software. A natural way to 
implement the domain model in code is to use commands and events. This 
is particularly true of applications that use a task-oriented UI. 

> **Note:** DDD is not the only approach in which it is common to see tasks and behaviors specified in the domain model implemented using commands and events. However, many advocates of the CQRS pattern are also strongly influenced by the DDD approach so it is common to see DDD terminology in use whenever there is a discussion of the CQRS pattern. 

*Commands are imperatives;* they are requests for the system to 
perform a task or action. For example, "book two places for conference X" 
or "allocate speaker Y to room Z." Commands are usually processed just 
once, by a single recipient.

*Events are notifications;* they report something that has already 
happened to other interested parties. For example, "the customer's 
credit card has been billed $200" or "ten seats have been booked for 
conference X." Events can be processed multiple times, by multiple 
consumers.

Both commands and events are types of message that are used to exchange 
data between objects. In DDD terms, these messages represent business 
behaviors and therefore help the system capture the business intent 
behind the message.

A possible implementation of the CQRS pattern uses separate data 
stores for the read side and the write side; each data store is optimized 
for the use cases it supports. Events provide the basis of a 
mechanism for synchronizing the changes on the write side (that result 
from processing commands) with the read side. If the write side raises 
an event whenever the state of the application changes, the read side 
should respond to that event and update the data that is used by its 
queries and views. Figure 2 shows how commands and events can be used 
if you implement the CQRS pattern.

![Figure 2][fig2]

**Commands and events in the CQRS pattern**

We also require some infrastructure to handle commands and events, and
we will explore this in more detail in later chapters.

> **Note:** Events are not the only way to manage the push 
  synchronization of updates from the write side to the read side.

# Why should I use CQRS?

Stepping back from CQRS for a moment, one of the benefits of dividing 
the domain into bounded contexts in DDD is to enable you to identify and 
focus on those portions (bounded contexts) of the system that are more 
complex, subject to ever-changing business rules, or deliver functionality 
that is a key business differentiator. 

You should consider applying the CQRS pattern within a specific bounded 
context only if it provides identifiable business benefits, not 
because it is the default pattern that you consider. 

The most common business benefits that you might gain from applying the CQRS pattern are enhanced scalability, the simplification of a complex aspect of your domain, increased flexibility of your solution, and greater adaptability to changing business requirements. 

## Scalability

In many enterprise systems, the number of reads vastly exceeds the 
number of writes, so your scalability requirements will be different for
each side. By separating the read-side and the write-side into separate 
models within the bounded context, you now have the ability to scale 
each one of them independently. For example, if you are hosting 
applications in Windows Azure, you can use a different role for each 
side and then scale them independently by adding a different number of 
role instances to each.

> **Note:** Scalability should not be the only reason why you choose to implement the CQRS pattern in a specific bounded context:
  "In a non-collaborative domain, where you can horizontally add more database servers to support more users, requests, and data at the same time you're adding web servers, there is no real scalability problem (until you're the size of Amazon, Google, or Facebook). Database servers can be cheap if you're using MySQL, SQL Server Express, or others."    
  Udi Dahan, [When to avoid CQRS][dahanavoid].

## Reduced complexity

In complex areas of your domain, designing and implementing objects that
are responsible for both reading and writing data can exacerbate the 
complexity. In many cases, the complex business logic is only applied 
when the system is handling updates and transactional operations; in 
comparison, read logic is often much simpler. When the business logic 
and read logic are mixed together in the same model, it becomes much 
harder to deal with difficult issues such as multiple-users, shared 
data, performance, transactions, consistency, and stale data. Separating
the read logic and business logic into separate models makes it easier 
to separate out and address these complex issues. However, in many cases
it may require some effort to disentangle and understand the existing 
model in the domain.

> **Note:** Separation of concerns is the key motivation behind 
  Bertrand Meyer's Command Query Separation Principle:  
  "The really valuable idea in this principle is that it's extremely 
  handy if you can clearly separate methods that change state from those
  that don't. This is because you can use queries in many situations 
  with much more confidence, introducing them anywhere, changing their 
  order. You have to be more careful with modifiers."  
  Martin Fowler, [CommandQuerySeparation][cqsterm]

Like many patterns, you can view the CQRS pattern as a mechanism for 
shifting some of the complexity inherent in your domain into something 
that is well known, well understood, and that offers a standard approach 
to solving certain categories of problems. 

Another potential benefit of simplifying the bounded context by 
separating out the read logic and the business logic is that it can make
testing easier.

## Flexibility

The flexibility of a solution that uses the CQRS pattern largely derives 
from the separation into the read-side and the write-side models. It 
becomes much easier to make changes on the read-side, such as adding 
a new query to support a new report screen in the UI, when you can be 
confident that you won't have any impact on the behavior of the business 
logic. On the write-side, having a model that concerns itself solely 
with the core business logic in the domain means that you have a simpler 
model to deal with than a model that includes read logic as well. 

In the longer term, a good, useful model that accurately describes your 
core domain business logic will become a valuable asset. It will enable 
you to be more agile in the face of a changing business environment and 
competitive pressures on your organization. 

This flexibility and agility relates to the concept of continuous integration in DDD: 

> Continuous integration means that all work within the context is 
  being merged and made consistent frequently enough that when splinters
  happen they are caught and corrected quickly.  
  Eric Evans, "Domain-Driven Design," p342. 

In some cases, it may be possible to have different development teams 
working on the write-side and the read-side, although in practice this 
will probably depend on how large the particular bounded context is. 

## Focus on the business

If you use an approach like CRUD, then the technology tends to shape the 
solution. Adopting the CQRS pattern helps you to focus on the business 
and build task-oriented UIs. A consequence of separating the 
different concerns into the read-side and the write-side is a solution 
that is more adaptable in the face of changing business requirements. 
This results in lower development and maintenance costs in the 
longer term. 

## Facilitates building task-based UIs

When you implement the CQRS pattern, you use commands (often from the 
UI), to initiate operations in the domain. These commands are typically 
closely tied to the domain operations and the ubiquitous language. For 
example, "book two seats for conference X." You can design your UI to 
send these commands to the domain instead of initiating CRUD-style 
operations. This makes it easier to design intuitive, task-based UIs. 

# Barriers to adopting the CQRS pattern

Although you can list a number of clear benefits to adopting the CQRS 
pattern in specific scenarios, you may find it difficult in practice to 
convince your stakeholders that these benefits warrant the additional 
complexity of the solution. 

> "In my experience, the most important disadvantage of using CQRS/event
> sourcing and DDD is the fear of change. This model is different from
> the well-known three-tier layered architecture that many of our
> stakeholders are accustomed to."
> Pawel Wilkosz (Customer Advisory Council)

> "The learning curve of developer teams is steep. Developers usually
> think in terms of relational database development. From my experience,
> the lack of business, and therefore domain rules and specifications,
> became the biggest hurdle."
> Jose Miguel Torres  (Customer Advisory Council)

# When should I use CQRS?

Although we have outlined some of the reasons why you might decide to 
apply the CQRS pattern to some of the bounded contexts in your system, 
it is helpful to have some rules of thumb to help identify the bounded 
contexts that might benefit from applying the CQRS pattern. 

In general, applying the CQRS pattern may provide the most value in those 
bounded contexts that are collaborative, complex, include ever-changing business rules, 
and deliver a significant competitive advantage to the business. 
Analyzing the business requirements, building a useful model, expressing 
it in code, and implementing it using the CQRS pattern for such a 
bounded context all take time and cost money. You should expect this 
investment to pay dividends in the medium to long term. It is probably 
not worth making this investment if you don't expect to see returns such 
as increased adaptability and flexibility in the system, or reduced 
maintenance costs. 

## Collaborative domains

Both Udi Dahan and Greg Young identify collaboration as the 
characteristic of a bounded context that provides the best indicator 
that you may see benefits from applying the CQRS pattern. 

> In a collaborative domain, an inherent property of the domain is that
  multiple actors operate in parallel on the same set of data. A 
  reservation system for concerts would be a good example of a 
  collaborative domain; everyone wants the good seats.  
  Udi Dahan,
  [Why you should be using CQRS almost everywhere...][dahaneverywhere]

The CQRS pattern is particularly useful where the collaboration involves 
complex decisions about what the outcome should be when you have 
multiple actors operating on the same, shared data. For example, does 
the rule "last one wins" capture the expected business outcome for your 
scenario, or do you need something more sophisticated? It's important to 
note that actors are not necessarily people; they could be other parts 
of the system that can operate independently on the same data. 

> **Note:** Collaborative behavior is a *good indicator* that there 
  will be benefits from applying the CQRS pattern; however, this is not a hard 
  and fast rule!

Such collaborative portions of the system are often the most complex, 
fluid, and significant bounded contexts. However, this characteristic is 
only a guide: not all collaborative domains benefit from the CQRS 
pattern, and some non-collaborative domains do benefit from the CQRS 
pattern. 

## Stale data

In a collaborative environment where multiple users can operate on the 
same data simultaneously, you will also encounter the issue of stale data; if 
one user is viewing a piece of data while another user changes it, then 
the first user's view of the data is stale. 

Whatever architecture you choose, you must address this problem. For 
example, you can use a particular locking scheme in your database, or 
define the refresh policy for the cache from which your users read 
data. 

The two previous examples show two different areas in a system where you 
might encounter and need to deal with stale data; in most collaborative 
enterprise systems there will be many more. The CQRS pattern helps you 
address the issue of stale data explicitly at the architecture level. 
Changes to data happen on the write side, users view data by querying 
the read side. Whatever mechanism you chose to use to push the changes 
from the write side to the read side is also the mechanism that controls 
when the data on the read side becomes stale, and how long it remains 
so. This differs from other architectures, where management of stale 
data is more of an implementation detail that is not always addressed in 
a standard or consistent manner. 

> Standard layered architectures don't explicitly deal with either of 
  these issues. While putting everything in the same database may be one
  step in the direction of handling collaboration, staleness is usually 
  exacerbated in those architectures by the use of caches as a 
  performance-improving afterthought.  
  Udi Dahan talking about collaboration and staleness,
  [Clarified CQRS][dahanclarify].

In the chapter "[A CQRS and ES Deep Dive][r_chapter4]," we will look at how 
the synchronization mechanism between write-side and the read-side 
determines how you manage the issue of stale data in your application.

## Moving to the cloud

Moving an application to the cloud or developing an application for the 
cloud is not a sufficient reason for choosing to implement the CQRS 
pattern. However, many of the drivers for using the cloud such as 
requirements for scalability, elasticity, and agility are also drivers 
for adopting the CQRS pattern. Furthermore, many of the services 
typically offered as part of a platform as a service (PaaS) 
cloud-computing platform are well suited for building the infrastructure 
for a CQRS implementation: for example, highly scalable data stores, 
messaging services, and caching services. 

#When should I avoid CQRS?

Simple, static, non-core bounded contexts are less likely to warrant the 
upfront investment in detailed analysis, modeling, and complex 
implementation. Again, non-collaborative bounded contexts are less 
likely to see benefits from applying the CQRS pattern. 

In most systems, the majority of bounded contexts will probably not 
benefit from using the CQRS pattern. You should only use the pattern 
when you can identify clear business benefits from doing so.

> "Most people using CQRS (and Event Sourcing too) shouldn't have done
> so."  
> Udi Dahan: [When to avoid CQRS][dahanavoid]

> "It's important to note though, that these are things you _can_ do, not
> necessarily things you _should_ do. Separating the read and write models
> can be quite costly."  
> Greg Young: [CQRS and CAP Theorem][cqrscap]

# Summary

The CQRS pattern is an enabler for building individual portions (bounded 
contexts) in your system. Identifying where to use the CQRS pattern 
requires you to analyze the trade-offs between the initial cost and 
overhead of implementing the pattern and the future business benefits. 
Useful heuristics for identifying where you might apply the CQRS pattern 
are to look for components that are complex, involve fluid business 
rules, deliver competitive advantage to the business, and are 
collaborative. 

The next chapters will discuss how to implement the CQRS pattern in more 
detail. For example, we'll explain specific class-pattern 
implementations, explore how to synchronize the data between the write 
side and read side, and describe different options for storing data. 




[j_chapter2]:     Journey_02_BoundedContexts.markdown
[r_chapter1]:     Reference_01_CQRSContext.markdown
[r_chapter4]:     Reference_04_DeepDive.markdown

[meyerbook]:      http://www.amazon.com/gp/product/0136291554
[cqsterm]:        http://martinfowler.com/bliki/CommandQuerySeparation.html
[gregyoungcqrs]:  http://codebetter.com/gregyoung/2010/02/16/cqrs-task-based-uis-event-sourcing-agh/
[dahaneverywhere]:http://www.udidahan.com/2011/10/02/why-you-should-be-using-cqrs-almost-everywhere%E2%80%A6/
[dahanclarify]:   http://www.udidahan.com/2009/12/09/clarified-cqrs/
[dahanavoid]:     http://www.udidahan.com/2011/04/22/when-to-avoid-cqrs/
[udigreg]:        http://www.udidahan.com/2012/02/10/udi-greg-reach-cqrs-agreement/
[cqrscap]:        http://codebetter.com/gregyoung/2010/02/20/cqrs-and-cap-theorem/
[fig1]:           images/Reference_02_Arch_01.png?raw=true
[fig2]:           images/Reference_02_Messages.png?raw=true