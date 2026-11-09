# Development Guidelines

## Core principle

Prefer existing project patterns over inventing new ones.

The existing codebase is the primary source of truth for implementation
style, architecture, testing, mocking, naming and conventions.

Do not replace an existing project convention with a personally preferred
alternative.

## Before implementing

Before modifying or creating code:

1. Inspect the target class and its surrounding code.
2. Search the repository for similar implementations.
3. Search for existing tests for the target class or related classes.
4. Identify the testing framework and libraries actually used by the project.
5. Inspect at least one representative existing implementation.
6. Follow the existing pattern unless there is a concrete reason not to.

Do not assume conventions from the model's general knowledge.

The repository's existing code takes precedence over generic conventions.

## Referenced files

When the user says that a change should be:

- based on X
- similar to X
- like X
- following X
- using X as a reference

you MUST inspect X before implementing the change.

Do not infer X's implementation from its filename, class name or description.

Use X as the primary implementation reference.

## Tests

When creating tests:

1. Find existing tests for the target class.
2. Identify the test framework used by those tests.
3. Identify the assertion library.
4. Identify the mocking framework.
5. Identify test naming and organization conventions.
6. Follow the existing structure.

Do not introduce another test framework when the project already has
an established testing framework.

For example, if the project uses xUnit v3, use xUnit v3.
Do not introduce MSTest, NUnit, or another framework.

When the user explicitly provides an existing test class as a reference,
read that class before creating the new test.

## Minimal changes

Prefer the smallest change that satisfies the request.

Do not refactor unrelated code.

Do not introduce new abstractions, helpers or frameworks unless necessary.

## Verification

After implementing a change:

1. Build the affected project.
2. Run the relevant tests.
3. Inspect failures.
4. Fix problems caused by the implementation.
5. Report what was changed and what was verified.