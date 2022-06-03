namespace CloutCast
{
    public enum EntityAction
    {
        UnDefined = 0,
        UserAddFunds = 1,
        UserWithdrawFunds = 2,
        UserRegister = 10,
        UserDidPromotion = 15,
        SystemFee = 17,
        PromotionStart = 20,
        PromotionExtend = 25,
        PromotionExpire = 30,
        PromotionStop = 35,

        UserWorkPayOut = 41,
        UserWorkRefund = 42
    }
}