#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SRStatEditor
{
    public class StatEntry
    {
        // Used to determine whether averaging or summ should be used for merging entries values.
        // true for averaging (prices)
        // false for summation (expenses)
        static Dictionary<string, bool> averaging = new Dictionary<string, bool>()
        {
            { "$Economy_PurchaseCostUSD", true },
            { "$Economy_PurchaseCostRUB", true },
            { "$Economy_SellCostUSD", true },
            { "$Economy_SellCostRUB", true },
            { "$Economy_BaseRUB", true },
            { "$Economy_BaseUSD", true },
            { "$Resources_ImportUSD", false },
            { "$Resources_SpendConstructions", false },
            { "$Resources_SpendFactories", false },
            { "$Resources_SpendShops", false },
            { "$Resources_SpendVehicles", false },
            { "$Vehicles_ImportUSD", false },
            { "$Vehicles_ImportRUB", false },
            { "$Vehicles_ExportUSD", false },
            { "$Resources_Produced", false },
            { "$Citizens_Born", false },
            { "$Citizens_Dead", false },
            { "$Citizens_Escaped", false },
            { "$Citizens_ImigrantSoviet", false },
            { "$Citizens_ImigrantAfrica", false },
            { "$Economy_ImigrantCitySpendRUB", false },
            { "$Economy_ImigrantCitySpendUSD", false },
            { "$Citizens_Status", true },
            { "$Citizens_AverageProductivity", true },
            { "$Citizens_AverageLifespan", true },
            { "$Citizens_AverageAge", true },
            { "$Citizens_SmallChilds", true },
            { "$Citizens_MediumChilds", true },
            { "$Citizens_AdultsParent", true },
            { "$Citizens_Adults", true },
            { "$Citizens_Unemployed", true },
            { "$Citizens_NoEducation", true },
            { "$Citizens_BasicEducationNum", true },
            { "$Citizens_HighEducationNum", true },
            { "$Citizens_EletronicNone", true },
            { "$Citizens_EletrinicRadio", true },
            { "$Citizens_EletronicTV", true },
            { "$Citizens_EletronicComputer", true },
            { "$Citizens_CarOwners", true },
            { "$Tourism_ToursitGeneratedUSD", false },
            { "$Tourism_ToursitGeneratedRUB", false },
            { "$Tourism_ToursitReturnedUSD", true },
            { "$Tourism_ToursitReturnedRUB", true },
            { "$Tourism_ToursitEnteredUSD", true },
            { "$Tourism_ToursitEnteredRUB", true },
            { "$Tourism_ToursitEndedUSD", true },
            { "$Tourism_ToursitEndedRUB", true },
            { "$Tourism_ToursitDiedUSD", true },
            { "$Tourism_ToursitDiedRUB", true },
            { "$Tourism_ToursitScoreUSD", true },
            { "$Tourism_ToursitScoreRUB", true },
            { "$Tourism_SpendUSD 0", false },
            { "$Tourism_SpendUSD 1", false },
            { "$Tourism_SpendUSD 2", false },
            { "$Tourism_SpendUSD 3", false },
            { "$Tourism_SpendUSD 4", false },
            { "$Tourism_SpendUSD 5", false },
            { "$Tourism_SpendUSD 6", false },
            { "$Tourism_SpendUSD 7", false },
            { "$Tourism_SpendUSD 8", false },
            { "$Tourism_SpendUSD 9", false },
            { "$Tourism_SpendUSD 10", false },
            { "$Tourism_SpendUSD 11", false },
            { "$Tourism_SpendUSD 12", false },
            { "$Tourism_SpendUSD 13", false },
            { "$Tourism_SpendUSD 14", false },
            { "$Tourism_SpendUSD 15", false },
            { "$Tourism_SpendRUB 0", false },
            { "$Tourism_SpendRUB 1", false },
            { "$Tourism_SpendRUB 2", false },
            { "$Tourism_SpendRUB 3", false },
            { "$Tourism_SpendRUB 4", false },
            { "$Tourism_SpendRUB 5", false },
            { "$Tourism_SpendRUB 6", false },
            { "$Tourism_SpendRUB 7", false },
            { "$Tourism_SpendRUB 8", false },
            { "$Tourism_SpendRUB 9", false },
            { "$Tourism_SpendRUB 10", false },
            { "$Tourism_SpendRUB 11", false },
            { "$Tourism_SpendRUB 12", false },
            { "$Tourism_SpendRUB 13", false },
            { "$Tourism_SpendRUB 14", false },
            { "$Tourism_SpendRUB 15", false },
            { "$Tourism_SpendShopsUSD", false },
            { "$Tourism_SpendShopsRUB", false },
            { "$Tourism_SpendCinemasUSD", false },
            { "$Tourism_SpendCinemasRUB", false },
            { "$Tourism_SpendPubsUSD", false },
            { "$Tourism_SpendPubsRUB", false },
            { "$Tourism_SpendSportUSD", false },
            { "$Tourism_SpendSportRUB", false },
            { "$Tourism_SpendHotelsUSD", false },
            { "$Tourism_SpendHotelsRUB", false },
            { "$Tourism_SpendRidesUSD", false },
            { "$Tourism_SpendRidesRUB", false },
            { "Crime_Executed_0", false },
            { "Crime_Executed_1", false },
            { "Crime_Executed_2", false },
            { "Crime_Executed_3", false },
            { "Crime_Executed_4", false },
            { "Crime_Error_NoPolice", false },
            { "Crime_Error_NotInvestigated", false },
            { "Crime_Error_NotCourt", false },
            { "Crime_Prisoners_Escaped", false },

			//{ "$", false },
			//{ "$", true },
		};

        public int Year = -1;
        public int Day = -1;
        public double Date
        {
            get => Year + Day / 365f;
            set
            {
                Year = (int)Math.Floor(value);
                Day = (int)Math.Round((value - Math.Truncate(value)) * 365.25);
            }
        }
        public List<(string Entry, object[]? Values)> Lines { get; } = new List<(string Entry, object[]? Value)>();

        public static StatEntry? ReadEntry(string record, bool throwOnError = true)
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
                return entry;
            }
            if (throwOnError)
                throw new System.IO.FileLoadException("Can't parse record: " + record);
            return null;
        }

        public void ModifyYear(int modifier)
        {
            Year += modifier;
        }

        public static IEnumerable<StatEntry> ClearOverlaps(IEnumerable<StatFile> files)
        {
            double lastDate = 0;
            foreach (var file in files)
            {
                foreach (var entry in file.Entries)
                {
                    if (entry.Date < lastDate)
                        continue;
                    if (entry.Date > lastDate)
                        lastDate = entry.Date;
                    yield return entry;
                }
            }
        }

        public static IEnumerable<StatEntry> FixDuplicates(IEnumerable<StatEntry> entries)
        {
            return entries.GroupBy(x => x.Date).Select(group => Merge(group, group.First().Day, group.First().Date));
        }
        // range is range of entries that require merging into a single entry
        // day is the $ENTRY_DAY value of resulting entry
        public static StatEntry Merge(IEnumerable<StatEntry> range, int day, double minDate)
        {
            // used for linq incrementing indices, simple action but with ref.
            static void Increment(ref int i)
            {
                i++;
            }
            StatEntry resultEntry = new StatEntry() { Day = day, Year = range.First().Year };
            if (resultEntry.Date < minDate)
            {
                resultEntry.Date = minDate;
                resultEntry.Day++;
            }
            var statEntries = range as StatEntry[] ?? range.ToArray();
            var indices = new int[statEntries.Length]; // indices to lines in each entry, required to merge enumerations with different line count
            bool mergingByAveraging = false; // merging technique, true - average, false - summ.
            while (true)
            {
                // Group (presumably same) lines from multiple stats together
                var lineGrp = statEntries.Select((x, i) => (x, i)).GroupBy(x => x.x.Lines[indices[x.i]].Entry);
                var lineEntry = lineGrp.First().Key;
                // Choose merging technique by line entries occurence in `averading` map.
                // For enumerations, merging technique would stay the same across entire enumeration,
                // because no line entry would be found in `averaging` map.
                if (averaging.TryGetValue(lineEntry, out var merging))
                {
                    mergingByAveraging = merging;
                }
                // Check if lines are actually same
                if (lineGrp.Count() == 1)
                {
                    // If lines in each statistics entry has same line entry,
                    // use simple form for speed
                    var lineGroup = lineGrp.First();

                    if (string.IsNullOrEmpty(lineGroup.Key))
                        // Skip empty lines beforehand
                        resultEntry.Lines.Add((lineGroup.Key, null));
                    else if (lineGroup.Key.Equals("$DATE_DAY"))
                        // Skip days, those don't require merging
                        resultEntry.Lines.Add(("$DATE_DAY", new object[] { day }));
                    else if (lineGroup.Key.Equals("$DATE_YEAR"))
                        // Skip year for same reason
                        resultEntry.Lines.Add(("$DATE_YEAR", new object[] { lineGroup.First().x.Year }));
                    else
                        // And what's left may require merging
                        resultEntry.Lines.Add((lineGroup.Key,
                            MergeLineValues(lineGroup.Select(x => x.x.Lines[indices[x.i]].Values), mergingByAveraging)));
                }
                else
                {
                    // If simple form had failed, check if we are in an enumeration
                    // That means that each line currently either inside enumeration or at it's end:
                    if (lineGrp.All(x => x.Key.Equals("$end") || x.Key.StartsWith("   ")))
                    {
                        // Now we need to correspond values of each entry in an enumeration of each stat entry.
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
                            resultEntry.Lines.Add((entry.Key, new object[]
                            {
                                    mergingByAveraging ? entry.Value.Item1 / statEntries.Length : entry.Value.Item1,
                                    mergingByAveraging ? entry.Value.Item2 / statEntries.Length : entry.Value.Item2
                            }));
                        resultEntry.Lines.Add(("$end", new object[0]));

                    }
                    else
                    {
                        // This is not an enumeration, I don't know how to merge stat entries...
                        Console.WriteLine(
                            "Unexpected statistics layout! Create a new issue here: https://github.com/Aberro/SRStatEditor/issues and attach your save game archive");
                        throw new Exception();
                    }
                }
                indices.ForEach(Increment);
                if (statEntries.Select((x, i) => (x, i)).All(x => indices[x.i] >= x.x.Lines.Count))
                    return resultEntry;
            }
        }

        public static IEnumerable<StatEntry> DoMinimize(IEnumerable<StatEntry> entries, int recordsPerYear, int unchangedYears)
        {
            var max_year = entries.Max(x => x.Year);
            var threshold_year = max_year - unchangedYears;
            // Yet again, nothing to change.
            if (entries.First().Year > threshold_year)
                foreach (var entry in entries)
                    yield return entry;
            // first record is always "empty" - it has no import/export statistics
            var minDate = entries.First().Date;
            var query = entries.Skip(1)
                .Select(x => (BelowThreshold: x.Year <= max_year - unchangedYears, Entry: x))
                .GroupBy(x =>
                    x.BelowThreshold) // this results in two groups: entries that below threshold and above it.
                .SelectMany(x => x.Key // this would merge them back
                    ? x.Select(y => y.Entry) // this removed BelowThreshold from enumeration
                        .GroupBy(y => y.Year) // this groups entries by year
                        .Select(y => MinimizeYear(y, minDate, recordsPerYear)) // this will calculate averages and leave only MinimizeArgs.Records number of records per year.
                        .SelectMany(y => y)
                    : x.Select(y => y.Entry));
            // Return that first "empty" entry.
            yield return entries.First();
            // And then return our minimized result
            foreach (var entry in query)
                yield return entry;
        }

        private static (string Entry, object[]? Values) ParseLine(string line)
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
        private static object[]? MergeLineValues(IEnumerable<object[]?> values, bool mergingByAveraging)
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
                    result[i] = mergingByAveraging
                        ? (int)valuesArray.Select(x => x![i]).Cast<int>().Average()
                        : (int)valuesArray.Select(x => x![i]).Cast<int>().Sum();
                }
                else if (valuesArray.First()![i] is double)
                {
                    if (valuesArray.Any(x => !(x![i] is double)))
                        throw new ApplicationException("Unexpected line value types discrepancy!");
                    result[i] = mergingByAveraging
                        ? valuesArray.Select(x => x![i]).Cast<double>().Average()
                        : valuesArray.Select(x => x![i]).Cast<double>().Sum();
                }
                else if (valuesArray.First()![i] is string)
                {
                    if (valuesArray.Any(x => !(x![i] is string)))
                        throw new ApplicationException("Unexpected line value types discrepancy!");
                    result[i] = (string)valuesArray.First()![i];
                }
            return result;
        }
        private static IEnumerable<StatEntry> MinimizeYear(IGrouping<int, StatEntry> year, double minDate, int recordsPerYear)
        {
            var range = 365.25 * (1.0 / recordsPerYear);
            return year.GroupBy(x => (int)Math.Floor(x.Day / range)) // group by range
                .Select(x => StatEntry.Merge(x, (int)Math.Floor(x.Key * range), minDate)); // convert whole range by averaging entry values
        }
    }
}
