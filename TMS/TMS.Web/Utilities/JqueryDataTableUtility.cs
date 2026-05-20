namespace TMS.Web
{
    public static class JqueryDataTableUtility
    {
        public static DataTableRequest GetDataTableRequest(HttpRequest httpRequest)
        {
            DataTableRequest request = new();

            request.Draw = Convert.ToInt32(httpRequest.Form["draw"].FirstOrDefault());
            request.Start = Convert.ToInt32(httpRequest.Form["start"].FirstOrDefault());
            request.Length = Convert.ToInt32(httpRequest.Form["length"].FirstOrDefault());
            request.Search = new DataTableSearch()
            {
                Value = httpRequest.Form["search[value]"].FirstOrDefault()
            };
            request.Order = GetOrderingList(httpRequest).ToArray();
            return request;
        }
        private static List<DataTableOrder> GetOrderingList(HttpRequest httpRequest)
        {
            List<DataTableOrder> list = new();
            for (int i = 0; i <= 1000; i++)
            {
                var dir = httpRequest.Form[$"order[{i}][dir]"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(dir)) break;
                list.Add(new() { Dir = dir, Column = Convert.ToInt32(httpRequest.Form[$"order[{i}][column]"].FirstOrDefault()) });
            }
            return list;
        }
        public static string? GetRequestValue(this HttpRequest httpRequest, string key, bool decrypt = false)
        {
            var value = Convert.ToString(httpRequest.Form[key].FirstOrDefault());
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (decrypt)
                return value.Decrypt();
            return value;
        }
    }
}
