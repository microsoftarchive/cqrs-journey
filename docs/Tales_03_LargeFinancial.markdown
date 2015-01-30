### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Tales from the Trenches:  DDD/CQRS for large financial company

## Project overview

The following is a list of the overall goals of the project. We wanted to:

* Build a sample reference architecture for enterprise level applications with the main emphasis on performance, scalability, reliability, extensibility, testability, and modularity.
* Enforce SOLID (single responsibility, open-closed, Liskov substitution, interface segregation, and dependency inversion) principles.
* Utilize test-driven development and evaluate performance early and often as part of our application lifecycle management (ALM).
* Provide abstraction and interoperability with third-party and legacy systems.
* Address infrastructure concerns such as authentication (by using claims-based, trusted sub systems), and server and client side caching (by using AppFabric for Windows Server).
* Include the capabilities necessary to support various types of clients.

We wanted to use the CQRS pattern to help us to improve the performance, scalability, and reliability of the system. 

On the read side, we have a specialized query context (read side) that exposes the data in the exact format that the UI clients require which minimizes the amount of processing they must perform. This separation provided great value in terms of a performance boost and enabled us to get very close to the optimal performance of our webserver with the given hardware specification.

On the write side, our command service allows us to add queuing for commands if necessary and to add event sourcing to create an audit log of the changes performed, which is a critical component for any financial system. Commands provided a very loosely coupled model to work with our domain. From the ALM perspective, commands provide a useful abstraction for our developers enabling them to work against a concrete interface and with clearly defined contracts. Handlers can be maintained independently and changed on demand through a registration process: this won't break any service contracts, and no code re-complication will be required. 

The initial reference architecture application deals with financial advisor allocation models. The application shows the customers assigned to the financial advisor, and the distribution of their allocations as compared to the modeled distribution that the customer and financial advisor had agreed upon.

## Lessons learned

This section summarizes some of the lessons learned during this project.

### Query performance

During testing of querying de-normalized context for one of the pilot applications, we couldn't get the throughput, measured in requests per second, that we expected even though the CPU and memory counters were all showing in range values. Later on, we observed severe saturation of the network both on the testing clients and on the server. Reviewing the amount of data we were querying for each call, we discovered it to be about 1.6 Mb.

To resolve this issue we:

* Enabled compression on IIS, which significantly reduced amount of data returned from the Open Data Protocol (OData) service.
* Created a highly de-normalized context that invokes a stored procedure that uses pivoting in SQL to return just the final "model layout" back to the client.
* Cached the results in the query service.

### Commands

We developed both execute and compensate operations for command handlers and use a technique of batching commands that are wrapped in a transaction scope. It is important to use the correct scope in order to reduce the performance impact.

One-way commands needed a special way to pass error notifications or results back to the caller. Different messaging infrastructures (Windows Azure Service Bus, NServiceBus) support this functionality in different ways, but for our on-premises solution, we had to come up with our own custom approach. 

### Working with legacy databases

Our initial domain API relied on single GUID key type, but the customer's DBA team has a completely different set of requirements to build normalized databases. They use multiple key types including shorts, integers, and strings. The two solutions we explored that would enable our domain to work with these key types were: 

* Allow the use of generic keys.
* Use a mapping mechanism to translate between GUIDs and the legacy keys.

### Using an Inversion of Control (IoC) container

Commands help to decouple application services functionality into a loosely coupled, message-driven tier. Our bootstrapping process registers commands and command handlers during the initialization process, and the commands are resolved dynamically using the generic type **ICommandHandler<CommandType>** from a Unity container. Therefore, the command service itself doesn't have an explicit set of commands to support, it is all initialized through the bootstrapping process. 
 
Because the system is very loosely coupled, it is critical that we have a highly organized bootstrapping mechanism that is generic enough to provide modularity and materialization for the specific container, mapping and logging choices.

### Key lessons learned

* There is no one right way to implement CQRS. However, having specific infrastructure elements in place, such as a service bus and a distributed cache, may reduce the overall complexity.
* Have clear performance SLAs on querying throughput and query flexibility.
* Test performance early and often using performance unit tests.
* Choose your serialization format wisely and only return the data that's needed: for OData services prefer JSON serialization over AtomPub.
* Design your application with upfront enforcement of SOLID principals.

Originally told by Tim Walton, Senior Application Dev Mgr II, Microsoft and Alex Dubinkov, Senior Premier Field Engineer, Microsoft
