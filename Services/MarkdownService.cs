using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Markdig;
using MarkView.Models;

namespace MarkView.Services
{
    public class MarkdownService : IMarkdownService
    {
        private readonly MarkdownPipeline _pipeline;
        private readonly IPlantUmlService _plantUmlService;

        public MarkdownService(IPlantUmlService plantUmlService)
        {
            _plantUmlService = plantUmlService;
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .Build();
        }

        public string ConvertToHtml(string markdown, bool isDarkTheme, int fontSize)
        {
            // PlantUMLコードブロックを事前処理
            var processedMarkdown = ProcessPlantUmlCodeBlocks(markdown);

            var htmlContent = Markdown.ToHtml(processedMarkdown, _pipeline);
            return CreateHtmlTemplate(htmlContent, isDarkTheme, fontSize);
        }

        public ObservableCollection<TocItem> GenerateTableOfContents(string markdown)
        {
            var tocItems = new ObservableCollection<TocItem>();
            var lines = markdown.Split('\n');
            var tocStack = new Stack<TocItem>();

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
                if (match.Success)
                {
                    var level = match.Groups[1].Value.Length;
                    var title = match.Groups[2].Value;
                    var tocItem = new TocItem
                    {
                        Title = title,
                        Level = level,
                        Children = new ObservableCollection<TocItem>()
                    };

                    while (tocStack.Count > 0 && tocStack.Peek().Level >= level)
                    {
                        tocStack.Pop();
                    }

                    if (tocStack.Count == 0)
                    {
                        tocItems.Add(tocItem);
                    }
                    else
                    {
                        tocStack.Peek().Children?.Add(tocItem);
                    }

                    tocStack.Push(tocItem);
                }
            }

            return tocItems;
        }

        private string ProcessPlantUmlCodeBlocks(string markdown)
        {
            try
            {
                // PlantUMLコードブロックを検出してHTML画像に変換する正規表現
                var plantUmlPattern = @"```(?:plantuml|puml|uml)\s*\n([\s\S]*?)\n```";

                return Regex.Replace(markdown, plantUmlPattern, match =>
                {
                    var plantUmlCode = match.Groups[1].Value.Trim();

                    if (string.IsNullOrWhiteSpace(plantUmlCode))
                        return match.Value; // 元のコードブロックをそのまま返す

                    try
                    {
                        // PlantUMLサービスでHTML画像を生成
                        var htmlImage = _plantUmlService.ProcessPlantUmlCode(plantUmlCode);

                        // マークダウンとして認識されるようにHTMLを包む
                        return $"\n\n{htmlImage}\n\n";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PlantUML処理エラー: {ex.Message}");
                        return match.Value; // エラー時は元のコードブロックを返す
                    }
                }, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlantUMLコードブロック処理エラー: {ex.Message}");
                return markdown; // エラー時は元のマークダウンを返す
            }
        }

        private static string CreateHtmlTemplate(string content, bool isDarkTheme, int fontSize)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Markdown Preview</title>
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/{(isDarkTheme ? "github-dark" : "github")}.min.css"">
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js""></script>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
            font-size: {fontSize}px;
            line-height: 1.6;
            color: {(isDarkTheme ? "#e6edf3" : "#1f2328")};
            background-color: {(isDarkTheme ? "#0d1117" : "#ffffff")};
            max-width: none;
            margin: 0;
            padding: 20px;
        }}

        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
            border-bottom: {(isDarkTheme ? "1px solid #30363d" : "1px solid #d0d7de")};
            padding-bottom: 0.3em;
        }}

        h1 {{ font-size: 2em; }}
        h2 {{ font-size: 1.5em; }}
        h3 {{ font-size: 1.25em; }}
        h4 {{ font-size: 1em; }}
        h5 {{ font-size: 0.875em; }}
        h6 {{ font-size: 0.85em; color: {(isDarkTheme ? "#7d8590" : "#656d76")}; }}

        p {{ margin-bottom: 16px; }}

        code {{
            background-color: {(isDarkTheme ? "#161b22" : "#f6f8fa")};
            color: {(isDarkTheme ? "#f0f6fc" : "#1f2328")};
            padding: 0.2em 0.4em;
            border-radius: 6px;
            font-size: 85%;
            font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
        }}

        pre {{
            background-color: {(isDarkTheme ? "#161b22" : "#f6f8fa")};
            border-radius: 6px;
            padding: 16px;
            overflow: auto;
            margin-bottom: 16px;
        }}

        pre code {{
            background-color: transparent;
            padding: 0;
            border-radius: 0;
            font-size: inherit;
        }}

        blockquote {{
            margin: 0 0 16px 0;
            padding: 0 1em;
            color: {(isDarkTheme ? "#7d8590" : "#656d76")};
            border-left: 0.25em solid {(isDarkTheme ? "#30363d" : "#d0d7de")};
        }}

        table {{
            border-collapse: collapse;
            border-spacing: 0;
            margin-bottom: 16px;
            width: 100%;
        }}

        table th, table td {{
            padding: 6px 13px;
            border: 1px solid {(isDarkTheme ? "#30363d" : "#d0d7de")};
        }}

        table th {{
            background-color: {(isDarkTheme ? "#161b22" : "#f6f8fa")};
            font-weight: 600;
        }}

        ul, ol {{ margin-bottom: 16px; }}

        img {{
            max-width: 100%;
            height: auto;
        }}

        a {{
            color: {(isDarkTheme ? "#58a6ff" : "#0969da")};
            text-decoration: none;
        }}

        a:hover {{
            text-decoration: underline;
        }}

        hr {{
            height: 0.25em;
            padding: 0;
            margin: 24px 0;
            background-color: {(isDarkTheme ? "#30363d" : "#d0d7de")};
            border: 0;
        }}
    </style>
</head>
<body>
    {content}
    <script>
        hljs.highlightAll();

        document.addEventListener('click', function(e) {{
            if (e.target.tagName === 'A') {{
                e.preventDefault();
                window.external.OpenLink(e.target.href);
            }}
        }});
    </script>
</body>
</html>";
        }
    }
}