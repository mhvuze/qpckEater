﻿using System;
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
            if (args.Length < 2) { Console.WriteLine("ERROR: Not enough arguments specified.\n\nExtract: qpckEater -x <qpck>\nExtract and unpack: qpckEater -xp <qpck>\n\nCreate qpck: qpckEater -c <folder>\nRepack .blz4/.pres: qpckEater -r <folder>"); return; }
            mode = args[0];
            input_str = args[1];

            // Check mode
            if (mode != "-x" && mode != "-xp" && mode != "-c" && mode != "-r") { Console.WriteLine("ERROR: Unsupported mode specified."); return; }

            // Unpacking
            if (mode == "-x" || mode == "-xp")
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
                        if (mode == "-xp")
                        {
                            string file = folder_path + "\\" + file_name;

                            // .pres unpacking
                            if (type_magic == 0x73657250)
                            {
                                long reader_index_root = 0;
                                long reader_index_set = 0;
                                long reader_index_file = 0;

                                using (BinaryReader pres_reader = new BinaryReader(File.Open(file, FileMode.Open)))
                                {
                                    // Get general pres info
                                    pres_reader.BaseStream.Seek(4, SeekOrigin.Begin);
                                    int unk1 = pres_reader.ReadInt32();
                                    int unk2 = pres_reader.ReadInt32();
                                    int unk3 = pres_reader.ReadInt32();
                                    int offset_data = pres_reader.ReadInt32();
                                    int unk4 = pres_reader.ReadInt32();
                                    int unk5 = pres_reader.ReadInt32();
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
                                        int names_off = pres_reader.ReadInt32();
                                        int names_elements = pres_reader.ReadInt32();
                                        int set_unk1 = pres_reader.ReadInt32();
                                        int set_unk2 = pres_reader.ReadInt32();
                                        int info_off = pres_reader.ReadInt32();
                                        int count_file = pres_reader.ReadInt32();
                                        int set_unk3 = pres_reader.ReadInt32();
                                        int set_unk4 = pres_reader.ReadInt32();
                                        int set_unk5 = pres_reader.ReadInt32();
                                        int set_unk6 = pres_reader.ReadInt32();
                                        int set_unk7 = pres_reader.ReadInt32();
                                        int set_unk8 = pres_reader.ReadInt32();

                                        reader_index_set = pres_reader.BaseStream.Position;

                                        for (int k = 0; k < count_file; k++)
                                        {
                                            // Get individual file info
                                            pres_reader.BaseStream.Seek(info_off, SeekOrigin.Begin);
                                            int offset_file = pres_reader.ReadInt32();
                                            int csize_file = pres_reader.ReadInt32();
                                            int name_off_file = pres_reader.ReadInt32();
                                            int name_elements_file = pres_reader.ReadInt32();
                                            int file_unk1 = pres_reader.ReadInt32();
                                            int file_unk2 = pres_reader.ReadInt32();
                                            int file_unk3 = pres_reader.ReadInt32();
                                            int usize_file = pres_reader.ReadInt32();

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

                                            // Finally extract individual file...
                                            string file_folder = str_complete_final.Substring(0, str_complete_final.LastIndexOf("/") + 1);
                                            string folder_path_pres = folder_path + "\\" + (i + 1).ToString("D8") + "_" + hash.ToString("X16") + "\\" + file_folder;
                                            if (!Directory.Exists(folder_path_pres)) { Directory.CreateDirectory(folder_path_pres); }

                                            int shifted_offset = offset_file & ((1 << (32 - 4)) - 1);
                                            pres_reader.BaseStream.Seek(shifted_offset, SeekOrigin.Begin);
                                            byte[] data = pres_reader.ReadBytes(csize_file);
                                            File.WriteAllBytes(folder_path_pres + "\\" + str_name_final + "." + str_ext_final, data);

                                            // End loop
                                            pres_reader.BaseStream.Seek(reader_index_file + (k * 0x20), SeekOrigin.Begin);
                                        }
                                        if (count_set > 1) { pres_reader.BaseStream.Seek(reader_index_root + ((j + 1) * 8), SeekOrigin.Begin); }                                            
                                    }
                                }
                            }

                            // .blz4 unpacking
                            if (type_magic == 0x347a6c62)
                            {                                
                                string folder_path_blz = folder_path + "\\" + (i + 1).ToString("D8") + "_" + hash.ToString("X16");
                                if (!Directory.Exists(folder_path_blz)) { Directory.CreateDirectory(folder_path_blz); }

                                int chunk = 0;
                                int c_size = 0;
                                byte[] unknown = new byte[0x10];

                                using (BinaryReader blz_reader = new BinaryReader(File.Open(file, FileMode.Open)))
                                {
                                    // Get file info
                                    blz_reader.BaseStream.Seek(0x10, SeekOrigin.Current);
                                    unknown = blz_reader.ReadBytes(0x10);

                                    while (blz_reader.BaseStream.Position < size)
                                    {
                                        // Get compressed chunk
                                        c_size = blz_reader.ReadUInt16();
                                        byte[] data = new byte[c_size];
                                        data = blz_reader.ReadBytes(c_size);

                                        // Uncompress data
                                        string file_name_blz = (i + 1).ToString("D8") + "_" + hash.ToString("X16") + "_" + (chunk + 1).ToString("D2") + GetExtension(type_magic);

                                        MemoryStream stream = new MemoryStream(data);
                                        using (Stream extract = File.OpenWrite(folder_path_blz + "\\" + file_name_blz))
                                        {
                                            byte[] out_file;
                                            Helper.DecompressData(data, out out_file);
                                            extract.Write(out_file, 0, out_file.Length);
                                        }
                                        chunk++;
                                    }
                                }
                                // End
                                using (Stream unknown_stream = File.Create(folder_path_blz + "\\id.txt")) { unknown_stream.Write(unknown, 0, 0x10); }
                                Console.WriteLine("INFO: Detected and unpacked .blz4 file. {0} chunk(s).", chunk);
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

            // Create qpck
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

            // Repack blz4/pres
            if (mode == "-r")
            {
                // Check folder
                if (!Directory.Exists(input_str)) { Console.WriteLine("ERROR: Specified folder doesn't exist."); return; }

                Console.WriteLine("INFO: Repacking .blz4/.pres is currently not supported.");
                return;
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
            if (!extension_dic.TryGetValue(magic, out extension_str)) { extension_str = ".bin"; }

            return extension_str;
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
