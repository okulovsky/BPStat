using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Statistics
{
	class Graph2D : UserControl
	{
		public double[,] data;
		public string xTitle;
		public string yTitle;

		protected override void OnPaint(PaintEventArgs e)
		{
			MakePaint(e.Graphics);
		}

		void MakePaint(Graphics g)
		{
			var w = (float)Size.Width / data.GetLength(0);
			int margin = 20;
			var h = (float)(Size.Height-2*margin) / data.GetLength(1);
			
			var max = data.OfType<double>().Max();
			var min = data.OfType<double>().Max();
			for (int x=0;x<data.GetLength(0);x++)
				for (int y = 0; y < data.GetLength(1); y++)
				{
					var c = (int)(255*(data[x, y] - min) / max - min);
					g.FillRectangle(new SolidBrush(Color.FromArgb(c, c, c)), w * x, h * h, w, h);
				}
		}
	}
}
