using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qpckEater
{
    class Program
    {
        static void Main(string[] args)
        {
            // Variables
            string mode = "";
            string qpck_folder = "";

            // Print header
            Console.WriteLine("qpckEater by MHVuze");
            Console.WriteLine("=========================");

            // Handle arguments
            if (args.Length < 2) { Console.WriteLine("ERROR: Not enough arguments specified.\nExtract: qpck -x <qpck_folder>"); return; }
            mode = args[0];
            qpck_folder = args[1];

            // Check mode
            if (mode != "-x" && mode != "-c") { Console.WriteLine("ERROR: Unsupported mode specified."); return; }

            // Check file
            if (Directory.Exists(qpck_folder) == false) { Console.WriteLine("ERROR: Specified qpck folder doesn't exist."); return; }

            // Unpacking
            if (mode == "-x")
            {
                // Iterate directory for required qpck's
                string[] qpck_files = Directory.GetFiles(qpck_folder, "*.qpck").Select(file => Path.GetFileName(file)).ToArray();
                Console.WriteLine("Found the following files:");
                foreach (string file in qpck_files) { Console.WriteLine(file); };
                Console.WriteLine("=========================");

                if (!qpck_files.Any("conf.qpck".Contains)) { Console.WriteLine("ERROR: conf.qpck is missing."); return; };
                if (!qpck_files.Any("data.qpck".Contains)) { Console.WriteLine("ERROR: data.qpck is missing."); return; };
                if (!qpck_files.Any("bin.qpck".Contains)) { Console.WriteLine("ERROR: bin.qpck is missing."); return; };

                //
            }
        }
    }
}
