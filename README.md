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

## Installation

- Download the 2MB binary from the [releases](/../../releases) page (MacOS arm64 or win-x64)
- `chmod +x` the binary
- run the binary
```bash
./ClaudeChatManager
```

(if Gatekeeper prevents you from launching it on a Mac, open "System Settings → Privacy & Security", scroll down to "Security", click "Open anyway" next to the app)

## Usage

Use arrow keys to navigate, **Enter** to view details, **Delete/Backspace** to remove a conversation, and **Esc** to go back.

## Build requirements

- [.NET 10](https://dotnet.microsoft.com/) SDK or later

I don't have access to a Linux machine at the moment (only Mac/Win) so haven't built a linux version. If you'd like to run on Linux, - install .NET SDK and build it your self

## Building sources

```bash
dotnet build --project ClaudeChatManager
```

Run from sources


```bash
dotnet run --project ClaudeChatManager
```

To publish a compiled native AOT binary:

```bash
dotnet publish ClaudeChatManager -c Release
```
