## Soviet Republic save game year changing tool

### Command line arguments:
-i|--input=<path|paths> (required) - Input file path (usually, stats.ini). Could set multiple with semicolon separator, or with '*' symbol in filename for wildcard.  
ATTENTION: if stat records overlap in time in multiple files, only values from preceding files are used, the ones in following files are ignored. Therefore, input files should be in chronological order.

-o|--output=<path> (required) - Output file path
  
-t|--throw (optional) - throw exceptions when parsing input file has failed at some lines, otherwise such lines will be removed.
  
-c|--compact=<int> (optional) - Compact records, so they're rearranged from given start year to maximum recorded date.
  
-m|--modify=<int> (optional) - Sets offset in years that applies to all stats records.
  
-h|--header=<path> (optional) header.bin file path, used when --modify is set to modify year in given header.bin.
  
-r|--records=<int> (optional) Number of records per year (all exceeding records will be removed, and their values averaged)
  
-u|--unchanged=<int> (optional) Number of last years for which records won't be changed by --records-per-year (default=5)

### Usage:
`SRStatEditor.exe [args] -i|--input <input path>[;<input path>] -o|--output <output path>`

### **_WARNING:_**
ALWAYS make backups! Especially for header.bin. Though I've added check that value in header.bin is same as last year in given stats.ini.
I recommend renaming your stats.ini to stats.old_<n>.ini each time before using this tool. This way you can set input path as "<path_to_savegame>\stats.old_*.ini", and each time this tool is executed it will read original statistics instead of modified by this tool. And you'll have backup information in case something went wrong. Using this tool on both original and modified statistics would lead to undefined behavior. 

### Examples 
(input and output are ommited here for simplicity, but should be present in actual case)
  
`SRStatEditor.exe -c=1970` - this will rearrange records, so that if, for example, game has started in 1960, new statistics will now show 1970 as earlies year. Latest record left unchanged. The further in past a record goes, the more it's date is changed.
  
`SRStatEditor.exe -m=-5 -h=<path to header.bin>` - move current game year in past by 5 years
  
`SRStatEditor.exe -r=2' - Leave two records per year for all years except last 5 years (useful when you've played long enough so that full statistics hits performance when shown). All records during last 5 years stays in statistics.
  
`SRStatEditor.exe -r=1 -u=1` - Leave one record per year for all years except last 1 year. All records during last year stays in statistics.
  
`SRStatEditor.exe -c=1930 -r=1 -m=-10` - modifies all records dates by 10 years in past, then rearranges them so that statistics starting year will be 1930 and finally leaves one record per year for all resulting years except last 5 years.

