# Integration with SS-ProfileService

## Overview
SS-AuthService is responsible for user authentication and identity management.
SS-ProfileService manages user profile data.

To ensure that a user's profile is ready upon registration and verification, SS-AuthService publishes events that SS-ProfileService consumes to automatically create a base profile.

## Events Published by SS-AuthService

### UserRegistered
- **Routing Key:** `auth.user.registered`
- **Payload:**
  ```json
  {
    "userId": 123,
    "publicId": "guid-here",
    "email": "user@example.com",
    "fullName": "Full Name"
  }
  ```
- **When Published:** After a user successfully registers (before email verification).

### UserVerified
- **Routing Key:** `auth.user.verified`
- **Payload:**
  ```json
  {
    "userId": 123,
    "publicId": "guid-here",
    "email": "user@example.com"
  }
  ```
- **When Published:** After a user verifies their email address.

## Integration Flow

```mermaid
sequenceDiagram
    participant Auth as SS-AuthService
    participant RB as RabbitMQ<br/>(samstore.events)
    participant Profile as SS-ProfileService
    participant AuthDB as Auth DB<br/>(PostgreSQL)
    participant ProfileDB as Profile DB<br/>(PostgreSQL)

    %% User Registration Flow
    Auth->>AuthDB: 1. Create user (txn begins)
    AuthDB-->>Auth: 2. User record saved
    Auth->>AuthDB: 3. Insert outbox event:<br/>EventType='UserRegistered',<br/>Payload={userId, publicId, email, fullName}
    AuthDB-->>Auth: 4. Outbox event saved (txn commits)
    Auth->>RB: 5. OutboxWorker publishes:<br/>RoutingKey='auth.user.registered',<br/>MessageId='auth-event-123',<br/>CorrelationId='123'
    RB-->>Auth: 6. Message published (confirms)
    Auth->>RB: 7. Outbox event status='published'
    RB->>Profile: 8. Deliver to queue:<br/>ss-profile-service.user-events
    Profile->>ProfileDB: 9. Check InboxEvents<br/>WHERE MessageId='auth-event-123'
    alt Message NOT found
        Profile->>ProfileDB: 10. Check UserProfiles<br/>WHERE UserId=123
        alt Profile NOT exists
            Profile->>ProfileDB: 11. Insert UserProfile<br/>+ Insert InboxEvent<br/>(txn begins)
            ProfileDB-->>Profile: 12. Records saved
            Profile->>RB: 13. Ack message
        else Profile exists
            Profile->>ProfileDB: 14. Insert InboxEvent only<br/>(txn begins)
            ProfileDB-->>Profile: 15. InboxEvent saved
            Profile->>RB: 16. Ack message
        end
    else Message found (duplicate)
        Profile->>RB: 17. Ack message (skip)
    end
```

## Infrastructure
- **Message Broker:** RabbitMQ
- **Exchange:** `samstore.events` (Topic exchange)
- **Outbox Pattern:** SS-AuthService uses an outbox table and worker to reliably publish events.

## Recommendations for Consumers (SS-ProfileService)
1. Create a queue bound to the `samstore.events` exchange with routing key `auth.user.registered` (or `auth.user.verified`).
2. Implement a background worker to consume messages from this queue.
3. Use an idempotent inbox pattern (store processed message IDs) to prevent duplicate processing.
4. On receiving a `UserRegistered` or `UserVerified` event, create a UserProfile if one does not already exist.

## Notes
- The payload structure is considered stable for version 1.0.
- Any changes to the event structure should be versioned and backward compatible.