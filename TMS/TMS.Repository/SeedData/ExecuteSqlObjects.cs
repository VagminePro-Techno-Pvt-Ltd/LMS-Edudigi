using TMS.Repository.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository
{
    internal static class ExecuteSqlObjects
    {
        public static void UpdateSQLObjects(this MigrationBuilder migrationBuilder, string? folderName=null)
        {
            var basePath = System.IO.Directory.GetCurrentDirectory();
            string path = Path.Combine(basePath.Replace("TMS.Web", "TMS.Repository"), "SqlObjects");
            if (!string.IsNullOrWhiteSpace(folderName))
                path = Path.Combine(path, folderName);

            foreach (var file in Directory.GetFiles(path, "*.sql"))
            {
                string fileText = File.ReadAllText(file);
                fileText = fileText.Replace("{", "{{").Replace("}", "}}");
                migrationBuilder.Sql(fileText);
            }
        }
    }
}
