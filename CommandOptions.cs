#nullable enable
using CommandLine;
using System.Collections.Generic;

namespace SRStatEditor
{

    public class CommonOptions
    {
        [Option('i', "input", Required = true, Separator = ';', HelpText = "Input file path (usually, stats.ini)." +
                                                                           "Could set multiple with semicolon separator, " +
                                                                           "or with '*' symbol in filename for wildcard." +
                                                                           "ATTENTION: if stat records overlap in time in given files," +
                                                                           "only values from preceding files are used, the ones in" +
                                                                           "following files are ignored. Therefore, input files should be" +
                                                                           "in chronological order.", Min = 1)]
        public IEnumerable<string> Input { get; set; }

        [Option('t', "throw", Required = false,
            HelpText = "<flag> (optional) Throw exception on parsing errors (when false - they are ignored)")]
        public bool Throw { get; set; }
    }

    public class OutputOptions : CommonOptions
    {
        [Option('o', "output", Required = true, HelpText = "Output file path")]
        public string Output { get; set; }
    }

    [Verb("default", isDefault: true)]
    public class CommandLineOptions : OutputOptions
    {

        [Option('c', "compact", Required = false,
            HelpText = "<int> (optional) Compact records, so they're rearranged from given start year to maximum recorded date")]
        public int? CompactYears { get; set; }

        [Option('m', "modify", Required = false,
            HelpText = "<int> (optional) Sets offset in years that applies to all stats records.")]
        public int? ModifyYears { get; set; }

        [Option('r', "records", Required = false,
            HelpText = "<int> (optional) Number of records per year (all exceeding records will be removed, and their values averaged)")]
        public int? Records { get; set; }

        [Option('u', "unchanged", Required = false,
            HelpText = "<int> (optional) Number of last years for which records won't be changed by --records-per-year (default=5)")]
        public int? Unchanged { get; set; }
        //[Option("fix", Required = false)]
        public bool Fix { get; set; }
    }
    [Verb("info", HelpText = "Read input files and print information from them.")]
    public class InfoCommandOptions : CommonOptions
    {
    }

    [Verb("drop",
        HelpText = "Removes all records before/after given dates. To get date format, use \"info\" command")]
    public class DropCommandOptions : OutputOptions
    {

        [Option('b', "before", Required = false, HelpText = "Removes all records before given date. To get date format, use \"info\" command.")]
        public string? DropBefore { get; set; }
        [Option('a', "after", Required = false, HelpText = "Removes all records after given date. To get date format, use \"info\" command.")]
        public string? DropAfter { get; set; }
    }

    [Verb("header", HelpText = "Modify header.bin file")]
    public class HeaderCommandOptions
    {

        [Option('h', "path", Required = true, HelpText = "<path> header.bin file path, used when --modify is set")]
        public string HeaderPath { get; set; }
        [Option('o', "original", Required = true,
            HelpText = "<int> Original value in header.bin, used for ensuring that this tool is able to modify header.bin")]
        public int OriginalYears { get; set; }

        [Option('m', "modify", Required = true,
            HelpText = "<int> Sets offset in years that applies to header.bin.")]
        public int ModifyYears { get; set; }

    }
}
