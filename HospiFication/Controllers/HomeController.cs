using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HospiFication.Models; // пространство имен моделей
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Collections;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace HospiFication.Controllers
{
    public class HomeController : Controller
    {
        BaseContext db;
        public static string Role = "";
        public static string UserName = "";
        public static string CurrentPage = "";
        public static int DocIdWithThisRole = new int();
        public HomeController(BaseContext context)
        {
            db = context;
        }
        public static UserDataPatients userdatapatients = new UserDataPatients();
        public static UserDataAttendingDocs userdataattendingdocs = new UserDataAttendingDocs();
        public static UserDataEmergencyDocs userdataemergencydocs = new UserDataEmergencyDocs();
      
        [Authorize(Roles = "Администратор, Лечащий врач, Врач приёмного отделения")]
        public ActionResult Index()
        {
            CurrentPage = "Главная";
            userdataattendingdocs.search = null;
            userdataattendingdocs.sortstate = SortState.Null;
            userdataattendingdocs.page = 1;

            userdataemergencydocs.search = null;
            userdataemergencydocs.sortstate = SortState.Null;
            userdataemergencydocs.page = 1;

            userdatapatients.datesearch = null;
            userdatapatients.search = null;
            userdatapatients.filter = null;
            userdatapatients.sortstate = SortState.Null;
            userdatapatients.page = 1;

            
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            CurrentPage = "Авторизация";
            if (String.IsNullOrEmpty(UserName))
                return View();
            else
                return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {

            if (ModelState.IsValid)
            {
                string hashedpassword = model.Password;
                if (db.Users.FirstOrDefault(s => s.UserName == model.UserName) != null)
                {
                    byte[] salt = new byte[128 / 8];
                    salt = db.Users.FirstOrDefault(s => s.UserName == model.UserName).salt;
                    hashedpassword=Hashing(hashedpassword, salt);
                } 
                User user = await db.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserName == model.UserName && u.HashedPassword == hashedpassword);
                if (user != null)
                {
                    await Authenticate(user); // аутентификация
                    UserName = user.UserName;
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }
        private async Task Authenticate(User user)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.UserName),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role?.Name)
            };
            Role = user.Role.Name;
            if (Role == "Лечащий врач")
                DocIdWithThisRole = (db.AttendingDocs.FirstOrDefault(d => d.Attending_Doc_FIO.Equals(user.UserName))).AttendingDocID;
            else if (Role == "Врач приёмного отделения")
                DocIdWithThisRole = (db.EmergencyDocs.FirstOrDefault(d => d.Emergency_Doc_FIO.Equals(user.UserName))).EmergencyDocID;
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        [Authorize(Roles = "Администратор")]
        public IActionResult AddAttendDocUser()
        {
            CurrentPage = "Добавление лечащего врача";
            User user = new User();
            user.RoleId = 2;
            user.Role = db.Roles.FirstOrDefault(i => i.Id.Equals(user.RoleId));
            return View(user);
        }

        //===========================================
        [HttpPost]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> AddAttendDocUser(User user)
        {
            user.salt = new byte[128 / 8];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetNonZeroBytes(user.salt);
            }
            user.HashedPassword=Hashing(user.HashedPassword, user.salt);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return RedirectToAction("AddAttendDoc");
        }

        [Authorize(Roles = "Администратор")]
        public IActionResult AddEmergeDocUser()
        {
            CurrentPage = "Добавление врача приёмного отделения";
            User user = new User();
            user.RoleId = 3;
            user.Role = db.Roles.FirstOrDefault(i => i.Id.Equals(user.RoleId));
            return View(user);
        }
        [HttpPost]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> AddEmergeDocUser(User user)
        {
            user.salt = new byte[128 / 8];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetNonZeroBytes(user.salt);
            }
            user.HashedPassword=Hashing(user.HashedPassword, user.salt);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return RedirectToAction("AddEmergeDoc");
        }

        ////////////////////////////////////////////////////
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> AddEmergeDoc()
        {
            EmergencyDoc emergencyDoc = new EmergencyDoc();
            emergencyDoc.Emergency_Doc_FIO = db.Users.OrderByDescending(p => p.Id).First(p => p.RoleId==3).UserName;
            emergencyDoc.Password = db.Users.OrderByDescending(p => p.Id).First(p => p.RoleId == 3).HashedPassword;
            db.EmergencyDocs.Add(emergencyDoc);
            await db.SaveChangesAsync();
            return RedirectToAction("EmergeDocs");
        }
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> AddAttendDoc()
        {
            AttendingDoc attendingDoc = new AttendingDoc();
            attendingDoc.Attending_Doc_FIO = db.Users.OrderByDescending(p => p.Id).First(p => p.RoleId == 2).UserName;
            attendingDoc.Password = db.Users.OrderByDescending(p => p.Id).First(p => p.RoleId == 2).HashedPassword;
            db.AttendingDocs.Add(attendingDoc);
            await db.SaveChangesAsync();
            return RedirectToAction("AttendDocs");
        }

        [Authorize(Roles = "Администратор")]
        public IActionResult AttendDocs(string search, SortState sortOrder=SortState.Null, int page = 0)
        {
            CurrentPage = "Лечащие врачи";
            int pageSize = 5;
            if ((userdataattendingdocs.page != 0) & (page == 0))
                page = userdataattendingdocs.page;
            if ((userdataattendingdocs.sortstate != SortState.Null) & (sortOrder == SortState.Null))
                sortOrder = userdataattendingdocs.sortstate;
            if ((!String.IsNullOrEmpty(userdataattendingdocs.search)) & (String.IsNullOrEmpty(search)))
                search = userdataattendingdocs.search;
            IEnumerable<AttendingDoc> attendingDocs = new List<AttendingDoc>();
            attendingDocs = db.AttendingDocs;
            if (!String.IsNullOrEmpty(search) && search!="Все")
            {
                attendingDocs=attendingDocs.Where(s => s.Attending_Doc_FIO.Contains(search));
            }
            ViewData["IDSort"] = sortOrder == SortState.IDAsc ? SortState.IDDesc : SortState.IDAsc;
            ViewData["FIOSort"] = sortOrder == SortState.FIOAsc ? SortState.FIODesc : SortState.FIOAsc;

            attendingDocs = sortOrder switch
            {
                SortState.IDAsc => attendingDocs.OrderBy(s => s.AttendingDocID),
                SortState.IDDesc => attendingDocs.OrderByDescending(s => s.AttendingDocID),
                SortState.FIOAsc => attendingDocs.OrderBy(s => s.Attending_Doc_FIO),
                SortState.FIODesc => attendingDocs.OrderByDescending(s => s.Attending_Doc_FIO),
                _ => attendingDocs.OrderBy(s => s.AttendingDocID),
            };
            var count = attendingDocs.Count();
            attendingDocs = attendingDocs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            PageViewModel pageViewModel = new PageViewModel(count, page, pageSize);
            CommonList common = new CommonList
            {
                AttendingDocs = attendingDocs,
                PageViewModel = pageViewModel
            };
            userdataattendingdocs.search = search;
            userdataattendingdocs.page = page;
            userdataattendingdocs.sortstate = sortOrder;
            return View(common);
        }

        [Authorize(Roles = "Администратор")]
        public IActionResult EmergeDocs(string search, SortState sortOrder = SortState.Null, int page = 0)
        {
            CurrentPage = "Врачи приёмного отделения";
            int pageSize = 5;
            if ((userdataemergencydocs.page != 0) & (page == 0))
                page = userdataemergencydocs.page;
            if ((userdataemergencydocs.sortstate != SortState.Null) & (sortOrder == SortState.Null))
                sortOrder = userdataemergencydocs.sortstate;
            if ((!String.IsNullOrEmpty(userdataemergencydocs.search)) & (String.IsNullOrEmpty(search)))
                search = userdataemergencydocs.search;
            IEnumerable<EmergencyDoc> emergencyDocs = new List<EmergencyDoc>();
            emergencyDocs = db.EmergencyDocs;

            if (!String.IsNullOrEmpty(search) && search != "Все")
            {
                emergencyDocs = emergencyDocs.Where(s => s.Emergency_Doc_FIO.Contains(search));
            }
            ViewData["IDSort"] = sortOrder == SortState.IDAsc ? SortState.IDDesc : SortState.IDAsc;
            ViewData["FIOSort"] = sortOrder == SortState.FIOAsc ? SortState.FIODesc : SortState.FIOAsc;

            emergencyDocs = sortOrder switch
            {
                SortState.IDAsc => emergencyDocs.OrderBy(s => s.EmergencyDocID),
                SortState.IDDesc => emergencyDocs.OrderByDescending(s => s.EmergencyDocID),
                SortState.FIOAsc => emergencyDocs.OrderBy(s => s.Emergency_Doc_FIO),
                SortState.FIODesc => emergencyDocs.OrderByDescending(s => s.Emergency_Doc_FIO),
                _ => emergencyDocs.OrderBy(s => s.EmergencyDocID),
            };

            var count = emergencyDocs.Count();
            emergencyDocs = emergencyDocs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            PageViewModel pageViewModel = new PageViewModel(count, page, pageSize);

            CommonList common = new CommonList
            {
                EmergencyDocs = emergencyDocs,
                PageViewModel = pageViewModel
            };
            userdataemergencydocs.page = page;
            userdataemergencydocs.search = search;
            userdataemergencydocs.sortstate = sortOrder;
            return View(common);
        }

        [HttpGet]
        [ActionName("DeleteAttend")]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> ConfirmDeleteAttend(int? id)
        {
            CurrentPage = "Удаление лечащего врача";
            AttendingDoc attendingDoc = await db.AttendingDocs.FirstOrDefaultAsync(p => p.AttendingDocID == id);
            return View(attendingDoc);
        }

        [HttpPost]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> DeleteAttend(int? id)
        {
            AttendingDoc attendingDoc = await db.AttendingDocs.FirstOrDefaultAsync(p => p.AttendingDocID == id);
            User user = await db.Users.FirstOrDefaultAsync(p => p.UserName.Equals(attendingDoc.Attending_Doc_FIO));
            db.AttendingDocs.Remove(attendingDoc);
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return RedirectToAction("AttendDocs");
        }

        [HttpGet]
        [ActionName("DeleteEmerge")]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> ConfirmDeleteEmerge(int? id)
        {
            CurrentPage = "Удаление врача приёмного отделения";
            EmergencyDoc emergencyDoc = await db.EmergencyDocs.FirstOrDefaultAsync(p => p.EmergencyDocID == id);
            return View(emergencyDoc);
        }

        [HttpPost]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> DeleteEmerge(int? id)
        {
            EmergencyDoc emergencyDoc = await db.EmergencyDocs.FirstOrDefaultAsync(p => p.EmergencyDocID == id);
            User user = await db.Users.FirstOrDefaultAsync(p => p.UserName.Equals(emergencyDoc.Emergency_Doc_FIO));
            db.EmergencyDocs.Remove(emergencyDoc);
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return RedirectToAction("EmergeDocs");
        }

        [Authorize(Roles = "Врач приёмного отделения")]
        public IActionResult AddPatient()
        {
            CurrentPage = "Добавление пациента";
            var attendingdocs = db.AttendingDocs.Select(x => new { ID = x.AttendingDocID, FIO = x.Attending_Doc_FIO }).ToArray();
            Patient patient = new Patient();
            patient.Extracted = "Не выписан";
            patient.EmergencyDocID = db.EmergencyDocs.FirstOrDefault(p => p.Emergency_Doc_FIO.Equals(UserName)).EmergencyDocID;
            patient.EntranceDate = DateTime.Today.ToShortDateString();
            patient.AttendingDocIDs = new SelectList(attendingdocs, "ID", "FIO");
            return View(patient);
        }

        [HttpPost]
        [Authorize(Roles = "Врач приёмного отделения")]
        public async Task<IActionResult> AddPatient(Patient patient)
        {
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            return RedirectToAction("AddRelative");
        }

        [Authorize(Roles = "Врач приёмного отделения")]
        public IActionResult AddRelative()
        {
            CurrentPage = "Добавление родственника";
            Relative relative = new Relative();
            relative.PatientID = db.Patients.OrderByDescending(p => p.PatientID).FirstOrDefault(p => p.EmergencyDocID.Equals(DocIdWithThisRole)).PatientID;
            return View(relative);
        }

        [HttpPost]
        [Authorize(Roles = "Врач приёмного отделения")]
        public async Task<IActionResult> SaveAndAddRelative(Relative relative)
        {
            db.Relatives.Add(relative);
            await db.SaveChangesAsync();
            return RedirectToAction("AddRelative");
        }
        [HttpPost]
        [Authorize(Roles = "Врач приёмного отделения")]
        public async Task<IActionResult> SaveRelative(Relative relative)
        {
            db.Relatives.Add(relative);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Лечащий врач")]
        public IActionResult Patients(string search, string datesearch, string extracted, int page=0, SortState sortOrder = SortState.Null)
        {
            CurrentPage = "Пациенты";
            int pageSize = 5;
            IEnumerable<Patient> patients = new List<Patient>();
            if ((userdatapatients.page != 0) & (page == 0))
                page = userdatapatients.page;
            if ((userdatapatients.sortstate != SortState.Null) & (sortOrder == SortState.Null))
                sortOrder = userdatapatients.sortstate;
            if ((!String.IsNullOrEmpty(userdatapatients.search)) & (String.IsNullOrEmpty(search)))
                search = userdatapatients.search;
            if ((!String.IsNullOrEmpty(userdatapatients.datesearch)) & (String.IsNullOrEmpty(datesearch)))
                datesearch = userdatapatients.datesearch;
            if ((!String.IsNullOrEmpty(userdatapatients.filter)) & (String.IsNullOrEmpty(extracted)))
                extracted = userdatapatients.filter;
            List<string> extractHelps = new List<string>();
            extractHelps.Insert(0, "Все");
            foreach (Patient p in db.Patients)
            {
                extractHelps.Add(p.Extracted);
            }
            extractHelps = extractHelps.Distinct().ToList();
            patients = db.Patients.Where(p => p.AttendingDocID.Equals(DocIdWithThisRole)).ToList();

            if (extracted != null && extracted != "Все")
            {
                patients = patients.Where(e => e.Extracted.Equals(extracted));
            }
            if (!String.IsNullOrEmpty(search)&&search!="Все")
            {
                patients = patients.Where(s => s.FIO.Contains(search));
            }
            if ((!String.IsNullOrEmpty(datesearch) && datesearch != "Все"))
            {
                patients = patients.Where(d => d.EntranceDate.Equals(datesearch));
            }
            ViewData["IDSort"] = sortOrder == SortState.IDAsc ? SortState.IDDesc : SortState.IDAsc;
            ViewData["FIOSort"] = sortOrder == SortState.FIOAsc ? SortState.FIODesc : SortState.FIOAsc;
            ViewData["ExtraSort"] = sortOrder == SortState.ExtractedAsc ? SortState.ExtractedDesc : SortState.ExtractedAsc;

            patients = sortOrder switch
            {
                SortState.IDAsc => patients.OrderBy(s => s.PatientID),
                SortState.IDDesc => patients.OrderByDescending(s => s.PatientID),
                SortState.FIOAsc => patients.OrderBy(s => s.FIO),
                SortState.FIODesc => patients.OrderByDescending(s => s.FIO),
                SortState.ExtractedAsc => patients.OrderBy(s => s.Extracted),
                SortState.ExtractedDesc => patients.OrderByDescending(s => s.Extracted),
                _ => patients.OrderByDescending(s => s.Extracted),
            };
            var count = patients.Count();
            patients = patients.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            PageViewModel pageViewModel = new PageViewModel(count, page, pageSize);
                
            CommonList common = new CommonList
            {
                PageViewModel = pageViewModel,
                Patients = patients,
                Extracteds = new SelectList(extractHelps)
            };
            userdatapatients.page = page;
            userdatapatients.sortstate = sortOrder;
            userdatapatients.datesearch = datesearch;
            userdatapatients.filter = extracted;
            userdatapatients.search = search;
            return View(common);
        }

        [Authorize(Roles = "Лечащий врач")]
        public IActionResult Relatives(int? id)
        {
            CurrentPage = "Родственники";
            CommonList common = new CommonList
            {
                Relatives = db.Relatives.Where(p => p.PatientID.Equals(id)).ToList()  
            };  
            return View(common);

        }

        [Authorize(Roles = "Лечащий врач")]
        public IActionResult Extract(int id)
        {
            CurrentPage = "Выписать";
            Extraction extraction = new Extraction();
            extraction.PatientID = id;
            extraction.AttendingDocID = db.Patients.FirstOrDefault(p => p.PatientID.Equals(id)).AttendingDocID;
            extraction.ExtractionDate = DateTime.Today.ToShortDateString();
            return View(extraction);
        }
        [HttpPost]
        [Authorize(Roles = "Лечащий врач")]
        public async Task<IActionResult> Extract(Extraction extraction)
        {
            db.Extractions.Add(extraction);
            Patient patient = db.Patients.FirstOrDefault(p => p.PatientID.Equals(extraction.PatientID));
            patient.Extracted = "Выписан";
            db.Patients.Update(patient);
            await db.SaveChangesAsync();
            return RedirectToAction("AddNotification");
        }

        [Authorize(Roles = "Лечащий врач")]
        public async Task <IActionResult> AddNotification()
        {
            Notification notificatio = new Notification();
            notificatio.ExtractionID=db.Extractions.OrderByDescending(p=>p.ExtractionID).First(p=>p.AttendingDocID.Equals(DocIdWithThisRole)).ExtractionID;
            notificatio.PID = db.Extractions.FirstOrDefault(p => p.ExtractionID.Equals(notificatio.ExtractionID)).PatientID;
            Relative[] relative = db.Relatives.Where(p => p.PatientID.Equals(notificatio.PID)).ToArray();
            Patient patient = db.Patients.FirstOrDefault(p => p.PatientID.Equals(notificatio.PID));
            if (relative.Length != 0)
            {
                foreach (Relative p in relative)
                {
                    Notification notification = new Notification();
                    notification.ExtractionID = db.Extractions.OrderByDescending(p => p.ExtractionID).First(p => p.AttendingDocID.Equals(DocIdWithThisRole)).ExtractionID;
                    notification.PID = db.Extractions.FirstOrDefault(p => p.ExtractionID.Equals(notification.ExtractionID)).PatientID;
                    notification.RID = p.RelativeID;
                    notification.NotificationText = $"Ваш(а) родственник(ца) {patient.FIO} будет выписан(а) сегодня в 11:00";
                    string notificationforsend = notificatio.NotificationText;
                    string phone = db.Relatives.FirstOrDefault(p => p.RelativeID.Equals(notification.RID)).RelativePhone;
                    string mail = db.Relatives.FirstOrDefault(p => p.RelativeID.Equals(notification.RID)).RelativeMail;
                    if (!String.IsNullOrEmpty(mail))
                        await NotifyByMailAsync(notification, mail);
                    if (!String.IsNullOrEmpty(phone))
                        NotifyByPhone(notification, phone);
                    db.Notifications.Add(notification);
                    await db.SaveChangesAsync();
                }
            }
            
            return RedirectToAction("Patients");
        }

        [Authorize(Roles = "Лечащий врач")]
        static async Task NotifyByMailAsync(Notification notification, string mail)
        {
            EmailService emailService = new EmailService();
            await emailService.SendEmailAsync(mail, "Выписка родственника", notification.NotificationText);
        }
        [Authorize(Roles = "Лечащий врач")]
        static void NotifyByPhone(Notification notification, string phone)
        {
            string file = @"J:\Учёба\4курс\2 семестр\Дипломный проект\Программа\HospiFication\HospiFication\LoginAndPassForNotification.txt";
            string[] lines = System.IO.File.ReadAllLines(file);
            string message = notification.NotificationText;
            string number = phone;
            string sender = "hello1";
            string login = lines[0];;
            var XML = "XML=<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
             "<SMS>\n" +
             "<operations>\n" +
             "<operation>SEND</operation>\n" +
             "</operations>\n" +
             "<authentification>\n" +
             $"<username>{login}</username>\n" +
             $"<password>{lines[1]}</password>\n" +
             "</authentification>\n" +
             "<message>\n" +
             $"<sender>{sender}</sender>\n" +
             $"<text>{message}</text>\n" +
             "</message>\n" +
             "<numbers>\n" +
             $"<number messageID=\"msg11\">{number}</number>\n" +
             "</numbers>\n" +
             "</SMS>\n";
            HttpWebRequest request = WebRequest.Create("http://api.myatompark.com/members/sms/xml.php") as HttpWebRequest;
            request.Method = "Post";
            request.ContentType = "application/x-www-form-urlencoded";
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] data = encoding.GetBytes(XML);
            request.ContentLength = data.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(data, 0, data.Length);
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception(String.Format(
                    "Server error (HTTP {0}: {1}).",
                    response.StatusCode,
                    response.StatusDescription));
                StreamReader reader = new StreamReader(response.GetResponseStream());
            }
        }
        [Authorize(Roles = "Лечащий врач")]
        public IActionResult Extractions(int? id)
        {
            CurrentPage = "Выписки";
            CommonList common = new CommonList
            {
                Extractions = db.Extractions.Where(p => p.PatientID.Equals(id)).ToList()
            };
            return View(common);

        }
        [Authorize(Roles = "Лечащий врач")]
        public IActionResult Notifications(int? id)
        {
            CurrentPage = "Уведомления";
            CommonList common = new CommonList
            {
                Notifications = db.Notifications.Where(p => p.ExtractionID.Equals(id)).ToList()
            };
            return View(common);

        }

        [Authorize(Roles = "Администратор, Лечащий врач, Врач приёмного отделения")]
        public IActionResult Logout()
        {
            Role = "";
            UserName = "";
            DocIdWithThisRole = 0;
            return RedirectToAction("Login");
        }

        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> EditAttendDocPass(int? id)
        {
            CurrentPage = "Редактирование пароля лечащего врача";
            if (id != null)
            {
                AttendingDoc attendingDoc = await db.AttendingDocs.FirstOrDefaultAsync(p => p.AttendingDocID == id);
                if (attendingDoc != null)
                    return View(attendingDoc);
            }
            return NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> EditAttendDocPass(AttendingDoc attendingDoc)
        {
            User user = db.Users.FirstOrDefault(p => p.UserName == attendingDoc.Attending_Doc_FIO);
            user.salt = new byte[128 / 8];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetNonZeroBytes(user.salt);
            }
            attendingDoc.Password = Hashing(attendingDoc.Password, user.salt);
            user.HashedPassword = attendingDoc.Password;
            db.AttendingDocs.Update(attendingDoc);
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return RedirectToAction("AttendDocs");
        }

        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> EditEmergeDocPass(int? id)
        {
            CurrentPage = "Редактирование пароля врача приёмного отделения";
            if (id != null)
            {
                EmergencyDoc emergencyDoc = await db.EmergencyDocs.FirstOrDefaultAsync(p => p.EmergencyDocID == id);
                if (emergencyDoc != null)
                    return View(emergencyDoc);
            }
            return NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "Администратор")]
        public async Task<IActionResult> EditEmergeDocPass(EmergencyDoc emergencyDoc)
        {
            User user = db.Users.FirstOrDefault(p => p.UserName == emergencyDoc.Emergency_Doc_FIO);
            user.salt = new byte[128 / 8];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetNonZeroBytes(user.salt);
            }
            emergencyDoc.Password = Hashing(emergencyDoc.Password, user.salt);
            user.HashedPassword = emergencyDoc.Password;
            db.EmergencyDocs.Update(emergencyDoc);
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return RedirectToAction("EmergeDocs");
        }

        [Authorize(Roles = "Лечащий врач")]
        public async Task<IActionResult> EditPatientAttendDoc(int? id)
        {
            CurrentPage = "Выбор другого врача для пациента";
            if (id != null)
            {
                Patient patient = await db.Patients.FirstOrDefaultAsync(p => p.PatientID == id);
                var attendingdocs = db.AttendingDocs.Select(x => new { ID = x.AttendingDocID, FIO = x.Attending_Doc_FIO }).ToArray();
                patient.AttendingDocIDs = new SelectList(attendingdocs, "ID", "FIO");
                if (patient != null)
                    return View(patient);

            }
            return NotFound();
        }

        [HttpPost]
        [Authorize(Roles = "Лечащий врач")]
        public async Task<IActionResult> EditPatientAttendDoc(Patient patient)
        {
            db.Patients.Update(patient);
            await db.SaveChangesAsync();
            return RedirectToAction("Patients");
        }

        static string Hashing (string Password, byte[] Salt)
        {
                Password = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: Password,
                salt: Salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));
            return (Password);
        }
        public ActionResult Privacy()
        {
            CurrentPage = "Конфиденциальность";
            userdataattendingdocs.search = null;
            userdataattendingdocs.sortstate = SortState.Null;
            userdataattendingdocs.page = 1;

            userdataemergencydocs.search = null;
            userdataemergencydocs.sortstate = SortState.Null;
            userdataemergencydocs.page = 1;

            userdatapatients.datesearch = null;
            userdatapatients.search = null;
            userdatapatients.filter = null;
            userdatapatients.sortstate = SortState.Null;
            userdatapatients.page = 1;


            return View();
        }
    }
}

