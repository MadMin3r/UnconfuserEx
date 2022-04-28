namespace UnConfuserEx.Protections.Constants
{
    internal interface IDecryptor
    {

        byte[] DecryptData(uint[] data, uint[] key);

    }
}
