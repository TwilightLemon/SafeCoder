using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SafeCoder
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            cs.EPValueChanged += Cs_EPValueChanged;
            var c = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
            this.BeginAnimation(OpacityProperty, c);
        }

        private void Cs_EPValueChanged(object sender, EPChangeEventArgs e)
        {
            pro.Value = e.EpsValue;
            if (e.EpsValue == 1)
                tbf.Text = "已完成";
        }

        private void CLOSE_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var c = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            c.Completed += delegate { Environment.Exit(0); };
            this.BeginAnimation(OpacityProperty, c);
        }

        private void window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
        string path = "";
        string vpath = "";
        private void window_Drop(object sender, DragEventArgs e)
        {
            path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            tbf.Text = "已选择文件:" + path;
            tbf.ToolTip = tbf.Text;
            ed= TxtFileEncoder.GetEncoding(path);
            FileInfo f = new FileInfo(path);
            vpath = path;
            bc.Text = "保存到:"+vpath;
            bc.ToolTip = bc.Text;
        }
        Encoding ed = Encoding.Default ;
        EPClass cs = new EPClass();
        private async void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            tbf.Text = "加密中...";
            if (path != "")
                await cs.EncryptAsync(path, vpath, MD5.EncryptToMD5string(psw.Password));
            else tbf.Text = "请选择加密文件";
        }

        private async void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            tbf.Text = "解密中...";
            if (path != "")
            {
                if (!path.Contains(".sc")) path += ".sc";
                await cs.DecryptAsync(path, vpath, MD5.EncryptToMD5string(psw.Password));
            }
            else tbf.Text = "请选择解密文件";
        }

        private void bc_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.SaveFileDialog();
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                vpath= openFileDialog.FileName;
                bc.Text = "保存到:"+vpath;
                bc.ToolTip= "保存到:" + vpath;
            }
        }
    }
    public class MD5
    {
        public static byte[] EncryptToMD5(string str)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] str1 = System.Text.Encoding.UTF8.GetBytes(str);
            byte[] str2 = md5.ComputeHash(str1, 0, str1.Length);
            md5.Clear();
            (md5 as IDisposable).Dispose();
            return str2;
        }
        public static string EncryptToMD5string(string str)
        {
            byte[] bytHash = EncryptToMD5(str);
            string sTemp = "";
            for (int i = 0; i < bytHash.Length; i++)
            {
                sTemp += bytHash[i].ToString("X").PadLeft(2, '0');
            }
            return sTemp.ToLower();
        }
    }
}
