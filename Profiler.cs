using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NProfiler.Utilities
{
    public static class Profiler
    {
        private static long? firstStart = null;
        private static List<(string, long, long, long, long)> logs;
        private static Stopwatch _macrosw;
        private static Stopwatch metasw;

        private static long macroTotal = 0L;
        private static StackTrace restartEx, logEx, parseEx;
        public static void Restart() 
        {
#if DEBUG
            restartEx = new StackTrace();
            Console.Beep(50, 500);
            Ensure();
            metasw.Restart();
            logs.Clear();
#endif
        }

        public static void Log(string msg)
        {
#if DEBUG
            logEx = new StackTrace(); 
            Ensure();
            var el = _macrosw.ElapsedMilliseconds;
            var mel = metasw.ElapsedMilliseconds;
            macroTotal += el;
            logs.Add((msg, el, macroTotal, metasw.ElapsedMilliseconds, macroTotal - mel));
            _macrosw.Restart();
            if (firstStart == null && mel > 0)
                firstStart = mel;
#endif
        }

        private static void Ensure()
        {
#if DEBUG
            logs ??= new();
            _macrosw ??= new Stopwatch();
            metasw ??= new Stopwatch();
#endif
        }

        public static void Parse(bool restart) => Parse(restart);

        public static void Parse(string fileName) => Parse(false, fileName);

        /// <summary>
        /// Output file
        /// </summary>
        /// <param name="fileName">do not use : in file name, will truncate output, do not use invalid chars like </param>
        public static void Parse(bool restart = false, string fileName = "")
        {
#if DEBUG
            parseEx = new StackTrace(); 
            //For human stuff
            var finalTime = metasw.ElapsedMilliseconds;
            var _info = JsonConvert.SerializeObject(logs.ToList()); //to list to prevent breakage in certain circumstances of concurrent access
            var _infoSorted = logs.OrderByDescending(z => z.Item2).ToList();
            var _infoSort = JsonConvert.SerializeObject(_infoSorted);

            var metatd = logs.GroupBy(z => z.Item1)
                .ToDictionary(z => z.Key, z => z.ToList());
            var metaat = metatd.Select(z =>
            {
                var v = metatd[z.Key];
                return new
                {
                    LogMsg = z.Key,
                    Min = v.Min(i => i.Item2),
                    Max = v.Max(i => i.Item2),
                    Avg = v.Average(i => i.Item2),
                    Total = v.Sum(i => i.Item2),
                    Count = v.Count
                };
            }).OrderByDescending(z => z.Total)
            .ToList();

            var total = metaat.Sum(z => z.Total);

            var _meta_td_dict = JsonConvert.SerializeObject(metaat, Formatting.Indented);

            Console.Beep();
            //Debugger.Break();

            var stacktrace = new StackTrace();
            //Debugger.Launch();

            var dateStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var writefileName = $@"C:\dev\profiling output\{dateStamp}.{(int)total / 1000}.{fileName}.json";
            var fileBody = $"{fileName} FinalTime: {finalTime}, TotalTime: {total}{Environment.NewLine}{_meta_td_dict}{Environment.NewLine}{stacktrace}";
            File.WriteAllText(writefileName, fileBody);

            logs = null;
            _macrosw = null;
            metasw = null;

            if (restart) Restart();
#endif
            //throw new Exception("Debug kill thread");
        }
    }
}
