#nullable enable
using System;
using System.IO;

namespace SRStatEditor
{
    public class Header
    {
        public static int ReadYear(string headerFile)
        {
            using (var file = new FileStream(headerFile ?? "", FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(file))
            {
                file.Position = 0x19c;
                var year = reader.ReadInt16();
                return year;
            }
        }

        public static void ModifyHeader(string headerFile, int ensureYear, int modify)
        {
            using (var file = new FileStream(headerFile ?? "", FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(file))
            using (var writer = new BinaryWriter(file))
            {
                file.Position = 0x19c;
                var year = reader.ReadInt16();
                if (year != ensureYear)
                {
                    Console.WriteLine("header.bin year value does not correspond to given original value!");
                    Console.WriteLine("This could be due to either invalid value, or header.bin file structure was modified in new version of the game.");
                    return;
                }
                file.Position = 0x19c;
                writer.Write((short)(year + modify));
            }
        }
    }
}
