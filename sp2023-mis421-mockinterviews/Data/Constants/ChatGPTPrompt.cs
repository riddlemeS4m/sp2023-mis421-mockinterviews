using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace sp2023_mis421_mockinterviews.Data.Constants
{
    public class ChatGPTPrompt
    {
        public string Question { get; set; }
        public string Prompt { get; set; }
        public ChatGPTPrompt(string question)
        {
            Question = question;
            Prompt = $"I'm trying to prepare for some questions that I may be asked in a job interview. " +
                $"If an interviewer asks me a question like, \"{Question}\", " +
                $"what are some ways I can answer this question? " +
                $"Then, can you give me an example?";
        }
    }
}
