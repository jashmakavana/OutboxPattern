# OutboxPattern
The dual-write problem: if your API writes to the DB and then tries to publish an event to a broker (RabbitMQ, Kafka, Azure Service Bus), a crash between those two steps loses the event forever. The outbox pattern makes this atomic.
