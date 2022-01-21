namespace VOICEVOX_CPU_NUM_THREADS_TEST
{
    public class ListDataSource
    {
        public ListDataSource(int threadNum, double averageTimeRequired)
        {
            ThreadNum = threadNum;
            AverageTimeRequired = averageTimeRequired;
        }

        /// <summary>
        /// スレッド数
        /// </summary>
        public int ThreadNum { get; set; }

        /// <summary>
        /// 平均所要時間
        /// </summary>
        public double AverageTimeRequired { get; set; }
    }
}