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
        static Visit[] visits;
        static Unit[] units;
        static UserSolutions[] solutions;
        static Slide[] slides;
        static DateTime beginning;



        static void Clean<T>(ref T[] array, Func<T,bool> filter)
        {
            array = array.Where(filter).ToArray();
        }

        static void Load()
        {
            slides = Loader.Load<Slide>(@"..\..\..\slides.csv");

            beginning = new DateTime(2014, 9, 6);

            units = slides.Select(z => z.UnitTitle).Distinct().Select(z => new Unit { Title = z }).ToArray();
            for (int i = 0; i < units.Length; i++)
            {
                units[i].Number = i;
                if (i < 2) units[i].PublishingData = new DateTime(2014, 9, 6);
                else units[i].PublishingData = new DateTime(2014, 9, 15).AddDays((i - 2) * 7);
            }
            foreach (var s in slides)
                s.UnitData = units.Where(z => z.Title == s.UnitTitle).First();


            visits = Loader.Load<Visit>(@"..\..\..\visits.csv");
            solutions = Loader.Load<UserSolutions>(@"..\..\..\usersolutions.csv");

            foreach (var e in visits)
                e.SlideInfo = slides.Where(z => z.Id == e.SlideId).FirstOrDefault();

            foreach(var e in solutions)
                e.SlideInfo = slides.Where(z => z.Id == e.SlideId).FirstOrDefault();

            Clean(ref visits, z => z.SlideInfo != null);
            Clean(ref solutions, z => z.SlideInfo != null);
        }


        static Dictionary<string, double> UserDistribution<T>(bool show, IEnumerable<T> array, Func<T, bool> filter, Func<T, string> userSelector, Func<IEnumerable<T>, double> agregator, int compression)
        {
            var data = array
              .Where(filter)
              .GroupBy(userSelector)
              .Select(z => new { UserId = z.Key, Value = agregator(z) })
              .OrderBy(z => z.Value)
              .ToDictionary(z => z.UserId, z => z.Value);

            if (show)
            {
                data
                  .GroupBy(z => (int)(z.Value / compression))
                  .ShowHisto(z => z.Key * compression, z => z.Count());
            }

            return data;
        }
           
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Load();

            /*
            //Исследуем, как по времени пользователи пользуются системой.

            //количество заходов на слайд в зависимости от времени, прошедшего с начала семестра
            visits
                .GroupBy(z => (int)((z.TimeStamp - beginning).TotalDays)/7)
                .Where(z=>z.Key>0)
                .ShowHisto(z => z.Key, z => z.Count());


            //Количество заходов на слайд в зависимости от времени, прошедшего с момента его публикации
            visits
                .GroupBy(z => (int)((z.TimeStamp - z.SlideInfo.UnitData.PublishingData).TotalDays)/7)
                .Where(z=>z.Key>0)
                .ShowHisto(z => z.Key, z => z.Count());
            */

            //В выборке до фига мусорных пользователей, которые курс до конца не то что не досмотрели, но даже не начали.
            var usersToSlideCount = UserDistribution(false,
                visits,
                visit => visit.SlideInfo.Type == SlideType.Lecture,
                visit => visit.UserId,
                z => z.Count(),
                10);
            //нас будут интересовать только пользователи, которые посмотрели хотя бы 50 лекций
            var interestingUsers = usersToSlideCount.Where(z => z.Value > 50).Select(z => z.Key).ToList();

            //Распределение пользователей по запаздыванию захода на слайд
            var usersToLatetime = UserDistribution(false,
                visits,
                visit=>interestingUsers.Contains(visit.UserId),
                visit=>visit.UserId,
                z=>z.Average(x => 1+(x.TimeStamp - x.SlideInfo.UnitData.PublishingData).TotalDays),
                7);
            //В основном пользователи, которые смотрели курс, смотрели его ВОВРЕМЯ, с запаздыванием до 1-2 недель. На всякий случай поставил отсечение на 40
            interestingUsers = usersToLatetime.Where(z => z.Value < 40).Select(z => z.Key).ToList();

            //После этих двух фильтраций у нас осталось 148 пользователей, что примерно совпадает с количеством студентов.
            Clean(ref solutions, z => interestingUsers.Contains(z.UserId));
            Clean(ref visits, z => interestingUsers.Contains(z.UserId));



            var usersToExercise = UserDistribution(false,
                solutions,
                solution => solution.IsRightAnswer,
                solution => solution.UserId,
                z => z.Count(),
                20);

            //На сколько недель в среднем продвинулись пользователи в упражнениях?
           UserDistribution(false,
                solutions,
                solution => true,
                solution => solution.UserId,
                z=>z.Max(x=>x.SlideInfo.UnitData.Number),
                1);

           
           solutions
               .GroupBy(z => z.SlideInfo)
               .ShowHisto(z => z.Key.Number, z => z.Count() - z.Count(x=>x.IsRightAnswer));
            


        }
    }
}