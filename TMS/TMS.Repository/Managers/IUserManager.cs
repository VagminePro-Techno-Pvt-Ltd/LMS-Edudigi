using TMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository.Managers
{
    public interface IUserManager
    {
        Task<int> UserAddUpdate(UserViewModel model, int userId);
    }
}
