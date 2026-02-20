# Claude Chat Manager

A terminal UI tool for browsing and deleting [Claude Code](https://docs.anthropic.com/en/docs/claude-code) chat history.

Claude Code stores conversation logs as JSONL files under `~/.claude/projects/` and provides no tools to manage/clear AI session history, which makes it painful to go back to something. This tiny app provides an interactive console interface to explore those conversations by project, view details, and delete ones you no longer need.

<img width="867" height="301" alt="image" src="https://github.com/user-attachments/assets/05c150ce-5044-4c1d-ab52-d1fd861c5116" />


## Features

- Browse conversations grouped by project
- View conversation title, date, and first message
- Inspect conversation details
- Delete conversations (with confirmation prompt)
- Keyboard-driven navigation

## Usage

- Download the binary from the [releases](/../../releases) page (MacOS arm64, will build a win/linux binary soon)
- `chmod +x` the binary
- run the binary
```bash
./ClaudeChatManager
```

## Runtime requirements

- [none] build produces a native AOT binary

## Build requirements

- [.NET 10](https://dotnet.microsoft.com/) SDK or later

## Building srouces

```bash
dotnet build --project ClaudeChatManager
```

To publish a native AOT binary:

```bash
dotnet publish ClaudeChatManager -c Release
```

## Usage

Use arrow keys to navigate, **Enter** to view details, **Delete/Backspace** to remove a conversation, and **Esc** to go back.
