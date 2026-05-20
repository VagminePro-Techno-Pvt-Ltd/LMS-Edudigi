using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace TMS
{
    public static class EncriptorUtility
    {
        private const string _encriptionKey = "kZe13KlAdsee44erxll";

        public static string Encrypt(this string strToEncrypt, bool bURLEncode = true)
        {
            try
            {
                string sOutPut = string.Empty;
                var objDESCrypto = TripleDES.Create();
                var objHashMD5 = MD5.Create();
                byte[] byteHash, byteBuff;
                byteHash = objHashMD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(_encriptionKey));
                objHashMD5 = null;
                objDESCrypto.Key = byteHash;
                objDESCrypto.Mode = CipherMode.ECB;
                byteBuff = ASCIIEncoding.ASCII.GetBytes(strToEncrypt);
                sOutPut = Convert.ToBase64String(objDESCrypto.CreateEncryptor().
                    TransformFinalBlock(byteBuff, 0, byteBuff.Length));

                if (bURLEncode)
                    sOutPut = HttpUtility.UrlEncode(sOutPut);
                return sOutPut;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public static string Encrypt(this int intToEncrypt, bool bURLEncode = true)
        {
            string strToEncrypt = intToEncrypt.ToString();
            return strToEncrypt.Encrypt(bURLEncode);
        }
        public static string Encrypt(this int? intToEncrypt, bool bURLEncode = true)
        {
            if (!intToEncrypt.HasValue)
                return string.Empty;
            return intToEncrypt.Value.ToString().Encrypt(bURLEncode);
        }

        public static string Decrypt(this string strEncrypted, bool bURLDecode = true)
        {
            if (string.IsNullOrWhiteSpace(strEncrypted))
                return string.Empty;
            try
            {
                if (bURLDecode)
                {
                    strEncrypted = HttpUtility.UrlDecode(strEncrypted);
                    if (strEncrypted.Contains((string)" "))
                        strEncrypted = strEncrypted.Replace(" ", "+");
                }
                var objDESCrypto = TripleDES.Create();
                var objHashMD5 = MD5.Create();
                byte[] byteHash, byteBuff;
                byteHash = objHashMD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(_encriptionKey));
                objHashMD5 = null;
                objDESCrypto.Key = byteHash;
                objDESCrypto.Mode = CipherMode.ECB;
                byteBuff = Convert.FromBase64String(strEncrypted);
                string strDecrypted = ASCIIEncoding.ASCII.GetString(objDESCrypto.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
                objDESCrypto = null;
                return strDecrypted;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public static string ConvertToEmbedUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            try
            {
                var uri = new Uri(url);
                var host = uri.Host.ToLowerInvariant();

                // 🎥 Handle YouTube short URLs
                if (host.Contains("youtu.be"))
                {
                    var videoId = uri.AbsolutePath.Trim('/');
                    return $"https://www.youtube.com/embed/{videoId}";
                }

                // 🎥 Handle YouTube watch URLs
                if (host.Contains("youtube.com"))
                {
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var videoId = query["v"];
                    if (!string.IsNullOrEmpty(videoId))
                        return $"https://www.youtube.com/embed/{videoId}";
                }

                // 🌐 Vimeo embed (optional)
                if (host.Contains("vimeo.com"))
                {
                    var videoId = uri.AbsolutePath.Trim('/');
                    return $"https://player.vimeo.com/video/{videoId}";
                }

                // 🔗 Other URLs (e.g., direct MP4, stream, etc.) — keep as-is
                return url;
            }
            catch
            {
                return url;
            }
        }


    }
}