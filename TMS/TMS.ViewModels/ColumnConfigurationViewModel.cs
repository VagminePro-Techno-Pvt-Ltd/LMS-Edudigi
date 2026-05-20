using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.ViewModels
{
    public class ColumnConfigurationViewModel
    {
        public string Name { get; set; } = default!;

        public string DataType { get; set; } = default!;

        public string Nullable { get; set; } = default!;

        public string Parameter { get; set; } = default!;
        public bool IsNullable
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Nullable))
                    return false;
                if (Nullable.Equals("Y", StringComparison.InvariantCultureIgnoreCase) || Nullable.Equals("YES", StringComparison.InvariantCultureIgnoreCase))
                    return true;
                return false;
            }
        }
    }

    public class ColumnCollection
    {
        public ColumnConfigurationViewModel[]? ColumnConfigurationViewModels { get; set; }
    }
}

