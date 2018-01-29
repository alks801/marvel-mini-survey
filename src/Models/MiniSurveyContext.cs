using Microsoft.EntityFrameworkCore;

namespace MiniSurvey.Models
{
    public class MiniSurveyContext : DbContext
    {
        public MiniSurveyContext(DbContextOptions<MiniSurveyContext> options) : base(options)
        {

        }

        public DbSet<Answer> Answers { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<Result> Results { get; set; }

        public DbSet<Character> Characters { get; set; }

        public DbSet<CharacterResult> CharacterResults { get; set; }
    }
}
