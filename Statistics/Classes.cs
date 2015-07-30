using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statistics
{


    public interface IReadable
    {
        void Load(string[] data);
    }

	public class Visit : IReadable
	{
        public string SlideId { get; set; }
        public DateTime TimeStamp { get; set;}
        public int Score { get; set; }
        public int AttemptsCount { get; set; }
        public bool IsSkiped { get; set; }
        public bool IsPassed { get; set; }
        public string UserId { get; set; }

        public Slide SlideInfo { get; set; }

        public void Load(string[] data)
        {
            SlideId = data[0];
            TimeStamp = DateTime.Parse(data[1]);
            Score = int.Parse(data[2]);
            AttemptsCount = int.Parse(data[3]);
            IsSkiped = data[4] != "0";
            IsPassed = data[5] != "0";
            UserId = data[6];
        }
	}

    public class UserSolutions : IReadable
    {
        public string SlideId { get; set; }
        public DateTime TimeStamp { get; set; }
        public bool IsRightAnswer { get; set; }
        public bool IsCompilationError { get; set; }
        public string UserId { get; set; }

        public Slide SlideInfo { get; set;  }

        public void Load(string[] data)
        {
            SlideId = data[0];
            TimeStamp = DateTime.Parse(data[1]);
            IsRightAnswer = data[2] != "0";
            IsCompilationError = data[3] != "0";
            UserId = data[4];
        }
    }

    public enum SlideType
    {
        Lecture,
        Quiz,
        Exercise
    }

    public class Unit
    {
        public string Title { get; set; }
        public int Number { get; set; }
        public DateTime PublishingData { get; set; }

    }

    public class Slide : IReadable
    {
        public int Number { get; set; }
        public string Id { get; set; }
        public string Caption { get; set; }
        public string UnitTitle { get; set; }

        public Unit UnitData { get; set; }

        public SlideType Type { get; set; }

        public void Load(string[] data)
        {
            Number = int.Parse(data[0]);
            Id = data[1];
            Caption = data[2];
            UnitTitle = data[3];
            if (data[4] == "QuizSlide") Type = SlideType.Quiz;
            else if (data[4] == "ExerciseSlide") Type = SlideType.Exercise;
            else Type = SlideType.Lecture;
        }

        public override string ToString()
        {
            return "[" + Type + "]" + Caption + " (" + UnitTitle + ")";
        }

    }

    public class Loader
    {


        public static T[] Load<T>(string path)
            where T : IReadable,new()
        {
          
            return File.
                ReadLines(path,System.Text.Encoding.Default).
                Skip(1).
                Select(line => line.Split(';')).
                Select(z => { var r = new T(); r.Load(z); return r; })
                .ToArray();
        }
    }
}
