using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.IO;
using System.Data.SQLite;
using mailstorage;

namespace mailpop3
{
    class Program
    {
        static void Main(string[] args)
        {
            var pop3Srv = new pop_server();
            var pop3Thread = new Thread(pop3Srv.run);
            pop3Thread.Start();
        }
    }
    public class pop_server
    {
        private TcpListener _masterSocket;
        private IPEndPoint _ep;

        public pop_server(int backlog = 20)
        {
            try
            {
               // this._masterSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this._ep = new IPEndPoint(IPAddress.Any, 110);//+10000
                this._masterSocket = new TcpListener(this._ep);
                _masterSocket.Start();
               // this._masterSocket.Bind(this._ep);
              //  this._masterSocket.Listen(backlog);
            }
            catch (SocketException se)
            {
                Console.WriteLine("An exception has been caught: socket: " + se.Message);
                throw;
            }
            finally
            {
                Console.WriteLine("Socket listenning on 127.0.0.1:110 (TCP)");
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
            System.IO.File.AppendAllText("pop3" + DateTime.Now.ToString("yyyyMMdd") + ".txt", text);
        }
    }
    public class handleClient
    {
        public TcpClient _socket;
        public string secret;
        public Authentificator auth;

        public handleClient(TcpClient client)
        {
            this.auth = new Authentificator();
            this._socket =client;
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

        public bool init()
        {
            try
            {
                var the_secret = this.GetHashCode().ToString() + "." + DateTime.UtcNow.Ticks.ToString();
                Write(Protocol.welcome.Replace("%hash", the_secret));
                this.secret = "<" + the_secret + Settings.atdomain + ">";
                return true;
            }
            catch (System.IO.IOException ioexcep)
            {
                Console.WriteLine(ioexcep.Message);
                exit();
            }
            return false;
        }

        public void run()
        {
            while (this._socket.Connected)
            {
                try
                {
                    var command = "";
                    try
                    {
                        command = Read();
                    }
                    catch (Exception e)
                    {
                        //a socket error has occured
                        Console.WriteLine(e.StackTrace +
                           e.Message);
                        break;
                    }

                    var commands = command.Trim().Split('\n');
                    foreach (var piece in commands)
                        commandManager.execute(this, piece.Trim());

                }
                catch (SocketException se)
                {
                    Console.WriteLine("server kick " + this.GetHashCode());
                }
                catch (Exception e)
                {
                    //a socket error has occured
                    Console.WriteLine(e.StackTrace +
                           e.Message);
                    break;
                }
            }
            Console.WriteLine(this.GetHashCode() + " left");
        }
        public void Write(String strMessage)
        {
            if (_socket.Connected)
            {
                NetworkStream clientStream = _socket.GetStream();

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(strMessage);// + "\r\n");

                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
                pop_server.txtlog("s:" + strMessage);
            }
            else
            {
                pop_server.txtlog("disconn"+strMessage);
            }

        }

        private String Read()
        {
	
            byte[] messageBytes = new byte[8192];
            int bytesRead = 0;
            NetworkStream clientStream = _socket.GetStream();

            bytesRead = clientStream.Read(messageBytes, 0, 8192);

            string strMessage = System.Text.Encoding.UTF8.GetString(messageBytes, 0, bytesRead);

             pop_server.txtlog("C:"+strMessage);
            return strMessage;
        }
      /*  public int Send(byte[] byteArray)
        {
            return this._socket.Send(byteArray);
        }

        public int Send(string s)
        {
            //Console.Write("SEND[" + s + "]");
            return this._socket.Send(Protocol.StrToByteArray(s));
        }*/

        public void exit()
        {
           // this._socket.Disconnect(false);
            this._socket.Close();
        }
    }
    public class Protocol
    {
        public static string welcome = "+Ok Mendoza ready <%hash@mbcmbc.mbc.edu.mo>\n";

        public static string[] commands = {
                                            "APOP",
                                            "DELE",
                                            "LIST",
                                            "STAT",
                                            "RETR",
                                            "TOP",
                                            "CAPA",
                                            "QUIT",
                                            "USER",
                                            "PASS",
                                            "UIDL"
                                          };

        public static byte[] StrToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        public static string ByteArrayToStr(byte[] byteArray, int length)
        {
            return System.Text.Encoding.UTF8.GetString(byteArray, 0, length);
        }

        public static bool IsValidEmail(string email)
        {
            Regex rx = new Regex(
            @"^[-!#$%&'*+/0-9=?A-Z^_a-z{|}~](\.?[-!#$%&'*+/0-9=?A-Z^_a-z{|}~])*@[a-zA-Z](-?[a-zA-Z0-9])*(\.[a-zA-Z](-?[a-zA-Z0-9])*)+$");
            return rx.IsMatch(email);
        }

        public static string CalculateMD5Hash(string input)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
    public class commandManager
    {
        public static Func<handleClient, string[], int>[] funcPtr = {
                                            apop_command,
                                            dele_command,
                                            list_command,
                                            stat_command,
                                            retr_command,
                                            top_command,
                                            capa_command,
                                            quit_command,
                                            user_command,
                                            pass_command,
                                            uidl_command
                                                                    };

        public static int apop_command(handleClient hc, string[] param)
        {
            if (param.Length != 3)
            {
                hc.Write("-ERR Missing parameter.\r\n");
                return 0;
            }
            var idx = Array.IndexOf(Settings.users, param[1] + Settings.atdomain);
            var idx2 = Array.IndexOf(Settings.users, param[1]);
            if (idx < 0 && idx2 < 0)
            {
                hc.Write("-ERR Unknow user.\r\n");
                return 0;
            }
            if (idx >= 0)
                idx2 = idx;
            else
                idx = idx2;
            var full_secret = hc.secret + Settings.passwords[idx];
            if (param[2] == Protocol.CalculateMD5Hash(full_secret))
            {
                hc.Write("+Ok\r\n");
                hc.auth.login = param[1];
                hc.auth.password = Settings.passwords[idx];
            }
            return 0;
        }

        public static int dele_command(handleClient hc, string[] param)
        {
            messageManager.load();
            var lst = messageManager.list();
            int i = 1;

            if (param.Length != 2)
            {
                hc.Write("-ERR Invalid command.\r\n");
                return 0;
            }
            var no = int.Parse(param[1]);

            if (!hc.auth.auth())
            {
                hc.Write("-ERR Authentification required.\r\n");
                return 0;
            }
            foreach (Message m in lst)
            {
                if (m.to == hc.auth.login || m.to == (hc.auth.login + Settings.atdomain))
                {
                    if (i == no)
                    {
                        messageManager.list().Remove(m);
                        //messageManager.save();
                        hc.Write("+Ok message " + no + " deleted\r\n");
                        return 0;
                    }
                    i++;
                }
            }
            hc.Write("-ERR Message not found\r\n");
            return 0;
        }

        public static int list_command(handleClient hc, string[] param)
        {
            //messageManager.save();
            messageManager.load();
            var lst = messageManager.list();
            int i = 1;

            if (!hc.auth.auth())
            {
                hc.Write("-ERR Authentification required.\r\n");
                return 0;
            }
            hc.Write("+Ok Mailbox contents follows\r\n");
            foreach (Message m in lst)
            {
                if (m.to == hc.auth.login || m.to == (hc.auth.login + Settings.atdomain))
                {
                    hc.Write(i++ + " " + m.getSize() + "\r\n");
                }
            }
            hc.Write(".\r\n");
            return 0;
        }


        public static int uidl_command(handleClient hc, string[] param)
        {
            //messageManager.save();
            messageManager.load();
            var lst = messageManager.list();
            int i = 1;

            if (!hc.auth.auth())
            {
                hc.Write("-ERR Authentification required.\r\n");
                return 0;
            }
            hc.Write("+Ok Mailbox contents follows\r\n");
            foreach (Message m in lst)
            {
                if (m.to == hc.auth.login || m.to == (hc.auth.login + Settings.atdomain))
                {
                    hc.Write(i++ + " " + Protocol.CalculateMD5Hash(m.body) + "\r\n");
                }
            }
            hc.Write(".\r\n");
            return 0;
        }

        public static int stat_command(handleClient hc, string[] param)
        {
            messageManager.load();
            var lst = messageManager.list();
            int count = 0;
            int size = 0;

            if (!hc.auth.auth())
            {
                hc.Write("-ERR Authentification required.\r\n");
                return 0;
            }

            foreach (Message m in lst)
            {
                if (m.to == hc.auth.login || m.to == (hc.auth.login + Settings.atdomain))
                {
                    count++;
                    size += m.getSize();
                }
            }
            hc.Write("+Ok " + count + " " + size + "\r\n");
            return 0;
        }

        public static int retr_command(handleClient hc, string[] param)
        {
            messageManager.load();
            var lst = messageManager.list();
            int count = 0;
            int tosend = int.Parse(param[1]);
            int size = 0;

            if (!hc.auth.auth())
            {
                hc.Write("-ERR Authentification required.\r\n");
                return 0;
            }

            foreach (Message m in lst)
            {
                if (m.to == hc.auth.login || m.to == (hc.auth.login + Settings.atdomain))
                {
                    count++;
                    size += m.getSize();
                    if (count == tosend)
                    {
                        hc.Write("+Ok " + m.getSize() + " octets\r\n");
                        hc.Write(m.body);
                        hc.Write(".\r\n");
                    }
                }
            }
            return 0;
        }

        public static int top_command(handleClient hc, string[] param)
        {
            return 0;
        }

        public static int capa_command(handleClient hc, string[] param)
        {
            var tosend = "+Ok\nUIDL\nLOGIN PLAIN\nUSER\n.\r\n";
            hc.Write(tosend);
            return 0;
        }

        public static int quit_command(handleClient hc, string[] param)
        {
            hc.exit();
            return 0;
        }

        public static int user_command(handleClient hc, string[] param)
        {
            if (param.Length > 1)
            {
                hc.auth.login = param[1];
                hc.Write("+Ok User accepted\r\n");
            }
            else
            {
                hc.Write("-ERR Need USER\r\n");
            }
            return 0;
        }

        public static int pass_command(handleClient hc, string[] param)
        {
            if (param.Length > 1)
            {
                hc.auth.password = param[1];
                if (hc.auth.auth())
                {
                    hc.Write("+Ok Pass accepted\r\n");
                }
                else
                {
                    hc.Write("-ERR Authentification failed.\r\n");
                }
            }
            else
            {
                hc.Write("-ERR Need USER\r\n");
            }
            return 0;
        }

        public static void execute(handleClient hc, string command)
        {
            
           
            char[] splitTab = new char[2];

            splitTab[0] = ' ';
            splitTab[1] = ' ';
            var commandTab = command.Split(splitTab);

            if (Protocol.commands.Contains(commandTab[0].ToUpper()))
            {
                var index = Array.IndexOf(Protocol.commands, commandTab[0].ToUpper());
                funcPtr[index](hc, commandTab);
            }
            else
            {
                
                hc.Write("-ERR Unknow command.\r\n");
                // hc.Write("+Ok\r\n");
            }
        }
    }
    public class Authentificator
    {
        public string login;
        public string password;

        public Authentificator()
        {
            this.login = string.Empty;
            this.password = string.Empty;
        }

        public bool auth()
        {
            if (this.login == string.Empty || this.password == string.Empty)
                return false;
            var idx = Array.IndexOf(Settings.users, this.login + Settings.atdomain);
            var idx2 = Array.IndexOf(Settings.users, this.login);
            if (idx == -1 && idx2 == -1)
                return false;
            if (idx == -1)
                idx = idx2;
            else
                idx2 = idx;
            if (this.password == Settings.passwords[idx])
                return true;
            return false;
        }
    }
   
}
