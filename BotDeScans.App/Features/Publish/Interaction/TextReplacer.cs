namespace BotDeScans.App.Features.Publish.Interaction;

public class TextReplacer
{
    public static readonly IReadOnlyDictionary<string, Func<State, string?>>
        ReplaceRules = new Dictionary<string, Func<State, string?>>()
        {
            { "TITLE",                 state => state.Title.Name },
            { "CHAPTER_NAME",          state => state.ChapterInfo.ChapterName },
            { "CHAPTER_NUMBER",        state => state.ChapterInfo.ChapterNumber },
            { "VOLUME_NUMBER",         state => state.ChapterInfo.ChapterVolume },
            { "MESSAGE",               state => state.ChapterInfo.Message },
            { "MEGA_ZIP_LINK",         state => state.MegaZipLink },
            { "MEGA_PDF_LINK",         state => state.MegaPdfLink },
            { "BOX_ZIP_LINK",          state => state.BoxZipLink },
            { "BOX_PDF_LINK",          state => state.BoxPdfLink },
            { "GOOGLE_DRIVE_ZIP_LINK", state => state.DriveZipLink },
            { "GOOGLE_DRIVE_PDF_LINK", state => state.DrivePdfLink },
            { "MANGADEX_LINK",         state => state.MangaDexLink },
            { "SAKURAMANGAS_LINK",     state => state.SakuraMangasLink },
            { "BLOGGER_COVER_IMAGE",   state => state.BloggerImageAsBase64 },
            { "BOX_PDF_READER",        state => state.BoxPdfReaderKey }
        };

    public virtual string Replace(string text, State state)
    {
        foreach (var rule in ReplaceRules)
        {
            var replaceKey = $"!##{rule.Key}##!";
            var replaceValue = rule.Value(state);
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
