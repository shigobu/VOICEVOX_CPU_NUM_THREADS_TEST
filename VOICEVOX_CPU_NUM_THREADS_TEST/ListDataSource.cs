namespace VOICEVOX_CPU_NUM_THREADS_TEST
{
    public class ListDataSource
    {
        public ListDataSource(int threadNum, double averageTimeRequired, double standardDeviation)
        {
            ThreadNum = threadNum;
            AverageTimeRequired = averageTimeRequired;
            StandardDeviation = standardDeviation;
        }

        /// <summary>
        /// スレッド数
        /// </summary>
        public int ThreadNum { get; set; }

        /// <summary>
        /// 平均所要時間
        /// </summary>
        public double AverageTimeRequired { get; set; }

        /// <summary>
        /// 標準偏差
        /// </summary>
        public double StandardDeviation { get; set; }
    }
}