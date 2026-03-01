-- Migration script to move tables from separate schemas to the unified 'communications' schema.
-- Run this BEFORE applying EF Core migrations for CommunicationsDbContext.
-- This script is idempotent and safe to re-run.

-- Create the communications schema
CREATE SCHEMA IF NOT EXISTS communications;

-- Move Email tables from 'email' schema
ALTER TABLE IF EXISTS email.email_messages SET SCHEMA communications;
ALTER TABLE IF EXISTS email.email_preferences SET SCHEMA communications;
ALTER TABLE IF EXISTS email."__EFMigrationsHistory" SET SCHEMA communications;

-- Move Notifications tables from 'notifications' schema
ALTER TABLE IF EXISTS notifications.notifications SET SCHEMA communications;
ALTER TABLE IF EXISTS notifications."__EFMigrationsHistory" SET SCHEMA communications;

-- Move Announcements tables from 'announcements' schema
ALTER TABLE IF EXISTS announcements.announcements SET SCHEMA communications;
ALTER TABLE IF EXISTS announcements.announcement_dismissals SET SCHEMA communications;
ALTER TABLE IF EXISTS announcements.changelog_entries SET SCHEMA communications;
ALTER TABLE IF EXISTS announcements.changelog_items SET SCHEMA communications;
ALTER TABLE IF EXISTS announcements."__EFMigrationsHistory" SET SCHEMA communications;

-- Drop old schemas (only if empty)
-- DROP SCHEMA IF EXISTS email;
-- DROP SCHEMA IF EXISTS notifications;
-- DROP SCHEMA IF EXISTS announcements;
