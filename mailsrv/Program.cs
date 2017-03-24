using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Data.SQLite;
using mailstorage;
namespace mailsrv
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 25;
            Regex rgx = new Regex(@"^\d+$");
            if (args.Length > 0 && rgx.IsMatch(args[0]))
            {
                if (!int.TryParse(args[0], out port)) port = 25;
            }
            smtp_server smtpSrv = new smtp_server(port);

            Thread smtpThread = new Thread(smtpSrv.run);
            
            smtpThread.Start();
           
        }
    }
    
    public class smtp_server
    {
        private TcpListener _masterSocket;
        private IPEndPoint _ep;

        public smtp_server(int _port=25,int backlog = 20)
        {
            try
            {
                this._ep = new IPEndPoint(IPAddress.Any, _port);
                this._masterSocket = new TcpListener(this._ep);
                _masterSocket.Start();
               // this._masterSocket.Bind(this._ep);
               // this._masterSocket.Listen(backlog);
            }
            catch (SocketException se)
            {
                Console.WriteLine("An exception has been caught: socket: " + se.Message);
                throw;
            }
            finally
            {
                Console.WriteLine("Socket listenning on 127.0.0.1:{0} (TCP)(25,465,587)",_port);
            }
        }

        public void run()
        {
            while (true)
            {
                
                var client = this._masterSocket.AcceptTcpClient();
                handleClient handle = new handleClient(client);
                if (handle.init())
                {
                    handle.start();
                }
            }
        }
        public static void txtlog(String text)
        {
            System.IO.File.AppendAllText("coolmaillog" + DateTime.Now.ToString("yyyyMMdd") + ".txt", text);
        }
    }
    public class handleClient
    {
        private TcpClient _socket;
        public Message currentMessage;
        public int READ_MAIL_DATA_COUNT = 0;

        public handleClient(object sock)
        {
            this.currentMessage = new Message();
            this._socket = (TcpClient)sock;
            _socket.ReceiveBufferSize = 8192;

            // Set the timeout for synchronous receive methods to 
            // 1 second (1000 milliseconds.)
            _socket.ReceiveTimeout = 1000;

            // Set the send buffer size to 8k.
            _socket.SendBufferSize = 8192;

            // Set the timeout for synchronous send methods
            // to 1 second (1000 milliseconds.)			
            _socket.SendTimeout = 1000;

            // Set the Time To Live (TTL) to 42 router hops.
            
        }

        public void start()
        {
            try
            {
                Thread runner = new Thread(this.run);
                runner.Start();
            }
            catch (ThreadStartException tse)
            {
                Console.WriteLine("A thread exception was caught => " + tse.Message);
            }
            finally
            {
                Console.WriteLine(this.GetHashCode() + " join");
            }
        }
        public void Write(String strMessage)
        {
            
                NetworkStream clientStream = _socket.GetStream();

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(strMessage);// + "\r\n");

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
                smtp_server.txtlog("S: " + strMessage);
          
        }

        private String Read()
        {
            byte[] messageBytes = new byte[8192];
            int bytesRead = 0;
            NetworkStream clientStream = _socket.GetStream();

            bytesRead = clientStream.Read(messageBytes, 0, 8192);
          
            string strMessage = System.Text.Encoding.UTF8.GetString(messageBytes, 0, bytesRead);
            smtp_server.txtlog("C: "+strMessage);
           
            return strMessage;
        }
        public bool init()
        {
           // this._socket.Send(Protocol.StrToByteArray(Protocol.welcome));
            try
            {
                Write(Protocol.welcome);
                 return true;
            }
            catch (System.IO.IOException ioexcep)
            {
               
                Console.WriteLine(ioexcep.Message );
                exit();
                
            }
            return false;
        }

        public void run()
        {

           // Write("220 localhost -- Fake proxy server");

            while (this._socket.Connected)
            {
                try
                {
                    var command = "";
                    try
                    {
                        command= Read();
                    }
                    catch (Exception e)
                    {
                        //a socket error has occured
                        break;
                    }

                        var commands = command.Trim().Split('\n');
                        bool linefeed = false;
                        //if(command.Length>0 && command[command.Length - 1]=='\n') linefeed=true;

                        if (currentMessage.dot && commands.Length > 3)
                        {
                            commandManager.execute(this, command, false);
                        }
                        else
                        {
                          
                            for(int i=0;i<commands.Length;i++)
                                commandManager.execute(this, commands[i].TrimStart(), false);
                        }
                }
                catch (SocketException se)
                {
                    Console.WriteLine("server kick " + this.GetHashCode());
                }catch (Exception e)
                    {
                        Console.WriteLine(e.StackTrace+
                            e.Message);
                        //a socket error has occured
                        break;
                    }
            }
            Console.WriteLine(this.GetHashCode() + " left");
        }

        public int Send(byte[] buffer)
        {
            NetworkStream clientStream = _socket.GetStream();
           // byte[] buffer = System.Text.Encoding.UTF8.GetBytes(strMessage + "\r\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
            return buffer.Length;
          //  return this._socket.Send(byteArray);
        }

        public string getIpAddress()
        {
            return "127.0.0.1";
        }

        public void exit()
        {
            try
            {
               // this._socket.Disconnect(false);
                this._socket.Close();
            }
            catch (SocketException se)
            { }
        }
    }
    public class commandManager
    {
        
        public static Func<handleClient, string[], bool, int>[] funcPtr = {
                                            helo_command,
                                            ehlo_command,
                                            mail_from,
                                            rcpt_to,
                                            data_body,
                                            dot,
                                            quit,
                                            quit_maj
                                        };


        public static int helo_command(handleClient hc, string[] param,bool linefeed)
        {
            if (param.Length <= 1)
            {
                var tosend = Protocol.StrToByteArray(Protocol.helo_error501);
                //hc.Send(tosend);
                hc.Write(Protocol.helo_error501);
            }
            else
            {
                var tosend = Protocol.StrToByteArray(Protocol.helo_accept250.Replace("%1", hc.getIpAddress()));
               // hc.Send(tosend);
                hc.Write(Protocol.helo_accept250.Replace("%1", hc.getIpAddress()));
            }
            return 0;
        }

        public static int ehlo_command(handleClient hc, string[] param,bool linefeed)
        {
            if (param.Length <= 1)
            {
                var tosend = Protocol.StrToByteArray(Protocol.helo_accept250.Replace("%1", hc.getIpAddress()) +
                       Protocol.abilities);
                //hc.Send(tosend);
                hc.Write(Protocol.helo_accept250.Replace("%1", hc.getIpAddress()) +
                       Protocol.abilities);
            }
            else
            {
                var tosend = Protocol.StrToByteArray(Protocol.helo_accept250.Replace("%1", hc.getIpAddress()));
                hc.Write(Protocol.helo_accept250.Replace("%1", hc.getIpAddress()));
                //hc.Send(tosend);
            }
            return 0;
        }

        public static int mail_from(handleClient hc, string[] param,bool linefeed)
        {
            foreach(string s in param)
            Console.Write("[" + s + "]");
			Console.WriteLine();
            if (param.Length <= 2)
            {
                var tab = param[1].Split(':');
                if (param[1].Substring(0, 5).ToUpper() != "FROM:")
                {
                    hc.Write(Protocol.mail_error501);
                   // var tosend = Protocol.StrToByteArray(Protocol.mail_error501);
                   // hc.Send(tosend);
                    return 0;
                }
                if (tab.Length == 0)
                {
                    hc.Write(Protocol.mail_error501);
                  //  var tosend = Protocol.StrToByteArray(Protocol.mail_error501);
                  //  hc.Send(tosend);
                    return 0;
                }
                if (tab.Length > 0 && Protocol.IsValidEmail(tab[1]))
                {

                    var tosend2 = Protocol.StrToByteArray(Protocol.mail_accept250.Replace("%1", tab[1]));
                    hc.currentMessage.from = tab[1];
                    hc.Write(Protocol.mail_accept250.Replace("%1", tab[1]));
                    //hc.Send(tosend2);
                }
                else
                {
                    var tosend = Protocol.StrToByteArray(Protocol.mail_error501);
                    hc.Write(Protocol.mail_error501);
                   // hc.Send(tosend);
                }
                return 0;
            }
            string mailfrom=null;
            if (param.Length > 2 && Protocol.IsValidEmail(param[2]))
            {
                mailfrom=param[2];
            }
            if (param.Length > 3 && Protocol.IsValidEmail(param[3]))
            {
                mailfrom=param[3];
            }
            if (param.Length > 2)
            {
                if (mailfrom==null || param[1].Length < 5 || (param[1].Substring(0, 5).ToUpper() != "FROM:"))
                {
                    var tosend = Protocol.StrToByteArray(Protocol.mail_error501_wrongparam.Replace("%1", param[2]));
                    //hc.Send(tosend);
                    hc.Write(Protocol.mail_error501_wrongparam.Replace("%1", param[2]));
                    return 0;
                }
                var tosend2 = Protocol.StrToByteArray(Protocol.mail_accept250.Replace("%1",mailfrom));
                hc.currentMessage.from = mailfrom;
                hc.Write(Protocol.mail_accept250.Replace("%1", mailfrom));
                //hc.Send(tosend2);
            }
            return 0;
        }

        public static int rcpt_to(handleClient hc, string[] param, bool linefeed)
        {
            foreach(string s in param)
            Console.Write("[" + s + "]");
			Console.WriteLine();
            if (hc.currentMessage.from == string.Empty)
            {
                var tosend = Protocol.StrToByteArray(Protocol.rcpt_need_mail);
                hc.Write(Protocol.rcpt_need_mail);
                //hc.Send(tosend);
                return 0;
            }

            if (param.Length < 3)
            {
                var tosend = Protocol.StrToByteArray(Protocol.rcpt_error501_noparam);
                hc.Write(Protocol.rcpt_error501_noparam);
                //hc.Send(tosend);
                return 0;
            }//A CHANGER
            else if (Settings.users.Contains(param[2])  || Settings.users.Contains(param[2] + "@" + Settings.domain))
            {

                var tosend = Protocol.StrToByteArray(Protocol.rcpt_accept250.Replace("%1", param[2]));
                if (hc.currentMessage.to != string.Empty)
                {
                    hc.currentMessage.to += ";" + param[2];
                }
                else
                {
                    hc.currentMessage.to = param[2];
                }
                hc.Send(tosend);

            }
            else if ( param.Length > 3 && Settings.users.Contains(param[3]))
            {

                var tosend = Protocol.StrToByteArray(Protocol.rcpt_accept250.Replace("%1", param[3]));
                if (hc.currentMessage.to != string.Empty)
                {
                    hc.currentMessage.to += ";" + param[3];
                }
                else
                {
                    hc.currentMessage.to = param[3];
                }
                hc.Send(tosend);

            }
            else
            {
                var tosend = Protocol.StrToByteArray(Protocol.rcpt_error550_relaydenied.Replace("%1", param[2]));
                //hc.Send(tosend);
                hc.Write(Protocol.rcpt_error550_relaydenied.Replace("%1", param[2]));
            }
            return 0;
        }

        public static int data_body(handleClient hc, string[] param,bool linefeed)
        {
            hc.READ_MAIL_DATA_COUNT++;
            if (hc.currentMessage.from == string.Empty ||
                hc.currentMessage.to == string.Empty)
            {
               // var tosend = Protocol.StrToByteArray(Protocol.data_need_mail);
               // hc.Send(tosend);
                hc.Write(Protocol.data_need_mail);
                return 0;
            }

            if (hc.currentMessage.subject == string.Empty)
            {
                String[] temp_=param[0].Split('\n');
                foreach (String s in temp_)
                {
                    if(s.StartsWith("Subject:")){
                        hc.currentMessage.subject = s.Substring(8);
						break;
                    }else{
						Console.WriteLine(s);
					}
                }
            }
            if (param[0] == "." )//|| (param[0].Length > 3 && param[0][param[0].Length - 3] == '.'))
            {
                dot(hc, param, false);
                return 0;
            }
           
            if (hc.currentMessage.dot == false)
                hc.Write("354 Enter mail, end with \".\" on a line by itself\r\n");
                //hc.Send(Protocol.StrToByteArray("354 Enter mail, end with \".\" on a line by itself\n"));
            hc.currentMessage.dot = true;

            //desplit params
            string o = String.Join(" ", param);
           
            if (param[0] != "DATA")
            {
                hc.currentMessage.body += (o + "\n");
				 if ((param[0].Length > 3 && param[0][param[0].Length - 3] == '.'))
                {
                dot(hc, param, false);
                return 0;
                }
            }
            /*
            foreach (string s in param)
                hc.currentMessage.body = hc.currentMessage.body + s + "\n";
            */
            return 0;
        }

        public static int quit_maj(handleClient hc, string[] param,bool linefeed)
        {
            //hc.Send(Protocol.StrToByteArray("221 2.0.0 Bye\n"));
            hc.Write("221 2.0.0 Bye\r\n");
            hc.exit();
            return 0;
        }

        public static int quit(handleClient hc, string[] param, bool linefeed)
        {
            //hc.Send(Protocol.StrToByteArray("221 2.0.0 Bye\n"));
            hc.Write("221 2.0.0 Bye\r\n");
            hc.exit();
            return 0;
        }

        public static int dot(handleClient hc, string[] param, bool linefeed)
        {
            hc.currentMessage.dot = false;
            //hc.Send(Protocol.StrToByteArray("250 2.0.0 Message accepted for delivery\n"));
            
            if (hc.currentMessage.getSize() > 4194304)
            {
                hc.Write("501 data bigger then 2 mb !\r\n");
            }
            else
            {
                hc.Write("250 2.0.0 Message accepted for delivery\r\n");
                messageManager.push(hc.currentMessage);
            }
            hc.currentMessage = new Message();
            return 0;
        }

        public static void execute(handleClient hc, string command,bool lastLineFeed)
        {
        
            char[] splitTab = new char[2];
            splitTab[0] = ' ';
            splitTab[1] = ' ';
            if(command.StartsWith("MAIL FROM:")||command.StartsWith("RCPT TO:")){
            command = command.Replace("<", " ").Replace(">", " ");
			Console.WriteLine(command);
            }
            var commandTab = command.Split(splitTab);

            if (hc.currentMessage.dot)
            {
                data_body(hc,new string[]{command}, lastLineFeed);
                return;
            }

            if (Protocol.commands.Contains(commandTab[0].ToUpper()))
            {
                var index = Array.IndexOf(Protocol.commands, commandTab[0].ToUpper());
                try
                {
                    funcPtr[index](hc, commandTab, lastLineFeed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Server Parse Error");
                }
            }
            else
            {
                hc.Write("500 5.5.1 Command unrecognized: \"" + command + "\"\n");
            }
        }
    }
    
  
}
