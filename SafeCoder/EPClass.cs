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
        private double EPe;
        public EPChangeEventArgs(double message)
        {
            this.EPe = message;
        }
        public double EpsValue
        {
            get { return EPe; }
        }
    }
    public class EPClass
    {
        public delegate void EPeChangedHandle(object sender, EPChangeEventArgs e);
        public event EPeChangedHandle EPValueChanged;
        public async Task EncryptAsync(string inFile, string outFile, string encryptKey)
        {
            FileInfo f = new FileInfo(inFile);
            string filename = f.Name.Substring(0, f.Name.IndexOf('.'));
            string dic = f.DirectoryName + "\\" + filename;
            Directory.CreateDirectory(dic);
            File.WriteAllText(dic + "\\md5", MD5File(inFile));
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
                everylen = await inFs.ReadAsync(byteIn, 0, 1024);
                await encStream.WriteAsync(byteIn, 0, everylen);
                readLen = readLen + everylen;
                double fo = (double)readLen / (double)totalLen;
                EPValueChanged?.Invoke(this, new EPChangeEventArgs(fo));
            }
            encStream.Close();
            inFs.Close();
            outFs.Close();
            ZipFile.CreateFromDirectory(dic, outFile+".sc");
            new DirectoryInfo(dic).Delete(true);
        }
        public static string MD5File(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int bufferSize = 1048576;
            byte[] buff = new byte[bufferSize];
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            md5.Initialize();
            long offset = 0;
            while (offset < fs.Length)
            {
                long readSize = bufferSize;
                if (offset + readSize > fs.Length) readSize = fs.Length - offset;
                fs.Read(buff, 0, Convert.ToInt32(readSize));
                if (offset + readSize < fs.Length) md5.TransformBlock(buff, 0, Convert.ToInt32(readSize), buff, 0);
                else md5.TransformFinalBlock(buff, 0, Convert.ToInt32(readSize));
                offset += bufferSize;
            }
            fs.Close();
            byte[] result = md5.Hash;
            md5.Clear();

            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < result.Length; i++)
                sb.Append(result[i].ToString("X2"));

            return sb.ToString();
        }
        public async Task<bool> DecryptAsync(string inFile, string outFile, string encryptKey)
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
                    everylen = await inFs.ReadAsync(byteIn, 0, 1024);
                    await encStream.WriteAsync(byteIn, 0, everylen);
                    readLen = readLen + everylen;
                    double fo = (double)readLen / (double)totalLen;
                    EPValueChanged?.Invoke(this, new EPChangeEventArgs(fo));
                }
                encStream.Close();
                inFs.Close();
                outFs.Close();
                if (MD5File(outFile + ".data") == md5)
                {
                    File.Copy(outFile + ".data", outFile, true);
                    File.Delete(outFile + ".data");
                    new DirectoryInfo(dic).Delete(true);
                }
                return true;
            }
            else { new DirectoryInfo(dic).Delete(true); return false; }
        }
    }
}
