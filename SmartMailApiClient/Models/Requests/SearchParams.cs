namespace SmartMailApiClient.Models.Requests
{
    public class SearchParams
    {
        public int skip {  get; set; }
        public int take { get; set; }
        public string search { get; set; }
        public string sortField { get; set; }
        public bool sortDescending { get; set; }
    }
}