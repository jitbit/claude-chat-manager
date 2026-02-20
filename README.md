# Claude Chat Manager

A terminal UI tool for browsing and deleting [Claude Code](https://docs.anthropic.com/en/docs/claude-code) chat history.

Claude Code stores conversation logs as JSONL files under `~/.claude/projects/`. This tool provides an interactive console interface to explore those conversations by project, view details, and delete ones you no longer need.

## Features

- Browse conversations grouped by project
- View conversation title, date, and first message
- Inspect conversation details
- Delete conversations (with confirmation prompt)
- Keyboard-driven navigation

## Usage

- Download the binary from the [releases](releases) page (MacOS arm64, will build a win/linux binary soon)
- `chmod +x` the binary
- run the binary
```bash
./ClaudeChatManager
```

## Runtime requirements

- [none] build produces a native AOT binary

## Build requirements

- [.NET 10](https://dotnet.microsoft.com/) SDK or later

## Build & Run

```bash
dotnet build --project ClaudeChatManager
```

To publish a native AOT binary:

```bash
dotnet publish ClaudeChatManager -c Release
```

## Usage

Use arrow keys to navigate, **Enter** to view details, **Delete/Backspace** to remove a conversation, and **Esc** to go back.
