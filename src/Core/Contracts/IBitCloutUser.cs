using FluentValidation;

namespace CloutCast.Contracts
{
    public interface IBitCloutUser : IBitCloutPublicKey, IEntity
    {
        bool BlackList { get; }
        string Handle { get; }
        IBitCloutUser CopyFrom(IBitCloutUser source);
        IUserProfile Profile { get; }

        ulong? CoinPrice { get; }
        long? FollowerCount { get; }
    }

    public static class BitCloutUserExtensions
    {
        public static bool HasProfile(this IBitCloutUser user) => user?.Profile != null;

        public static bool IsSystemUser(this IBitCloutUser user) => user != null && 
                                                                    user.PublicKey.Equals("BC1YLiVetFBCYjuHZY5MPwBSY7oTrzpy18kCdUnTjuMrdx9A22xf5DE");

        public static string ToDescription(this IBitCloutUser user) => user == null
            ? ""
            : user.Handle.IsEmpty()
                ? $"User[{user.Id}]"
                : $"{user.Handle}";

        public static IRuleBuilderOptions<T, IBitCloutUser> BitCloutUser<T>(this IRuleBuilder<T, IBitCloutUser> ruleBuilder) 
            => ruleBuilder.SetValidator(new BitCloutUserValidator());
    }

    public class BitCloutUserValidator : AbstractValidator<IBitCloutUser>
    {
        public BitCloutUserValidator()
        {
            RuleFor(user => user.Id).Must(id => id > 0).WithMessage("User Id must be greater than 0");
            RuleFor(user => user.PublicKey).NotEmpty().WithMessage("User Missing PublicKey");
        }
    }
}