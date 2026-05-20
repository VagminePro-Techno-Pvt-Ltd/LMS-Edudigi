using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TMS.ViewModels.Excel
{
    public class ExcelColumnViewModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("columnName")]
        public string? ColumnName { get; set; }
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }
}
