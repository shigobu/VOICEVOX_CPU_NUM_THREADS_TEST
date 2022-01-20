using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Diagnostics;

namespace VOICEVOX_CPU_NUM_THREADS_TEST
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            int processorCount = Environment.ProcessorCount;
            maxThreadTextBox.Text = processorCount.ToString();

            for (int i = 1; i <= processorCount; i++)
            {
                testThreadListTextBox.Text += i.ToString() + ",";
            }
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
            string enginePath = selectedEnginePath.Text;

            Process.Start(enginePath);

            int.TryParse(intervalSecondsTextBox.Text, out int interval);

            for (int i = interval; i > 0; i--)
            {
                SetStatus($"次のテストまであと{i}秒");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            SetStatus("テスト開始");
        }
    }
}
