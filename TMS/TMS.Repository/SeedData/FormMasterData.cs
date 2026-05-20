using TMS.Common;
using TMS.Models.Account;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Masters;

namespace TMS.Repository.SeedData
{
    internal static class FormMasterData
    {
        public static void SeedFormMasterData(this ModelBuilder builder)
        {
            //DateTime CreatedOn = new (Constants.CreatedOnYear, Constants.CreatedOnMonth, Constants.CreatedOnDay);
            builder.Entity<FormMaster>().HasData(
                //Common
                  new FormMaster() { Id = (int)FormDefination.CourseCategoryMaster, Name = "Programs Offered", Area = "Admin", Controller = "CourseCategory", Action = "Index", IconClass = "fas fa-paw", Sequence = 8, MenuId = 1, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.CourseMaster, Name = "Course", Area = "Admin", Controller = "Course", Action = "Index", IconClass = "fas fa-discourse", Sequence = 9, MenuId = 1, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.SemesterMaster, Name = "Semester", Area = "Admin", Controller = "SemesterMaster", Action = "Index", IconClass = "fas fa-discourse", Sequence = 10, MenuId = 1, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.QuadrantMaster, Name = "Quadrant", Area = "Admin", Controller = "QuadrantMaster", Action = "Index", IconClass = "fas fa-discourse", Sequence = 11, MenuId = 1, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.LectureMaterial, Name = "Course Content", Area = "Admin", Controller = "LectureMaterial", Action = "Index", IconClass = "fas fa-discourse", Sequence = 12, MenuId = 1, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.FacultyMapping, Name = "Faculty Mapping", Area = "Admin", Controller = "FacultyMapping", Action = "Index", IconClass = "fas fa-discourse", Sequence = 13, MenuId = 1, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.Quiz, Name = "Quiz Master", Area = "Admin", Controller = "Quiz", Action = "Index", IconClass = "fas fa-discourse", Sequence = 14, MenuId = 1, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.Enrollment, Name = "Course Enrollment", Area = "Admin", Controller = "CourseEnrollment", Action = "Index", IconClass = "fas fa-discourse", Sequence = 15, MenuId = 1, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.QuizAssemement, Name = "Quiz Assessment", Area = "", Controller = "FacultyQuizAssessment", Action = "Index", IconClass = "fas fa-discourse", Sequence = 15, MenuId = 1, IsAdmin = true }


           

                //User Master == 3
                , new FormMaster() { Id = (int)FormDefination.Role, Name = "Role", Area = "Admin", Controller = "Roles", Action = "Index", IconClass = "fas fa-user-tag", Sequence = 1, MenuId = 3, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.RolePermissions, Name = "Role Permissions", Area = "Admin", Controller = "RolePermissions", Action = "Index", IconClass = "fas fa-universal-access", Sequence = 3, MenuId = 3, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.RoleControlPermission, Name = "Role Control Permission", Area = "Admin", Controller = "RoleControlPermission", Action = "Index", IconClass = "fas fa-comments-dollar", Sequence = 4, MenuId = 3, IsAdmin = true }
                , new FormMaster() { Id = (int)FormDefination.User, Name = "User", Area = "Admin", Controller = "User", Action = "Index", IconClass = "fas fa-user-tie", Sequence = 5, MenuId = 3, IsAdmin = true }



                //Training
               
               // , new FormMaster() { Id = (int)FormDefination.Quiz, Name = "Quiz", Area = "Admin", Controller = "Quiz", Action = "Quiz", IconClass = "fas fa-question", Sequence = 2, MenuId = 4, IsAdmin = true }
               // , new FormMaster() { Id = (int)FormDefination.TrainingSchedule, Name = "Training Schedule", Area = "Admin", Controller = "TrainingSchedule", Action = "Index", IconClass = "fas fa-calendar-alt", Sequence = 2, MenuId = 4, IsAdmin = true }
                );
        }
    }
}
