using ExcelDataReader;
using System.Data;

namespace TMS.Web.Utilities
{
    public class ExcelReaderUtility
    {
        internal DataTable ReadExcel(Stream stream)
        {
            IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            var conf = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            };
            return excelReader.AsDataSet(conf).Tables[0];
        }

        internal bool IsFileExists(string folderLocation, string value)
        {
            string path = Path.Combine(folderLocation, value);
            if (File.Exists(path))
                return true;
            return false;
        }
        internal Stream GetFileStream(string folderLocation, string value)
        {
            string path = Path.Combine(folderLocation, value);
            if (File.Exists(path))
            {
                using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read);
                return stream;
            }
            return null;
        }
    }
}
