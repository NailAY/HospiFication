using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HospiFication.Models
{
    public class MedicineIDs
    {
        public int MedicineIDsID { get;set; }
        public int MedicineID { get; set; }
        public int ExtractionID { get; set; }
    }
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string HashedPassword { get; set; }
        public byte[] salt { get; set; }

        public int? RoleId { get; set; }
        public Role Role { get; set; }
    }
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<User> Users { get; set; }
        public Role()
        {
            Users = new List<User>();
        }
    }
    public class LoginModel
    {
        [Required(ErrorMessage = "Не указано имя пользователя")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Не указан пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
    public class AttendingDoc
    {
        public int AttendingDocID { get; set; }
        public string Attending_Doc_FIO { get; set; }
        public string Password { get; set; }
    }

    public class EmergencyDoc
    {
        public int EmergencyDocID { get; set; }
        public string Emergency_Doc_FIO { get; set; }
        public string Password { get; set; }
    }

    public class Patient
    {
        public int PatientID { get; set; }
        public string FIO { get; set; }
        public string BirthDay { get; set; }
        public int AttendingDocID { get; set; }
        public AttendingDoc AttendingDoc { get; set; }
        public string Symptoms { get; set; }
        public string Extracted { get; set; }
        public string EntranceDate { get; set; }
        public int EmergencyDocID { get; set; }
        public EmergencyDoc EmergencyDoc { get; set; }
        
        [NotMapped]
        public SelectList AttendingDocIDs { get; set; }
    }


    public class Relative
    {
        public int RelativeID { get; set; } 
        public int PatientID { get; set; }
        public Patient Patient { get; set; }
        public string RelativeFIO { get; set; }
        public string RelativePhone { get; set; }
        public string RelativeMail { get; set; }
        public string WhatsRelative { get; set; }
    }

    public class Extraction
    {
        public int ExtractionID { get; set; }
        public string ExtractionDate { get; set; }
        public int PatientID { get; set; }
        public Patient Patient { get; set; }
        public int AttendingDocID { get; set; }
        public string Conclusion { get; set; }
        public List<MedicineIDs> Medicines { get; set; }
        
        [NotMapped]
        public SelectList MedicineIDs { get; set; }
    }

    public class Notification
    {
        public int NotificationId { get; set; }
        public int ExtractionID { get; set; }
        public Extraction Extraction { get; set; }
        public int PID { get; set; }
        public Patient Patient { get; set; }
        public string NotificationText { get; set; }
        public int RID { get; set; }
        public Relative Relative { get; set; }
    }

    public class Medicine
    {
        public int MedicineID { get; set; }
        public string MedicineName { get; set; }
        public string Disease { get; set; }
    }


    public class CommonList
    {
        public IEnumerable<AttendingDoc> AttendingDocs { get; set; }
        public IEnumerable<EmergencyDoc> EmergencyDocs { get; set; }
        public IEnumerable<Patient> Patients { get; set; }
        public IEnumerable<Relative> Relatives { get; set; }
        public IEnumerable<Extraction> Extractions{ get; set; }
        public IEnumerable<Notification> Notifications { get; set; }
        public IEnumerable<Medicine> Medicines { get; set; }
        public IEnumerable<MedicineIDs> MedicineIDss { get; set; }
        public PageViewModel PageViewModel { get; set; }
        [NotMapped]
        public SelectList Extracteds { get; set; }
    }

    public enum SortState
    {
        FIOAsc,    // по фамилии по возрастанию
        FIODesc,   // по фамилии по убыванию
        IDAsc,        // по ID по возрастанию
        IDDesc,       // по ID по убыванию 
        ExtractedAsc,       // сначала выписаны
        ExtractedDesc,      // сначала не выписаны 
        Null, // Для принятия доп переменной за null 
    }
    public class UserDataPatients
    {
        public string search { get; set; }
        public string datesearch { get; set; }
        public string filter { get; set; }
        public SortState sortstate { get; set; }
        public int page { get; set; }
    }
    public class UserDataAttendingDocs
    {
        public string search { get; set; }
        public SortState sortstate { get; set; }
        public int page { get; set; }

    }
    public class UserDataEmergencyDocs
    {
        public string search { get; set; }
        public SortState sortstate { get; set; }
        public int page { get; set; }

    }

    public class UserDataMedicines
    {
        public string search { get; set; }
        public SortState sortstate { get; set; }
        public int page { get; set; }

    }

    public class PageViewModel
    {
        public int PageNumber { get; private set; }
        public int TotalPages { get; private set; }
        public PageViewModel(int count, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        }
        public bool HasPreviousPage
        {
            get
            {
                return (PageNumber > 1);
            }
        }
        public bool HasNextPage
        {
            get
            {
                return (PageNumber < TotalPages);
            }
        }
    }
}
