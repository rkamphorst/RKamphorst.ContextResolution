# Context Resolution

Most operations require some data to be retrieved prior to performing the
operation logic. We call this data to be retrieved the *context* of the
operation.

For example, the operation: *given a list of product IDs and quantities,
calculate the total price*. Prior to multiplying with quantities and summing
into a total, the needed *context* is the list of current prices for the given
products IDs.

We could of course just create a database query that fetches the context as part
of the operation itself. However, what should we do when this context needs to
come from multiple external sources ("other domains") that change over time?
What if we want to use multiple slightly different versions of the same
operation, each having similar but slightly different context needs?

We want to *decouple* the operation(s) from the other domains. This will make it
easier to add or update sources and operations indepentently.

This repository provides a way in which:

* an operation can request context without knowing where the context comes from;
* context sources don't have to be aware of which operation(s) consume them;
* given    
  * multiple operations to execute that require context, and    
  * multiple context sources that may in turn require other context,
  
any context source is queried at most once, and context sources are queried in the correct order.



