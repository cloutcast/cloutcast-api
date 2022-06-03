namespace CloutCast
{
    public enum BitCloutTransactionTypes
    {
        BASIC_TRANSFER,
        CREATOR_COIN,
        BITCOIN_EXCHANGE,
        FOLLOW, //will include => FollowTxindexMetadata
        LIKE, // will include => LikeTxindexMetadata
        PRIVATE_MESSAGE, // will include => PrivateMessageTxindexMetadata
        SUBMIT_POST, // will include => SubmitPostTxindexMetadata
        UPDATE_PROFILE, // will include => UpdateProfileTxindexMetadata
    }
}