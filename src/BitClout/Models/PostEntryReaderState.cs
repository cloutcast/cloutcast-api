namespace CloutCast.Models
{
    [JetBrains.Annotations.UsedImplicitly]
    public class PostEntryReaderState
    {
        public bool LikedByReader { get; set; }
        public ulong DiamondLevelBestowed { get; set; }
        public bool RecloutedByReader { get; set; }
        public string RecloutPostHashHex { get; set; }
    }
}