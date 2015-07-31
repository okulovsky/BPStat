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

		public static void ShowDistribution<T>(this IEnumerable<T> data, Func<T, double> value, Func<T, bool> filter, int compression)
		{
			data
				.Where(filter)
				.GroupBy(z => (int)(value(z) / compression))
				.ShowHisto(z => z.Key * compression, z => z.Count());
		}

		public static Tuple<IEnumerable<T>,Func<object,string>[]> Prepare<T>(this IEnumerable<T> data, params string[] captions)
		{
			var list=new List<Func<object,string>>();
			foreach(var e in captions)
			{
				Console.Write("{0} ",e);
				list.Add(new Func<object,string>(o=>
					{
						var s = o.ToString();
						if (s.Length>e.Length) s=s.Substring(0,e.Length);
						var fmt=("{0,"+e.Length+"}");
						s=string.Format(fmt,s);
						return s;
					}));
				
			}
			Console.WriteLine();
			return Tuple.Create(data, list.ToArray());
		}
	
		public static void Output<T>(this Tuple<IEnumerable<T>,Func<object,string>[]> prep, bool skip, params Func<T,object>[] selectors)
		{
			if (skip) return;
			var data = prep.Item1;
			var fms = prep.Item2;
			if (fms.Length != selectors.Length) throw new ArgumentException();
			

			foreach(var e in data)
			{
				for (int i = 0; i < selectors.Length; i++)
				{
					var s = fms[i](selectors[i](e));
					Console.Write(s + " ");
				}
				Console.WriteLine();
			}
		}


		public static T ArgMax<T>(this IEnumerable<T> data, Func<T, int> selector)
		{
			return data.ArgMin(x => -selector(x));
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



		static void Clean<T>(ref T[] array, Func<T, bool> filter)
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

			foreach (var e in solutions)
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

		class UserVisitingData
		{
			public int ViewedLecturesCount { get; set; }
			public double LecturesViewsLate { get; set; }
			public bool GoodUser { get; set; }
		}

		static UserVisitingData ProcessUserVisits(IEnumerable<Visit> visits)
		{
			var data = new UserVisitingData();
			data.ViewedLecturesCount = visits.Count();
			data.LecturesViewsLate = visits.Average(x => 1 + (x.TimeStamp - x.SlideInfo.UnitData.PublishingData).TotalDays);
			return data;
		}


		class UserSolutionsData
		{
			public Slide LastExercise { get; set; }
		}

		static UserSolutionsData ProcessUserSolutions(IEnumerable<UserSolutions> solutions)
		{
			var data = new UserSolutionsData();
			data.LastExercise = solutions.Where(z => z.SlideInfo.Type == SlideType.Exercise).ArgMax(z => z.SlideInfo.Number).SlideInfo;
			return data;
		}

		class ExerciseData
		{
			public double FailurePercentage { get; set; }
			public double AverageAttempts { get; set; }
			public double MaxAttempts { get; set; }
			public double Popularity { get; set; }
			public int CountOfBreakdownTimes { get; set; }
		}

		static ExerciseData ProcessExerciseAttempts(this IEnumerable<Visit> v)
		{
			var data = new ExerciseData();
			data.Popularity = v.Count();
			data.FailurePercentage = (double)v.Count(z=>!z.IsPassed)/data.Popularity;
			data.MaxAttempts = v.Max(z => z.AttemptsCount);
			data.AverageAttempts = v.Average(z => z.AttemptsCount);
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
			var visiting = visits
			  .GroupBy(v => v.UserId)
			  .ToDictionary(z => z.Key, ProcessUserVisits);
             
            //В выборке до фига мусорных пользователей, которые курс до конца не то что не досмотрели, но даже не начали.
			//visiting.ShowDistribution(z => z.Value.ViewedLecturesCount, z=>true, 10);
            //нас будут интересовать только пользователи, которые посмотрели хотя бы 50 лекций
            
			
			//Распределение пользователей по запаздыванию захода на слайд
           // visiting.ShowDistribution(z=>z.Value.LecturesViewsLate,z=>true,7);

			foreach (var e in visiting)
				e.Value.GoodUser = e.Value.LecturesViewsLate < 40 && e.Value.ViewedLecturesCount > 50;

			var solving = solutions
			  .Where(v=>v.UserId!="NULL")
			  .GroupBy(v => v.UserId)
			  .ToDictionary(z => z.Key, ProcessUserSolutions);

			//solving.ShowDistribution(z => z.Value.LastExercise.UnitData.Number,z=>visiting[z.Key].GoodUser, 1);

			//visits.ShowDistribution(z => z.AttemptsCount, z => visiting[z.UserId].GoodUser && z.AttemptsCount>20, 1);

			//visits.ShowDistribution(z => z.IsPassed ? 2 : (z.IsSkiped ? 1 : 0), z => visiting[z.UserId].GoodUser,1);

			var lastProblems = solving
				.Where(z=>visiting[z.Key].GoodUser)
				.GroupBy(z=>z.Value.LastExercise)
				.Select(z=>new { Exercise = z.Key, CountOfUsersThatBrokeOnIt = z.Count()})
				.OrderByDescending(z=>z.CountOfUsersThatBrokeOnIt)
				.ToArray();

			var exercises = visits
				.Where(z => visiting[z.UserId].GoodUser && z.SlideInfo.Type == SlideType.Exercise)
				.GroupBy(z => z.SlideInfo)
				.ToDictionary(z=>z.Key, ProcessExerciseAttempts);

			foreach (var e in exercises)
				e.Value.CountOfBreakdownTimes = solving.Count(z => z.Value.LastExercise.Id == e.Key.Id);

			exercises
				.Prepare("Заголовок         ","Fail%", "Attem", "Pop", "Brkdwn")
				.Output(false, 
				z => z.Key.Caption,
				z => z.Value.FailurePercentage,
				z => z.Value.AverageAttempts,
				z => z.Value.Popularity,
				z => z.Value.CountOfBreakdownTimes);
			
       }
	}
}