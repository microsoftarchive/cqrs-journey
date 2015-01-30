### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Tales from the Trenches: Lokad Hub

## Project overview

Lokad Hub is an infrastructure element that unifies the metered, pay-as-you-go, forecasting subscription offered by Lokad. It also provides an intelligent, self-managing, business backend for Lokad's internal teams.
Lokad requires this piece of infrastructure to be extremely flexible, focused, self-managing, and capable of surviving cloud outages. Key features of Lokad Hub include:

* Multi-tenancy
* Scalability
* Instant data replication to multiple locations
* Deployable to any cloud
* Supports multiple production deployments daily
* Full audit logs and the ability to roll back to any point in time
* Integration with other systems

The current version was developed using the domain-driven design (DDD) approach, implements the CQRS pattern, and uses event sourcing (ES). It is a replacement for a legacy, CRUD-style system.
For Lokad, the two key benefits of the new system are the low development friction that makes it possible to perform multiple deployments per day, and the ability to respond quickly to changes in the system's complex business requirements.

## Lessons learned

This section summarizes some of the key lessons learned by Lokad during the development of Lokad Hub.

### Benefits of DDD

The team at Lokad adopted the DDD approach in the design and development of Lokad Hub. The DDD approach helped to divide the complex domain into multiple bounded contexts. It was then possible to model each bounded context separately and select to most appropriate technologies for that bounded context. In this project, Lokad chose a CQRS/ES implementation for each bounded context.

Lokad captured all the business requirements for the system in the models as code. This code became the foundation of the new system.

However, it did take some time (and multiple iterations) to build these models and correctly capture all of the business requirements.

### Reducing dependencies

The core business logic depends only on message contracts and the Lokad.CQRS portability interfaces. Therefore, the core business logic does not have any dependencies on specific storage providers, object-relational mappers, specific cloud services, or dependency injection containers. This makes it extremely portable, and simplifies the development process.

### Using sagas

Lokad decided not to use sagas in Lokad Hub because they found them to be overly complex and non-transparent. Lokad also found issues with trying to use sagas when migrating data from the legacy CRUD system to the new event sourced system.

### Testing and documentation

Lokad uses unit tests as the basis of a mechanism that generates documentation about the system. This is especially valuable in the cases where Lokad uses unit tests to define specifications for complex business behaviors. These specifications are also used to verify the stability of message contracts and to help visualize parts of the domain.

### Migration to ES

Lokad developed a custom tool to migrate data from the legacy SQL data stores into event streams for the event-sourced aggregates in the new system.

### Using  projections

Projections of read-side data, in combination with state of the art UI technologies, made it quicker and easier to build a new UI for the system.

The development process also benefited from the introduction of smart projections that are rebuilt automatically on startup if the system detects any changes in them.

### Event sourcing 

Event sourcing forms the basis of the cloud failover strategy for the system, by continuously replicating events from the primary system. This strategy has three goals:

* All data should be replicated to multiple clouds and datacenters within one second.
* There should be read-only versions of the UI available immediately if the core system becomes unavailable for any reason.
* A full read/write backup system can be enabled manually if the primary system becomes unavailable.

Although, it would be is possible to push this further and even have a zero downtime strategy, this would bring additional complexity and costs. For this system, a guaranteed recovery within a dozen minutes is more than adequate.

The most important aspect of this strategy is the ability to keep valuable customer data safe and secure even in the face of global cloud outages.

Event sourcing also proved invaluable when a glitch in the code was discovered soon after the initial deployment. It was possible to roll the system back to a point in time before the glitch manifested itself, fix the problem in the code, and then restart the system

### Infrastructure

When there are multiple bounded contexts to integrate (at least a dozen in the case of Lokad Hub) it's important to have a high-level view of how they integrate with each other. The infrastructure that supports the integration should also make it easy to support and manage the integration in a clean and enabling fashion.

Once you have over 100,000 events to keep and replay, simple file-based or blob-based event stores becoming limiting. With these volumes, it is better to use a dedicated event-streaming server.

## References

For further information relating to Lokad Hub, see:

* [Case: Lokad Hub][lokadhub]
* [Lokad.com][lokadcom]
* [Lokad Team][lokadteam]

Originally told by Rinat Abdullin, Technology Leader, Lokad

[lokadhub]:     http://cqrsguide.com/case:lokad-hub
[lokadcom]:     http://www.lokad.com/
[lokadteam]:    http://www.lokad.com/aboutus.ashx
