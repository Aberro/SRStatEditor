#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace SRStatEditor
{
	static class Program
	{

		static CommonOptions InputOptions { get; set; }
		static CommandLineOptions? Options { get; set; }
		static InfoCommandOptions? InfoOptions { get; set; }
		static DropCommandOptions? DropOptions { get; set; }
		static HeaderCommandOptions? HeaderOptions { get; set; }
		static int Main(string[] args)
		{
			bool errors = false;
			Parser.Default.ParseArguments<CommandLineOptions, InfoCommandOptions, DropCommandOptions, Header>(args)
				.WithParsed<CommandLineOptions>(x =>
				{
					InputOptions = x;
					Options = x;
				})
				.WithParsed<InfoCommandOptions>(x =>
				{
					InputOptions = x;
					InfoOptions = x;
				})
				.WithParsed<DropCommandOptions>(x =>
				{
					InputOptions = x;
					DropOptions = x;
				})
				.WithParsed<HeaderCommandOptions>(x => HeaderOptions = x)
				.WithNotParsed(x => errors = true);
			if (errors)
				return -1;

			if (HeaderOptions != null)
			{
				return HeaderCommand();
			}
			var files = InputOptions.Input.Select(x =>
			{
				var filename = System.IO.Path.GetFileName(x);
				if (filename.Contains('*'))
				{
					// Wildcard is used
					var dir = System.IO.Path.GetDirectoryName(x);
					if (!System.IO.Directory.Exists(dir))
					{
						Console.WriteLine("Input directory does not exists!");
						throw new ArgumentException("Input directory does not exists!");
					}
					return System.IO.Directory.GetFiles(dir, filename);
				}
				return new[] { x };
			}).SelectMany(x => x).Select(x => StatFile.ReadFile(x, InputOptions.Throw)).ToArray();
			if (files.Length <= 0)
			{
				Console.WriteLine("No input files found!");
				return -1;
			}
			Console.WriteLine($"Loaded input files...");
			if (InfoOptions != null)
			{
				return InfoCommand(files);
			}
			if (DropOptions != null)
			{
				return DropCommand(files);
			}
			if (Options != null)
			{
				return DefaultCommand(files);
			}
			return -1;
		}
		private static int HeaderCommand()
		{
			if (HeaderOptions == null)
				throw new NullReferenceException(); // never should throw this, because this method is called after check for null;
			if (!System.IO.File.Exists(HeaderOptions.HeaderPath))
			{
				Console.WriteLine("Header file does not exists!");
				return -1;
			}
			Header.ModifyHeader(HeaderOptions.HeaderPath, HeaderOptions.OriginalYears, HeaderOptions.ModifyYears);
			return 0;
		}
		private static int DefaultCommand(StatFile[] files)
		{
			if (Options.Records > 26)
			{
				Console.WriteLine(
					"Setting --records to value greater than 26 has no meaning (value averaging would return undefined results)");
				Options.Records = null;
			}
			if (Options.Records == null && Options.Unchanged != null)
			{
				Console.WriteLine("Setting --unchanged without --records have no meaning");
			}
			if (Options.Records == null && Options.ModifyYears == null && Options.CompactYears == null && !Options.Fix)
			{
				Console.WriteLine("No action selected. Exiting...");
				return 0;
			}
			// ReSharper disable PossibleMultipleEnumeration
			var entries = StatEntry.ClearOverlaps(files).ToArray();
			Console.WriteLine("Arranged entries...");
            if (Options.Fix)
            {
                entries = StatEntry.FixDuplicates(entries).ToArray();
                Console.WriteLine("Removed duplicates...");
            }

            if (Options.ModifyYears != null && Options.ModifyYears != 0)
			{
				entries.ForEach(x => x.ModifyYear(Options.ModifyYears.Value));
			}
			// ReSharper restore PossibleMultipleEnumeration
			var endDate = entries.Last().Date;
			if (Options.CompactYears != null)
			{
				var startDate = entries.First().Date;
				var factor = (endDate - Options.CompactYears.Value) / (endDate - startDate);
				foreach (var entry in entries)
					entry.Date = endDate + (entry.Date - endDate) * factor;
				Console.WriteLine($"Compacted records (from {(int)startDate}-{(int)endDate} to {Options.CompactYears.Value}-{(int)endDate})...");
			}
			var countBefore = entries.Length;
			if(Options.Records != null)
			    entries = StatEntry.DoMinimize(entries, Options.Records.Value, Options.Unchanged ?? 5).OrderBy(x => x.Date).ToArray();
			if (Options.Records != null)
			{
				Console.WriteLine($"Minimized records for past years (entries before: {countBefore}, entries after: {entries.Length})...");
            }
            files.Last().ReplaceEntries(entries);
            files.Last().WriteFile();
			return 0;
		}
		private static int DropCommand(
			StatFile[] files)
		{
			if (DropOptions == null)
				throw new NullReferenceException(); // never should throw this, because this method is called after check for null;
			DateTime beforeDate = DateTime.MinValue, afterDate = DateTime.MaxValue;
			if (DropOptions.DropAfter != null && !DateTime.TryParse(DropOptions.DropAfter, out afterDate))
			{
				Console.WriteLine("Can't parse `after` value");
				return -1;
			}
			if (DropOptions.DropBefore != null && !DateTime.TryParse(DropOptions.DropBefore, out beforeDate))
			{
				Console.WriteLine("Can't parse `before` value");
				return -1;
			}
			double before = beforeDate.Year + beforeDate.DayOfYear / 365.25,
				after = afterDate.Year + afterDate.DayOfYear / 365.25;
			var entries = files.SelectMany(x => x.Entries.Where(y => y.Date > before && y.Date < after)).ToArray();

            files.Last().ReplaceEntries(entries);
            files.Last().WriteFile();
			return 0;
		}
		private static int InfoCommand(StatFile[] files)
		{
			foreach (var file in files)
			{
				var first = file.Entries.First();
				var last = file.Entries.Last();
				Console.WriteLine($"{System.IO.Path.GetFileName(file.Path)}: {file.Entries.Count()} records\n" +
								  $"From {new DateTime(first.Year, 1, 1).AddDays(first.Day - 1).ToShortDateString()} " +
								  $"to {new DateTime(last.Year, 1, 1).AddDays(last.Day - 1).ToShortDateString()}");
			}
			return 0;
		}
		private static void WriteOutput(string outputPath, StatEntry[] entries, string intro, string outro)
		{
			using (var writer = System.IO.File.CreateText(outputPath))
			{
				// Use crime sentences from last
				writer.Write(intro);
				int idx = 0;
				foreach (var entry in entries)
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
				writer.Write(outro);
			}
		}
		static bool TryReadValue(string line, string search, ref int value)
		{
			var idx = line.IndexOf(search);
			if (idx >= 0 && int.TryParse(line.Substring(idx + search.Length), out var v))
			{
				value = v;
				return true;
			}
			return false;
		}
	}
}
