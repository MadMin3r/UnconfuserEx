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

        public static (IList<T> split, IList<T> remaining) SplitArray<T>(IList<T> array, int index)
        {
            return (array.Take(index).ToList(), array.Skip(index).ToList());
        }

        public static OpCode InvertBranch(OpCode branch)
        {
            switch (branch.Code)
            {
                case Code.Beq:
                    return OpCodes.Bne_Un;
                case Code.Bge:
                    return OpCodes.Blt;
                case Code.Bge_Un:
                    return OpCodes.Blt_Un;
                case Code.Bgt:
                    return OpCodes.Ble;
                case Code.Bgt_Un:
                    return OpCodes.Ble_Un;
                case Code.Ble:
                    return OpCodes.Bgt;
                case Code.Ble_Un:
                    return OpCodes.Bgt_Un;
                case Code.Blt:
                    return OpCodes.Bge;
                case Code.Blt_Un:
                    return OpCodes.Bge_Un;
                case Code.Bne_Un:
                    return OpCodes.Beq;

                case Code.Brtrue:
                    return OpCodes.Brfalse;
                case Code.Brfalse:
                    return OpCodes.Brtrue;

                case Code.Beq_S:
                    return OpCodes.Bne_Un_S;
                case Code.Bge_S:
                    return OpCodes.Blt_S;
                case Code.Bge_Un_S:
                    return OpCodes.Blt_Un_S;
                case Code.Bgt_S:
                    return OpCodes.Ble_S;
                case Code.Bgt_Un_S:
                    return OpCodes.Ble_Un_S;
                case Code.Ble_S:
                    return OpCodes.Bgt_S;
                case Code.Ble_Un_S:
                    return OpCodes.Bgt_Un_S;
                case Code.Blt_S:
                    return OpCodes.Bge_S;
                case Code.Blt_Un_S:
                    return OpCodes.Bge_Un_S;
                case Code.Bne_Un_S:
                    return OpCodes.Beq_S;

                case Code.Brtrue_S:
                    return OpCodes.Brfalse_S;
                case Code.Brfalse_S:
                    return OpCodes.Brtrue_S;

                default:
                    throw new NotSupportedException($"Can't invert branch opcode {branch}");
            }
        }

        public static int Mod(int x, int n)
        {
            return (x % n + n) % n;
        }

        private static char[] InvalidChars = "!@#$%^&*()-=+\\,<>".ToArray();

        public static bool IsInvalidName(string name)
        {
            return Encoding.UTF8.GetByteCount(name) != name.Length
                    || (name.Any(c => InvalidChars.Contains(c)));
        }
    }
}
