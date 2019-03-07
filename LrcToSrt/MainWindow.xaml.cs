using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace LrcToSrt
{
    /// <summary>
    /// 主界面
    /// </summary>
    public partial class MainWindow : Window
    {

        #region 初始化

        // 歌词列表
        private ObservableCollection<LRC> _LrcList = new ObservableCollection<LRC>();
        public ObservableCollection<LRC> LrcList { get { return _LrcList; } }

        // 字幕行数
        public int Index { get; set; }

        // 字幕文件名
        public string FileName
        {
            get { return _FileName; }
            set
            {
                value = value.Trim();                
                if (value.Length > 4 && value.Substring(value.Length - 4, 4) == ".srt")
                    _FileName = value;
                else
                    _FileName = value + ".srt";
            }
        }
        private string _FileName = "新建字幕文件.srt";
        private string a;

        // 构造函数
        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region 前台交互

        // 添加LRC
        private void BTN_Add_Click(object sender, RoutedEventArgs e)
        {            
            var dialog = new OpenFileDialog();
            dialog.Title = "请选择要导入的LRC文件";
            dialog.Filter = "歌词文件(*.lrc)|*.lrc";
            dialog.Multiselect = true;
            dialog.FileOk += (obj, arg) =>
            {
                var files = dialog.FileNames.Except(LrcList.Select(c => c.Path)).ToArray();
                if (files.Length != 0)
                {
                    for (int i = 0; i < files.Length; ++i)
                        LrcList.Add(new LRC(files[i], LrcList.Count + 1));
                    TB_Message.Text = "添加成功，共添加了" + files.Length + "个新文件！";
                }
                else
                    TB_Message.Text = "没有文件需要添加！";
            };
            dialog.ShowDialog();
        }

        // 移除歌词
        private void BTN_Del_Click(object sender, RoutedEventArgs e)
        {
            var items = LV_Lrc.SelectedItems;
            while (items.Count != 0)
                LrcList.Remove(items[0] as LRC);
            for (int i = 0; i < LrcList.Count; )
                LrcList[i].Rank = ++i;
            TB_Message.Text = "移除成功！";
        }

        // 选中lrc
        private void LV_Lrc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LV_Lrc.SelectedItem != null)
            {
                BTN_Del.IsEnabled = true;
                BTN_Up.IsEnabled = true;
                BTN_Down.IsEnabled = true;
            }
            else
            {
                BTN_Del.IsEnabled = false;
                BTN_Up.IsEnabled = false;
                BTN_Down.IsEnabled = false;
            }
        }

        // 上移
        private void BTN_Up_Click(object sender, RoutedEventArgs e)
        {
            var lrc = LV_Lrc.SelectedItem as LRC;
            if (lrc != null && lrc.Rank > 1)
            {
                int index = LrcList.IndexOf(lrc);
                var upperItem = LrcList[index - 1];
                upperItem.Rank = lrc.Rank--;
                LrcList.RemoveAt(index - 1);
                LrcList.Insert(index, upperItem);
                TB_Message.Text = "上移成功!";
            }
            else
                TB_Message.Text = "不需要移动!";
        }

        // 下移
        private void BTN_Down_Click(object sender, RoutedEventArgs e)
        {
            var lrc = LV_Lrc.SelectedItem as LRC;
            if (lrc != null && lrc.Rank < LrcList.Last().Rank)
            {
                int index = LrcList.IndexOf(lrc);
                var lowerItem = LrcList[index + 1];
                lowerItem.Rank = lrc.Rank++;
                LrcList.RemoveAt(index + 1);
                LrcList.Insert(index, lowerItem);
                TB_Message.Text = "下移成功!";
            }
            else
                TB_Message.Text = "不需要移动!";
        }

        //教程
        private  void BTN_Helper_Click(object sender, RoutedEventArgs e)
        {
           System.Diagnostics.Process.Start("https://www.bilibili.com/video/av45475611");  
        }


        #endregion

        #region 生成SRT

        // 生成
        private async void BTN_Produce_Click(object sender, RoutedEventArgs e)
        {
            if (LrcList.Count != 0)
            {
                TB_Message.Text = "准备导出，请稍候...";
                // 检查文件名
                Regex fileReg = new Regex("^[^\\*\\\\/:?<>|\"]*$");
                if (!fileReg.IsMatch(FileName))
                {
                    TB_Message.Text = "文件名不合法：不能包含 \\ / : * ? \" < > | 字符！";
                    TBX_FileName.Focus(); TBX_FileName.SelectAll();
                    return;
                }
                // 导出
                try
                {
                    using (var ms = await CreateStream())
                    using (FileStream fs = new FileStream(FileName, FileMode.Create))
                    {
                        ms.WriteTo(fs);
                        TB_Message.Text = "导出成功！";
                        if (MessageBoxResult.Yes == MessageBox.Show("是否打开导出的SRT所在目录？", "打开", MessageBoxButton.YesNo, MessageBoxImage.Question))
                        {
                            Process p = new Process();
                            ProcessStartInfo pi = new ProcessStartInfo();
                            pi.Verb = "Open";
                            pi.CreateNoWindow = false;
                            pi.FileName = ".";
                            p.StartInfo = pi;
                            p.Start();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    TB_Message.Text = "导出失败：" + ex.Message;
                }
            }
            else
                TB_Message.Text = "没有文件需要导出！";
        }

        // 新建工作流
        private async Task<MemoryStream> CreateStream()
        {
            var stream = new MemoryStream();
            TimeSpan curTime = new TimeSpan();
            Index = 1;
            foreach (var lrc in LrcList)
                curTime = await WriteStream(stream, curTime, lrc);
            return stream;
        }

        // 读写数据
        private async Task<TimeSpan> WriteStream(MemoryStream stream, TimeSpan startTime, LRC lrc)
        {
            var baseTime = startTime.Add(new TimeSpan(0, 0, 0, 0, lrc.Delay));
            var preTime = baseTime;
            bool isFirstLine = true;
            string preStr = "pp";
            StreamReader reader = new StreamReader(lrc.Path, Encoding.UTF8);

            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
            Regex timeReg = new Regex(@"(?<=^\[)(\d|\:|\.)+(?=])");
            Regex strReg = new Regex(@"(?<=]).+", RegexOptions.RightToLeft);
            do
            {
                try
                {
                    string line = await reader.ReadLineAsync();
                    line = line.Trim();
                    if (line != "")
                    {
                        var match = timeReg.Match(line);
                        // 是时间
                        if (match.Success)
                        {
                            // 计时
                            if (isFirstLine)
                            {
                                preTime = baseTime.Add(TimeSpan.Parse("00:" + match.Value));        // 第一行
                                isFirstLine = false;
                            }
                            else
                            {
                                if (!preStr.Equals("")) {
                                    var curTime = baseTime.Add(TimeSpan.Parse("00:" + match.Value));    // 歌词行                                
                                                                                                        // 写入前一行的歌词  LRC格式01:48.292  SRT格式00:01:48,292
                                    await writer.WriteAsync((Index++).ToString() + "\n" +
                                        string.Format("{0:d2}:{1:d2}:{2:d2},{3:d3}", preTime.Hours, preTime.Minutes, preTime.Seconds, preTime.Milliseconds) + " --> " +
                                        string.Format("{0:d2}:{1:d2}:{2:d2},{3:d3}", curTime.Hours, curTime.Minutes, curTime.Seconds, curTime.Milliseconds) + "\n" +
                                        preStr + "\n\n");
                                    await writer.FlushAsync();
                                    preTime = curTime;
                                }
                            }
                            // 歌词
                            var strMatch = strReg.Match(line);
                            preStr = strMatch.Success ? strMatch.Value : "";

                        }
                        else
                        {
                            Regex offsetReg = new Regex(@"(?<=^\[offset:)\d+(?=])");
                            match = offsetReg.Match(line);
                            // 是延时
                            if (match.Success)
                            {
                                var offset = Convert.ToInt32(match.Value);
                                baseTime = baseTime.Add(new TimeSpan(0, 0, 0, 0, offset));
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    TB_Message.Text = "转化时遇到了一个错误，行：" + Index + ",错误：" + ex.Message;
                }
            } while (!reader.EndOfStream);
            // 根据歌曲长度延长时间
            var addTime = lrc.Length + baseTime - preTime;
            if (addTime.TotalMilliseconds > 0)
                preTime = preTime.Add(addTime);
            // 返回新的起始时间
            return preTime;
        }

        #endregion       

    }
}
