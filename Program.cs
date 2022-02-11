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
		[Verb("default", isDefault: true)]
		public class CommandLineOptions
		{
			[CommandLine.Option('i', "input", Required = true, Separator = ';', HelpText = "Input file path (usually, stats.ini)." +
																			   "Could set multiple with semicolon separator, " +
																			   "or with '*' symbol in filename for wildcard." +
																			   "ATTENTION: if stat records overlap in time in given files," +
																			   "only values from preceding files are used, the ones in" +
																			   "following files are ignored. Therefore, input files should be" +
																			   "in chronological order.", Min = 1)]
			public IEnumerable<string> Input { get; set; }

			[CommandLine.Option('o', "output", Required = true, HelpText = "Output file path")]
			public string Output { get; set; }

			[CommandLine.Option('t', "throw", Required = false,
				HelpText = "<flag> (optional) Throw exception on parsing errors (when false - they are ignored)")]
			public bool Throw { get; set; }

			[CommandLine.Option('c', "compact", Required = false,
				HelpText = "<int> (optional) Compact records, so they're rearranged from given start year to maximum recorded date")]
			public int? CompactYears { get; set; }

			[CommandLine.Option('m', "modify", Required = false,
				HelpText = "<int> (optional) Sets offset in years that applies to all stats records.")]
			public int? ModifyYears { get; set; }

			[CommandLine.Option('h', "header", Required = false, HelpText = "<path> (optional) header.bin file path, used when --modify is set")]
			public string Header { get; set; }

			[CommandLine.Option('r', "records", Required = false,
				HelpText = "<int> (optional) Number of records per year (all exceeding records will be removed, and their values averaged)")]
			public int? Records { get; set; }

			[CommandLine.Option('u', "unchanged", Required = false,
				HelpText = "<int> (optional) Number of last years for which records won't be changed by --records-per-year (default=5)")]
			public int Unchanged { get; set; }
		}

		class StatEntry
		{
			public int Year = -1;
			public int Day = -1;
			public double Date
			{
				get => Year + Day / 365f;
				set
				{
					Year = (int) Math.Floor(value);
					Day = (int) Math.Round(Math.Truncate(value) * 365.25);
				}
			}
			public List<(string Entry, object[]? Values)> Lines { get; } = new List<(string Entry, object[]? Value)>();
		}

		static CommandLineOptions Options { get; set; }
		static int Main(string[] args)
		{
			args = new[]
			{
				//"--help",
				"-i", "E:\\Games\\SteamLibrary\\steamapps\\common\\SovietRepublic\\media_soviet\\save_cloud\\30315 - Cosmonaut1\\stats.old_*.ini",
				"-o", "E:\\Games\\SteamLibrary\\steamapps\\common\\SovietRepublic\\media_soviet\\save_cloud\\30315 - Cosmonaut1\\stats.ini",
				"-h", "E:\\Games\\SteamLibrary\\steamapps\\common\\SovietRepublic\\media_soviet\\save_cloud\\30315 - Cosmonaut1\\header.bin",
				"--records=2",
				"--modify=-5",
				"--compact=1940"
			};
			bool errors = false;
			Parser.Default.ParseArguments<CommandLineOptions>(args)
				.WithParsed<CommandLineOptions>(x => Options = x)
				.WithNotParsed(x => errors = true);
			if (errors)
				return -1;

			if (Options.Records > 26)
			{
				Console.WriteLine("Setting --records to value greater than 26 has no meaning (value averaging would return undefined results)");
				Options.Records = null;
			}
			if (Options.Records == null && Options.Unchanged != null)
			{
				Console.WriteLine("Setting --unchanged without --records have no meaning");
			}
			if (Options.ModifyYears == null && Options.Header != null)
			{
				Console.WriteLine("Setting --header without --modify have no meaning");
			}
			if (Options.Records == null && Options.ModifyYears == null && Options.CompactYears == null)
			{
				Console.WriteLine("No action selected. Exiting...");
				return 0;
			}
			if (Options.Header != null)
			{
				if (!System.IO.File.Exists(Options.Header))
				{
					Console.WriteLine("Header file does not exists!");
					return -1;
				}
			}
			var files = Options.Input.Select(x =>
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
			}).SelectMany(x => x).Select(ReadFile).ToArray();
			Console.WriteLine($"Loaded input files...");
			// ReSharper disable PossibleMultipleEnumeration
			var intro = files.Last().Intro;
			var outro = files.Last().Outro;
			var entries = ClearOverlaps(files.Select(x => x.Entries)).ToArray();
			Console.WriteLine("Arranged entries...");
			// ReSharper restore PossibleMultipleEnumeration
			var endDate = entries.Last().Date;
			if (Options.CompactYears != null)
			{
				var startDate = entries.First().Date;
				var factor = (endDate - Options.CompactYears.Value)/(endDate - startDate);
				foreach (var entry in entries)
					entry.Date = Options.CompactYears.Value + (endDate - entry.Date) * factor;
			}
			ModifyHeader(Options.Header, (int)Math.Floor(endDate));
			entries = DoMinimize(entries).OrderBy(x => x.Date).ToArray();
			Console.WriteLine("Minimized records for past years...");
			using (var writer = System.IO.File.CreateText(Options.Output))
			{
				// Use crime sentences from last
				writer.Write(files.Last().Intro);
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
			return 0;
		}
		private static void ModifyHeader(string? headerFile, int ensureYear)
		{
			if (headerFile == null || (Options.ModifyYears ?? 0) == 0)
				return;
			using(var file = new System.IO.FileStream(headerFile ?? "", System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite))
			using(var reader = new System.IO.BinaryReader(file))
			using (var writer = new System.IO.BinaryWriter(file))
			{
				file.Position = 0x19c;
				var year = reader.ReadInt16();
				if (ensureYear - Options.ModifyYears != year)
				{
					Console.WriteLine("header.bin year value does not correspond to stats.ini!");
					return;
				}
				file.Position = 0x19c;
				writer.Write(year + Options.ModifyYears.Value);
			}
		}
		private static IEnumerable<StatEntry> DoMinimize(IEnumerable<StatEntry> entries)
		{
			// Nothing to change
			if (Options.Records == null)
				return entries;
			var max_year = entries.Max(x => x.Year);
			var threshold_year = max_year - Options.Unchanged;
			// Yet again, nothing to change.
			if (entries.First().Year > threshold_year)
				return entries;
			return entries.Select(x => (BelowThreshold: x.Year < max_year - Options.Unchanged, Entry: x))
				.GroupBy(x =>
					x.BelowThreshold) // this results in two groups: entries that below threshold and above it.
				.SelectMany(x => x.Key // this would merge them back
					? x.Select(y => y.Entry) // this removed BelowThreshold from enumeration
						.GroupBy(y => y.Year) // this groups entries by year
						.Select(MinimizeYear) // this will calculate averages and leave only MinimizeArgs.Records number of records per year.
						.SelectMany(y => y)
					: x.Select(y => y.Entry));
		}
		private static IEnumerable<StatEntry> MinimizeYear(IGrouping<int, StatEntry> year)
		{
			var range = 365.25 * (1.0 / Options.Records!.Value);
			return year.GroupBy(x => (int)Math.Floor(x.Day / range)) // group by range
				.Select(x => Average(x, (int)Math.Floor(x.Key * range))); // convert whole range by averaging entry values
		}
		private static StatEntry Average(IEnumerable<StatEntry> range, int day)
		{
			static void Increment(ref int i)
			{
				i++;
			}
			StatEntry resultEntry = new StatEntry() { Day = day, Year = range.First().Year };
			var statEntries = range as StatEntry[] ?? range.ToArray();
			var indices = new int[statEntries.Length];
			while (true)
			{
				var lineGrp = statEntries.Select((x, i) => (x, i)).GroupBy(x => x.x.Lines[indices[x.i]].Entry);
				if (lineGrp.Count() == 1) // Ensure that indexed line is same in each entry.
				{
					// Try simple form for speed
					var lineGroup = lineGrp.First();
					if (string.IsNullOrEmpty(lineGroup.Key))
						resultEntry.Lines.Add((lineGroup.Key, null));
					else if (lineGroup.Key.Equals("$DATE_DAY"))
						resultEntry.Lines.Add(("$DATE_DAY", new object[] { day }));
					else if (lineGroup.Key.Equals("$DATE_YEAR"))
						resultEntry.Lines.Add(("$DATE_YEAR", new object[] { lineGroup.First().x.Year }));
					else
						resultEntry.Lines.Add((lineGroup.Key,
							AverageLineValues(lineGroup.Select(x => x.x.Lines[indices[x.i]].Values))));
				}
				else
				{
					// If simple form has failed, check if we are in an enumeration
					if (lineGrp.All(x => x.Key.Equals("$end") || x.Key.StartsWith("   ")))
					{
						// Now we need to correspond values of each entry in an enumeration of each stat entry.
						// All indices should be inside or at the end of an enumeration
						var enumerationEntries = new Dictionary<string, (double, double)>(); // here we will accumulate all available values
						for (int i = 0; i < statEntries.Length; i++)
						{
							var line = statEntries[i].Lines[indices[i]];
							var entry = line.Entry;
							while (!entry.Equals("$end"))
							{
								if (enumerationEntries.ContainsKey(entry))
								{
									enumerationEntries[entry] = (
										enumerationEntries[entry].Item1 + (double)line.Values![0],
										enumerationEntries[entry].Item2 + (double)line.Values[1]);
								}
								else
									enumerationEntries.Add(entry, ((double)line.Values![0], (double)line.Values[1]));
								indices[i]++;
								line = statEntries[i].Lines[indices[i]];
								entry = line.Entry;
							}
						}
						foreach (var entry in enumerationEntries)
							resultEntry.Lines.Add((entry.Key, new object[] { entry.Value.Item1 / statEntries.Length, entry.Value.Item2 / statEntries.Length }));

					}
					else
					{

					}

				}
				indices.ForEach(Increment);
				if (statEntries.Select((x, i) => (x, i)).All(x => indices[x.i] >= x.x.Lines.Count))
					return resultEntry;
			}
		}
		private static object[]? AverageLineValues(IEnumerable<object[]?> values)
		{
			var valuesArray = values as object[]?[] ?? values.ToArray();
			if (valuesArray.First() == null)
				if (valuesArray.Any(x => x != null))
					throw new ApplicationException("Unexpected line value types discrepancy!");
				else
					return null;
			var count = valuesArray.First()!.Length;
			var result = new object[count];
			if (valuesArray.Any(x => x!.Length != count))
				throw new ApplicationException("Unexpected line value types discrepancy!");
			if (count == 0)
				return result;
			for (var i = 0; i < count; i++)
				if (valuesArray.First()![i] is int)
				{
					if (valuesArray.Any(x => !(x![i] is int)))
						throw new ApplicationException("Unexpected line value types discrepancy!");
					result[i] = valuesArray.Select(x => x![i]).Cast<int>().Average();
				}
				else if (valuesArray.First()![i] is double)
				{
					if (valuesArray.Any(x => !(x![i] is double)))
						throw new ApplicationException("Unexpected line value types discrepancy!");
					result[i] = valuesArray.Select(x => x![i]).Cast<double>().Average();
				}
				else if (valuesArray.First()![i] is string)
				{
					if (valuesArray.Any(x => !(x![i] is string)))
						throw new ApplicationException("Unexpected line value types discrepancy!");
					result[i] = (string)valuesArray.First()![i];
				}
			return result;
		}
		private static (string Intro, IEnumerable<StatEntry> Entries, string Outro) ReadFile(string path)
		{
			string all_text;
			using (var reader = System.IO.File.OpenText(path))
				all_text = reader.ReadToEnd();
			var cityIdx = all_text.IndexOf("$STAT_CURRENT");
			string outro = "";
			if (cityIdx >= 0)
			{
				outro = all_text.Substring(cityIdx);
				all_text = all_text.Substring(0, cityIdx);
				if (Options.ModifyYears != null && Options.ModifyYears != 0)
				{
					var idx = 0;
					var anchor = "$DATE_YEAR ";
					while (idx >= 0)
					{
						idx = outro.IndexOf(anchor, idx+1);
						if (idx >= 0)
						{
							var line = outro.Substring(idx, outro.IndexOf('\r', idx) - idx);
							outro = outro.Remove(idx, line.Length);
							var value = line.Substring(anchor.Length, line.Length - anchor.Length - 1);
							int.TryParse(value, out var year);
							if(year != 0)
								year += Options.ModifyYears.Value;
							line = anchor + year + " ";
							outro = outro.Insert(idx, line);
						}
					}
					anchor = "$GlobalEventStartYear ";
					idx = outro.IndexOf(anchor);
					if (idx >= 0)
					{
						var line = outro.Substring(idx, outro.IndexOf('\r', idx) - idx);
						outro = outro.Remove(idx, line.Length);
						var value = line.Substring(anchor.Length, line.Length - anchor.Length);
						int.TryParse(value, out var year);
						if(year != 0)
							year += Options.ModifyYears.Value;
						line = anchor + year;
						outro = outro.Insert(idx, line);
					}
				}
			}
			var records = all_text.Split(new[] { "$STAT_RECORD " }, int.MaxValue, StringSplitOptions.RemoveEmptyEntries);
			var query = records.Skip(1)
				.Select(x => "$STAT_RECORD " + x)
				.Select(ReadEntry).Where(x => x != null);
			return (records.First(), query, outro);
		}
		private static IEnumerable<StatEntry> ClearOverlaps(IEnumerable<IEnumerable<StatEntry>> inputs)
		{
			double lastDate = 0;
			foreach (var fileEntries in inputs)
			{
				foreach (var entry in fileEntries)
				{
					if (entry.Date < lastDate)
						continue;
					if (entry.Date > lastDate)
						lastDate = entry.Date;
					yield return entry;
				}
			}
		}
		private static StatEntry? ReadEntry(string record)
		{
			StatEntry? entry = new StatEntry();
			using (var lineReader = new System.IO.StringReader(record))
			{
				var line = lineReader.ReadLine();
				while (line != null)
				{
					var parsed = ParseLine(line);
					if (parsed.Entry == "$STAT_RECORD")
					{
						// Skip this line, it would be added during compiling output stats.ini file.
						line = lineReader.ReadLine();
						continue;
					}
					if (parsed.Entry.Equals("$DATE_YEAR"))
						entry.Year = (int)parsed.Values![0];
					else if (parsed.Entry.Equals("$DATE_DAY"))
						entry.Day = (int)parsed.Values![0];
					entry.Lines.Add(parsed);
					line = lineReader.ReadLine();
				}
			}
			if (entry.Day != -1 && entry.Year != -1)
			{
				entry.Year += Options.ModifyYears ?? 0;
				return entry;
			}
			if (Options.Throw)
				throw new System.IO.FileLoadException("Can't parse record: " + record);
			return null;
		}

		static (string Entry, object[]? Values) ParseLine(string line)
		{
			if (line.StartsWith("=") || line.StartsWith("-") || line.Length <= 1)
				return (line, null);

			var sentences = line.Split(' ').MergeWhitespace();
			return (sentences.First(), sentences.Skip(1)
				.Select(str =>
				{
					if (int.TryParse(str, out var i))
						return i;
					if (double.TryParse(str, out var f))
						return f;
					return (object)str;
				}).ToArray());
		}
		static IEnumerable<string> MergeWhitespace(this IEnumerable<string> source)
		{
			var spaces = 0;
			var it = source.GetEnumerator();
			while (it.MoveNext())
			{
				if (it.Current!.Length == 0)
				{
					spaces++;
					continue;
				}
				yield return new String(' ', spaces) + it.Current;
				break;
			}
			while (it.MoveNext())
				yield return it.Current!;
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
		public delegate void RefAction<T>(ref T value);

		public static void ForEach<T>(this T[] array, RefAction<T> action)
		{
			var it = array.GetEnumerator(); // just to ensure array has not changed.
			for (int i = 0; i < array.Length && it.MoveNext(); i++)
			{
				action(ref array[i]);
			}
		}
		public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
		{
			foreach (T item in enumeration)
			{
				action(item);
			}
		}
		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
		{
			var it = source.GetEnumerator();
			bool hasRemainingItems = false;
			bool isFirst = true;
			T item = default(T);

			do
			{
				hasRemainingItems = it.MoveNext();
				if (hasRemainingItems)
				{
					if (!isFirst)
						yield return item;
					item = it.Current;
					isFirst = false;
				}
			} while (hasRemainingItems);
		}
	}
}
