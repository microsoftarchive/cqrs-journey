### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Tales from the Trenches: Twilio

## Product overview

Twilio provides high-availability voice and SMS APIs, hosted in the cloud, that enable developers to add automated voice and SMS capabilities to a wide range of applications.

Although Twilio did not explicitly implement the CQRS pattern or use event sourcing, many of the fundamental concepts implicit in their designs are very similar to concepts that relate to the CQRS pattern including splitting read and write models and relaxing consistency requirements.

## Lessons learned

This section summarizes some of the key lessons learned by Twilio during the development of the Twilio APIs and services.

### Separating reads and writes

Rather than separating out the read side and write side explicitly as in the CQRS pattern, Twilio uses a slightly different pair of concepts: in-flight data and post-flight data. In-flight data captures all of the transactional data that is accessed by operations that are currently running through the system. Once an operation completes, any data that needs to be saved becomes immutable post-flight data. In-flight data must be very high performance and support inserts, updates, and reads. Post-flight data is read-only and supports use cases such as analysis and logging. As such, post-flight data has very different performance characteristics.

Typically, there is very little in-flight data in the system, which makes it easy to support no-downtime upgrades that impact in these parts of the system. There is typically a lot more, immutable, post-flight data and any schema change here would be very expensive to implement. Hence, a schema-less data store makes a lot of sense for this post-flight data.

### Designing for high availability

One of the key design goals for Twilio was to achieve high availability for their systems in a cloud environment, and some of the specific architectural design principles that help to achieve this are:

* It's important to understand, for a system, what are the units of failure for the different pieces that make up that system, and then to design the system to be resilient to those failures. Typical units of failure might be an individual host, a datacenter or zone, a geographic region, or a cloud service provider. Identifying units of failure applies both to code deployed by Twilio, and to technologies provided by a vendor, such as data storage or queuing infrastructure. From the perspective of a risk profile, units of failure at the level of a host are to be preferred because it is easier and cheaper to mitigate risk at this level.
* Not all data requires the same level of availability. Twilio gives its developers different primitives to work with that offer three levels of availability for data; a distributed queuing system that is resilient to host and zone failures, a replicated database engine that replicates across regions, and an in-memory distributed data store for high availability. These primitives enable the developers to select a storage option with a specified unit of failure. They can then choose a store with appropriate characteristics for a specific part of the application.

### Idempotency

An important lesson that Twilio learned in relation to idempotency is the importance of assigning the token that identifies the specific operation or transaction that must be idempotent as early in the processing chain as possible. The later the token is assigned, the harder it is to test for correctness and the more difficult it is to debug. Although Twilio don't currently offer this, they would like to be able to allow their customers to set the idempotency token when they make a call to one of the Twilio APIs.

### No-downtime deployments

To enable no-downtime migrations as part of the continuous deployment of their services, Twilio use risk profiles to determine what process must be followed for specific deployments. For example, a change to the content of a website can be pushed to production with a single click, while a change to a REST API requires continuous integration testing and a human sign-off. Twilio also tries to ensure that changes to data schemas do not break existing code: therefore the application can keep running, without losing requests as the model is updated using a pivoting process. 

Some features are also initially deployed in a learning mode. This means that the full processing pipeline is deployed with a no-op at the end so that the feature can be tested with production traffic, but without any impact on the existing system.

### Performance

Twilio have four different environments: a development environment, an integration environment, a staging environment, and a production environment. Performance testing, which is part of cluster testing, happens automatically in the integration and staging environments. The performance tests that take a long time to run happen in an ongoing basis in the integration environment and may not be repeated in the staging environment.

If load-levels are predictable, there is less of a requirement to use asynchronous service implementations within the application because you can scale your worker pools to handle the demand. However, when you experience big fluctuations in demand and you don't want to use a callback mechanism because you want to keep the request open, then it makes sense make the service implementation itself asynchronous.

Twilio identified a trade-off in how to effectively instrument their systems to collect performance monitoring data. One option is to use a common protocol for all service interactions that enables the collection of standard performance metrics through a central instrumentation server. However, it's not always desirable to enforce the use of a common protocol and enforce the use of specific interfaces because it may not be the best choice in all circumstances. Different teams at Twilio make their own choices about protocols and instrumentation techniques based on the specific requirements of the pieces of the application they are responsible for.

## References

For further information relating to Twilio, see:

* [Twilio.com][twilio]
* [High-Availability Infrastructure in the Cloud][highavail]
* [Scaling Twilio][scaletwilio]
* [Asynchronous Architectures for Implementing Scalable Cloud Services][async]
* [Why Twilio Wasn't Affected by Today's AWS Issues][awsissues]

Originally told by Evan Cooke, CTO, Twilio

[twilio]:         http://www.twilio.com/
[highavail]:      http://www.slideshare.net/twilio/highavailability-infrastructure-in-the-cloud-evan-cooke-web-20-expo-nyc-2011
[scaletwilio]:    http://www.slideshare.net/twilio/scaling-twilio-evan-cooke-twilio-conference-2011-9451159
[async]:          http://www.slideshare.net/twilio/asynchronous-architectures-for-implementing-scalable-cloud-services-evan-cooke-gluecon-2012
[awsissues]:      http://www.twilio.com/engineering/2011/04/22/why-twilio-wasnt-affected-by-todays-aws-issues