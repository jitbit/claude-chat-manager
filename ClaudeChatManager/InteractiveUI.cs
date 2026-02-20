using Spectre.Console;

namespace ClaudeChatManager;

public static class InteractiveUI
{
	private static bool _alwaysDelete;
	public static void Run()
	{
		while (true)
		{
			AnsiConsole.Clear();
			AnsiConsole.MarkupLine("[bold]Claude Chat Manager[/]\n");

			var projects = ChatScanner.GetProjects();
			if (projects.Count == 0)
			{
				AnsiConsole.MarkupLine("[red]No projects found in ~/.claude/projects[/]");
				return;
			}

			var project = AnsiConsole.Prompt(
				new SelectionPrompt<ProjectInfo>()
					.Title("Select a [green]project[/]:")
					.PageSize(20)
					.UseConverter(p => Markup.Escape(p.Name))
					.AddChoices(projects));

			ShowConversations(project);
		}
	}

	private static void ShowConversations(ProjectInfo project)
	{
		var conversations = ChatScanner.GetConversations(project.Path);
		if (conversations.Count == 0)
		{
			AnsiConsole.Clear();
			AnsiConsole.MarkupLine("[yellow]No conversations found.[/]");
			AnsiConsole.MarkupLine("Press any key to go back...");
			Console.ReadKey(true);
			return;
		}

		int selected = 0;
		int scrollOffset = 0;

		while (true)
		{
			DrawConversationList(project, conversations, selected, ref scrollOffset);

			var key = Console.ReadKey(true);

			switch (key.Key)
			{
				case ConsoleKey.UpArrow:
					if (selected > 0) selected--;
					break;
				case ConsoleKey.DownArrow:
					if (selected < conversations.Count - 1) selected++;
					break;
				case ConsoleKey.Enter:
					ShowConversationDetail(conversations[selected]);
					break;
				case ConsoleKey.Delete:
				case ConsoleKey.Backspace:
					if (ConfirmDelete(conversations[selected]))
					{
						ChatScanner.DeleteConversation(conversations[selected]);
						conversations.RemoveAt(selected);
						if (selected >= conversations.Count && selected > 0) selected--;
						if (conversations.Count == 0) return;
					}
					break;
				case ConsoleKey.Escape:
					return;
			}
		}
	}

	private static void DrawConversationList(ProjectInfo project, List<ConversationInfo> conversations, int selected, ref int scrollOffset)
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine($"[bold]{Markup.Escape(project.Name)}[/]  [dim]({conversations.Count} chats)[/]");
		AnsiConsole.MarkupLine("[dim]Up/Down: navigate | Enter: details | Del/Backspace: delete | Esc: back[/]\n");

		// 3 lines used by header above, leave 1 line margin at bottom
		int pageSize = Math.Max(1, Console.WindowHeight - 4);

		// adjust scroll offset to keep selected item visible
		if (selected < scrollOffset)
			scrollOffset = selected;
		if (selected >= scrollOffset + pageSize)
			scrollOffset = selected - pageSize + 1;

		int endIdx = Math.Min(scrollOffset + pageSize, conversations.Count);

		for (int i = scrollOffset; i < endIdx; i++)
		{
			var conv = conversations[i];
			var prefix = i == selected ? "> " : "  ";
			var style = i == selected ? "bold white on blue" : "default";

			var line = $"{conv.Date:yyyy-MM-dd HH:mm}  {conv.Title,-30}  {conv.FirstMessage}";

			int maxWidth = Math.Max(10, Console.WindowWidth - 3);
			if (line.Length > maxWidth)
				line = line[..maxWidth];

			line = line.PadRight(maxWidth);

			AnsiConsole.Markup($"[{style}]{prefix}{Markup.Escape(line)}[/]");
			Console.WriteLine();
		}
	}

	private static void ShowConversationDetail(ConversationInfo conv)
	{
		AnsiConsole.Clear();
		var table = new Table().Border(TableBorder.Rounded);
		table.AddColumn("Property");
		table.AddColumn("Value");
		table.AddRow("Title", Markup.Escape(conv.Title));
		table.AddRow("Date", conv.Date.ToString("yyyy-MM-dd HH:mm"));
		table.AddRow("First Message", Markup.Escape(conv.FirstMessage));
		table.AddRow("File", Markup.Escape(conv.FilePath));
		AnsiConsole.Write(table);
		AnsiConsole.MarkupLine("\nPress any key to go back...");
		Console.ReadKey(true);
	}

	private static bool ConfirmDelete(ConversationInfo conv)
	{
		if (_alwaysDelete) return true;

		var choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title($"Delete [red]{Markup.Escape(conv.Title)}[/] ({conv.Date:yyyy-MM-dd})?")
				.AddChoices("Yes", "No", "Always (don't ask again)"));

		if (choice == "Always (don't ask again)")
		{
			_alwaysDelete = true;
			return true;
		}

		return choice == "Yes";
	}
}
