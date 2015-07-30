using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Statistics
{
    static class LinqExtensions
    {
        public static void ShowHisto<T>(this IEnumerable<T> data, Func<T, int> x, Func<T, int> y)
        {
            var chart = new Chart();
            chart.ChartAreas.Add(new ChartArea());

            var serie = new Series();

            foreach (var e in data)
                serie.Points.Add(new DataPoint(x(e), y(e)));

            serie.ChartType = SeriesChartType.Bar;
            chart.Series.Add(serie);
            chart.Dock = DockStyle.Fill;
            var form = new Form();
            form.Controls.Add(chart);
            Application.Run(form);
        }

        public static T ArgMax<T>(this IEnumerable<T> data, Func<T,int> selector)
        {
            return data.ArgMin(x=>-selector(x));
        }

        public static T ArgMin<T>(this IEnumerable<T> data, Func<T, int> selector)
        {
            T result = default(T);
            var measure = -1;
            bool first = true;
            foreach (var e in data)
            {
                if (first)
                {
                    result = e;
                    measure = selector(e);
                    first = false;
                    continue;
                }
                var tmeasure = selector(e);
                if (tmeasure < measure)
                {
                    result = e;
                    measure = tmeasure;
                }
            }
            if (first) throw new Exception("No elements");
            return result;
        }
    }

    static class Program
    {


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var slides = Loader.Load<Slide>(@"..\..\..\slides.csv");
           
            var visits = Loader.Load<Visit>(@"..\..\..\visits.csv");
           var solutions = Loader.Load<UserSolutions>(@"..\..\..\usersolutions.csv");
           
            foreach(var e in visits)
                e.SlideInfo = slides.Where(z=>z.Id==e.SlideId).FirstOrDefault();

            visits=visits.Where(z=>z.SlideInfo!=null).ToArray();

            //visits
            //    .GroupBy(z => z.UserId)
            //    .Select(z => new { UserId = z.Key, Count = z.Count()})
            //    .GroupBy(z => z.Count/10)
            //    .Select(z => new { CountOfTasks = 10*z.Key, CountOfUsers = z.Count() })
            //    .OrderBy(z=>z.CountOfTasks)
            //    .Skip(3)
            //    .ShowHisto(z => z.CountOfTasks, z => z.CountOfUsers);

            var lastSlides =
                visits
                .GroupBy(z => z.UserId)
                .Select(z => new { UserId = z.Key, Last = z.ArgMax(x => x.SlideInfo.Number) })
                .GroupBy(z => z.Last.SlideInfo.Number)
                .Select(z => new { LastTask = z.Key, CountOfUsers = z.Count() })
                .ToArray();

            lastSlides
                .OrderBy(z => z.LastTask)
                .ShowHisto(z => z.LastTask, z => z.CountOfUsers);

            //var hardProblems =
            //    lastSlides
            //    .OrderByDescending(z => z.CountOfUsers)
            //    .Take(20)
            //    .Select(z => z.LastTask.SlideInfo.Caption)
            //    .ToArray();
        }
    }
}