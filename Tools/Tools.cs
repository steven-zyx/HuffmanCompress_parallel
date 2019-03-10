using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Tool
{
    public static class Tools
    {
        public static string GetDigest(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
                byte[] data = md5Hasher.ComputeHash(stream);
                StringBuilder sb = new StringBuilder(data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
