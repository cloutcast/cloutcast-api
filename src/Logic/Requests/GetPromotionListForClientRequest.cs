using System.Collections.Generic;
using FluentValidation;
using MediatR;

namespace CloutCast.Requests
{
    using Entities;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetPromotionListForClientRequest : ValidatedRequest<GetPromotionListForClientRequest,List<Promotion>>
    {
        private ActiveFlag _flag = ActiveFlag.Both;
        public ActiveFlag Active() => _flag;
        public GetPromotionListForClientRequest Active(ActiveFlag? flag) => this.Fluent(x => _flag = flag ?? ActiveFlag.Both);

        private string _clientKey;
        public string ClientKey() => _clientKey;
        public GetPromotionListForClientRequest ClientKey(string clientKey) => this.Fluent(x => _clientKey = clientKey);
        
        protected override void SetupValidation(RequestValidator validator)
        {
            validator.RuleFor(request => request._clientKey).MaximumLength(58);
        }
    }
}