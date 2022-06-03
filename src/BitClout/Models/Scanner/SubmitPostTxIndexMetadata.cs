namespace CloutCast.Models.Scanner
{
    public class SubmitPostTxIndexMetadata {
        public string PostHashBeingModifiedHex { get; set; }
        public string ParentPostHashHex { get; set; }

        public bool IsAComment() => ParentPostHashHex.IsNotEmpty();
    }
}