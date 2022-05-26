using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

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
}
