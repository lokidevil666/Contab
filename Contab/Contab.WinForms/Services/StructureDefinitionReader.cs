using System.Text;
using Contab.WinForms.Models;

namespace Contab.WinForms.Services;

public sealed class StructureDefinitionReader
{
    static StructureDefinitionReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public StructureDefinition Read(string structurePath)
    {
        if (string.IsNullOrWhiteSpace(structurePath))
        {
            throw new ArgumentException("Structure path cannot be empty.", nameof(structurePath));
        }

        if (!File.Exists(structurePath))
        {
            throw new FileNotFoundException("Structure file not found.", structurePath);
        }

        var input = new List<StructureField>();
        var output = new List<StructureField>();
        var header = new List<StructureField>();
        var order = 0;

        foreach (var line in File.ReadLines(structurePath, Encoding.GetEncoding(1252)))
        {
            var raw = line.Trim();
            if (raw.Length == 0)
            {
                continue;
            }

            if (raw.StartsWith('"') && raw.EndsWith('"') && raw.Length >= 2)
            {
                raw = raw[1..^1];
            }

            if (raw.StartsWith(';') || raw.StartsWith('-'))
            {
                continue;
            }

            var parts = raw.Split(';');
            if (parts.Length < 4)
            {
                continue;
            }

            var name = parts[0].Trim();
            var start = ParseInt(parts[1]);
            var length = ParseInt(parts[2]);
            var sectionToken = parts[3].Trim().ToUpperInvariant();
            var defaultValue = parts.Length > 4 ? parts[4].Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(name) || start <= 0 || length <= 0)
            {
                continue;
            }

            var section = sectionToken switch
            {
                "IN" => StructureSection.In,
                "OUT" => StructureSection.Out,
                "HDR" => StructureSection.Header,
                _ => (StructureSection?)null
            };

            if (section is null)
            {
                continue;
            }

            var field = new StructureField(name, start, length, section.Value, defaultValue, order++);
            switch (field.Section)
            {
                case StructureSection.In:
                    input.Add(field);
                    break;
                case StructureSection.Out:
                    output.Add(field);
                    break;
                case StructureSection.Header:
                    header.Add(field);
                    break;
            }
        }

        input.Sort((a, b) => a.Start.CompareTo(b.Start));
        output.Sort((a, b) => a.Start.CompareTo(b.Start));
        header.Sort((a, b) => a.Start.CompareTo(b.Start));

        return new StructureDefinition(input, output, header);
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value.Trim(), out var parsed) ? parsed : 0;
    }
}
