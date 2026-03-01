#!/bin/bash
docker compose -f docker/docker-compose.yml -f docker/docker-compose.dev.yml --env-file docker/.env logs -f "$@"
