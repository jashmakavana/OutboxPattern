# OutboxPattern
The dual-write problem: if your API writes to the DB and then tries to publish an event to a broker (RabbitMQ, Kafka, Azure Service Bus), a crash between those two steps loses the event forever. The outbox pattern makes this atomic.

personal Token for git 
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

<img width="2720" height="2480" alt="outbox_pattern_architecture" src="https://github.com/user-attachments/assets/8719306f-c354-442f-a3e9-b36b631da370" />
