using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace mailstorage
{
    public class MailStorage
    {
    }
    public class Settings
    {
        public static string[] users = { "info@school.edu",
                                         "admin@school.edu",
                                         "contact@school.edu",
                                         "tester@school.edu",
                                         "lovelord@school.edu"
                                       };
        public static string[] passwords = {
                                               "123",
                                               "123",
                                               "123",
                                               "123",
                                               "123"
                                           };
        public static string domain = "school.edu";
        public static string atdomain = "@school.edu";
        private static string _appPath = null;
        /// <summary>
        /// 應用程式目錄
        /// </summary>
        public static string AppPath
        {
            get
            {
                if (_appPath == null)
                {
                    _appPath = System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6);
                }
                return _appPath;
            }
        }
    }
    public class Message
    {
        public Message()
        {
            this.from = string.Empty;
            this.to = string.Empty;
            this.subject = string.Empty;
            this.body = string.Empty;
            this.dot = false;
        }
        public int getSize()
        {
            return this.body.Length;
        }
        public bool dot { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
    }
    public class messageManager
    {
        private static List<Message> _mails;
        private static int maxrowid = 0;
        public messageManager()
        {
            _mails = new List<Message>();
            loaddb();
        }
        public static List<Message> list()
        {
            if (_mails == null)
            {
                _mails = new List<Message>();
            }
            loaddb();
            return _mails;
        }
        public static void push(Message m)
        {
            if (_mails == null)
            {
                _mails = new List<Message>();
                loaddb();
            }
            _mails.Add(m);
            insertdb(m);
            save();
        }

        public static void save()
        {
            /*
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<Message>));
                using (StreamWriter wr = new StreamWriter("box.xml"))
                {
                    xs.Serialize(wr, _mails);
                }
            }catch(Exception ep)
            {
                Console.WriteLine(ep.Message);
            }*/
        }

        public static void load()
        {
            if (_mails == null)
            {
                _mails = new List<Message>();
                loaddb();
            }
            
            /*
            try{
             XmlSerializer xs = new XmlSerializer(typeof(List<Message>));
             using (StreamReader rd = new StreamReader("box.xml"))
            {
                _mails = xs.Deserialize(rd) as List<Message>;
            }
                        }catch(Exception ep)
            {
                Console.WriteLine(ep.Message);
            }*/
        }

        ///
        public static string getdb()
        {
            String fileName = Settings.AppPath + @"\mail.sqlite";
            FileInfo finfo = new FileInfo(fileName);
            string _connstr = "Data Source=" + fileName;
            if (!finfo.Exists)
            {
                System.Data.SQLite.SQLiteConnection.CreateFile(fileName);
                string sql = "create table mail(from_ Text, to_ Text , subject_ Text ,body_ Text, dot_ boolean,reci_dt Text);";
                using (SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection(_connstr))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }
            }
            return _connstr;
        }
        public static void loaddb()
        {
            string _connstr = getdb();
            string ins_ = "select rowid,from_,to_,subject_,body_,dot_ from  mail where rowid>"+ maxrowid+";";
            using (SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection(_connstr))
            {
                conn.Open();
                try
                {
                    using (SQLiteDataReader dr = new SQLiteCommand(ins_, conn).ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Message m = new Message();
                            maxrowid = dr.GetInt32(0);
                            m.from = dr.GetString(1);
                            m.to = dr.GetString(2);
                            m.subject = dr.GetString(3);
                            m.body = dr.GetString(4);
                            m.dot = dr.GetBoolean(5);
                            _mails.Add(m);
                        }
                    }
                }
                catch (Exception ep)
                {
                    Console.WriteLine(ep.Message);
                }
                conn.Close();
            }
        }
        public static void insertdb(Message m)
        {
            string _connstr = getdb();
            string ins_ = "insert into mail (from_,to_,subject_,body_,dot_,reci_dt)values(?,?,?,?,?,?);";
            using (SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection(_connstr))
            {
                conn.Open();
                try
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(ins_, conn))
                    {
                        cmd.Parameters.AddWithValue("@from_", m.from);
                        cmd.Parameters.AddWithValue("@to_", m.to);
                        cmd.Parameters.AddWithValue("@subject_", m.subject);
                        cmd.Parameters.AddWithValue("@body_", m.body);
                        cmd.Parameters.AddWithValue("@dot_", m.dot);
                        cmd.Parameters.AddWithValue("@reci_dt", DateTime.Now.ToString("yyyyMMddHHmmss"));
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ep)
                {
                    Console.WriteLine(ep.Message);
                }
                conn.Close();
            }

        }
    }
    public class Protocol
    {
        public static string welcome = "220 tolsa.mbcmbc.mbc.edu.mo ESMTP Mendoza\r\n";
        public static string hello = "HELO mbcmbc.mbc.edu.mo\r\n";
        public static string helo_accept250 = "250 mbcmbc.mbc.edu.mo Hello [%1], pleased to meet you\r\n";
        public static string helo_error501 = "501 5.0.0 HELO requires domain address\r\n";
        public static string ehlo_error501 = "501 5.0.0 EHLO requires domain address\r\n";
        public static string mail_error501 = "501 5.5.2 Syntax error in parameters scanning \"FROM\"\r\n";
        public static string mail_error501_wrongparam = "501 5.5.4 %1... %1 parameter unrecognized\r\n";
        public static string mail_accept250 = "250 2.1.0 %1... Sender ok\r\n";
        public static string rcpt_error501_noparam = "501 5.5.2 Syntax error in parameters scanning \"TO\"\r\n";
        public static string rcpt_error501_unknowuser = "501 5.1.1 %1... User unknow\r\n";
        public static string rcpt_error550_relaydenied = "550 5.7.1 %1... Relaying denied\r\n";
        public static string rcpt_accept250 = "250 2.1.5 %1... Recipient ok\r\n";
        public static string rcpt_need_mail = "503 5.0.0 Need MAIL before RCPT\r\n";
        public static string data_need_mail = "503 5.0.0 Need MAIL command\r\n";
        public static string abilities = "250 MY_ABILITY1\n250 MY_ABILITY2\r\n";

        public static string[] commands = {
                                              "HELO",
                                              "EHLO",
                                              "MAIL",
                                              "RCPT",
                                              "DATA",
                                              ".",
                                              "quit",
                                              "QUIT"
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
    }
}
