using Easy.Common;

namespace CloutCast
{
    public class SessionKeyModel
    {
        public static string Name = "SessionKey";
        public static string HeaderName = "x-cloutcast-session-key";

        private readonly string _uniqueIdentifier = IDGenerator.Instance.Next;
        public override string ToString() => _uniqueIdentifier;
    }
}