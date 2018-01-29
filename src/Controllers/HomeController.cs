using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MiniSurvey.Models;

namespace MiniSurvey.Controllers
{
    public class HomeController : Controller
    {
        private readonly MiniSurveyContext _db;

        //static client data (questions, answers)
        private static ClientData _data;

        //static client data about characters
        
        private static List<Character> _characters;
        public HomeController(MiniSurveyContext db)
        {
            _db = db;
        }

        [Route("interview")]
        public IActionResult Interview()
        {
            ViewBag.MobileClass = IsMobile(Request) ? "mobile" : "";

            return View();
        }

        [Route("")]
        public IActionResult Pre(int charId = 0)
        {
            ViewBag.MobileClass = IsMobile(Request) ? "mobile" : "";
            if (charId != 0)
                ViewBag.CharId = charId;
            return View();
        }


        [Route("post-characters")]
        [HttpPost]
        public async Task<IActionResult> PostCharacters(Dictionary<int, CharacterClientInfo> characterPoints, bool isMan, int interviewId)
        {
            if (characterPoints == null)
                characterPoints = _data.CharacterPoints;

            int charId = 1;
            try
            {
                var curCharacters = characterPoints
                    .Where(x => x.Value.IsMan == isMan)
                    .OrderByDescending(x => x.Value.Points)
                    .Take(2) //Take 2 top characters
                    .Select(s => new { s.Key, s.Value.Points }).ToList();

                //if characters have similar point we can just randomly pick one
                if (curCharacters[0].Points == curCharacters[1].Points)
                {
                    Random r = new Random(DateTime.Now.Millisecond);
                    charId = curCharacters[r.Next(0, 2)].Key;
                }
                else
                    charId = curCharacters.Where(p => p.Points == curCharacters.Max(m => m.Points)).FirstOrDefault().Key;

                await _db.CharacterResults.AddAsync(new CharacterResult { InterviewId = interviewId, CharacterId = charId });
                await _db.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                await _db.CharacterResults.AddAsync(new CharacterResult { InterviewId = interviewId, CharacterId = -1, AddInfo = ex.Message });
                await _db.SaveChangesAsync();
            }

            return Content(charId.ToString());
        }


        [Route("get-model")]
        [HttpPost]
        public async Task<IActionResult> GetModel()
        {
            if (_data == null)
            {
                //simpliest caching server data about characters
                _data = new ClientData();
                _data.Questions = (await _db.Questions.Include("Answers").ToListAsync()).Select(x => new ClientQuestion(x)).ToList();
                _data.CharacterPoints = _db.Characters.Select(c => new { c.Id, c.IsMan }).ToDictionary(k => k.Id, v => new CharacterClientInfo { Points = 0, IsMan = v.IsMan });
            }

            var interview = new Interview
            {
                DateCreated = DateTime.Now,
                QuestionId = 1
            };

            _db.Interviews.Add(interview);
            _db.SaveChanges();

            var model = new ClientModel
            {
                Data = _data,
                InterviewId = interview.Id,
                StatTotalInterviewCount = await _db.Interviews.Where(i => i.IsEnded).CountAsync(),
                IsMobile = IsMobile(Request),
                CurrentQuestionId = 1
            };

            return Json(model);
        }

        [Route("post-result")]
        [HttpPost]
        //get stats for answered question (percents per answer)
        public async Task<IActionResult> GetStats(int interviewId, int questionId, int answerId)
        {
            try
            {
                await _db.Results.AddAsync(new Result { InterviewId = interviewId, QuestionId = questionId, AnswerId = answerId });
                await _db.SaveChangesAsync();

                var interview = await _db.Interviews.FirstAsync(i => i.Id == interviewId);
                interview.QuestionId = questionId;
                if (questionId == 11)
                    interview.IsEnded = true;

                _db.Interviews.Update(interview);
                await _db.SaveChangesAsync();

                var qResuts = _db.Results.Where(r => r.QuestionId == questionId);

                int totalQResults = await qResuts.CountAsync();
                var qst = await _db.Questions.Include("Answers").FirstAsync(q => q.Id == questionId);
                var answerIds = qst.Answers.ToList().Select(a => a.Id).ToList();
                Dictionary<int, int> res = new Dictionary<int, int>();//answerId, percent
                foreach (var aId in answerIds)
                {
                    var curAnswerCount = await qResuts.Where(r => r.AnswerId == aId).CountAsync();
                    var percent = totalQResults == 0 ? 0 : GetPercent(curAnswerCount, totalQResults);
                    res.Add(aId, percent);
                }

                //if we have summ != 100 (decimal) we should make it = 100 for better view
                var sum = res.Sum(x => x.Value);
                int dp = 100 - sum;
                var last = res.Last();
                int val = last.Value + dp;
                if (val >= 0)
                    res[last.Key] = val;

                else
                {
                    var k = res.FirstOrDefault(r => r.Value > 0);
                    res[k.Key] = k.Value + dp;
                }
                return Json(res);
            }
            catch (Exception ex)
            {
                return Json("");
            }
        }

        //Get percent from total count
        private int GetPercent(int current, int total)
        {
            double curr = current;
            double tot = total;
            double res = (curr / tot) * 100;
            res = Math.Round(res, 1);
            return Convert.ToInt32(res);
        }

        [Route("result")]
        public IActionResult Result(int charId = 1)
        {
            ViewBag.MobileClass = IsMobile(Request) ? "mobile" : "";


            //simpliest character info caching
            if (_characters == null)
                _characters = _db.Characters.ToList();

            if (charId < 1 || charId > 11)
                charId = 1;

            var _char = _characters.Find(c => c.Id == charId);
            ViewBag.CharDescription = _char?.Description;
            ViewBag.CharName = _char?.Name;
            ViewBag.CharId = charId;
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //Identify mobile device by Request
        private bool IsMobile(HttpRequest req)
        {
            string userAgent = req.Headers[HeaderNames.UserAgent].ToString();
            var ualower = userAgent.ToLowerInvariant();

            if (ualower.Contains("android") || ualower.Contains("ios") || ualower.Contains("ipad") || ualower.Contains("iphone"))
            {
                return true;
            }

            if (string.IsNullOrEmpty(userAgent)) return false;

            var b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return b.IsMatch(userAgent) || v.IsMatch(userAgent.Substring(0, 4));
        }
    }
}
