# Demo Smoke Checklist

## OpenAI Provider (No Azure)
- Set `Chat__Provider=OpenAI`.
- Set `OpenAI__ApiKey` and `OpenAI__Model` (use a test model).
- Open Phase 2 chat and send a message.
- Confirm the assistant reply is returned and no errors are shown.

## Fallback Behavior (Rules)
- Remove `OpenAI__ApiKey` while keeping `Chat__Provider=OpenAI`.
- Send a message in chat.
- Confirm a friendly rule-based reply appears and no errors are shown.

## Ticket Flow (Phase 2 -> Service Desk)
- Create a ticket from the chat.
- Confirm ticket appears in Service Desk Swagger with `transcriptJson` populated.
