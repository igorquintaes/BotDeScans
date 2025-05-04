using BotDeScans.App.Features.Publish.State;

namespace BotDeScans.App.Features.Publish;

public class PublishReplacerService(PublishState publishState)
{
    public static readonly IReadOnlyDictionary<string, Func<PublishState, string?>>
        ReplaceRules = new Dictionary<string, Func<PublishState, string?>>()
        {
            { "TITLE",                 state => state.Title.Name },
            { "CHAPTER_NAME",          state => state.ReleaseInfo.ChapterName },
            { "CHAPTER_NUMBER",        state => state.ReleaseInfo.ChapterNumber },
            { "VOLUME_NUMBER",         state => state.ReleaseInfo.ChapterVolume },
            { "MESSAGE",               state => state.ReleaseInfo.Message },
            { "BLOGGER_COVER_IMAGE",   state => state.InternalData.BloggerImageAsBase64 },
            { "MEGA_ZIP_LINK",         state => state.ReleaseLinks.MegaZip },
            { "MEGA_PDF_LINK",         state => state.ReleaseLinks.MegaPdf },
            { "BOX_ZIP_LINK",          state => state.ReleaseLinks.BoxZip },
            { "BOX_PDF_LINK",          state => state.ReleaseLinks.BoxPdf },
            { "GOOGLE_DRIVE_ZIP_LINK", state => state.ReleaseLinks.DriveZip },
            { "GOOGLE_DRIVE_PDF_LINK", state => state.ReleaseLinks.DrivePdf },
            { "MANGADEX_LINK",         state => state.ReleaseLinks.MangaDex },
            { "SAKURAMANGAS_LINK",     state => state.ReleaseLinks.SakuraMangas },
            { "BOX_PDF_READER",        state => state.InternalData.BoxPdfReaderKey }
        };

    public virtual string Replace(string text)
    {
        foreach (var rule in ReplaceRules)
        {
            var replaceKey = $"!##{rule.Key}##!";
            var replaceValue = rule.Value(publishState);
            text = text.Replace(replaceKey, replaceValue);

            var startRemoveIfEmptyKey = $"!##START_REMOVE_IF_EMPTY_{rule.Key}##!";
            var endRemoveIfEmptyKey = $"!##END_REMOVE_IF_EMPTY_{rule.Key}##!";
            if (text.Contains(startRemoveIfEmptyKey) &&
                text.Contains(endRemoveIfEmptyKey) &&
                string.IsNullOrWhiteSpace(replaceValue))
            {
                var startIndex = text.IndexOf(startRemoveIfEmptyKey);
                var endIndex = text.IndexOf(endRemoveIfEmptyKey) + endRemoveIfEmptyKey.Length - startIndex;
                text = text.Remove(startIndex, endIndex);
            }
            else
            {
                text = text.Replace(startRemoveIfEmptyKey, string.Empty)
                           .Replace(endRemoveIfEmptyKey, string.Empty);
            }

        }

        return text;
    }
}
