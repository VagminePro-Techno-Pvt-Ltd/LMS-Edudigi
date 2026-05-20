using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS
{
    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public T[]? Data { get; set; }
        public string? Error { get; set; }
    }
    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }

        public DataTableOrder[]? Order { get; set; }
        public DataTableColumn[]? Columns { get; set; }
        public DataTableSearch? Search { get; set; }
        public string? SearchField1 { get; set; }
        public string? SearchField2 { get; set; }
        public string? SearchField3 { get; set; }
        public string? SearchField4 { get; set; }
        public string? SearchField5 { get; set; }
        public string? SearchField6 { get; set; }
        public string? SearchField7 { get; set; }
        public string? SearchField8 { get; set; }
        public string? SearchField9 { get; set; }
        public string? SearchField10 { get; set; }
        public string? SearchField11 { get; set; }
        public string? SearchField12 { get; set; }
        public string? SearchField13 { get; set; }
        public string? SearchField14 { get; set; }
        public string? SearchField15 { get; set; }
        public string? SearchField16 { get; set; }
        public string? SearchField17 { get; set; }
        public string? SearchField18 { get; set; }
        public string? SearchField19 { get; set; }
        public string? SearchField20 { get; set; }
        public string? SearchField21 { get; set; }
        public string? SearchField22 { get; set; }
        public string? SearchField23 { get; set; }
        public string? SearchField24 { get; set; }
        public string? SearchField25 { get; set; }
        public string? SearchField26 { get; set; }
        public string? SearchField27 { get; set; }
        public string? SearchField28 { get; set; }
        public string? SearchField29 { get; set; }
        public string? SearchField30 { get; set; }
        public string? SearchField31 { get; set; }
    }
    public class DataTableSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string? Dir { get; set; }
    }

    public class DataTableColumn
    {
        public string? Data { get; set; }
        public string? Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }

        public DataTableSearch Search { get; set; }=new DataTableSearch();
    }

    public class DataTableColumnsOrder
    {
        public string? Column { get; set; }
        public string? Dir { get; set; }
    }
}
