# jsondiff

[![CI](https://github.com/BoogeyCodes/jsondiff/actions/workflows/ci.yml/badge.svg)](https://github.com/BoogeyCodes/jsondiff/actions/workflows/ci.yml)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

A command-line tool that structurally compares two JSON files and reports what was **added**,
**removed** and **changed**, each located by a precise path. Unlike `diff`, it ignores formatting and
key order and compares the data, so reordered keys and `1` versus `1.0` are not reported as changes.

```console
$ jsondiff before.json after.json
+ /features/2: billing
+ /tracing: true
- /debug: false
~ /database/host: db-1 -> db-2
~ /replicas: 3 -> 5
~ /version: 1.4.0 -> 1.5.0

6 difference(s): 2 added, 1 removed, 3 changed.
```

## Why

Text `diff` on JSON is noisy: it flags indentation, trailing commas and reordered keys as
differences, and it cannot tell you *where* in the structure something changed. jsondiff parses both
files and walks the trees, so it reports real, structural differences and addresses each one with a
[JSON Pointer](https://datatracker.ietf.org/doc/html/rfc6901)-style path like `/database/host` or
`/features/2`. That makes it useful in config review, API contract checks and CI assertions ("fail if
this generated file changed").

## Install

As a .NET global tool:

```bash
dotnet tool install --global jsondiff-cli
jsondiff a.json b.json
```

Or build from source:

```bash
dotnet build -c Release
dotnet run --project src/JsonDiff -- a.json b.json
```

## Usage

```
jsondiff <left.json> <right.json> [options]

  --json                 output the diff as JSON instead of text
  --ignore-array-order   compare arrays as unordered multisets
  -h, --help             show help
```

**Exit codes** make it scriptable: `0` when the files are equal, `1` when they differ, `2` on a usage
or input error. So `jsondiff expected.json actual.json` fails a CI step exactly when there is a real
difference.

### Machine-readable output

```console
$ jsondiff before.json after.json --json
[
  { "kind": "added",   "path": "/tracing",        "oldValue": null,  "newValue": "true" },
  { "kind": "removed", "path": "/debug",          "oldValue": "false", "newValue": null },
  { "kind": "changed", "path": "/database/host",  "oldValue": "db-1",  "newValue": "db-2" }
]
```

Pipe it into `jq` or assert on it in a test.

## How it compares

- **Objects** are matched by key. A key only on the left is *removed*, only on the right is *added*,
  present on both is compared recursively.
- **Arrays** are matched by index by default, so a changed element reports its position and a
  longer/shorter array reports the added/removed tail. With `--ignore-array-order` they are compared
  as multisets, so `[1, 2, 3]` and `[3, 2, 1]` are equal.
- **Scalars** are compared by type and value. Numbers are compared numerically, so `1`, `1.0` and
  `1e0` are equal; a `string` and a `number` with the same text are a change, because their types
  differ.
- **Paths** use JSON Pointer escaping, so a key containing `/` or `~` is encoded unambiguously.

## Project layout

```
src/JsonDiff.Core     the comparison library (no CLI concerns)
├── Change.cs          a single added/removed/changed difference
├── JsonComparer.cs    the recursive structural comparison
└── DiffReport.cs      text and JSON rendering
src/JsonDiff          the CLI: argument parsing, file I/O, exit codes
tests/JsonDiff.Tests  xUnit tests for the comparer, the reports and the CLI
```

The comparison logic lives in a library with no knowledge of the command line, and the CLI is a thin
shell over it. That split is what lets the comparer be unit-tested exhaustively and the CLI be driven
in-process in tests (argument parsing, exit codes and error handling included).

## Testing

```bash
dotnet test
```

Twenty-seven tests: the comparer against scalars, nested objects, arrays by index and as multisets,
numeric equality, type changes and pointer escaping; the reports in both formats; and the CLI end to
end for every exit code and error path. CI runs them on Linux and Windows and packs the tool.

## Ideas for later

- A `--only added,changed` filter.
- A unified side-by-side colour view for terminals.
- Reading one or both documents from stdin for pipelines.

## License

MIT
