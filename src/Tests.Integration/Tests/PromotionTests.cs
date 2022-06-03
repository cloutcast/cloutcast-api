using System.Threading.Tasks;
using Alba;
using Xunit;

namespace CloutCast.Tests
{
    using Entities;
    using Models;
    using Requests;

    [Collection("CloutCast Fixture")]
    public class PromotionTests 
    {
        protected readonly WebAppFixture App;
        private AppSource _appSource;
        private BitCloutUser _awesomeDev;
        public PromotionTests(WebAppFixture app)
        {
            App = app;
            app.Database
                .UseConnection(c =>
                {
                    _awesomeDev = c
                        .CleanUp()
                        .AddUser("smartalec", "BC1YLg4sz2dEa81Zhw3ktsrEMKRz5J9ucxAczoZrM3zZRPgMoCGJM7r")
                        .AddUser("JasonDevlin", "BC1YLgy8GzQq19AL87UNQRCxpqTtWnKK7SMVECvbEtYoG54x9nPVk7v")
                        .AddAndGetUser("awesome_dev", "BC1YLiRDwmSUdGKgf2Mo7tux31skoi378MckGRGVguzfZWQpcV3eKcp");

                    _appSource = c.App("CloutCast");
                });
        }

        [Fact]
        public async Task Should_Get_EngagementCount_For_Promotion()
        {
            //ASSIGN
            var initialBalance = App.Mediator<AddFundsToUserRequest, AccountBalanceModel>(r => r.Funding(_appSource, 400,_awesomeDev.Id));
            var createModel = new CreatePromotionModel
            {
                Criteria = new PromotionCriteriaModel {MinFollowerCount = 10},
                Header = new PromotionHeaderModel
                {
                    BitCloutToUsdRate = 168.89m,
                    Engagements = 7,
                    Duration = 10,
                    Rate = 50
                },
                Target = new PromotionTargetModel
                {
                    Action = PromotionActivity.Quote,
                    Hex = "5610c5c780d0c71fca26c9c74d20b9406b78333845e57683d16fe90cd248f544"
                }
            };

            //ACT
            var response = await App
                .SetActiveUser(_awesomeDev)
                .SystemUnderTest.Scenario(_ =>
                {
                    _.Post
                        .Json(createModel)
                        .ToUrl("/promotion/create");
                    _.StatusCodeShouldBeOk();
                });

            //ASSERT
            var result = response.ResponseBody.ReadAsJson<ResultWrapper<Promotion>>();
            Assert.NotNull(result?.Data);
            var promotion = result.Data;
            Assert.True(promotion.Id > 0);
            Assert.Equal(_awesomeDev.Id, promotion.Client.Id);
            Assert.Equal(7, promotion.Header.Engagements);
        }

        [Fact]
        public async Task Should_Get_Balance_For_Promotion()
        {
            //ASSIGN
            var initialBalance = App.Mediator<AddFundsToUserRequest, AccountBalanceModel>(r => r.Funding(_appSource, 104,_awesomeDev.Id));
            var createModel = new CreatePromotionModel
            {
                Criteria = new PromotionCriteriaModel {MinFollowerCount = 10},
                Header = new PromotionHeaderModel
                {
                    BitCloutToUsdRate = 168.89m,
                    Engagements = 2,
                    Duration = 10,
                    Rate = 50
                },
                Target = new PromotionTargetModel
                {
                    Action = PromotionActivity.Quote,
                    Hex = "5610c5c780d0c71fca26c9c74d20b9406b78333845e57683d16fe90cd248f544"
                }
            };

            //ACT
            var response = await App
                .SetActiveUser(_awesomeDev)
                .SystemUnderTest.Scenario(_ =>
                {
                    _.Post
                        .Json(createModel)
                        .ToUrl("/promotion/create");
                    _.StatusCodeShouldBeOk();
                });

            //ASSERT
            Assert.NotNull(initialBalance);
            Assert.Equal(104, initialBalance.Settled);

            var result = response.ResponseBody.ReadAsJson<ResultWrapper<Promotion>>();
            Assert.NotNull(result?.Data);
            var promotion = result.Data;
            Assert.True(promotion.Id > 0);
            Assert.Equal(_awesomeDev.Id, promotion.Client.Id);
        }
    }
}
