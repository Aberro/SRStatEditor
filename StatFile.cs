#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SRStatEditor
{
    public class StatFile
    {
        private string _intro;
        private string _outro;

        public string Path { get; }
        public IEnumerable<StatEntry> Entries { get; private set; }

        private StatFile(string path, string intro, IEnumerable<StatEntry> entries, string outro)
        {
            _intro = intro;
            _outro = outro;
            Path = path;
            Entries = entries;
        }

        public static StatFile ReadFile(string path, bool throwOnError = true)
        {
            string allText;
            using (var reader = System.IO.File.OpenText(path))
                allText = reader.ReadToEnd();
            var outroIdx = allText.IndexOf("$STAT_CURRENT");
            string outro = "";
            if (outroIdx >= 0)
            {
                outro = allText.Substring(outroIdx);
                allText = allText.Substring(0, outroIdx);
            }
            var records = allText.Split(new[] { "$STAT_RECORD " }, int.MaxValue, StringSplitOptions.RemoveEmptyEntries);
            var query = records.Skip(1)
                .Select(x => "$STAT_RECORD " + x)
                .Select(x => StatEntry.ReadEntry(x, throwOnError)).Where(x => x != null).Cast<StatEntry>();
            return new StatFile(path, records.First(), query, outro);
        }

        public void ReplaceEntries(IEnumerable<StatEntry> entries)
        {
            Entries = entries;
        }

        public void WriteFile()
        {
            using (var writer = System.IO.File.CreateText(Path))
            {
                // Use crime sentences from last
                writer.Write(_intro);
                int idx = 0;
                foreach (var entry in Entries)
                {
                    writer.WriteLine($"$STAT_RECORD {idx}");

                    foreach (var line in entry.Lines)
                    {
                        if (line.Entry.Equals("$DATE_DAY"))
                            writer.WriteLine($"$DATE_DAY {entry.Day}");
                        else if (line.Entry.Equals("$DATE_YEAR"))
                            writer.WriteLine($"$DATE_YEAR {entry.Year} ");
                        else
                        {
                            writer.Write($"{line.Entry}");
                            if (line.Values != null)
                                foreach (var value in line.Values)
                                    if (value is double)
                                        writer.Write($" {((double)value):0.000000}");
                                    else
                                        writer.Write($" {value}");
                            writer.WriteLine();
                        }
                    }
                    idx++;
                }
                writer.Write(_outro);
            }
        }
    }
}
