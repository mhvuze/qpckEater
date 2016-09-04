using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

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
            if (args.Length < 2) { Console.WriteLine("ERROR: Not enough arguments specified.\n\nExtract: qpckEater -x <qpck>\nExtract and unpack: qpckEater -xp <qpck>"); return; }
            mode = args[0];
            input_str = args[1];

            // Check mode
            if (mode != "-x" && mode != "-xp") { Console.WriteLine("ERROR: Unsupported mode specified."); return; }

            // Unpacking
            if (mode == "-x" || mode == "-xp")
            {
                // Check files
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

                    if (File.Exists(folder_path + "\\pres_list.txt")) { File.Delete(folder_path + "\\pres_list.txt"); }
                    if (File.Exists(folder_path + "\\pres_list_clean.txt")) { File.Delete(folder_path + "\\pres_list_clean.txt"); }

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

                        // Unpack extracted archives if intended
                        if (mode == "-xp")
                        {
                            // .pres unpacking
                            if (type_magic == 0x73657250)
                            {
                                reader.BaseStream.Seek(-4, SeekOrigin.Current);
                                byte[] file_bytes = reader.ReadBytes(size);

                                long reader_index_root = 0;
                                long reader_index_file = 0;

                                Stream stream = new MemoryStream(file_bytes);
                                using (BinaryReader pres_reader = new BinaryReader(stream))
                                {
                                    // Get general pres info
                                    pres_reader.BaseStream.Seek(28, SeekOrigin.Begin);
                                    int count_set = pres_reader.ReadInt32();

                                    reader_index_root = pres_reader.BaseStream.Position;

                                    for (int j = 0; j < count_set; j++)
                                    {
                                        if (count_set > 1)
                                        {
                                            int set_offset = pres_reader.ReadInt32();
                                            int unk6 = pres_reader.ReadInt32();
                                            pres_reader.BaseStream.Seek(set_offset, SeekOrigin.Begin);
                                        }

                                        // Read set info
                                        pres_reader.BaseStream.Seek(16, SeekOrigin.Current);
                                        int info_off = pres_reader.ReadInt32();
                                        int count_file = pres_reader.ReadInt32();                                     

                                        for (int k = 0; k < count_file; k++)
                                        {
                                            // Get individual file info
                                            pres_reader.BaseStream.Seek(info_off + 8, SeekOrigin.Begin);
                                            int name_off_file = pres_reader.ReadInt32();
                                            int name_elements_file = pres_reader.ReadInt32();
                                            pres_reader.BaseStream.Seek(16, SeekOrigin.Current);

                                            reader_index_file = pres_reader.BaseStream.Position;

                                            // Get individual file name info
                                            pres_reader.BaseStream.Seek(name_off_file, SeekOrigin.Begin);
                                            int name_off_final = pres_reader.ReadInt32();
                                            int ext_off_final = pres_reader.ReadInt32();
                                            int folder_off_final = pres_reader.ReadInt32();
                                            int complete_off_final = pres_reader.ReadInt32();

                                            // Get individual file path
                                            string str_name_final = "";
                                            string str_ext_final = "";
                                            string str_folder_final = "";
                                            string str_complete_final = "";
                                            if (name_elements_file >= 1) { pres_reader.BaseStream.Seek(name_off_final, SeekOrigin.Begin); str_name_final = readNullterminated(pres_reader); }
                                            if (name_elements_file >= 2) { pres_reader.BaseStream.Seek(ext_off_final, SeekOrigin.Begin); str_ext_final = readNullterminated(pres_reader); }
                                            if (name_elements_file >= 3) { pres_reader.BaseStream.Seek(folder_off_final, SeekOrigin.Begin); str_folder_final = readNullterminated(pres_reader); }
                                            if (name_elements_file >= 4) { pres_reader.BaseStream.Seek(complete_off_final, SeekOrigin.Begin); str_complete_final = readNullterminated(pres_reader); }

                                            // Print path to file                                            
                                            using (StreamWriter pres_list = new StreamWriter(folder_path + "\\pres_list.txt", true))
                                            {
                                                if (str_complete_final != "") { pres_list.WriteLine(str_complete_final); }
                                            }                                                

                                            // End loop
                                            pres_reader.BaseStream.Seek(reader_index_file + (k * 0x20), SeekOrigin.Begin);
                                        }
                                        if (count_set > 1) { pres_reader.BaseStream.Seek(reader_index_root + ((j + 1) * 8), SeekOrigin.Begin); }
                                    }
                                }
                            }
                        }

                        reader.BaseStream.Seek(start_offset, SeekOrigin.Begin);
                        progress++;
                    }

                    // End
                    Console.WriteLine("=========================");
                    Console.WriteLine("INFO: Finished processing {0} files.", count);

                    // Clean up dupes in list
                    var previous = new HashSet<string>();
                    File.WriteAllLines(folder_path + "\\pres_list_clean.txt", File.ReadLines(folder_path + "\\pres_list.txt").Where(line => previous.Add(line)));
                }
            }
        }

        // Read null-terminated string
        static string readNullterminated(BinaryReader reader)
        {
            var char_array = new List<byte>();
            string str = "";
            if (reader.BaseStream.Position == reader.BaseStream.Length)
            {
                byte[] char_bytes2 = char_array.ToArray();
                str = Encoding.UTF8.GetString(char_bytes2);
                return str;
            }
            byte b = reader.ReadByte();
            while ((b != 0x00) && (reader.BaseStream.Position != reader.BaseStream.Length))
            {
                char_array.Add(b);
                b = reader.ReadByte();
            }
            byte[] char_bytes = char_array.ToArray();
            str = Encoding.UTF8.GetString(char_bytes);
            return str;
        }
    }
}
