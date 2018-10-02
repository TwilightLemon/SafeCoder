using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SafeCoder
{
    public class EPChangeEventArgs : EventArgs
    {
        public EPChangeEventArgs(double message) => EpsValue = message;
        public double EpsValue { get; }
    }
    public class EPClass
    {
        public static bool IsStop = false;
        public delegate void EPeChangedHandle(object sender, EPChangeEventArgs e);
        public event EPeChangedHandle EPValueChanged;
        public async Task EncryptAsync(string inFile, string outFile, string encryptKey)
        {
            FileInfo f = new FileInfo(inFile);
            string filename = f.Name.Substring(0, f.Name.IndexOf('.'));
            string dic = f.DirectoryName + "\\" + filename;
            Directory.CreateDirectory(dic);
            File.WriteAllText(dic + "\\md5",await MD5File(inFile));
            if (IsStop) return;
            File.WriteAllText(dic + "\\psw", MD5.EncryptToMD5string(MD5.EncryptToMD5string(encryptKey)));
            byte[] rgb = { 0x14, 0x28, 0x23, 0x98, 0x86, 0xCD, 0xDF, 0xEF };
            byte[] rgbKeys = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
            FileStream inFs = new FileStream(inFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            FileStream outFs = new FileStream(dic + "\\data", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            outFs.SetLength(0);
            byte[] byteIn = new byte[1024];
            long readLen = 0;
            long totalLen = inFs.Length;
            int everylen = 0;
            DES des = new DESCryptoServiceProvider();
            CryptoStream encStream = new CryptoStream(outFs, des.CreateEncryptor(rgb, rgbKeys), CryptoStreamMode.Write);
            while (readLen < totalLen)
            {
                if (IsStop) break;
                everylen = await inFs.ReadAsync(byteIn, 0, 1024);
                await encStream.WriteAsync(byteIn, 0, everylen);
                readLen = readLen + everylen;
                double fo = (double)readLen / (double)totalLen;
                EPValueChanged?.Invoke(this, new EPChangeEventArgs(fo));
            }
            encStream.Close();
            inFs.Close();
            outFs.Close();
            if (IsStop) { new DirectoryInfo(dic).Delete(true); return; }
            if (File.Exists(outFile + ".sc"))
                File.Delete(outFile + ".sc");
            var t = new Task(new Action(delegate
              {
                  ZipFile.CreateFromDirectory(dic, outFile + ".sc");
                  new DirectoryInfo(dic).Delete(true);
              }));
            t.Start();
            t.Wait();
        }
        public async static Task<string> MD5File(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int bufferSize = 1048576;
            byte[] buff = new byte[bufferSize];
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            md5.Initialize();
            long offset = 0;
            while (offset < fs.Length)
            {
                if (IsStop)
                    break;
                long readSize = bufferSize;
                if (offset + readSize > fs.Length) readSize = fs.Length - offset;
                await fs.ReadAsync(buff, 0, Convert.ToInt32(readSize));
                if (offset + readSize < fs.Length) md5.TransformBlock(buff, 0, Convert.ToInt32(readSize), buff, 0);
                else md5.TransformFinalBlock(buff, 0, Convert.ToInt32(readSize));
                offset += bufferSize;
            }
            fs.Close();
            if (IsStop) return "STOP";
            byte[] result = md5.Hash;
            md5.Clear();

            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < result.Length; i++)
                sb.Append(result[i].ToString("X2"));

            return sb.ToString();
        }
        public async static Task<bool> CopyFileAsync(string soucrePath, string targetPath)
        {
            try
            {
                using (FileStream fsRead = new FileStream(soucrePath, FileMode.Open, FileAccess.Read))
                    using (FileStream fsWrite = new FileStream(targetPath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        byte[] buffer = new byte[1024 * 1024 * 2];
                        while (true)
                        {
                            int n = await fsRead.ReadAsync(buffer, 0, buffer.Count());
                            if (n == 0)
                                break;
                            await fsWrite.WriteAsync(buffer, 0, n);
                        }
                    }
                return true;
            }
            catch { return false; }
        }
        public async Task<int> DecryptAsync(string inFile, string outFile, string encryptKey)
        {
            FileInfo f = new FileInfo(inFile);
            string dic = f.DirectoryName + "\\" + f.Name.Substring(0, f.Name.IndexOf('.'));
            Directory.CreateDirectory(dic);
            ZipFile.ExtractToDirectory(inFile, dic);
            string filename = dic + "\\data";
            string md5 = File.ReadAllText(dic + "\\md5");
            string psw = File.ReadAllText(dic + "\\psw");
            string _psw = MD5.EncryptToMD5string(MD5.EncryptToMD5string(encryptKey));
            if (psw == _psw)
            {
                byte[] rgb = { 0x14, 0x28, 0x23, 0x98, 0x86, 0xCD, 0xDF, 0xEF };
                byte[] rgbKeys = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
                FileStream inFs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                FileStream outFs = new FileStream(outFile + ".data", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                outFs.SetLength(0);
                byte[] byteIn = new byte[1024];
                long readLen = 0;
                long totalLen = inFs.Length;
                int everylen = 0;
                DES des = new DESCryptoServiceProvider();
                CryptoStream encStream = new CryptoStream(outFs, des.CreateDecryptor(rgb, rgbKeys), CryptoStreamMode.Write);
                while (readLen < totalLen)
                {
                    if (IsStop) break;
                    everylen = await inFs.ReadAsync(byteIn, 0, 1024);
                    await encStream.WriteAsync(byteIn, 0, everylen);
                    readLen = readLen + everylen;
                    double fo = (double)readLen / (double)totalLen;
                    EPValueChanged?.Invoke(this, new EPChangeEventArgs(fo));
                }
                encStream.Close();
                inFs.Close();
                outFs.Close();
                if (IsStop) { new DirectoryInfo(dic).Delete(true); return 3; }
                if (await MD5File(outFile + ".data") == md5)
                {
                    await CopyFileAsync(outFile + ".data", outFile);
                    File.Delete(outFile + ".data");
                    new DirectoryInfo(dic).Delete(true);
                    return 0;
                }
                else return 1;        
            }
            else { new DirectoryInfo(dic).Delete(true); return 2; }
        }
    }
}
