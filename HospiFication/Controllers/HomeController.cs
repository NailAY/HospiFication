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

namespace HospiFication.Controllers
{
    public class HomeController : Controller
    {
        BaseContext db;
        public static string Role = "";
        public static string UserName = "";
        public static int UserIdWithThisRole = new int();
        public HomeController(BaseContext context)
        {
            db = context;
        }
        public static UserDataPatients userdatapatients = new UserDataPatients();
        public static UserDataAttendingDocs userdataattendingdocs = new UserDataAttendingDocs();
        public static UserDataEmergencyDocs userdataemergencydocs = new UserDataEmergencyDocs();

        public ActionResult Index()
        {
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

        public ActionResult Login()
        {
            if ((UserName != "") & (Role != ""))
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpPost]
        public IActionResult Login(AttendingDoc attendingdoc)
        {
            if ((UserName != "") & (Role != ""))
            {
                return RedirectToAction("Index");
            }
            AttendingDoc a = new AttendingDoc();
            string User = attendingdoc.Attending_Doc_FIO;
            int rolecounter = 0;
            string Password = "";
            if (db.AttendingDocs.Where((p => p.Attending_Doc_FIO.Equals(User))).Count() != 0)
            {
                attendingdoc.AttendingDocID = db.AttendingDocs.FirstOrDefault(p => p.Attending_Doc_FIO.Equals(User)).AttendingDocID;
                Password = db.AttendingDocs.FirstOrDefault(p => p.Attending_Doc_FIO.Equals(User)).Password;
            }

            if ((Password == "") & (db.EmergencyDocs.Where((p => p.Emergency_Doc_FIO.Equals(User))).Count() != 0))
            {
                attendingdoc.AttendingDocID = db.EmergencyDocs.FirstOrDefault(p => p.Emergency_Doc_FIO.Equals(User)).EmergencyDocID;
                Password = db.EmergencyDocs.FirstOrDefault(p => p.Emergency_Doc_FIO.Equals(User)).Password;
                rolecounter = 1;
            }
            if (Password == "")
            {
                Password = "%a1002a%";
                rolecounter = 2;
            }

            string key = "htmb";
            int keylength = key.Length;
            char[] char_key = new char[keylength];
            int[] int_key = new int[keylength];
            int passLength = attendingdoc.Password.Length;
            AttendingDoc check = new AttendingDoc();
            if (passLength % 2 == 1)
            {
                passLength = attendingdoc.Password.Length + 1;
                char[] char_password = new char[passLength];
                int[] int_password = new int[passLength];
                char_password[passLength - 1] = '.';
                check.Password = attendingdoc.Password;
                cypher(char_password, int_password, char_key, int_key, passLength, check, key, keylength);
            }
            else
            {
                passLength = attendingdoc.Password.Length;
                char[] char_password = new char[passLength];
                int[] int_password = new int[passLength];
                check.Password = attendingdoc.Password;
                cypher(char_password, int_password, char_key, int_key, passLength, check, key, keylength);
            }
            //if (Password != "%a1002a%")
            //{
            //    byte[] bytes = Encoding.UTF32.GetBytes(Password);
            //    Password = Encoding.Default.GetString(bytes);
            //}

            if ((Password == check.Password) | (Password == "%a1002a%"))
            {
                if (rolecounter == 0)
                {
                    Role = "Лечащий врач";
                    UserName = attendingdoc.Attending_Doc_FIO;
                    UserIdWithThisRole = attendingdoc.AttendingDocID;
                }
                else if (rolecounter == 1)
                {
                    Role = "Врач приёмного отделения";
                    UserName = attendingdoc.Attending_Doc_FIO;
                    UserIdWithThisRole = attendingdoc.AttendingDocID;
                }
                else if (rolecounter == 2)
                {
                    Role = "Администратор";
                    UserName = attendingdoc.Attending_Doc_FIO;
                }
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Login");
            }

        }

        public IActionResult AddAttendDoc()
        {
            return View();
        }

        //===========================================
        [HttpPost]
        public async Task<IActionResult> AddAttendDoc(AttendingDoc attendingdoc)
        {

            string key = "htmb";
            int keylength = key.Length;
            char[] char_key = new char[keylength];
            int[] int_key = new int[keylength];
            int passLength = attendingdoc.Password.Length;
            if (passLength % 2 == 1)
            {
                passLength = attendingdoc.Password.Length + 1;
                char[] char_password = new char[passLength];
                int[] int_password = new int[passLength];
                char_password[passLength - 1] = '.';
                cypher(char_password, int_password, char_key, int_key, passLength, attendingdoc, key, keylength);
            }
            else
            {
                passLength = attendingdoc.Password.Length;
                char[] char_password = new char[passLength];
                int[] int_password = new int[passLength];
                cypher(char_password, int_password, char_key, int_key, passLength, attendingdoc, key, keylength);
            }



            ///////////////////////////////////////
            db.AttendingDocs.Add(attendingdoc);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public IActionResult AddEmergeDoc()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddEmergeDoc(EmergencyDoc emergencyDoc)
        {

            string key = "htmb";
            int keylength = key.Length;
            char[] char_key = new char[keylength];
            int[] int_key = new int[keylength];
            int passLength = emergencyDoc.Password.Length;
            if (passLength % 2 == 1)
            {
                passLength = emergencyDoc.Password.Length + 1;
                char[] char_password = new char[passLength];
                int[] int_password = new int[passLength];
                char_password[passLength - 1] = '.';
                cypher(char_password, int_password, char_key, int_key, passLength, emergencyDoc, key, keylength);
            }
            else
            {
                passLength = emergencyDoc.Password.Length;
                char[] char_password = new char[passLength];
                int[] int_password = new int[passLength];
                cypher(char_password, int_password, char_key, int_key, passLength, emergencyDoc, key, keylength);
            }



            ///////////////////////////////////////
            db.EmergencyDocs.Add(emergencyDoc);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        ////////////////////////////////////////////////////


        static void cypher(char[] char_message, int[] int_message, char[] char_key, int[] int_key, int msglength, AttendingDoc message, string key, int keylength)
        {
            char[] char_message_left = new char[msglength / 2];
            char[] char_message_right = new char[msglength / 2];
            int[] int_message_left = new int[msglength / 2];
            int[] int_message_right = new int[msglength / 2];
            int count = 0;

            ///двоичный вид пароля
            for (int i = 0; i < message.Password.Length; i++)
            {

                char_message[i] = message.Password[i];
                int_message[i] = char_message[i];


            }

            //ключ в двоичном виде
            for (int j = 0; j < keylength; j++)
            {
                char_key[j] = key[j];
                int_key[j] = char_key[j];
            }

            for (int i = 0; i < msglength / 2; i++)
            {
                char_message_left[i] = char_message[i];
                int_message_left[i] = char_message_left[i];
            }



            for (int i = 0; i < msglength / 2; i++)
            {
                char_message_right[i] = char_message[i + (msglength / 2)];
                int_message_right[i] = char_message_right[i];
            }

            int[] int_temp = new int[msglength / 2];
            char[] temp = new char[msglength / 2];


            //шифрование

            for (int j = 0; j < keylength; j++)
            {
                int int_keys_letter = key[j];
                count = 0;

                for (int i = 0; i < msglength / 2; i++)
                {
                    temp[i] = char_message_left[i];
                    int_temp[i] = int_message_left[i];
                }

                for (int i = 0; i < msglength / 2; i++)
                {
                    if (count == (msglength / 2 - 1))
                    {
                        int_message_left[i] = int_message_left[i] ^ int_keys_letter;
                        int_message_left[i] = int_message_right[i] ^ int_message_left[i];
                    }
                    else
                    {
                        int_message_left[i] = int_message_left[i] ^ 0;
                        int_message_left[i] = int_message_left[i] ^ int_message_right[i];
                    }
                    char_message_left[i] = (char)int_message_left[i];
                    count++;
                }

                for (int i = 0; i < msglength / 2; i++)
                {
                    char_message_right[i] = temp[i];
                    int_message_right[i] = int_temp[i];
                }
                for (int i = 0; i < msglength / 2; i++)
                {
                    char_message[i] = char_message_left[i];
                    char_message[i + (msglength / 2)] = char_message_right[i];
                    int_message[i] = int_message_left[i];
                    int_message[i + (msglength / 2)] = int_message_right[i];
                }
            }
            message.Password = "";
            foreach (char letter in char_message_left)
            {
                message.Password += letter;
            }
            foreach (char letter in char_message_right)
            {
                message.Password += letter;
            }
        }
        static void cypher(char[] char_message, int[] int_message, char[] char_key, int[] int_key, int msglength, EmergencyDoc message, string key, int keylength)
        {
            char[] char_message_left = new char[msglength / 2];
            char[] char_message_right = new char[msglength / 2];
            int[] int_message_left = new int[msglength / 2];
            int[] int_message_right = new int[msglength / 2];
            int count = 0;

            ///двоичный вид пароля
            for (int i = 0; i < message.Password.Length; i++)
            {

                char_message[i] = message.Password[i];
                int_message[i] = char_message[i];


            }

            //ключ в двоичном виде
            for (int j = 0; j < keylength; j++)
            {
                char_key[j] = key[j];
                int_key[j] = char_key[j];
            }

            for (int i = 0; i < msglength / 2; i++)
            {
                char_message_left[i] = char_message[i];
                int_message_left[i] = char_message_left[i];
            }



            for (int i = 0; i < msglength / 2; i++)
            {
                char_message_right[i] = char_message[i + (msglength / 2)];
                int_message_right[i] = char_message_right[i];
            }

            int[] int_temp = new int[msglength / 2];
            char[] temp = new char[msglength / 2];


            //шифрование

            for (int j = 0; j < keylength; j++)
            {
                int int_keys_letter = key[j];
                count = 0;

                for (int i = 0; i < msglength / 2; i++)
                {
                    temp[i] = char_message_left[i];
                    int_temp[i] = int_message_left[i];
                }

                for (int i = 0; i < msglength / 2; i++)
                {
                    if (count == (msglength / 2 - 1))
                    {
                        int_message_left[i] = int_message_left[i] ^ int_keys_letter;
                        int_message_left[i] = int_message_right[i] ^ int_message_left[i];
                    }
                    else
                    {
                        int_message_left[i] = int_message_left[i] ^ 0;
                        int_message_left[i] = int_message_left[i] ^ int_message_right[i];
                    }
                    char_message_left[i] = (char)int_message_left[i];
                    count++;
                }

                for (int i = 0; i < msglength / 2; i++)
                {
                    char_message_right[i] = temp[i];
                    int_message_right[i] = int_temp[i];
                }
                for (int i = 0; i < msglength / 2; i++)
                {
                    char_message[i] = char_message_left[i];
                    char_message[i + (msglength / 2)] = char_message_right[i];
                    int_message[i] = int_message_left[i];
                    int_message[i + (msglength / 2)] = int_message_right[i];
                }
            }
            message.Password = "";
            foreach (char letter in char_message_left)
            {
                message.Password += letter;
            }
            foreach (char letter in char_message_right)
            {
                message.Password += letter;
            }
        }
        ///

        public IActionResult AttendDocs(string search, SortState sortOrder=SortState.Null, int page = 0)
        {
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

        public IActionResult EmergeDocs(string search, SortState sortOrder = SortState.Null, int page = 0)
        {
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
        public async Task<IActionResult> ConfirmDeleteAttend(int? id)
        {
            AttendingDoc attendingDoc = await db.AttendingDocs.FirstOrDefaultAsync(p => p.AttendingDocID == id);
            return View(attendingDoc);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttend(int? id)
        {
            AttendingDoc attendingDoc = await db.AttendingDocs.FirstOrDefaultAsync(p => p.AttendingDocID == id);
            db.AttendingDocs.Remove(attendingDoc);
            await db.SaveChangesAsync();
            return RedirectToAction("AttendDocs");
        }

        [HttpGet]
        [ActionName("DeleteEmerge")]
        public async Task<IActionResult> ConfirmDeleteEmerge(int? id)
        {
            EmergencyDoc emergencyDoc = await db.EmergencyDocs.FirstOrDefaultAsync(p => p.EmergencyDocID == id);
            return View(emergencyDoc);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEmerge(int? id)
        {
            EmergencyDoc emergencyDoc = await db.EmergencyDocs.FirstOrDefaultAsync(p => p.EmergencyDocID == id);
            db.EmergencyDocs.Remove(emergencyDoc);
            await db.SaveChangesAsync();
            return RedirectToAction("EmergeDocs");
        }


        public IActionResult AddPatient()
        {
            var attendingdocs = db.AttendingDocs.Select(x => new { ID = x.AttendingDocID, FIO = x.Attending_Doc_FIO }).ToArray();
            Patient patient = new Patient();
            patient.Extracted = "Не выписан";
            patient.EmergencyDocID = db.EmergencyDocs.FirstOrDefault(p => p.Emergency_Doc_FIO.Equals(UserName)).EmergencyDocID;
            patient.EntranceDate = DateTime.Today.ToShortDateString();
            patient.AttendingDocIDs = new SelectList(attendingdocs, "ID", "FIO");
            return View(patient);
        }

        [HttpPost]
        public async Task<IActionResult> AddPatient(Patient patient)
        {
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            return RedirectToAction("AddRelative");
        }

        public IActionResult AddRelative()
        {
            Relative relative = new Relative();
            relative.PatientID = db.Patients.OrderByDescending(p => p.PatientID).FirstOrDefault(p => p.EmergencyDocID.Equals(UserIdWithThisRole)).PatientID;
            return View(relative);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAndAddRelative(Relative relative)
        {
            db.Relatives.Add(relative);
            await db.SaveChangesAsync();
            return RedirectToAction("AddRelative");
        }
        [HttpPost]
        public async Task<IActionResult> SaveRelative(Relative relative)
        {
            db.Relatives.Add(relative);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        public IActionResult Patients(string search, string datesearch, string extracted, int page=0, SortState sortOrder = SortState.Null)
        {
            int pageSize = 5;
            if (Role == "Лечащий врач")
            {
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
                patients = db.Patients.Where(p => p.AttendingDocID.Equals(UserIdWithThisRole)).ToList();

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
            else
            {
                CommonList common = new CommonList
                {
                    Patients = db.Patients
                };
                return View(common);
            }
        }
        public IActionResult Relatives(int? id)
        {
            CommonList common = new CommonList
            {
                Relatives = db.Relatives.Where(p => p.PatientID.Equals(id)).ToList()  
            };  
            return View(common);

        }

        public IActionResult Extract(int id)
        {
            Extraction extraction = new Extraction();
            extraction.PatientID = id;
            extraction.AttendingDocID = db.Patients.FirstOrDefault(p => p.PatientID.Equals(id)).AttendingDocID;
            extraction.ExtractionDate = DateTime.Today.ToShortDateString();
            return View(extraction);
        }
        [HttpPost]
        public async Task<IActionResult> Extract(Extraction extraction)
        {
            db.Extractions.Add(extraction);
            Patient patient = db.Patients.FirstOrDefault(p => p.PatientID.Equals(extraction.PatientID));
            patient.Extracted = "Выписан";
            db.Patients.Update(patient);
            await db.SaveChangesAsync();
            return RedirectToAction("AddNotification");
        }

        public async Task <IActionResult> AddNotification()
        {
            Notification notificatio = new Notification();
            notificatio.ExtractionID=db.Extractions.OrderByDescending(p=>p.ExtractionID).First(p=>p.AttendingDocID.Equals(UserIdWithThisRole)).ExtractionID;
            notificatio.PatientID = db.Extractions.FirstOrDefault(p => p.ExtractionID.Equals(notificatio.ExtractionID)).PatientID;
            Relative[] relative = db.Relatives.Where(p => p.PatientID.Equals(notificatio.PatientID)).ToArray();
            Patient patient = db.Patients.FirstOrDefault(p => p.PatientID.Equals(notificatio.PatientID));
            if (relative.Length != 0)
            {
                foreach (Relative p in relative)
                {
                    Notification notification = new Notification();
                    notification.ExtractionID = db.Extractions.OrderByDescending(p => p.ExtractionID).First(p => p.AttendingDocID.Equals(UserIdWithThisRole)).ExtractionID;
                    notification.PatientID = db.Extractions.FirstOrDefault(p => p.ExtractionID.Equals(notification.ExtractionID)).PatientID;
                    notification.RelativeID = p.RelativeID;
                    notification.NotificationText = $"Ваш(а) родственник(ца) {patient.FIO} будет выписан(а) сегодня в 11:00";
                    string phone = db.Relatives.FirstOrDefault(p => p.RelativeID.Equals(notification.RelativeID)).RelativePhone;
                    string mail = db.Relatives.FirstOrDefault(p => p.RelativeID.Equals(notification.RelativeID)).RelativeMail;
                    NotifyByMail(notification, mail);
                    NotifyByPhone(notification, phone);
                    db.Notifications.Add(notification);
                    await db.SaveChangesAsync();
                }
            }
            
            return RedirectToAction("Patients");
        }

        static void NotifyByMail(Notification notification, string mail)
        {
            
        }
        static void NotifyByPhone(Notification notification, string phone)
        {

        }

        public IActionResult Extractions(int? id)
        {
            CommonList common = new CommonList
            {
                Extractions = db.Extractions.Where(p => p.PatientID.Equals(id)).ToList()
            };
            return View(common);

        }

        public IActionResult Notifications(int? id)
        {
            CommonList common = new CommonList
            {
                Notifications = db.Notifications.Where(p => p.ExtractionID.Equals(id)).ToList()
            };
            return View(common);

        }
        public IActionResult Logout()
        {
            Role = "";
            UserName = "";
            UserIdWithThisRole = 0;
            return RedirectToAction("Login");
        }
    }
}

