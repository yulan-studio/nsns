# Waiver Application

## Purpose

This application allows customers to complete and submit an online liability waiver for an event.

Customers open the waiver page using a URL similar to:

`https://waiver.example.com/?event=summer-camp-2026`

The event determines which activity the customer is registering for.

## Customer Workflow

1. Customer opens the waiver page.
2. The application displays the event name.
3. Customer enters:
   - First Name
   - Last Name
   - WeChat Name (optional)
   - Email
   - Phone Number
4. Customer adds any family members who are included in the waiver.
5. The application displays the waiver agreement loaded from `Content/waiver-agreement.html`.
6. Customer checks the agreement checkbox.
7. Customer types their legal name as an electronic signature.
8. The application generates the signed date and time on the server.
9. Customer clicks Submit.
10. The application saves the submission to MySQL.
11. The application queues:
    - a confirmation email to the customer
    - a notification email to the configured business owner
12. A confirmation page displays the submission reference and signed date.

## Family Members

A submission may include zero to ten family members.

Each family member has:

- First Name
- Last Name
- Relationship (optional)

Family members are stored in a separate table linked to the submission.

## Event Handling

The application uses the `event` query-string parameter.

Example:

`?event=summer-camp-2026`

The application validates the event against a server-side configuration.

The application stores:

- event_code
- event_name

with every submission.

## Agreement

The waiver agreement is stored in:

`Content/waiver-agreement.html`

It is not stored in MySQL.

The application does not maintain agreement versions or snapshots.

## Duplicate Submissions

Duplicate submissions are allowed.

The same customer may submit multiple waivers for the same event or different events.

## Database

The application stores:

- Waiver submissions
- Family members
- Email outbox messages

## Administrator Area

The business owner can sign in to a protected administrator area and view the
most recent waiver submissions.

The submission list can be sorted by event name, customer first name, customer
last name, or customer email.

## Future Features

The application may later include:

- Searching submissions by customer
- Exporting submissions
- Managing the allowed event list

These features are not part of the initial implementation.
