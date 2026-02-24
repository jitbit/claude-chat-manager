using Spectre.Console;

namespace ClaudeChatManager;

/// <summary>
/// Entry point for the interactive TUI. Shows a project picker in a loop;
/// selecting a project opens its conversation list.
/// </summary>
public static class InteractiveUI
{
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

			new ConversationListScreen(project).Run();
		}
	}
}

/// <summary>
/// Full-screen conversation list with keyboard navigation, inline filtering,
/// and delete support. Focus can be on the filter input or on a list item.
/// </summary>
public class ConversationListScreen
{
	private readonly ProjectInfo _project;
	private List<ConversationInfo> _allConversations;

	// Selection state: _selected is the highlighted list index (null = nothing highlighted),
	// _filterFocused means the filter textbox has focus instead of the list.
	private int? _selected = 0;
	private int _scrollOffset;
	private string _filter = "";
	private bool _filterFocused;

	// When true, skip the delete confirmation prompt for the rest of the session
	private bool _alwaysDelete;

	public ConversationListScreen(ProjectInfo project)
	{
		_project = project;
		_allConversations = ChatScanner.GetConversations(project.Path);
	}

	/// <summary>Main input loop. Returns when user presses Esc (with no active filter).</summary>
	public void Run()
	{
		if (_allConversations.Count == 0)
		{
			AnsiConsole.Clear();
			AnsiConsole.MarkupLine("[yellow]No conversations found.[/]");
			AnsiConsole.MarkupLine("Press any key to go back...");
			Console.ReadKey(true);
			return;
		}

		while (true)
		{
			var filtered = GetFilteredConversations();

			// Clamp selection if the filter narrowed the list
			if (_selected.HasValue && _selected.Value >= filtered.Count)
				_selected = filtered.Count > 0 ? filtered.Count - 1 : null;

			Draw(filtered);

			var key = Console.ReadKey(true);
			if (HandleKey(key, filtered))
				return; // signal to exit this screen
		}
	}

	private List<ConversationInfo> GetFilteredConversations()
	{
		if (string.IsNullOrEmpty(_filter))
			return _allConversations;

		return _allConversations
			.Where(c => c.FirstMessage.Contains(_filter, StringComparison.OrdinalIgnoreCase))
			.ToList();
	}

	/// <summary>
	/// Processes a single keypress. Returns true if the screen should close.
	/// </summary>
	private bool HandleKey(ConsoleKeyInfo key, List<ConversationInfo> filtered)
	{
		int pageSize = Math.Max(1, Console.WindowHeight - 5);

		switch (key.Key)
		{
			// --- Navigation ---
			case ConsoleKey.UpArrow:
				if (_selected.HasValue && _selected.Value > 0)
					_selected--;
				else if (_filter.Length > 0) // at top of list, move focus to filter
					FocusOnFilter();
				break;
			case ConsoleKey.DownArrow:
				if (_filterFocused && filtered.Count > 0)
					FocusOnList(0);
				else if (_selected.HasValue && _selected.Value < filtered.Count - 1)
					_selected++;
				break;
			case ConsoleKey.PageUp:
				if (filtered.Count > 0)
					FocusOnList(Math.Max(0, (_selected ?? 0) - pageSize));
				break;
			case ConsoleKey.PageDown:
				if (filtered.Count > 0)
					FocusOnList(Math.Min(filtered.Count - 1, (_selected ?? 0) + pageSize));
				break;
			case ConsoleKey.Home:
				if (filtered.Count > 0)
					FocusOnList(0);
				break;
			case ConsoleKey.End:
				if (filtered.Count > 0)
					FocusOnList(filtered.Count - 1);
				break;

			// --- Actions ---
			case ConsoleKey.Enter:
				if (_selected.HasValue)
					ShowConversationDetail(filtered[_selected.Value]);
				break;
			case ConsoleKey.Delete:
				if (_selected.HasValue && TryDeleteSelected(filtered))
					return true; // no conversations left
				break;
			case ConsoleKey.Backspace:
				if (_filterFocused)
					HandleBackspaceInFilter();
				else if (_selected.HasValue && TryDeleteSelected(filtered))
					return true;
				break;

			// --- Filter / exit ---
			case ConsoleKey.Escape:
				if (_filter.Length > 0)
					ClearFilter();
				else
					return true; // exit screen
				break;
			default:
				if (key.KeyChar >= ' ')
					AppendToFilter(key.KeyChar);
				break;
		}

		return false;
	}

	private void FocusOnList(int index)
	{
		_selected = index;
		_filterFocused = false;
	}

	private void FocusOnFilter()
	{
		_selected = null;
		_filterFocused = true;
	}

	private void AppendToFilter(char c)
	{
		_filter += c;
		FocusOnFilter();
		_scrollOffset = 0;
	}

	private void HandleBackspaceInFilter()
	{
		if (_filter.Length > 0)
		{
			_filter = _filter[..^1];
			if (_filter.Length == 0) // filter emptied, snap back to list
				FocusOnList(0);
		}
	}

	private void ClearFilter()
	{
		_filter = "";
		FocusOnList(0);
		_scrollOffset = 0;
	}

	/// <summary>
	/// Confirms and deletes the currently selected conversation.
	/// Returns true if no conversations remain (caller should exit).
	/// </summary>
	private bool TryDeleteSelected(List<ConversationInfo> filtered)
	{
		if (!_selected.HasValue)
			return false;

		var conv = filtered[_selected.Value];
		if (!ConfirmDelete(conv))
			return false;

		_allConversations.Remove(conv);
		ChatScanner.DeleteConversation(conv);
		return _allConversations.Count == 0;
	}

	private void Draw(List<ConversationInfo> conversations)
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine($"[bold]{Markup.Escape(_project.Name)}[/]  [dim]({conversations.Count} chats)[/]");

		// Filter bar (only visible when filter is active)
		if (_filter.Length > 0)
		{
			var filterStyle = _filterFocused ? "bold yellow" : "yellow";
			var cursor = _filterFocused ? "_" : "";
			AnsiConsole.MarkupLine($"[{filterStyle}]Filter: {Markup.Escape(_filter)}{cursor}[/]  [dim](Esc to clear)[/]");
		}
		AnsiConsole.MarkupLine("[dim]Up/Down: navigate | Enter: details | Del/Backspace: delete | Esc: back | Type to filter[/]\n");

		// Scroll the viewport to keep the selection visible
		int headerLines = _filter.Length > 0 ? 5 : 4;
		int pageSize = Math.Max(1, Console.WindowHeight - headerLines);

		if (_selected.HasValue)
		{
			if (_selected.Value < _scrollOffset)
				_scrollOffset = _selected.Value;
			if (_selected.Value >= _scrollOffset + pageSize)
				_scrollOffset = _selected.Value - pageSize + 1;
		}

		// Render visible rows
		int endIdx = Math.Min(_scrollOffset + pageSize, conversations.Count);
		for (int i = _scrollOffset; i < endIdx; i++)
		{
			var conv = conversations[i];
			bool isSelected = _selected.HasValue && i == _selected.Value;
			var prefix = isSelected ? "> " : "  ";
			var style = isSelected ? "bold white on blue" : "default";

			var line = $"{conv.Date:yyyy-MM-dd HH:mm}  {conv.FirstMessage}";

			// Truncate and pad to fill the row width
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

	private bool ConfirmDelete(ConversationInfo conv)
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
