namespace BotDeScans.App.Features.GoogleDrive.InternalServices;

public class GoogleDriveQueryBuilder
{
    private string? _mimeType;
    private string? _forbiddenMimeType;
    private string? _name;
    private string? _parentId;

    public GoogleDriveQueryBuilder WithMimeType(string? mimeType)
    {
        _mimeType = mimeType;
        return this;
    }

    public GoogleDriveQueryBuilder WithoutMimeType(string? forbiddenMimeType)
    {
        _forbiddenMimeType = forbiddenMimeType;
        return this;
    }

    public GoogleDriveQueryBuilder WithName(string? name)
    {
        _name = name;
        return this;
    }

    public GoogleDriveQueryBuilder WithParent(string? parentId)
    {
        _parentId = parentId;
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
}