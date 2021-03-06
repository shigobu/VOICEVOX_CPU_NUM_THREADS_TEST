using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net.Http;
using System.IO;
using System.Collections.ObjectModel;
using System.Reflection;

namespace VOICEVOX_CPU_NUM_THREADS_TEST
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// エンジンプロセス
        /// </summary>
        Process process = null;

        /// <summary>
        /// 唯一のHttpクライアント
        /// </summary>
        static HttpClient httpClient;

        /// <summary>
        /// パスワードの一覧
        /// </summary>
        public ObservableCollection<ListDataSource> ResultDataList { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            maxThreadTextBox.Text = Environment.ProcessorCount.ToString();
            SetTestThreadListFromMax();

            httpClient = new HttpClient();

            ResultDataList = new ObservableCollection<ListDataSource>();
            listView.DataContext = ResultDataList;

            //優先度コンボボックスの中身作成
            PriorityComboItem[] comboItems = new PriorityComboItem[]
            {
                new PriorityComboItem("リアルタイム", ProcessPriorityClass.RealTime),
                new PriorityComboItem("高", ProcessPriorityClass.High),
                new PriorityComboItem("通常以上", ProcessPriorityClass.AboveNormal),
                new PriorityComboItem("通常", ProcessPriorityClass.Normal),
                new PriorityComboItem("通常以下", ProcessPriorityClass.BelowNormal),
                new PriorityComboItem("低", ProcessPriorityClass.Idle)
            };
            priorityComboBox.ItemsSource = comboItems;
            priorityComboBox.SelectedValue = ProcessPriorityClass.Normal;
        }

        /// <summary>
        /// ステータスを変更します
        /// </summary>
        /// <param name="status"></param>
        private void SetStatus(string status)
        {
            statusText.Text = status;
            this.Title = status;
        }

        /// <summary>
        /// ウィンドウ終了時
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (process != null)
            {
                process.Kill();
                process = null;
            }

            if (httpClient != null)
            {
                httpClient.Dispose();
            }
        }

        /// <summary>
        /// エンジンパスの選択ボタン押下時のイベント
        /// </summary>
        private void SelectEnginePathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "実行ファイル (*.exe)|*.exe";
            openFileDialog.Title = "VOICEVOXエンジンの選択";
            if (openFileDialog.ShowDialog() ?? false)
            {
                selectedEnginePath.Text = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// 実行ボタンのイベント
        /// </summary>
        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            try
            {
                ResultDataList.Clear();

                string[] testThreadStrArray = testThreadListTextBox.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                List<int> threadList = new List<int>();
                foreach (string testThreadStr in testThreadStrArray)
                {
                    int.TryParse(testThreadStr, out int thread);
                    threadList.Add(thread);
                }

                string enginePath = selectedEnginePath.Text;
                if (!System.IO.File.Exists(enginePath))
                {
                    SetStatus("エンジンが見つかりません。");
                    return;
                }

                foreach (int thread in threadList)
                {
                    try
                    {
                        var startInfo = new ProcessStartInfo(enginePath, $"--cpu_num_threads={thread}");
                        startInfo.CreateNoWindow = !(showEngineWindowCheckBox.IsChecked ?? false);
                        startInfo.UseShellExecute = false;

                        process = Process.Start(startInfo);
                        process.PriorityClass = ((PriorityComboItem)priorityComboBox.SelectedItem).Class;

                        int.TryParse(intervalSecondsTextBox.Text, out int interval);
                        for (int i = interval; i > 0; i--)
                        {
                            SetStatus($"次のテストまであと{i}秒");
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }

                        SetStatus("テスト開始");
                        await ExecuteTest(thread);
                    }
                    finally
                    {
                        if (process != null)
                        {
                            process.Kill();
                            process = null;
                        }
                    }

                }
                SetStatus("テスト終了");
            }
            catch (Exception ex)
            {
                SetStatus("エラー:" + ex.Message);
                MessageBox.Show(this, ex.ToString(), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.IsEnabled = true;
            }
        }

        /// <summary>
        /// テストを実行し、結果を表示します。
        /// </summary>
        private async Task ExecuteTest(int numThread)
        {
            string speakingText = "日本国民は正当に選挙された国会における代表者を通じて行動しわれらとわれらの子孫のために諸国民との協和";
            Stopwatch stopwatch = new Stopwatch();

            List<long> timeRequiredList = new List<long>();
            for (int i = 0; i < 5; i++)
            {
                stopwatch.Start();
                await SendToVoiceVox(speakingText);
                stopwatch.Stop();

                long result = stopwatch.ElapsedMilliseconds;
                timeRequiredList.Add(result);
                SetStatus($"{i + 1}回目 {result}ミリ秒");

                stopwatch.Reset();
                await Task.Delay(100);
            }

            double averageTimeRequired = timeRequiredList.Average();
            ResultDataList.Add(new ListDataSource(numThread, timeRequiredList.Average(), Stdev(timeRequiredList)));
        }

        /// <summary>
        /// VOICEVOXへ音声合成の指示を送信します。
        /// </summary>
        /// <param name="text">読み上げ文字列</param>
        /// <param name="speakerNum">話者番号</param>
        static async Task SendToVoiceVox(string text, int speakerNum = 0)
        {
            string speakerString = speakerNum.ToString();

            var parameters = new Dictionary<string, string>()
            {
                { "text", text },
                { "speaker", speakerString },
            };
            string encodedParamaters = await new FormUrlEncodedContent(parameters).ReadAsStringAsync();
            using (HttpResponseMessage resultAudioQuery = await httpClient.PostAsync(@"http://127.0.0.1:50021/audio_query?" + encodedParamaters, null))
            {
                string resBodyStr = await resultAudioQuery.Content.ReadAsStringAsync();

                var content = new StringContent(resBodyStr, Encoding.UTF8, @"application/json");
                HttpResponseMessage resultSynthesis = await httpClient.PostAsync(@"http://127.0.0.1:50021/synthesis?speaker=" + speakerString, content);
                //音声の再生はしないので、即破棄。
                resultSynthesis.Dispose();
            }
        }

        /// <summary>
        /// 最大スレッド数テキストボックスでキーが押されて離された時
        /// </summary>
        private void MaxThreadTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SetTestThreadListFromMax();
            }
        }

        /// <summary>
        /// 最大スレッド数テキストボックスから、テストスレッドリストを作成し、更新します。
        /// </summary>
        /// <param name="maxThread"></param>
        private void SetTestThreadListFromMax()
        {
            int.TryParse(maxThreadTextBox.Text, out int maxThread);
            testThreadListTextBox.Text = "";
            for (int i = 1; i <= maxThread; i++)
            {
                testThreadListTextBox.Text += i.ToString() + ",";
            }
        }

        /// <summary>
        /// 標準偏差
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        /// <remarks>
        /// 参考
        /// https://qiita.com/c60evaporator/items/59ad0dbebc53c742a4c9
        /// </remarks>
        public double Stdev(List<long> src)
        {
            //平均値算出
            double mean = src.Average();
            //自乗和算出
            double sum2 = src.Select(a => a * a).Sum();
            //分散 = 自乗和 / 要素数 - 平均値^2
            double variance = sum2 / src.Count - mean * mean;
            //標準偏差 = 分散の平方根
            return Math.Sqrt(variance);
        }

        /// <summary>
        /// 保存ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "csvファイル(*.csv)|*.csv";
            saveFileDialog.InitialDirectory = GetAssemblyDirectoryName();
            saveFileDialog.FileName = "スレッド数違いによる所要時間の比較";

            if (saveFileDialog.ShowDialog() ?? false)
            {
                using(StreamWriter stream = new StreamWriter(saveFileDialog.FileName, false, Encoding.GetEncoding("Shift_JIS")))
                {
                    stream.WriteLine($"スレッド数,平均所要時間,標準偏差");

                    foreach (ListDataSource data in ResultDataList)
                    {
                        stream.WriteLine($"{data.ThreadNum},{data.AverageTimeRequired},{data.StandardDeviation}");
                    }
                }
            }
        }

        /// <summary>
        /// 自分自身のあるフォルダを返します。
        /// </summary>
        /// <returns></returns>
        private string GetAssemblyDirectoryName()
        {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            return Path.GetDirectoryName(myAssembly.Location);
        }
    }
}
