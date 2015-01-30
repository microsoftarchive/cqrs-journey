### This version of this chapter was part of our working repository during the project. The final version of this chapter is now available on MSDN at [http://aka.ms/cqrs](http://aka.ms/cqrs).

# Tales from the Trenches: TOPAZ Technologies

## What did we hope to accomplish by using CQRS/ES?

We were looking for a way to radically simplify the development process of our _off-the-shelf_ enterprise application. We wanted to minimize unnecessary complexity induced by heavyweight frameworks, middleware, and servers like Oracle and SQL Server RDBMS.

In the past we spent too much time with technical implementation details and as a consequence spent too little time on business relevant activities. Discussions about the business rules, the business processes, and workflows were neglected. We wanted to refocus and to spend significantly more time in discussions with our business analysts and testers. Ideally, we wanted to draft the workflow of a feature with the business analyst, the product manager, and the tester, and then code it without any translation into another language or model. The notions of a bounded context and a ubiquitous language should be natural to all our stakeholders. We also realized that, from a business perspective, verbs (commands, events and more general purpose messages) have a much higher significance than nouns (entities).

Another goal was to get away from the form-over-data type of application and UI, and to develop a more task oriented presentation layer.

Last but not least, we needed an easy way to horizontally scale our application. A short term goal is to self-host the solution on an array inexpensive standard servers but the ultimate goal is to run our software in the cloud.

What were the biggest challenges and how did we overcome them?

One of the biggest challenges was to convince management and other stakeholders in our company to believe in the benefits of this new approach. Initially they were skeptical or even frightened at the thought of not having the data stored in a RDBMS. DBAs, concerned about potential job loss, also tried to influence management in a subtle, negative way regarding this new architecture.

We overcame these objections by implementing just one product using CQRS/ES, then showing the stakeholders how it worked, and demonstrating how much faster we finished the implementation. We also demonstrated the significantly improved quality of the product compared to our other products.

Another challenge was the lack of knowledge in the development team of this area. For everyone CQRS and ES were completely new. 

As an architect, I did a lot of teaching in the form of _lunch-and-learns_ in which I discussed the fundamental aspects of this new architecture. I also performed live coding in front of the team and developed some end-to-end exercises, which all developers were required to solve. I encouraged our team to watch the various free videos in which Greg Young was presenting various topics related to CQRS and event sourcing.

Yet another challenge is the fact that this type of architecture is still relatively new and not fully established. Thus, finding good guidance or adhering to best practices is not as straightforward as with more traditional architectures. How to do CQRS and ES _right_ is still invokes lively discussions, and people have very different opinions about both the overall architecture and individual elements of it.

## What were the most important lessons learned?

When we choose the right tool for the job, we can spend much more time discussing the business relevant questions and much less time discussing technical details.

It is more straightforward to implement a user story or a feature as is. Just like in real life, in code, a feature is triggered by an action (command) that results in a sequence of events that might or might not cause side effects.

Issues caused by changing business rules or code defects in the past often did not surface because we could write SQL scripts to correct the wrong data directly in the database. Because the event store is immutable, this is not possible any more—which is good thing. Now we are forced to discuss how to address the issue from a business perspective. Business analysts, product managers and other stakeholders are involved in the process of finding a solution. Often this results in the finding of a previously hidden concept in the business domain.

## With hindsight, what would we have done differently?

We started to embrace CQRS and ES for the first time in one of our products, but we were forced to use a hybrid approach due to time constraints and our lack of experience. We were still using an RDBMS for the event store and the read model. We also generated the read model in a synchronous way. These were mistakes. The short-term benefit over a full or pure implementation of CQRS/ES was quickly annihilated by the added complexity and confusion amongst developers. In consequence, we need to refactor this product in the near future.

We will strictly avoid such hybrid implementations in the future. Either we will fully embrace CQRS and ES, or we will stick with a more traditional architecture.

## Further information

This [blog](http://lostechies.com/gabrielschenker/author/gabrielschenker/) series discusses the details of the implementation.
