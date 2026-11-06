# Changelog

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- `DiffStats` record and `DiffReport.Summarize(changes)` exposing added/removed/changed
  counts; the text report now reuses it for its summary line.

## [0.1.0]

### Added
- Structural JSON diff by JSON-Pointer path (added/removed/changed).
- `--json` and `--ignore-array-order`; scriptable exit codes; packable as a dotnet tool.
