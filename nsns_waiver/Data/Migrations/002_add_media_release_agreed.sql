-- Apply once to an existing NSNS Waiver database.
ALTER TABLE waiver_submissions
    ADD COLUMN media_release_agreed BOOLEAN NOT NULL DEFAULT TRUE
    AFTER agreed;
