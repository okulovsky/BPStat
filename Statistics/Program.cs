using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Statistics
{
	

	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Load();
			PrepareUsers();

			//В выборке до фига мусорных пользователей, которые курс до конца не то что не досмотрели, но даже не начали.
			//Диаграмма показывает количество пользователей в зависимости от просмотренных ими лекций
			//userData.ShowDistribution(z => z.Value.ViewedLecturesCount, z=>true, 10);
			//нас будут интересовать только пользователи, которые посмотрели хотя бы 50 лекций


			//Распределение пользователей по запаздыванию захода на слайд
			//Большая часть пользователь смотрела слайды сразу же после их опубликование. Имеющееся смещение объясняется, вероятно, 
			//просмотром при подготовке к экзамену
			//У нас также есть странные люди, которые запаздывали на 200 и более дней. Это даже не экзамен, это самостоятельное изучение.
			//Вероятно, их нужды нужно изучать отдельно.
			//userData.ShowDistribution(z => z.Value.LecturesViewsLate, z => true, 7);

			//Мы будем учитывать только тех людей, которые посмотрели достаточно много лекций с не слишком большим опозданием
			foreach (var e in userData)
				e.Value.GoodUser = e.Value.LecturesViewsLate > 40 && e.Value.ViewedLecturesCount > 50;

			var goodUsersCount = userData.Count(z => z.Value.GoodUser);
			//Их 160 человек

			var peopleDoNotSolveExercises = userData.Count(z => z.Value.GoodUser && z.Value.LastExercise == null);
			//Из них только 1 человек не сделал ни одного упражнения

			//Вот сколько они решают задач
			//userData.ShowDistribution(z => z.Value.SolvedExerciseCount, z => z.Value.GoodUser, 5);
			//Я вижу тут нормальное распределение в центре с 25 + упорные, которые доходят до конца. 
			//Учитывая, что у нас всего в 13 лекциях есть задачи, оптимумом является решение по 2 задачи в лекцию.


			PrepareExercises();

			//Распределение количества задач по количеству попыток. Видно, что задача занимает от 1 до 15 попыток.
			//exercisesData.ShowDistribution(z => z.Value.AverageAttempts, z => true, 1);

			//График показывает, какие пользователи когда бросили решать задачи
			//userData.ShowDistribution(z => z.Value.LastExercise.UnitData.Number, z => z.Value.GoodUser && z.Value.LastExercise != null , 1);
			//В основном, они бросили на последней лекции (что логично). Остальные в целом нормальным образом сидят между 0 и 10 лекцией. 
			//Учитывая то, что распределение действительно выглядит нормальным, я бы не стал связывать факт бросания человеком задачи с ее сложностью: 
			//решение о бросании задачи есть случайный процесс.

			//Собирательная статистика по задачам
			exercisesData
                .OrderBy(z=>z.Key.Number)
//				.OrderByDescending(z=>z.Value.Fails)
				.Prepare("Заголовок         ", "Unit", "Fail", "Attem", "Pop", "Brkdwn")
				.Output(false,
				z => z.Key.Caption,
				z=>z.Key.UnitData.Number,
				z => z.Value.Fails,
				z => z.Value.AverageAttempts,
				z => z.Value.Popularity,
				z => z.Value.CountOfBreakdownTimes);


            Console.WriteLine("\n\n");
            visits
                .Where(z=>z.SlideInfo.Type == SlideType.Quiz && userData[z.UserId].GoodUser)
                .GroupBy(z=>z.SlideInfo)
                .Select(z=>new 
                    { 
                        Quiz = z.Key, 
                        Replies = z.Count(), 
                        Users = z.Select(x=>x.UserId).Distinct().Count(),
                        RightAnswers  = z.Count(x=>x.IsPassed)
                    })
                //.OrderByDescending(x=>x.RightAnswers)
                .OrderBy(x=>x.Quiz.Number)
                .Prepare("Заголовок                ","Unit","Reps","Users","Answers", "Succ%")
                .Output(false,
                    z=>z.Quiz.Caption,
                    z=>z.Quiz.UnitData.Number,
                    z=>z.Replies,
                    z=>z.Users,
                    z=>z.RightAnswers,
                    z=>(double)z.RightAnswers/z.Users
                    );
                    

		}

		#region Чтение данных из файла

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
		#endregion
		#region Промежуточный обсчет данных

		static Dictionary<string, UserVisitingData> userData;
		static Dictionary<Slide, ExerciseData> exercisesData;

		class UserVisitingData
		{
			public int ViewedLecturesCount { get; set; }
			public double LecturesViewsLate { get; set; }
			public bool GoodUser { get; set; }
			public Slide LastExercise { get; set; }
			public int SolvedExerciseCount { get; set; }
			public int NonSolvedExerciseCount { get; set; }
		}

		static UserVisitingData ProcessUserVisits(IEnumerable<Visit> visits)
		{
			var data = new UserVisitingData();
			data.ViewedLecturesCount = visits.Count();
			data.LecturesViewsLate = visits.Average(x => 1 + (x.TimeStamp - x.SlideInfo.UnitData.PublishingData).TotalDays);
			return data;
		}


		static void ProcessUserSolutions(IEnumerable<UserSolutions> solutions, UserVisitingData data)
		{
			data.SolvedExerciseCount = solutions.GroupBy(z => z.SlideId).Select(z => z.Any(x => x.IsRightAnswer)).Count();
			data.NonSolvedExerciseCount = solutions.GroupBy(z => z.SlideId).Select(z => z.All(x => !x.IsRightAnswer)).Count();

			data.LastExercise = solutions.Where(z => z.SlideInfo.Type == SlideType.Exercise).ArgMax(z => z.SlideInfo.Number).SlideInfo;
		}

		class ExerciseData
		{
			public double Fails { get; set; }
			public double AverageAttempts { get; set; }
			public double MaxAttempts { get; set; }
			public double Popularity { get; set; }
			public int CountOfBreakdownTimes { get; set; }

		}

		static ExerciseData ProcessExerciseVisit(this IEnumerable<Visit> v)
		{
			var data = new ExerciseData();
			data.MaxAttempts = v.Max(z => z.AttemptsCount);
			data.AverageAttempts = v.Average(z => z.AttemptsCount);
			return data;
		}

		static void ProcessExerciseSolution(IEnumerable<UserSolutions> v, ExerciseData data)
		{
			data.Popularity = v.Select(z=>z.UserId).Distinct().Count();
			data.Fails = v.GroupBy(z => z.UserId).Where(z => z.All(x => !x.IsRightAnswer)).Count();
		}

		
		static void PrepareUsers()
		{
			userData = visits
			  .GroupBy(v => v.UserId)
			  .ToDictionary(z => z.Key, ProcessUserVisits);

			solutions
			 .Where(v => v.UserId != "NULL")
			 .GroupBy(v => v.UserId)
			 .ForEach(z => ProcessUserSolutions(z, userData[z.Key]));
		}

		static void PrepareExercises()
		{
			exercisesData = visits
				.Where(z => userData[z.UserId].GoodUser && z.SlideInfo.Type == SlideType.Exercise)
				.GroupBy(z => z.SlideInfo)
				.ToDictionary(z => z.Key, ProcessExerciseVisit);

			solutions
				.Where(z => z.UserId != "NULL")
				.Where(z => userData[z.UserId].GoodUser)
				.GroupBy(z => z.SlideInfo)
				.ForEach(z => ProcessExerciseSolution(z, exercisesData[z.Key]));

			foreach (var e in exercisesData)
				e.Value.CountOfBreakdownTimes = userData.Count(z => z.Value.GoodUser && z.Value.LastExercise != null && z.Value.LastExercise.Id == e.Key.Id);
		}

		#endregion
		
	}



}