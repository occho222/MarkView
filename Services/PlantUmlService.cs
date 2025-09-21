using System.IO;
using System.IO.Compression;
using System.Text;

namespace MarkView.Services
{
    public class PlantUmlService : IPlantUmlService
    {
        private const string PlantUmlServerUrl = "http://www.plantuml.com/plantuml/png/";

        // PlantUML用のBase64文字セット
        private const string Base64Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_";

        public string ProcessPlantUmlCode(string plantUmlCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(plantUmlCode))
                    return string.Empty;

                var imageUrl = GeneratePlantUmlImageUrl(plantUmlCode);

                // HTMLのimg要素として返す
                return $"<img src=\"{imageUrl}\" alt=\"PlantUML Diagram\" style=\"max-width: 100%; height: auto; border: 1px solid #ddd; border-radius: 4px; padding: 8px; margin: 8px 0; background: white;\" />";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlantUML処理エラー: {ex.Message}");

                // エラー時はコードブロックとして表示
                return $"<pre><code class=\"language-plantuml\">{HtmlEncode(plantUmlCode)}</code></pre>";
            }
        }

        public string GeneratePlantUmlImageUrl(string plantUmlCode)
        {
            try
            {
                // PlantUMLコードを前処理
                var processedCode = PreprocessPlantUmlCode(plantUmlCode);

                // エンコード
                var encoded = EncodeForPlantUml(processedCode);

                return PlantUmlServerUrl + encoded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlantUML URL生成エラー: {ex.Message}");
                throw;
            }
        }

        public bool IsPlantUmlCodeBlock(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return false;

            var normalizedLanguage = language.Trim().ToLowerInvariant();

            return normalizedLanguage == "plantuml" ||
                   normalizedLanguage == "puml" ||
                   normalizedLanguage == "uml";
        }

        public string EncodeForPlantUml(string content)
        {
            try
            {
                // UTF-8バイト配列に変換
                var bytes = Encoding.UTF8.GetBytes(content);

                // Deflate圧縮
                using var output = new MemoryStream();
                using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
                {
                    deflate.Write(bytes, 0, bytes.Length);
                }

                var compressed = output.ToArray();

                // PlantUML独自のBase64エンコード
                return EncodeBase64PlantUml(compressed);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlantUMLエンコードエラー: {ex.Message}");
                throw;
            }
        }

        private string PreprocessPlantUmlCode(string plantUmlCode)
        {
            var lines = plantUmlCode.Split('\n', StringSplitOptions.None);
            var processedLines = new List<string>();

            var hasStartTag = false;
            var hasEndTag = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("@startuml", StringComparison.OrdinalIgnoreCase))
                {
                    hasStartTag = true;
                }
                else if (trimmedLine.StartsWith("@enduml", StringComparison.OrdinalIgnoreCase))
                {
                    hasEndTag = true;
                }

                processedLines.Add(line);
            }

            // @startuml/@endumlタグが不足している場合は追加
            if (!hasStartTag)
            {
                processedLines.Insert(0, "@startuml");
            }

            if (!hasEndTag)
            {
                processedLines.Add("@enduml");
            }

            return string.Join("\n", processedLines);
        }

        private string EncodeBase64PlantUml(byte[] data)
        {
            var result = new StringBuilder();

            for (int i = 0; i < data.Length; i += 3)
            {
                var b1 = data[i];
                var b2 = (i + 1 < data.Length) ? data[i + 1] : (byte)0;
                var b3 = (i + 2 < data.Length) ? data[i + 2] : (byte)0;

                var c1 = b1 >> 2;
                var c2 = ((b1 & 0x3) << 4) | (b2 >> 4);
                var c3 = ((b2 & 0xF) << 2) | (b3 >> 6);
                var c4 = b3 & 0x3F;

                result.Append(Base64Alphabet[c1]);
                result.Append(Base64Alphabet[c2]);

                if (i + 1 < data.Length)
                    result.Append(Base64Alphabet[c3]);

                if (i + 2 < data.Length)
                    result.Append(Base64Alphabet[c4]);
            }

            return result.ToString();
        }

        private static string HtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}