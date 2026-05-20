using TMS.Models.Account;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository.SeedData
{
    internal static class MenuMasterData
    {
        public static void SeedMenuMasterData(this ModelBuilder builder)
        {
            DateTime CreatedOn = new(Constants.CreatedOnYear, Constants.CreatedOnMonth, Constants.CreatedOnDay);
            builder.Entity<MenuMaster>().HasData(
                    new MenuMaster() { Id = 1, Name = "Common", IconClass = "fab fa-cuttlefish", CreatedBy = Constants.UserId, CreatedOn = CreatedOn, Sequence = 2, IsActive = true, IsAdmin = true }
                    , new MenuMaster() { Id = 2, Name = "Access Master", IconClass = "fas fa-universal-access", CreatedBy = Constants.UserId, CreatedOn = CreatedOn, Sequence = 3, IsActive = true, IsAdmin = true }
                    , new MenuMaster() { Id = 3, Name = "User Master", IconClass = "fas fa-users-cog", CreatedBy = Constants.UserId, CreatedOn = CreatedOn, Sequence = 4, IsActive = true, IsAdmin = true }
                 //   , new MenuMaster() { Id = 4, Name = "Training", IconClass = "fas fa-chalkboard-teacher", CreatedBy = Constants.UserId, CreatedOn = CreatedOn, Sequence = 1, IsActive = true, IsAdmin = true }
            );
        }
    }
}

