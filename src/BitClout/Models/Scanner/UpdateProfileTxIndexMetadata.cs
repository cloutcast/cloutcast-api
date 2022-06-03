namespace CloutCast.Models.Scanner
{
    public class UpdateProfileTxIndexMetadata
    {
        public string ProfilePublicKeyBase58Check { get; set; }
        public string NewUsername { get; set; }
        public string NewDescription { get; set; }
        public string NewProfilePic { get; set; } 
        public ulong NewCreatorBasisPoints { get; set; }
        public ulong NewStakeMultipleBasisPoints { get; set; }
        public bool IsHidden { get; set; }
    }
}