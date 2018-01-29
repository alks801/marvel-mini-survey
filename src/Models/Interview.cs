namespace MiniSurvey.Models
{
    public class Interview
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public bool IsEnded { get; set; }
        public System.DateTime DateCreated { get; set; }
    }
}