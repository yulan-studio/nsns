-- NSNS Waiver initial schema. Safe to run against a new empty database.
CREATE TABLE IF NOT EXISTS waiver_submissions (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    submission_reference CHAR(36) NOT NULL,
    event_code VARCHAR(100) NOT NULL,
    event_name VARCHAR(200) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    wechat_name VARCHAR(100) NULL,
    email VARCHAR(320) NOT NULL,
    normalized_email VARCHAR(320) NOT NULL,
    phone VARCHAR(40) NOT NULL,
    normalized_phone VARCHAR(40) NOT NULL,
    signature_name VARCHAR(200) NOT NULL,
    agreed BOOLEAN NOT NULL,
    media_release_agreed BOOLEAN NOT NULL DEFAULT TRUE,
    signed_at_utc DATETIME(6) NOT NULL,
    ip_address VARCHAR(45) NULL,
    user_agent VARCHAR(500) NULL,
    created_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (id),
    UNIQUE KEY ux_waiver_submissions_submission_reference (submission_reference),
    KEY ix_waiver_submissions_event_code (event_code),
    KEY ix_waiver_submissions_normalized_email (normalized_email),
    KEY ix_waiver_submissions_normalized_phone (normalized_phone),
    KEY ix_waiver_submissions_event_email (event_code, normalized_email),
    KEY ix_waiver_submissions_signed_at_utc (signed_at_utc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS waiver_family_members (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    submission_id BIGINT UNSIGNED NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    relationship VARCHAR(100) NULL,
    created_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (id),
    KEY ix_waiver_family_members_submission_id (submission_id),
    CONSTRAINT fk_waiver_family_members_submission
        FOREIGN KEY (submission_id) REFERENCES waiver_submissions (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS email_outbox (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    submission_id BIGINT UNSIGNED NOT NULL,
    message_type VARCHAR(30) NOT NULL,
    recipient_email VARCHAR(320) NOT NULL,
    subject VARCHAR(300) NOT NULL,
    body_html LONGTEXT NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    attempt_count INT UNSIGNED NOT NULL DEFAULT 0,
    next_attempt_at_utc DATETIME(6) NULL,
    last_attempt_at_utc DATETIME(6) NULL,
    sent_at_utc DATETIME(6) NULL,
    last_error TEXT NULL,
    created_at_utc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (id),
    KEY ix_email_outbox_submission_id (submission_id),
    KEY ix_email_outbox_status_next_attempt (status, next_attempt_at_utc),
    KEY ix_email_outbox_created_at_utc (created_at_utc),
    CONSTRAINT fk_email_outbox_submission
        FOREIGN KEY (submission_id) REFERENCES waiver_submissions (id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
