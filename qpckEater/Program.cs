using System;
using System.IO;

namespace qpckEater
{
    class Program
    {
        static void Main(string[] args)
        {
            // Variables
            string mode = "";
            string input_str = "";

            // Print header
            Console.WriteLine("qpckEater by MHVuze");
            Console.WriteLine("=========================");

            // Handle arguments
            if (args.Length < 2) { Console.WriteLine("ERROR: Not enough arguments specified.\nExtract: qpck -x <qpck>"); return; }
            mode = args[0];
            input_str = args[1];

            // Check mode
            if (mode != "-x" && mode != "-c") { Console.WriteLine("ERROR: Unsupported mode specified."); return; }

            // Unpacking
            if (mode == "-x")
            {
                // Check file
                if (!File.Exists(input_str)) { Console.WriteLine("ERROR: Specified qpck file doesn't exist."); return; }

                // Variables
                int magic = 0x37402858;
                int count = 0;
                long start_offset = 0;

                using (BinaryReader reader = new BinaryReader(File.Open(input_str, FileMode.Open)))
                {
                    if (reader.ReadInt32() != magic) { Console.WriteLine("ERROR: Invalid header."); return; }
                    count = reader.ReadInt32();

                    // Create output directory
                    string folder_path = Path.GetDirectoryName(input_str) + "\\" + Path.GetFileNameWithoutExtension(input_str);
                    if (!Directory.Exists(folder_path)) { Directory.CreateDirectory(folder_path); }

                    Console.WriteLine("File index:");
                    for (int i = 0; i < count; i++)
                    {
                        // Get file info
                        long offset = reader.ReadInt64();
                        long hash = reader.ReadInt64();
                        int size = reader.ReadInt32();
                        Console.WriteLine("#{0}, File {1} @ 0x{2}, Size {3} bytes",
                            (i+1).ToString("D8"),
                            hash.ToString("X16"),
                            offset.ToString("X16"),
                            size);

                        start_offset = reader.BaseStream.Position;

                        // Extract file
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        byte[] file_bytes = reader.ReadBytes(size);
                        string file_name = (i + 1).ToString("D8") + "_" + hash.ToString("X16") + ".bin";
                        File.WriteAllBytes(folder_path + "\\" + file_name, file_bytes);

                        reader.BaseStream.Seek(start_offset, SeekOrigin.Begin);
                    }

                    // End
                    Console.WriteLine("=========================");
                    Console.WriteLine("INFO: Finished extracting {0} files.", count);
                }
            }

            // Repacking
            if (mode == "-c")
            {
                // Check folder
                if (!Directory.Exists(input_str)) { Console.WriteLine("ERROR: Specified folder doesn't exist."); return; }

                Console.WriteLine("ERROR: Mode currently unsupported.");
                return;
            }
        }
    }
}
