# Task Completion Checklist

When completing a coding task in this project, verify the following:

## Before Committing

- [ ] **Code compiles** - `dotnet build` succeeds without errors
- [ ] **Tests pass** - `dotnet test` for affected modules
- [ ] **Architecture rules followed**:
  - Domain has no external dependencies
  - Modules don't reference each other directly
  - Events defined in Shared.Contracts
- [ ] **Naming conventions followed** (see code_style_conventions.md)
- [ ] **One class per file** with matching filename

## For New Features

- [ ] Command/Query in correct folder structure
- [ ] Handler implemented
- [ ] Validator created (FluentValidation)
- [ ] Controller endpoint added (if API-facing)
- [ ] Unit tests written for handler logic

## For Database Changes

- [ ] Migration added to correct module's Infrastructure project
- [ ] Migration applied successfully
- [ ] DbContext configuration updated if needed

## For Cross-Module Communication

- [ ] Event defined in Shared.Contracts
- [ ] Event published from source module
- [ ] Consumer implemented in target module
- [ ] Consumer registered with MassTransit

## For Infrastructure Changes

- [ ] Docker compose updated if new services needed
- [ ] Docker services start successfully: `docker compose up -d`
