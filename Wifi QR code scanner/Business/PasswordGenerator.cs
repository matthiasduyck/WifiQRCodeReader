using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace Wifi_QR_code_scanner.Business
{
    public class PasswordGenerator
    {
        public static string GenerateRandomPassword(int length, string allowedChars)
        {
            //// Define the length, in bytes, of the buffer.
            //uint lengthBytes = (uint)length * 5;

            //// Generate random data and copy it to a buffer.
            //IBuffer buffer = CryptographicBuffer.GenerateRandom(lengthBytes);

            //// Encode the buffer to a hexadecimal string (for display).
            //string randomHex = CryptographicBuffer.EncodeToHexString(buffer);

            //return randomHex;

            var sb = new StringBuilder(length);
            using (var rng = new RNGCryptoServiceProvider())
            {
                int count = (int)Math.Ceiling(Math.Log(allowedChars.Length, 2) / 8.0);
                int offset = BitConverter.IsLittleEndian ? 0 : sizeof(uint) - count;
                int max = (int)(Math.Pow(2, count * 8) / allowedChars.Length) * allowedChars.Length;
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (sb.Length < length)
                {
                    rng.GetBytes(uintBuffer, offset, count);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    if (num < max)
                    {
                        sb.Append(allowedChars[(int)(num % allowedChars.Length)]);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
