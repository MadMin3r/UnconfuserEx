namespace UnConfuserEx.Protections.Constants
{
    internal class NormalDecryptor : IDecryptor
    {

        public byte[] DecryptData(uint[] data, uint[] key)
        {
            uint[] temp = new uint[key.Length];
            byte[] ret = new byte[data.Length << 2];
            int s = 0, d = 0;
            while (s < data.Length)
            {
                for (int j = 0; j < 16; j++)
                {
                    temp[j] = data[s + j];
                }
                temp[0] = temp[0] ^ key[0];
                temp[1] = temp[1] ^ key[1];
                temp[2] = temp[2] ^ key[2];
                temp[3] = temp[3] ^ key[3];
                temp[4] = temp[4] ^ key[4];
                temp[5] = temp[5] ^ key[5];
                temp[6] = temp[6] ^ key[6];
                temp[7] = temp[7] ^ key[7];
                temp[8] = temp[8] ^ key[8];
                temp[9] = temp[9] ^ key[9];
                temp[10] = temp[10] ^ key[10];
                temp[11] = temp[11] ^ key[11];
                temp[12] = temp[12] ^ key[12];
                temp[13] = temp[13] ^ key[13];
                temp[14] = temp[14] ^ key[14];
                temp[15] = temp[15] ^ key[15];
                for (int j = 0; j < 16; j++)
                {
                    uint t = temp[j];
                    ret[d++] = (byte)t;
                    ret[d++] = (byte)(t >> 8);
                    ret[d++] = (byte)(t >> 16);
                    ret[d++] = (byte)(t >> 24);
                    key[j] ^= t;
                }
                s += 16;
            }
            return ret;
        }

    }
}
