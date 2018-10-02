using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
            if (!EPClass.IsStop)
            {
                if (EnorDe == 1)
                    title.Text = $"ヾ(•ω•`)o 加密中... ({(int)(e.EpsValue * 100)}%)";
                else title.Text = $"ヾ(•ω•`)o 解密中... ({(int)(e.EpsValue * 100)}%)";
                pro.Value = e.EpsValue;
                if (e.EpsValue == 1)
                { tbf.Text = "完成啦 o((>ω< ))o"; title.Text = "SafeCoder"; }
            }
        }

        private void CLOSE_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var c = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            c.Completed += delegate { Environment.Exit(0); };
            this.BeginAnimation(OpacityProperty, c);
        }
        private void enPageEx() {
            title.SetBinding(TextBlock.TextProperty, "tbf");
            StartPage.Visibility = Visibility.Collapsed;
            enPage.Visibility = Visibility.Visible;
            (Resources["Begin"] as Storyboard).Begin();
        }
        private void StartPageEx()
        {
            title.Text = "SafeCoder";
            StartPage.Visibility = Visibility.Visible;
            enPage.Visibility = Visibility.Collapsed;
            (Resources["Begin"] as Storyboard).Stop();
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
            path = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            tbf.Text = "已选择文件:" + path;
            tbf.ToolTip = tbf.Text;
            ed= TxtFileEncoder.GetEncoding(path);
            FileInfo f = new FileInfo(path);
            if (path.Contains(".sc"))
                vpath = path.Replace(".sc", "");
            else vpath = path;
            bc.Text = "保存到:"+vpath;
            bc.ToolTip = bc.Text;
        }
        Encoding ed = Encoding.Default ;
        EPClass cs = new EPClass();
        int EnorDe = 0;
        private async void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EPClass.IsStop = false;
            enPageEx();
            EnorDe = 1;
            tbf.Text = "序列化文件中...ヾ(≧▽≦*)o";
            if (path != "")
                await cs.EncryptAsync(path, vpath, MD5.EncryptToMD5string(psw.Password));
            else tbf.Text = "请选择加密文件 (￣▽￣)\"";
            StartPageEx();
        }

        private async void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            if (path != "")
            {
                enPageEx();
                EPClass.IsStop = false;
                EnorDe = 0;
                tbf.Text = "解密中... ヾ(≧▽≦*)o";
                int result =await cs.DecryptAsync(path, vpath, MD5.EncryptToMD5string(psw.Password));
                if (result == 1)
                    tbf.Text = "文件损坏惹 (＃°Д°)";
                if (result==2) tbf.Text = "密码错误哦 (＃°Д°)";
                StartPageEx();
            }
            else tbf.Text = "请选择解密文件  (￣▽￣)\"";
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

        private void Border_MouseDown_2(object sender, MouseButtonEventArgs e)
        {
            EPClass.IsStop = true;
            tbf.Text = "取消啦 (/▽＼)";
            pro.Value = 0;
            StartPageEx();
        }
    }
    public class MD5
    {
        public static byte[] EncryptToMD5(string str)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] str1 = Encoding.UTF8.GetBytes(str);
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
