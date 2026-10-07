CREATE TABLE expenses (
    id UUID PRIMARY KEY,
    amount NUMERIC(12,2) NOT NULL,
    description TEXT NOT NULL
);

CREATE TABLE idempotency_keys (
    key UUID PRIMARY KEY,
    expense_id UUID NOT NULL REFERENCES expenses(id)
);

CREATE TABLE outbox_messages (
    message_id   UUID PRIMARY KEY,
    type         TEXT NOT NULL,
    payload      TEXT NOT NULL,
    created_at   TIMESTAMPTZ NOT NULL,
    published_at TIMESTAMPTZ DEFAULT NULL
);