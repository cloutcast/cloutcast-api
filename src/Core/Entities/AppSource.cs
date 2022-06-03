using CloutCast.Contracts;

namespace CloutCast.Entities
{
    public class AppSource : IAppSource
    {
        public long Id { get; set; }
        public string ApiKey { get; set; }
        public string Company { get; set; }
        public string Name { get; set; }
    }
}