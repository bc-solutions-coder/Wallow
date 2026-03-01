-- init-keycloak-db.sql
-- Creates a separate database for Keycloak within the same Postgres instance.
-- This script runs on first initialization only (when the postgres_data volume is empty).

SELECT 'CREATE DATABASE keycloak_db'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'keycloak_db')\gexec
