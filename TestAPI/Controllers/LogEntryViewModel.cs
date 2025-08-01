
namespace TestAPI.Controllers
{
    public class LoginEntryViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ExceptionType { get; set; }
        public string DeviceInfo { get; set; }
        public string ContentPreview { get; set; }
        public long FileSize { get; set; }
    }
}