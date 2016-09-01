using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace qpckEater
{
    class Program
    {
        static void Main(string[] args)
        {
            // Definitions
            int magic = 0x37402858;

            // Variables
            string mode = "";
            string input_str = "";

            // Print header
            Console.WriteLine("qpckEater by MHVuze");
            Console.WriteLine("=========================");

            // Handle arguments
            if (args.Length < 2) { Console.WriteLine("ERROR: Not enough arguments specified.\nExtract: qpckEater -x/-xd <qpck>\nCreate: qpckEater -c <folder>"); return; }
            mode = args[0];
            input_str = args[1];

            // Check mode
            if (mode != "-x" && mode != "-xd" && mode != "-c") { Console.WriteLine("ERROR: Unsupported mode specified."); return; }

            // Unpacking
            if (mode == "-x" || mode == "-xd")
            {
                // Check file
                if (!File.Exists(input_str)) { Console.WriteLine("ERROR: Specified qpck file doesn't exist."); return; }

                // Variables
                long start_offset = 0;
                int progress = 1;

                using (BinaryReader reader = new BinaryReader(File.Open(input_str, FileMode.Open)))
                {
                    if (reader.ReadInt32() != magic) { Console.WriteLine("ERROR: Invalid header."); return; }
                    int count = reader.ReadInt32();

                    // Create output directory
                    string folder_path = new FileInfo(input_str).Directory.FullName + "\\" + Path.GetFileNameWithoutExtension(input_str);
                    if (!Directory.Exists(folder_path)) { Directory.CreateDirectory(folder_path); }

                    Console.WriteLine("File index:");
                    for (int i = 0; i < count; i++)
                    {
                        // Get file info
                        long offset = reader.ReadInt64();
                        long hash = reader.ReadInt64();
                        int size = reader.ReadInt32();
                        Console.WriteLine("Processing file {0} / {1}: {2} [{3} | {4} bytes]", progress, count, hash.ToString("X16"), offset.ToString("X16"), size);

                        start_offset = reader.BaseStream.Position;

                        // Extract file
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        int type_magic = reader.ReadInt32();
                        reader.BaseStream.Seek(-4, SeekOrigin.Current);
                        byte[] file_bytes = reader.ReadBytes(size);
                        string file_name = (i + 1).ToString("D8") + "_" + hash.ToString("X16") + GetExtension(type_magic);
                        File.WriteAllBytes(folder_path + "\\" + file_name, file_bytes);

                        // Unpack extracted archives if intended
                        if (mode == "-xd")
                        {
                            // .pres unpacking
                            if (type_magic == 0x73657250)
                            {
                                using (BinaryReader pres_reader = new BinaryReader(File.Open(folder_path + "\\" + file_name, FileMode.Open)))
                                {
                                    // stuff
                                }
                            }

                            // .blz4 unpacking
                            if (type_magic == 0x347a6c62)
                            {
                                using (BinaryReader blz4_reader = new BinaryReader(File.Open(folder_path + "\\" + file_name, FileMode.Open)))
                                {
                                    // stuff
                                }
                            }
                        }

                        reader.BaseStream.Seek(start_offset, SeekOrigin.Begin);
                        progress++;
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

                // Get all files from directory (non-recursive)
                string[] repack_files = Directory.GetFiles(input_str, "*.*", SearchOption.TopDirectoryOnly).Select(file => Path.GetFileName(file)).ToArray();
                int count = repack_files.Count();
                if (count < 1) { Console.WriteLine("ERROR: No valid files found."); return; }

                // Prepare output file
                string file_path = new FileInfo(input_str).Directory.FullName + "\\" + Path.GetFileName(input_str) + "_new.qpck";
                File.WriteAllBytes(file_path, new byte[8 + (count * 20)]);
                BinaryWriter writer = new BinaryWriter(File.Open(file_path, FileMode.Open));
                writer.Write(magic);
                writer.Write(count);

                // Variables
                long start_offset = 0;
                long offset = 8 + (count * 20);
                int progress = 1;

                foreach (string file in repack_files)
                {
                    // Write file info
                    string import_path = input_str + "\\" + file;
                    string hash_str = Path.GetFileNameWithoutExtension(file).Substring(9);
                    long hash = long.Parse(hash_str, NumberStyles.HexNumber);
                    int size = Convert.ToInt32(new FileInfo(import_path).Length);

                    Console.WriteLine("Processing file {0} / {1}: {2} [{3} | {4} bytes]", progress, count, hash_str, offset.ToString("X16"), size);

                    writer.Write(offset);
                    writer.Write(hash);
                    writer.Write(size);

                    // Add file data
                    start_offset = writer.BaseStream.Position;

                    byte[] data = File.ReadAllBytes(import_path);
                    writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                    writer.Write(data);
                    writer.BaseStream.Seek(start_offset, SeekOrigin.Begin);

                    // Calculate offset/progress
                    offset += size;
                    progress++;
                };
                writer.Close();

                // End
                Console.WriteLine("=========================");
                Console.WriteLine("INFO: Finished packing {0} files.", count);
            }
        }

        // Better extensions
        static string GetExtension(int magic)
        {
            Dictionary<int, string> extension_dic = new Dictionary<int, string>
            {
                { 0x46534e42, ".bnsf" },
                { 0x6c566d47, ".gmvl" },
                { 0x3272742e, ".tr2" },
                { 0x73657250, ".pres" },
                { 0x347a6c62, ".blz4" },
            };

            string extension_str;
            if (extension_dic.TryGetValue(magic, out extension_str)) { }
            else { extension_str = ".bin";  }

            return extension_str;
        }
    }
}
