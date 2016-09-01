using System;
using System.IO;
using zlib;

namespace qpckEater
{
    class Helper
    {
        // partially from http://stackoverflow.com/a/6627194/5343630
        public static void DecompressData(byte[] inData, out byte[] outData)
        {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                CopyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[1000000];
            int len;
            while ((len = input.Read(buffer, 0, 1000000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
    }
}
