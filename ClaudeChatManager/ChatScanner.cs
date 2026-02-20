using System.Text.Json;

namespace ClaudeChatManager;

public static class ChatScanner
{
	private static readonly string ClaudeDir = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");

	private static readonly string ProjectsDir = Path.Combine(ClaudeDir, "projects");

	public static List<ProjectInfo> GetProjects()
	{
		if (!Directory.Exists(ProjectsDir))
			return [];

		return Directory.GetDirectories(ProjectsDir)
			.Select(dir =>
			{
				var folderName = Path.GetFileName(dir);
				// folder name is the path with dashes instead of slashes: -Users-jazz-project
				var projectPath = "/" + folderName.TrimStart('-').Replace('-', '/');
				return new ProjectInfo(projectPath, dir);
			})
			.OrderBy(p => p.Name)
			.ToList();
	}

	public static List<ConversationInfo> GetConversations(string projectDir)
	{
		var conversations = new List<ConversationInfo>();

		foreach (var file in Directory.GetFiles(projectDir, "*.jsonl"))
		{
			try
			{
				var conv = ParseConversation(file);
				if (conv != null)
					conversations.Add(conv);
			}
			catch
			{
				// skip corrupt files
			}
		}

		return conversations.OrderByDescending(c => c.Date).ToList();
	}

	private static ConversationInfo? ParseConversation(string filePath)
	{
		string? firstUserMessage = null;
		string? title = null;
		DateTime? timestamp = null;

		foreach (var line in File.ReadLines(filePath))
		{
			if (string.IsNullOrWhiteSpace(line)) continue;

			JsonlMessage? msg;
			try
			{
				msg = JsonSerializer.Deserialize(line, AppJsonContext.Default.JsonlMessage);
			}
			catch
			{
				continue;
			}

			if (msg == null) continue;

			// grab the timestamp from the first message
			timestamp ??= ParseTimestamp(msg.Timestamp) ?? File.GetCreationTime(filePath);

			// grab slug from first assistant message that has one
			if (title == null && !string.IsNullOrEmpty(msg.Slug))
				title = FormatSlug(msg.Slug);

			// grab first user message text
			if (firstUserMessage == null && msg.Type == "user" && msg.Message?.Content != null)
			{
				var text = ExtractTextContent(msg.Message.Content.Value);
				if (!string.IsNullOrWhiteSpace(text))
					firstUserMessage = text;
			}

			// once we have everything, stop reading
			if (title != null && firstUserMessage != null && timestamp != null)
				break;
		}

		if (firstUserMessage == null) return null;

		title ??= "(untitled)";
		timestamp ??= File.GetCreationTime(filePath);

		// truncate long messages for display
		if (firstUserMessage.Length > 120)
			firstUserMessage = firstUserMessage[..120] + "...";

		// collapse newlines for single-line display
		firstUserMessage = firstUserMessage.ReplaceLineEndings(" ");

		return new ConversationInfo(filePath, title, timestamp.Value, firstUserMessage);
	}

	private static string ExtractTextContent(JsonElement content)
	{
		// content can be a string or an array of content blocks
		if (content.ValueKind == JsonValueKind.String)
		{
			var s = content.GetString() ?? "";
			return s.TrimStart().StartsWith('<') ? "" : s;
		}

		if (content.ValueKind == JsonValueKind.Array)
		{
			foreach (var block in content.EnumerateArray())
			{
				if (block.TryGetProperty("type", out var type) && type.GetString() == "text"
					&& block.TryGetProperty("text", out var text))
				{
					var s = text.GetString() ?? "";
					// skip system-injected content blocks (e.g. <ide_selection>, <system-reminder>)
					if (!string.IsNullOrWhiteSpace(s) && !s.TrimStart().StartsWith('<'))
						return s;
				}
			}
		}

		return "";
	}

	private static string FormatSlug(string slug)
	{
		// "proud-tinkering-tiger" -> "Proud Tinkering Tiger"
		return string.Join(' ', slug.Split('-').Select(w =>
			w.Length > 0 ? char.ToUpper(w[0]) + w[1..] : w));
	}

	private static DateTime? ParseTimestamp(string? ts)
	{
		if (ts == null) return null;
		return DateTime.TryParse(ts, out var dt) ? dt.ToLocalTime() : null;
	}

	public static void DeleteConversation(ConversationInfo conv)
	{
		// delete the .jsonl file
		if (File.Exists(conv.FilePath))
			File.Delete(conv.FilePath);

		// delete companion directory (subagents etc.) if it exists
		var companionDir = Path.ChangeExtension(conv.FilePath, null);
		if (Directory.Exists(companionDir))
			Directory.Delete(companionDir, recursive: true);
	}
}
