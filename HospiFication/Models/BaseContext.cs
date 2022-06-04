using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using MimeKit;
using MailKit.Net.Smtp;
using System.Web;
using System.Net;
using System.IO;
using System.Text;

namespace HospiFication.Models
{
    public class BaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AttendingDoc> AttendingDocs { get; set; }
        public DbSet<EmergencyDoc> EmergencyDocs { get; set; }
        
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Relative> Relatives { get; set; }

        public DbSet<Extraction> Extractions { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public BaseContext(DbContextOptions<BaseContext> options)
               : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            string adminRoleName = "Администратор";
            string attendingDocRoleName = "Лечащий врач";
            string emergencyDocRoleName = "Врач приёмного отделения";

            string adminUserName = "Администратор";
            string adminPassword = "%a1002a%";
            byte[] salt = new byte[128 / 8];

            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetNonZeroBytes(salt);
            }
            
            string hashedpassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: adminPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            // добавляем роли
            Role adminRole = new Role { Id = 1, Name = adminRoleName };
            Role attendingDocRole = new Role { Id = 2, Name = attendingDocRoleName };
            Role emergencyDocRole = new Role { Id = 3, Name = emergencyDocRoleName };
            User adminUser = new User { Id = 1, UserName = adminUserName, HashedPassword = hashedpassword, RoleId = adminRole.Id, salt=salt };

            modelBuilder.Entity<Role>().HasData(new Role[] { adminRole, attendingDocRole, emergencyDocRole});
            modelBuilder.Entity<User>().HasData(new User[] { adminUser });
            base.OnModelCreating(modelBuilder);
        }
    }

    public class EmailService
    {
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            string file = @"J:\Учёба\4курс\2 семестр\Дипломный проект\Программа\HospiFication\HospiFication\LoginAndPassForNotification.txt";
            string[] lines = System.IO.File.ReadAllLines(file);
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Hospifi", "hospification@mail.ru"));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message
            };

            using (var client = new SmtpClient())
            {

                await client.ConnectAsync("smtp.mail.ru", 465, true);
                await client.AuthenticateAsync("hospification@mail.ru", lines[2]);
                await client.SendAsync(emailMessage);

                await client.DisconnectAsync(true);
            }
        }
    }
    public class SMSC
    {
        
        // Константы с параметрами отправки
        string SMSC_LOGIN = Startup.lines[0];          // логин клиента
        string SMSC_PASSWORD = Startup.lines[1];    // пароль или MD5-хеш пароля в нижнем регистре
        bool SMSC_POST = false;                     // использовать метод POST
        const bool SMSC_HTTPS = false;              // использовать HTTPS протокол
        const string SMSC_CHARSET = "utf-8";        // кодировка сообщения (windows-1251 или koi8-r), по умолчанию используется utf-8
        const bool SMSC_DEBUG = false;              // флаг отладки

        public string[][] D2Res;



        public string[] send_sms(string phones, string message, int translit = 0, string time = "", int id = 0, int format = 0, string sender = "", string query = "", string[] files = null)
        {
            if (files != null)
                SMSC_POST = true;

            string[] formats = { "flash=1", "push=1", "hlr=1", "bin=1", "bin=2", "ping=1", "mms=1", "mail=1", "call=1", "viber=1", "soc=1" };

            string[] m = _smsc_send_cmd("send", "cost=3&phones=" + _urlencode(phones)
                            + "&mes=" + _urlencode(message) + "&id=" + id.ToString() + "&translit=" + translit.ToString()
                            + (format > 0 ? "&" + formats[format - 1] : "") + (sender != "" ? "&sender=" + _urlencode(sender) : "")
                            + (time != "" ? "&time=" + _urlencode(time) : "") + (query != "" ? "&" + query : ""), files);


            if (SMSC_DEBUG)
            {
                if (Convert.ToInt32(m[1]) > 0)
                    _print_debug("Сообщение отправлено успешно. ID: " + m[0] + ", всего SMS: " + m[1] + ", стоимость: " + m[2] + ", баланс: " + m[3]);
                else
                    _print_debug("Ошибка №" + m[1].Substring(1, 1) + (m[0] != "0" ? ", ID: " + m[0] : ""));
            }

            return m;
        }

        // ПРИВАТНЫЕ МЕТОДЫ

        // Метод вызова запроса. Формирует URL и делает 3 попытки чтения

        private string[] _smsc_send_cmd(string cmd, string arg, string[] files = null)
        {
            string url, _url;

            arg = "login=" + _urlencode(SMSC_LOGIN) + "&psw=" + _urlencode(SMSC_PASSWORD) + "&fmt=1&charset=" + SMSC_CHARSET + "&" + arg;

            url = _url = (SMSC_HTTPS ? "https" : "http") + "://smsc.ru/sys/" + cmd + ".php" + (SMSC_POST ? "" : "?" + arg);

            string ret;
            int i = 0;
            HttpWebRequest request;
            StreamReader sr;
            HttpWebResponse response;

            do
            {
                if (i++ > 0)
                    url = _url.Replace("smsc.ru/", "www" + i.ToString() + ".smsc.ru/");

                request = (HttpWebRequest)WebRequest.Create(url);

                if (SMSC_POST)
                {
                    request.Method = "POST";

                    string postHeader, boundary = "----------" + DateTime.Now.Ticks.ToString("x");
                    byte[] postHeaderBytes, boundaryBytes = Encoding.ASCII.GetBytes("--" + boundary + "--\r\n"), tbuf;
                    StringBuilder sb = new StringBuilder();
                    int bytesRead;

                    byte[] output = new byte[0];

                    if (files == null)
                    {
                        request.ContentType = "application/x-www-form-urlencoded";
                        output = Encoding.UTF8.GetBytes(arg);
                        request.ContentLength = output.Length;
                    }
                    else
                    {
                        request.ContentType = "multipart/form-data; boundary=" + boundary;

                        string[] par = arg.Split('&');
                        int fl = files.Length;

                        for (int pcnt = 0; pcnt < par.Length + fl; pcnt++)
                        {
                            sb.Clear();

                            sb.Append("--");
                            sb.Append(boundary);
                            sb.Append("\r\n");
                            sb.Append("Content-Disposition: form-data; name=\"");

                            bool pof = pcnt < fl;
                            String[] nv = new String[0];

                            if (pof)
                            {
                                sb.Append("File" + (pcnt + 1));
                                sb.Append("\"; filename=\"");
                                sb.Append(Path.GetFileName(files[pcnt]));
                            }
                            else
                            {
                                nv = par[pcnt - fl].Split('=');
                                sb.Append(nv[0]);
                            }

                            sb.Append("\"");
                            sb.Append("\r\n");
                            sb.Append("Content-Type: ");
                            sb.Append(pof ? "application/octet-stream" : "text/plain; charset=\"" + SMSC_CHARSET + "\"");
                            sb.Append("\r\n");
                            sb.Append("Content-Transfer-Encoding: binary");
                            sb.Append("\r\n");
                            sb.Append("\r\n");

                            postHeader = sb.ToString();
                            postHeaderBytes = Encoding.UTF8.GetBytes(postHeader);

                            output = _concatb(output, postHeaderBytes);

                            if (pof)
                            {
                                FileStream fileStream = new FileStream(files[pcnt], FileMode.Open, FileAccess.Read);

                                // Write out the file contents
                                byte[] buffer = new Byte[checked((uint)Math.Min(4096, (int)fileStream.Length))];

                                bytesRead = 0;
                                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                                {
                                    tbuf = buffer;
                                    Array.Resize(ref tbuf, bytesRead);

                                    output = _concatb(output, tbuf);
                                }
                            }
                            else
                            {
                                byte[] vl = Encoding.UTF8.GetBytes(nv[1]);
                                output = _concatb(output, vl);
                            }

                            output = _concatb(output, Encoding.UTF8.GetBytes("\r\n"));
                        }
                        output = _concatb(output, boundaryBytes);

                        request.ContentLength = output.Length;
                    }

                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(output, 0, output.Length);
                }

                try
                {
                    response = (HttpWebResponse)request.GetResponse();

                    sr = new StreamReader(response.GetResponseStream());
                    ret = sr.ReadToEnd();
                }
                catch (WebException)
                {
                    ret = "";
                }
            }
            while (ret == "" && i < 5);

            if (ret == "")
            {
                if (SMSC_DEBUG)
                    _print_debug("Ошибка чтения адреса: " + url);

                ret = ","; // фиктивный ответ
            }

            char delim = ',';

            if (cmd == "status")
            {
                string[] par = arg.Split('&');

                for (i = 0; i < par.Length; i++)
                {
                    string[] lr = par[i].Split("=".ToCharArray(), 2);

                    if (lr[0] == "id" && lr[1].IndexOf("%2c") > 0) // запятая в id - множественный запрос
                        delim = '\n';
                }
            }

            return ret.Split(delim);
        }

        // кодирование параметра в http-запросе
        private string _urlencode(string str)
        {
            if (SMSC_POST) return str;

            return HttpUtility.UrlEncode(str);
        }

        // объединение байтовых массивов
        private byte[] _concatb(byte[] farr, byte[] sarr)
        {
            int opl = farr.Length;

            Array.Resize(ref farr, farr.Length + sarr.Length);
            Array.Copy(sarr, 0, farr, opl, sarr.Length);

            return farr;
        }

        // вывод отладочной информации
        private void _print_debug(string str)
        {
            //System.Windows.Forms.MessageBox.Show(str);
        }
    }
}
