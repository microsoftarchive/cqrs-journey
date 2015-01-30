### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Tales from the Trenches: eMoney Nexus

_This study is contributed by Jon  Wagner, SVP & Chief Architect, eMoney Advisor_

## eMoney Nexus: Some CQRS lessons

Now that the Microsoft Patterns & Practices CQRS Journey is coming to a close, I thought it would be a good time to relate some of our experiences with CQRS and Event Sourcing. We have been working with similar patterns for a few years, and our experiences and conclusions are pretty close to the MSPNP team.

## About eMoney & the Nexus

eMoney Advisor provides wealth management software to financial advisors and their clients. One of the core features of our product is the ability to aggregate financial account data from multiple financial institutions and use the data from within our client portal and planning products. The front-end application is updated several times a year, but must go through customer and legal review before each deployment, but the data processing system must be updated continuously to respond to daily changes to the data sources. After running our original system for several years, we decided to rebuild the data aggregation portion of our system to solve some performance, maintainability, and complexity issues. In our design of the eMoney Nexus, we used a message-based architecture combined with split read-write duties to solve our core issues.

Since we built the Nexus a few years ago, it is not a pure CQRS/ES implementation, but many of our design choices line up with these patterns and we see the same types of benefits. Now that we can take the learning from CQRS Journey, we will go back and evaluate how these patterns may help us take the next steps to improve our system.

## System overview

The job of the Nexus is to fetch account data from a number of financial institutions, and publish that data to a number of application servers.

**Inputs**

* Users – can tell the system to create a subscription to data updates from a source, force an instant refresh of data, or modify processing rules for their accounts
* Bulk Files – arrive daily with large workloads for account updates
* Timed Updates – arrive scheduled throughout the night to update individual subscriptions

**Subscribers**

* Users – user interfaces need to update when operations complete or data changes
* Planning Applications – multiple application instances need to be notified when data changes
* Outgoing Bulk Files – enterprise partners need a daily feed of the changes to the account data

Design **Goals**

* Decoupled Development – building and upgrading the Nexus should not be constrained by application deployment lifecycles
* Throughput Resilience – processing load for queries should not affect the throughput of the data updates and vice versa
* High Availability – processing should be fault tolerant for node outages
* Continuous Deployment – connections and business logic should be upgradable during business hours and should decouple Nexus changes from other systems
* Long-Running Processes – data acquisition can take a long time, so an update operation must be decoupled from any read/query operations
* Re-playable Operations – data acquisition has a high chance of failure due to network errors, timeouts, etc., so operations must be re-playable for retry scenarios
* Strong Diagnostics – since updated operations are complex and error-prone, diagnostic tools are a must for the infrastructure
* Non-Transactional – because our data is not the system of record, there is less of a need for data rollbacks (we can just get a new update), and eventual consistency of the data is acceptable to the end user

## The evolution of the system

The legacy system was a traditional 3-tier architecture with a Web UI Tier, Application Tier, and Database Tier.

![Figure 1][fig1]
 
The first step was to decouple the processing engine from the application system. We did that be adding a service layer to accept change requests and a publishing system to send change events back to the application. The application would have its own copy of the account data that is optimized for the planning and search operations for end users. The Nexus could store the data in the best way possible for high-throughput processing.

![Figure 2][fig2]
 
Partitioning the system allows us to decouple any changes to the Nexus from the other systems. Like all good Partition / Bounded Context / Service boundaries, the interfaces between the systems are contracts that must be adhered to, but can evolve over time with some coordination between the systems. For example, we have upgraded the publishing interface to the core application 5 or 6 times to add additional data points or optimize the data publishing process. Note that we publish to a SQL Server Service Broker, but this could be another application server in some scenarios.

![Figure 3][fig3]

This allowed us to achieve our first two design goals: **Decoupled Development** and **Throughput Resilience**. Large query loads on the application would be directed at its own database, and bulk load operations on the back end do not slow down the user experience. The Nexus could be deployed on a separate schedule from the application and we could continue to make progress on the system.

Next, we added Windows Load Balancing and WCF services to expose the Command service to consumers.
 
This allows us to add additional processing nodes, as well as remove nodes from the pool in order to upgrade them. This got us to our goal of **High Availability**, as well as **Continuous Deployment**. In most scenarios, we can take a node out of the pool during the day, upgrade it, and return it to the pool to take up work.

For processing, we decided to break up each unit of work into "Messages." Most Messages are Commands that tell the system to perform an operation. Messages can dispatch other messages as part of their processing, causing an entire workflow process to unfold. We don't have a great separation between Sagas (the coordination of Commands) and Commands themselves, and that is something we can improve in future builds.

Whenever a client calls the Command service, if the request cannot be completed immediately, it is placed in a queue for processing. This can be an end user, or one of the automated data load schedulers. We use SQL Server Service Broker for our Message processing Queues. Because each of our data sources have different throughput and latency requirements, we wrote our own thread pooling mechanism to allow us to apportion the right number of threads-per-source at runtime through a configuration screen. We also took advantage of Service Broker's message priority function to allow user requests to jump to the front of the worker queues to keep end users happy. We also separated the Command (API) service from the Worker service so we can scale the workloads differently.

![Figure 4][fig4]
 
This message processing design gave us a lot of benefits. First of all, with Command/Query Separation, you are forced to deal with the fact that a Command may not complete immediately. By implementing clients that need to wait for results, you are naturally going to be able to support **Long-Running Processes**. In addition, you can persist the Command messages to a store and easily support **Re-playable Operations** to handle retry logic or system restores. The Nexus Service has its own scheduler that sends itself Commands to start jobs at the appropriate time.

![Figure 5][fig5]
 
One unexpected benefit of using a queue infrastructure was more scalable performance. Partitioning the workloads (in our case, by data source) allows for more optimal use of resources. When workloads begin to block due to some resource slowness, we can dynamically partition that workload into a separate processing queue so other work can continue.

One of the most important features that we added early on in development was Tracing and Diagnostics. When an operation is started (by a user or by a scheduled process), the system generates a GUID (a "Correlation ID") that is assigned to the message. The Correlation ID is passed throughout the system, and any logging that occurs is tied to the ID. Even if a message dispatches another message to be processed, the Correlation ID is along for the ride. This lets us easily figure out which log events in the system go together (GUIDs are translated to colors for easy visual association). **Strong Diagnostics** was one of our goals. When the processing of a system gets broken into individual asynchronous pieces, it's almost impossible to analyze a production system without this feature.

![Figure 6][fig6]
 
To drive operations, the application calls the Nexus with Commands such as CreateSubscription, UpdateSubscription, and RepublishData. Some of these operations can take a few minutes to complete, and the user must wait until the operation is finished. To support this, each long-running Command returns an ActivityID. The application polls the Nexus periodically to determine whether the activity is still running or if it has completed. An activity is considered completed when the update has completed AND the data has been published to the read replica. This allows the application to immediately perform a query on the read replica to see the data results.

![Figure 7][fig7]
 
## Lessons learned

We've been running the Nexus in production for several years now, and for this type of system, the benefits CQRS and ES are evident, at least for the read-write separation and data change events that we use in our system.

* CQRS = Service Boundary + Separation of Concerns – the core of CQRS is creating service boundaries for your inputs and outputs, then realizing that input and output operations are separate concerns and _don't need to have the same (domain) model_.
* Partitions are Important – define your Bounded Context and boundaries carefully. You will have to maintain them over time.
* External systems introduce complexity – particularly when replaying an event stream, managing state against an external system or isolating against external state may be difficult. Martin Fowler has some great thoughts on it here.
* CQRS usually implies async but not always – because you generally want to see the results of your Commands as Query results. It is possible to have Commands complete immediately if it's not a Query. In fact, it's easier that way sometimes. We allow the CreateSubscription Command to return a SubscriptionID immediately. Then an async process fetches the data and updates the read model.
* User Experience for async is hard – users want to know when their operation completes.
* Build in Diagnostics from the beginning – trust me on this.
* Decomposing work into Commands is good – our BatchUpdate message just spawns off a lot of little SubscriptionUpdate messages. It makes it easier to extend and reuse workflows over time.
* Queue or Bus + Partitions = Performance Control – this lets you fan out or throttle your workload as needs change.
* Event Sourcing lets you have totally different read systems for your data – we split our event stream and send it to a relational database for user queries and into flat files for bulk delivery to partners.

If you want some more good practical lessons on CQRS, you should read [CQRS Journey, Chapter 8 – Lessons Learned][journey_08].

## Making it better

Like any system, there are many things we would like to do better. 

* Workflow Testing is Difficult – we didn't do quite enough work to remove dependencies from our objects and messages, so it is tough to test sequences of events without setting up large test cases. Doing a cleanup pass for DI/IOC would probably make this a lot easier.
* UI code is hard with AJAX and polling – but now that there are push libraries like SignalR, this can be a lot easier.
* Tracking the Duration of an Operation – because our workflows are long, but the user needs to know when they complete, we track each operation with an Activity ID. Client applications poll the server periodically to see if an operation completes. This isn't a scalability issue yet, but we will need to do more work on this at some point.

As you can see, this implementation isn't 100% pure CQRS/ES, but the practical benefits of these patterns are real.

For more information, see Jon Wagner's blog [Zeros, Ones and a Few Twos][jwagner].

[journey_08]: Journey_40_Conclusions.markdown
[jwagner]: http://code.jonwagner.com/

[fig1]:           images/Tales_05_01.png?raw=true
[fig2]:           images/Tales_05_02.png?raw=true
[fig3]:           images/Tales_05_03.png?raw=true
[fig4]:           images/Tales_05_04.png?raw=true
[fig5]:           images/Tales_05_05.png?raw=true
[fig6]:           images/Tales_05_06.png?raw=true
[fig7]:           images/Tales_05_07.png?raw=true
