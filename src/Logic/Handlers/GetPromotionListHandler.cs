using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using log4net;
using MediatR;

namespace CloutCast.Handlers
{
    using Contracts;
    using Entities;
    using Models;
    using Queries;
    using Requests;

    [JetBrains.Annotations.UsedImplicitly]
    public class GetPromotionListHandler : RequestHandler<GetPromotionListRequest, List<Promotion>>
    {
        private readonly IDapperPipeline _pipeline;
        private readonly ILog _log;

        public GetPromotionListHandler(IDapperPipeline pipeline, ILog log)
        {
            _pipeline = pipeline;
            _log = log;
        }

        protected override List<Promotion> Handle(GetPromotionListRequest request)
        {
            var promoter = request.ActiveUser();
            var matches = RunQuery(promoter, request.Active());

            return matches
                .Iterate(p => p.Context = Filter(p, promoter, request.CoinPrice(), request.FollowerCount()))
                .ToList();
        }

        protected static PromotionContextModel Filter(Promotion source, IBitCloutUser promoter, long coinPrice, long followerCount)
        {
            var context = new PromotionContextModel
            {
                ActiveUser = promoter,
                IsActive = source.Events.IsActiveOn(DateTimeOffset.UtcNow)
            };
            if (promoter == null) return context;

            context.IsUserAllowed = true;

            var criteria = source.Criteria;
            var clientRatio = source.Client.Profile.PromoterRatio();
            var promotionsDone = criteria.PromoterForClientPercentage;

            List<string> rejections = null;
            void DoCheck(bool canTest, Func<PromotionCriteriaModel, bool> test, string msg, Action onSuccess = null)
            {
                if (!canTest) return;
                rejections ??= new List<string>();
                if (test(criteria))
                    onSuccess?.Invoke();
                else
                    rejections.Add(msg);
            }
            
            DoCheck(criteria.MinCoinPrice > 0, c => coinPrice >= c.MinCoinPrice, "Coin price falls below promotion minimum");
            DoCheck(criteria.MinFollowerCount > 0, c => followerCount >= c.MinFollowerCount, "Follower count falls below promotion minimum");
            DoCheck(clientRatio > 0 && promotionsDone > 0, c => clientRatio > promotionsDone, "Client ratio exceeded");
            
            if (rejections != null && rejections.None()) return context; //at least 1 check was done and did not fail

            DoCheck(
                criteria.HasAllowedUsers(),
                c => c.AllowedUsers.Any(promoter.PublicKey.Equals),
                "Not a member of the promotion's allowed users",
                () => context.SpecifiedByName = true);

            context.IsUserAllowed = rejections?.None() ?? false;
            context.RejectionReasons = rejections;
            return context;
        }

        protected List<Promotion> RunQuery(IBitCloutUser promoter, ActiveFlag flag)
        {
            List<Promotion> results = null;

            _pipeline
                .Query<IGetMatchingPromotionsByQuery, List<Promotion>>(
                    q => _log.Info(q.Active(flag).ForPromoter(promoter)),
                    r => results = r?.ToList(),
                    () => _log.Info("No Promotions found"))
                .UseIsolationLevel(IsolationLevel.ReadCommitted)
                .Run();

            return results;
        }
    }
}