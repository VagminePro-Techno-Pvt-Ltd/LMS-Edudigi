using TMS.Common;
using TMS.Models.Account;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Masters;

namespace TMS.Repository.SeedData
{
    internal static class CommonMasterData
    {
        public static void SeedCommonMasterData(this ModelBuilder builder)
        {
            DateTime createdOn = new(Constants.CreatedOnYear, Constants.CreatedOnMonth, Constants.CreatedOnDay);

            builder.Entity<UserType>().HasData(
                    new UserType { Id = 1, Name = "Super Admin" }
                    , new UserType { Id = 2, Name = "Admin" }
                    , new UserType { Id = 3, Name = "User" }
                    , new UserType { Id = 4, Name = "Instructor" }
                    ,new UserType { Id = 5, Name = "Co-Ordinator"}
                    , new UserType { Id = 6, Name = "Sr.Engineer" }
                );

            builder.Entity<RoleMaster>().HasData(
                new RoleMaster { Id = 1, Name = "Super Admin", IsActive = true, UserTypeId = 1, CreatedBy = Constants.UserId, CreatedOn = createdOn }
                );

            List<RolePermissions> rolePermissions = new();
            foreach (var item in Enum.GetValues(typeof(FormDefination)).Cast<FormDefination>())
            {
                //if (item != FormDefination.Quotation)
                rolePermissions.Add(new RolePermissions
                {
                    RoleId = 1,
                    FormId = (int)item,
                    View = true,
                    Add = true,
                    Edit = true
                });
            }
            builder.Entity<RolePermissions>().HasData(rolePermissions);

            builder.Entity<UserMaster>().HasData(new UserMaster
            {
                Id = Constants.UserId,
                Name = "Super Admin User",
                Email = "admin@TMS.com",
                Password = EncriptorUtility.Encrypt("Admin@123", false),
                IsActive = true,
                CreatedBy = Constants.UserId,
                CreatedOn = createdOn
            });
            builder.Entity<UserRoles>().HasData(
                    new UserRoles { RoleId = 1, UserId = Constants.UserId }
                );
            builder.Entity<LanguageMaster>().HasData(
                   new LanguageMaster { Id = 1, Name = "English", CountryCode = "IND", LanguageCode = "en-IN", IsActive=true, CreatedBy=Constants.UserId, CreatedOn=createdOn }
                   , new LanguageMaster { Id = 2, Name = "Arabic", CountryCode = "UAE", LanguageCode = "ar-AE", IsActive = true, CreatedBy = Constants.UserId, CreatedOn = createdOn }
               );

            builder.Entity<TrainingScheduleStatusMaster>().HasData(
                new TrainingScheduleStatusMaster { Id=1, Name="Planned", IsActive = true, CreatedBy = Constants.UserId, CreatedOn = createdOn }
                ,new TrainingScheduleStatusMaster { Id=2, Name="Scheduled", IsActive = true, CreatedBy = Constants.UserId, CreatedOn = createdOn }
                ,new TrainingScheduleStatusMaster { Id=3, Name= "Rejected", IsActive = true, CreatedBy = Constants.UserId, CreatedOn = createdOn }
                ,new TrainingScheduleStatusMaster { Id=4, Name= "Completed", IsActive = true, CreatedBy = Constants.UserId, CreatedOn = createdOn }
                , new TrainingScheduleStatusMaster { Id = 5, Name = "Pending For Approval", IsActive = true, CreatedBy = Constants.UserId, CreatedOn = createdOn }
                , new TrainingScheduleStatusMaster { Id = 6, Name = "Approved", IsActive = true, CreatedBy = Constants.UserId, CreatedOn = createdOn }
                , new TrainingScheduleStatusMaster { Id = 7, Name = "Cancelled", IsActive = true, CreatedBy = Constants.UserId, CreatedOn = createdOn }
                , new TrainingScheduleStatusMaster { Id = 8, Name = "ReScheduled", IsActive = true, CreatedBy = Constants.UserId, CreatedOn = createdOn }
                );
        }
    }
}
