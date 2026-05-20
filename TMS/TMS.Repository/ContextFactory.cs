using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository
{
    internal class ContextFactory : IDesignTimeDbContextFactory<ApplicationDBContext>
    {
        public ContextFactory()
        {

        }

        private IConfiguration Configuration=> new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").Build();
        public ApplicationDBContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ApplicationDBContext>();
            builder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            return new ApplicationDBContext(builder.Options);
        }
    }
}
