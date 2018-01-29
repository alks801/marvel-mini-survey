using System.Collections.Generic;

namespace MiniSurvey.Models
{
    public class Question
    {
        public List<Answer> Answers { get; set; }
        public string Text { get; set; }
        public int Id { get; set; }
        public Question()
        {
            Answers = new List<Answer>();
        }
    }
}