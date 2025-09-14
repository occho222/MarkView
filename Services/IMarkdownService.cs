using System.Collections.ObjectModel;
using MarkView.Models;

namespace MarkView.Services
{
    public interface IMarkdownService
    {
        string ConvertToHtml(string markdown, bool isDarkTheme, int fontSize);
        ObservableCollection<TocItem> GenerateTableOfContents(string markdown);
    }
}