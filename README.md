# Claude Chat Manager

A terminal UI tool for browsing and deleting [Claude Code](https://docs.anthropic.com/en/docs/claude-code) chat history.

Claude Code stores conversation logs as JSONL files under `~/.claude/projects/`. This tool provides an interactive console interface to explore those conversations by project, view details, and delete ones you no longer need.

## Features

- Browse conversations grouped by project
- View conversation title, date, and first message
- Inspect conversation details
- Delete conversations (with confirmation prompt)
- Keyboard-driven navigation

## Build requirements

- [.NET 10](https://dotnet.microsoft.com/) SDK or later

## Run requirements

- [none] build produces a native AOT binary

## Build & Run

```bash
dotnet run --project ClaudeChatManager
```

To publish a native AOT binary:

```bash
dotnet publish ClaudeChatManager -c Release
```

## Usage

Use arrow keys to navigate, **Enter** to view details, **Delete/Backspace** to remove a conversation, and **Esc** to go back.
