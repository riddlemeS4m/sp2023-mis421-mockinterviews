using SendGrid.Helpers.Mail;
using SendGrid;
using sp2023_mis421_mockinterviews.Data.Constants;
using System.Text;

namespace sp2023_mis421_mockinterviews.Interfaces
{
    public abstract class ASendAnEmail
    {
        public EmailAddress FromEmail { get; set; }
        public string Subject { get; set; }
        public string PlainTextContent { get; set; }
        public EmailAddress ToEmail { get; set; }
        public string HTMLContent { get; set; }
        public string ToName { get; set; }
        public string Times { get; set; }
        public string FilePath { get; set; }

        public abstract void InjectHTMLContent();

        public async Task SendEmailAsync(ISendGridClient sendGridClient, string subject, string emailto, string emailname, string times)
        {
            FromEmail = new EmailAddress(SuperUser.Email, "UA MIS " + SuperUser.FirstName + " " + SuperUser.LastName);
            Subject = subject;
            PlainTextContent = "";
            ToName = emailname;
            Times = times;
            ToEmail = new EmailAddress(emailto);
            StringBuilder stringBuilder = new(FilePath);
            stringBuilder.Insert(0, "./Content/Emails/");
            FilePath = stringBuilder.ToString();
            HTMLContent = await File.ReadAllTextAsync(FilePath);

            InjectHTMLContent();

            var msg = MailHelper.CreateSingleEmail(FromEmail, ToEmail, Subject, PlainTextContent, HTMLContent);
            await sendGridClient.SendEmailAsync(msg);
        }
    }
}
