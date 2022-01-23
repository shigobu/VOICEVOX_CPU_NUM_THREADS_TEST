using System;
using System.Diagnostics;

namespace VOICEVOX_CPU_NUM_THREADS_TEST
{
    class PriorityComboItem
    {
        public PriorityComboItem(string name, ProcessPriorityClass @class)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Class = @class;
        }

        /// <summary>
        /// 優先度の日本語名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 優先度列挙体
        /// </summary>
        public ProcessPriorityClass Class { get; set; }
    }
}
