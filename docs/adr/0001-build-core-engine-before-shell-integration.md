# Build Core Engine Before Shell Integration

Status: accepted

Icon Replacer will first build and verify the Core Engine and CLI proof harness before implementing the final Explorer context-menu integration. This kept folder and shortcut icon mutation testable while the Modern vs Classic Shell Integration decision was evaluated; ADR-0002 now selects the Modern path for V1.

## Considered Options

- Build shell integration first.
- Build Core Engine first.

## Consequences

- The first implementation slice can proceed without resolving packaging details.
- Shell integration work starts with proven apply/restore behavior.
- The product will not claim context-menu completion until Explorer proof exists.
