using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
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

		public static void ForEach<T>(this IEnumerable<T> data, Action<T> action)
		{
			foreach (var e in data) action(e);
		}

		public static void ShowDistribution<T>(this IEnumerable<T> data, Func<T, double> value, Func<T, bool> filter, int compression)
		{
			data
				.Where(filter)
				.GroupBy(z => (int)(value(z) / compression))
				.ShowHisto(z => z.Key * compression, z => z.Count());
		}

		public static Tuple<IEnumerable<T>, Func<object, string>[]> Prepare<T>(this IEnumerable<T> data, params string[] captions)
		{
			var list = new List<Func<object, string>>();
			foreach (var e in captions)
			{
				Console.Write("{0} ", e);
				list.Add(new Func<object, string>(o =>
				{
					var s = o.ToString();
					if (s.Length > e.Length) s = s.Substring(0, e.Length);
					var fmt = ("{0," + e.Length + "}");
					s = string.Format(fmt, s);
					return s;
				}));

			}
			Console.WriteLine();
			return Tuple.Create(data, list.ToArray());
		}

		public static void Output<T>(this Tuple<IEnumerable<T>, Func<object, string>[]> prep, bool skip, params Func<T, object>[] selectors)
		{
			if (skip) return;
			var data = prep.Item1;
			var fms = prep.Item2;
			if (fms.Length != selectors.Length) throw new ArgumentException();


			foreach (var e in data)
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
}
