using System.Collections.Generic;
using FluentValidation;

namespace CloutCast.Requests
{
    using Contracts;
    
    public class GetPostsForUserRequest : ValidatedRequest<GetPostsForUserRequest, List<IBitCloutPost>>
    {
        public IBitCloutUser TargetUser { get; set; }
        public int FetchDepth { get; set; }
        public string StartFromPostHex { get; set; }

        protected override void SetupValidation(RequestValidator validator)
        {
            validator.RuleFor(req => req.TargetUser).NotNull().BitCloutUser();
        }
    }
}