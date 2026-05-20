using AutoMapper;
using TMS.Models.Account;
using TMS.Repository.Repositories;
using TMS.ViewModels;
using TMS.ViewModels.Account;
using Microsoft.EntityFrameworkCore;
using TMS.Common;

namespace TMS.Repository.Managers.Implementations
{
    internal class AccountManager : IAccountManager
    {
        private readonly IRepository<UserMaster> _repository;
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;

        public AccountManager(IRepository<UserMaster> repository, IAccountRepository accountRepository, IMapper mapper)
        {
            _repository = repository;
            _accountRepository = accountRepository;
            _mapper = mapper;
        }
        public async Task<UserViewModel?> GetUserByEmail(string email)
        {
            var list = await _repository.Find(t => t.Email == email);
            UserViewModel? model = list.FirstOrDefault()?.Map<UserMaster, UserViewModel>();
            //if (model != null && model.Id == 0)
            //{
            //    var ds = _accountRepository.CompanyLogoGetByUser(model.Id);
            //}
            return model;
        }

        public async Task<UserViewModel?> Login(string email, string password)
        {
            var users = await _repository.Find(t => t.Email == email, new[] { "Role" });
            if (users == null || !users.Any())
                return null;

            var user = users.First();
            bool isValid = false;

            // 1. Try modern hash
            if (SecurityUtility.IsHashed(user.Password ?? ""))
            {
                isValid = SecurityUtility.VerifyHash(password, user.Password!);
            }
            // 2. Try legacy encryption
            else
            {
                string encryptedInput = EncriptorUtility.Encrypt(password, false);
                isValid = (user.Password == encryptedInput);

                // 3. Migrate to hash on successful legacy login
                if (isValid)
                {
                    user.Password = SecurityUtility.HashPassword(password);
                    await _repository.UpdateAsync(user);
                    await _repository.SaveChangesAsync();
                }
            }

            if (!isValid)
                return null;

            return _mapper.Map<UserViewModel>(user);
        }

        public async Task<UserPermissions?> GetUserPermissions(int userId)
        {
            UserPermissions model = new();
            if (userId == 0)
                return null;
            var dataSet = _accountRepository.GetUserMenu(userId);
            if (dataSet != null && dataSet.Tables.Count > 0)
            {
                model.Menus = dataSet.Tables[0].MapTo<MenuViewModel>();
                if (dataSet.Tables.Count > 1)
                    model.Forms = dataSet.Tables[1].MapTo<FormViewModel>();
            }
            return await Task.FromResult(model);
        }

        public async Task<List<RolePermissionsViewModel>> GetRolePermissionsById(int roleId)
        {
            List<RolePermissionsViewModel> model = new();
            var ds = _accountRepository.GetRolePermissionsById(roleId);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                model = ds.Tables[0].MapTo<RolePermissionsViewModel>();
            return await Task.FromResult(model);
        }

        public async Task<int> UpdateRolePermissions(List<RolePermissionsViewModel> permissions, int roleId)
        {
            var model = permissions.Select(t => new { t.RoleId, t.FormId, FormView = t.View, FormAdd = t.Add, FormEdit = t.Edit });
            var result = 0;
            var ds = _accountRepository.UpdateRolePermissions(model.ToDataTable(), roleId);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                result = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
            return await Task.FromResult(result);
        }

        public async Task<List<UserTypeViewModel>> GetAllUserTypes()
        {
            List<UserTypeViewModel> model = new();
            var ds = _accountRepository.GetAllUserTypes();
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                model = ds.Tables[0].MapTo<UserTypeViewModel>();
            return await Task.FromResult(model);
        }

        public async Task<List<SelectViewModel>> GetRolesByCompanyId(int companyId, int userId)
        {
            List<SelectViewModel> model = new();
            var ds = _accountRepository.RolesByCompanyId(companyId, userId);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                model = ds.Tables[0].MapTo<SelectViewModel>();
            return await Task.FromResult(model);
        }

        public async Task<FormControlListViewModel> GetFormControlPermissionByUser(int formId, int userId)
        {
            FormControlListViewModel model = new();
            var ds = _accountRepository.GetFormControlPermissionByUser(formId, userId);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                model.FormControls = ds.Tables[0].MapTo<FormControlViewModel>();
            return await Task.FromResult(model);
        }

        public async Task<List<FormControlViewModel>> GetFormControlPermissionByRole(int formId, int roleId)
        {
            List<FormControlViewModel> model = new();
            var ds = _accountRepository.GetFormControlPermissionByRole(formId, roleId);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                model = ds.Tables[0].MapTo<FormControlViewModel>();
            return await Task.FromResult(model);
        }

        public async Task<int> UpdateFormControlPermissionByRole(List<FormControlViewModel> permissions, int roleId, int userId)
        {
            var result = 0;
            var ds = _accountRepository.UpdateFormControlPermissionByRole(permissions.ToDataTable(), roleId, userId);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                result = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
            return await Task.FromResult(result);
        }

        public async Task<bool> UpdatePassword(UserViewModel model)
        {
            var oldmodel = await _repository.GetAsync(model.Id);
            if (oldmodel == null) return false;

            // Securely hash the password if it's provided in the model
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                oldmodel.Password = SecurityUtility.HashPassword(model.Password);
            }
           
            oldmodel.UpdatedOn = DateTime.Now;
            var res = await _repository.UpdateAsync(oldmodel);
            await _repository.SaveChangesAsync();
            return res;
        }

        public async Task<bool> VerifyCurrentPassword(int userId, string currentPassword)
        {
            var user = await _repository.GetAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.Password))
                return false;

            // 1. Try modern hash
            if (SecurityUtility.IsHashed(user.Password))
            {
                return SecurityUtility.VerifyHash(currentPassword, user.Password);
            }
            
            // 2. Try legacy encryption
            string encryptedInput = EncriptorUtility.Encrypt(currentPassword, false);
            return user.Password == encryptedInput;
        }

        public async Task<bool> ChangePassword(int userId, string newPassword)
        {
            var user = await _repository.GetAsync(userId);
            if (user == null)
                return false;

            // Securely hash the new password
            user.Password = SecurityUtility.HashPassword(newPassword);
            user.ForcePasswordChange = false;
            user.PasswordLastChanged = DateTime.Now;
            user.UpdatedOn = DateTime.Now;

            var res = await _repository.UpdateAsync(user);
            await _repository.SaveChangesAsync();
            return res;
        }
    }
}
