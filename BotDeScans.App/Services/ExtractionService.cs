namespace BotDeScans.App.Services;

public class ExtractionService
{
    public virtual bool TryExtractGoogleDriveIdFromLink(string link, out string resourceId)
    {
        resourceId = null!;
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || uri.Authority != "drive.google.com")
            return false;

        resourceId = link
            .Replace("?id=", "/")
            .Replace("?usp=sharing", "")
            .Replace("?usp=share_link", "")
            .Split("/")
            .Last();

        return resourceId.Length == 33;
    }
}
