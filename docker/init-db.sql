-- init-db.sql
-- Creates separate schemas for each module (data isolation)

-- Identity module schema
CREATE SCHEMA IF NOT EXISTS identity;

-- Billing module schema
CREATE SCHEMA IF NOT EXISTS billing;

-- Communications module schema
CREATE SCHEMA IF NOT EXISTS communications;

-- Storage module schema
CREATE SCHEMA IF NOT EXISTS storage;

-- Configuration module schema
CREATE SCHEMA IF NOT EXISTS configuration;

-- Audit schema
CREATE SCHEMA IF NOT EXISTS audit;

-- Grant permissions to the application user
DO $$
DECLARE
    schema_name TEXT;
BEGIN
    FOR schema_name IN SELECT unnest(ARRAY['identity', 'billing', 'communications', 'storage', 'configuration', 'audit'])
    LOOP
        EXECUTE format('GRANT ALL PRIVILEGES ON SCHEMA %I TO %I', schema_name, current_user);
        EXECUTE format('GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I TO %I', schema_name, current_user);
        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL PRIVILEGES ON TABLES TO %I', schema_name, current_user);
    END LOOP;
END $$;
