using SendGrid.Helpers.Mail;
using SendGrid;
using sp2023_mis421_mockinterviews.Data.Constants;

namespace sp2023_mis421_mockinterviews.Interfaces
{
    public abstract class ASendAnEmail
    {
        public EmailAddress FromEmail;
        public string Subject;
        public string PlainTextContent;
        public EmailAddress ToEmail;
        public string HTMLContent;
        public string ToName;
        public string Times;
        public string FilePath;

        public abstract void InjectHTMLContent();

        public async Task SendEmailAsync(ISendGridClient sendGridClient, string subject, string emailto, string emailname, string times)
        {
            FromEmail = new EmailAddress(SuperUser.Email, "UA MIS " + SuperUser.FirstName + " " + SuperUser.LastName);
            Subject = subject;
            PlainTextContent = "";
            ToName = emailname;
            Times = times;
            ToEmail = new EmailAddress(emailto);
            HTMLContent = await File.ReadAllTextAsync(FilePath);

            InjectHTMLContent();

            var msg = MailHelper.CreateSingleEmail(FromEmail, ToEmail, Subject, PlainTextContent, HTMLContent);
            await sendGridClient.SendEmailAsync(msg);
        }
    }
}
