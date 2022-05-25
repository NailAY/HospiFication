using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HospiFication.Models
{
    public class BaseContext : DbContext
    {
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

    }
}
