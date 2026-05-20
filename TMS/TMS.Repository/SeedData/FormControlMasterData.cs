using TMS.Common;
using TMS.Common.FormControls;
using TMS.Models.Account;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository.SeedData
{
    internal static class FormControlMasterData
    {
        public static void SeedFormControlMasterData(this ModelBuilder builder)
        {
            //DateTime createdOn = new(Constants.CreatedOnYear, Constants.CreatedOnMonth, Constants.CreatedOnDay);

            List<FormControlMaster> formControlMasters = new();
            //Quiz
          
            builder.Entity<FormControlMaster>().HasData(formControlMasters);
        }
    }
}
