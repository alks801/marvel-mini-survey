namespace MiniSurvey.Models
{
    public class Answer
    {
        public string Text { get; set; }
        public int Id { get; set; }
        public string CharacterIdPoints { get; set; }
        public int QuestionId { get; set; }
    }
}
