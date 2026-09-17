# Copilot Instructions

## Shared Instructions

Shared Copilot instruction files are maintained centrally in the [.github](https://github.com/f2calv/.github) repository under `instructions/`, and are applied to every workspace from the VS Code user profile via `~/.copilot/instructions`. They are deliberately not copied into this repository, so a change there takes effect everywhere without a pull request here.

Everything below is specific to this repository.

## Repository Purpose

This repository contains the .NET client and ASP.NET Core integration packages for the signal-cli REST API, with unit and integration tests and a Generic Host sample.

## Signal Privacy

Beyond the general secret-redaction rule in `csharp.instructions.md`, never log Signal phone numbers, sender or recipient identities, group membership, message content, attachments, Basic Auth credentials, or registration data.

The same applies to CI and test output. Use synthetic E.164 numbers in tracked configuration and examples.
