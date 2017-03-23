 
using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using System.ComponentModel;
namespace Examples.SmptExamples.Async
{
    public class SimpleAsynchronousExample
    {
        static bool mailSent = false;
        private static void SendCompletedCallback1(object sender, AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
             String token = (string) e.UserState;

            if (e.Cancelled)
            {
                 Console.WriteLine("[{0}] Send canceled.", token);
            }
            if (e.Error != null)
            {
                 Console.WriteLine("[{0}] {1}", token, e.Error.ToString());
            } else
            {
                Console.WriteLine("Message sent.");
            }
            mailSent = true;
        }
		 private static void SendCompletedCallback2(object sender, AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
             String token = (string) e.UserState;

            if (e.Cancelled)
            {
                 Console.WriteLine("[{0}] Send canceled.", token);
            }
            if (e.Error != null)
            {
                 Console.WriteLine("[{0}] {1}", token, e.Error.ToString());
            } else
            {
                Console.WriteLine("Message sent.");
            }
            mailSent = true;
        }
		private static void send1()
		{
			// Command line argument must the the SMTP host.
            SmtpClient client = new SmtpClient("localhost");
            // Specify the e-mail sender.
            // Create a mailing address that includes a UTF8 character
            // in the display name.
            
			MailAddress from = new MailAddress("sa@school.edu",  "",  System.Text.Encoding.UTF8);
            // Set destinations for the e-mail message.
            MailAddress to = new MailAddress("sa@school.edu");
            // Specify the message content.
            
			MailMessage message = new MailMessage(from, to);
	        message.Body="afad";
			message.Subject="aasfd";
			 client.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback1);
			
            string userState = "test message1";
            client.SendAsync(message, userState);
            Console.WriteLine("Sending message... press c to cancel mail. Press any other key to exit.");
            string answer = Console.ReadLine();
            // If the user canceled the send, and mail hasn't been sent yet,
            // then cancel the pending operation.
            if (answer.StartsWith("c") && mailSent == false)
            {
                client.SendAsyncCancel();
            }
            // Clean up.
            message.Dispose();
		}
		private static void send2()
		{
			// Command line argument must the the SMTP host.
            SmtpClient client = new SmtpClient("localhost");
            // Specify the e-mail sender.
            // Create a mailing address that includes a UTF8 character
            // in the display name.
            
			MailAddress from = new MailAddress("sa@school.edu",  "",  System.Text.Encoding.UTF8);
            // Set destinations for the e-mail message.
            MailAddress to = new MailAddress("sa@school.edu");
            // Specify the message content.
            
			MailMessage message = new MailMessage(from, to);

			
			
			message.IsBodyHtml=true;
            message.Body =Environment.NewLine + "<a href=#>This is a test e-mail message sent by an application. ";
            // Include some non-ASCII characters in body and subject.
            string someArrows = new string(new char[] {'\u2190', '\u2191', '\u2192', '\u2193'});
            message.Body += Environment.NewLine + someArrows;
	        message.Body +=  Environment.NewLine ;
            message.BodyEncoding =  System.Text.Encoding.UTF8;
            message.Subject = "test message 1" + someArrows;
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            // Set the method that is called back when the send operation ends.
            client.SendCompleted += new 
            SendCompletedEventHandler(SendCompletedCallback2);
            // The userState can be any object that allows your callback 
            // method to identify this send operation.
            // For this example, the userToken is a string constant.
            string userState = "test message1";
            client.SendAsync(message, userState);
            Console.WriteLine("Sending message... press c to cancel mail. Press any other key to exit.");
            string answer = Console.ReadLine();
            // If the user canceled the send, and mail hasn't been sent yet,
            // then cancel the pending operation.
            if (answer.StartsWith("c") && mailSent == false)
            {
                client.SendAsyncCancel();
            }
            // Clean up.
            message.Dispose();
		}
        public static void Main(string[] args)
        {
            send2();
			 string answer = Console.ReadLine();
			 mailSent = false;
			send1();
            Console.WriteLine("Goodbye.");
        }
    }
}
