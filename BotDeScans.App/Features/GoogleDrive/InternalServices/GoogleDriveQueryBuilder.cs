namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDriveQueryBuilder
{
    private string? _mimeType;
    private string? _forbiddenMimeType;
    private string? _name;
    private string? _parentId;

    public GoogleDriveQueryBuilder WithMimeType(string? mimeType)
    {
        _mimeType = EscapeSpecialCharacters(mimeType);
        return this;
    }

    public GoogleDriveQueryBuilder WithoutMimeType(string? forbiddenMimeType)
    {
        _forbiddenMimeType = EscapeSpecialCharacters(forbiddenMimeType);
        return this;
    }

    public GoogleDriveQueryBuilder WithName(string? name)
    {
        _name = EscapeSpecialCharacters(name);
        return this;
    }

    public GoogleDriveQueryBuilder WithParent(string? parentId)
    {
        _parentId = EscapeSpecialCharacters(parentId);
        return this;
    }

    public string Build()
    {
        var conditions = new List<string> { "trashed = false" };

        if (_mimeType is not null)
            conditions.Add($"mimeType = '{_mimeType}'");

        if (_forbiddenMimeType is not null)
            conditions.Add($"mimeType != '{_forbiddenMimeType}'");

        if (_name is not null)
            conditions.Add($"name = '{_name}'");

        conditions.Add($"'{_parentId ?? GoogleDriveSettingsService.BaseFolderId}' in parents");

        return string.Join(" and ", conditions);
    }

    /// <summary>
    /// Escapes single quotes and backslashes in the input string.
    /// https://developers.google.com/workspace/drive/api/guides/search-files
    /// </summary>
    /// <param name="input">The string to process for escaping special characters.</param>
    /// <returns>A new string with single quotes and backslashes escaped, or null if the input is null.</returns>
    private static string? EscapeSpecialCharacters(string? input)
        => input?.Replace("\\", "\\\\")
                 .Replace("'", "\\'");
}