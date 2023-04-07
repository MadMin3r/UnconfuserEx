using dnlib.DotNet;
using dnlib.DotNet.Emit;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnConfuserEx
{
    internal class Utils
    {
        private static ILog Logger = LogManager.GetLogger("Utils");

        public static byte[]? DecompressDataLZMA(byte[] data)
        {
            var stream = new MemoryStream(data);
            var decompressedStream = new MemoryStream();
            var decoder = new SevenZip.Compression.LZMA.Decoder();

            var properties = new byte[5];
            if (stream.Read(properties, 0, 5) != 5)
            {
                Logger.Error("LZMA stream is too short");
                return null;
            }
            decoder.SetDecoderProperties(properties);

            // TODO: I've seen some binaries where this is actually a long
            //       so should probably handle both cases?

            byte[] size = new byte[4];
            if (stream.Read(size, 0, sizeof(int)) != sizeof(int))
            {
                Logger.Error("Failed to read stream length");
                return null;
            }

            long uncompressedSize = BitConverter.ToInt32(size);

            long compressedSize = stream.Length - stream.Position;
            decoder.Code(stream, decompressedStream, compressedSize, uncompressedSize, null);
            return decompressedStream.ToArray();
        }

        private static char[] InvalidChars = "!@#$%^&*()-=+\\,<>".ToArray();

        public static bool IsInvalidName(string name)
        {
            return Encoding.UTF8.GetByteCount(name) != name.Length
                    || (name.Any(c => InvalidChars.Contains(c)));
        }
    }
}
