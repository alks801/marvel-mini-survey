using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniSurvey.Models
{
    public class ClientModel
    {
        public ClientData Data { get; set; }
        public bool IsMobile { get; set; }
        public int StatTotalInterviewCount { get; set; }
        public int CurrentQuestionId { get; set; }
        public int InterviewId { get; set; }
    }

    public class ClientData
    {
        public List<ClientQuestion> Questions { get; set; }
        public Dictionary<int, CharacterClientInfo> CharacterPoints { get; set; }
    }

    public class ClientQuestion
    {
        public List<ClientAnswer> Answers { get; set; }
        public string Text { get; set; }
        public int Id { get; set; }
        public bool Checked { get; set; }

        public ClientQuestion(Question q)
        {
            Answers = q.Answers.Select(a => new ClientAnswer { Id = a.Id, Text = a.Text, CharacterIdPoints = a.CharacterIdPoints, Checked = false }).ToList();
            Text = q.Text;
            Id = q.Id;
            Checked = false;
        }
    }

    public class ClientAnswer : Answer
    {
        public bool Checked { get; set; }
    }
}