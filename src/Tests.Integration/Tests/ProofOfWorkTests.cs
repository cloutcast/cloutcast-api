using System;
using System.Threading.Tasks;
using Alba;
using Xunit;

namespace CloutCast.Tests
{
    using Entities;
    using Models;
    using Requests;

    [Collection("CloutCast Fixture")]
    public class ProofOfWorkTests 
    {
        private readonly WebAppFixture _app;
        private AppSource _appSource;
        private BitCloutUser _awesomeDev;
        private BitCloutUser _smartAlec;
        private BitCloutUser _jason;

        public ProofOfWorkTests(WebAppFixture app) 
        {
            _app = app;
            app.Database.UseConnection(c =>
            {
                c.CleanUp();

                _awesomeDev = c.AddAndGetUser("awesome_dev", "BC1YLiRDwmSUdGKgf2Mo7tux31skoi378MckGRGVguzfZWQpcV3eKcp");
                _smartAlec = c.AddAndGetUser("smartalec", "BC1YLg4sz2dEa81Zhw3ktsrEMKRz5J9ucxAczoZrM3zZRPgMoCGJM7r");
                _jason = c.AddAndGetUser("JasonDevlin", "BC1YLgy8GzQq19AL87UNQRCxpqTtWnKK7SMVECvbEtYoG54x9nPVk7v");
                _appSource = c.App("CloutCast");
            });
        }

        private AccountBalanceModel FundUser(long userId, long amount) =>
            _app.Mediator<AddFundsToUserRequest, AccountBalanceModel>(r =>r.Funding(_appSource, amount,_awesomeDev.Id));

        private async Task<Promotion> Create(BitCloutUser client, Action<CreatePromotionModel> setup=null)
        {
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
            setup?.Invoke(createModel);

            var response = await _app.SetActiveUser(client).SystemUnderTest.Scenario(_ =>
            {
                _.Post
                    .Json(createModel)
                    .ToUrl("/promotion/create");
                _.StatusCodeShouldBeOk();
            });

            var result = response.ResponseBody.ReadAsJson<ResultWrapper<Promotion>>();
            return result.Data;
        }

        [Fact]
        public async Task POW_Should_Close_Promotion_When_Out_Of_Funds()
        {
            //ASSIGN
            var initialBalance = FundUser(_awesomeDev.Id, 104);
            var promotion = await Create(_awesomeDev);

            //ACT
            var response1 = await _app.SetActiveUser(_smartAlec).SystemUnderTest.Scenario(_ =>
            {
                _.Post.Url($"/promotion/pow/{promotion.Id}/8bca72d99d5fd6b3bd8a2fc5c1b9bc5581613816f8de3cc9b5b8836ac54695d4");
                _.StatusCodeShouldBeOk();
            });
            var promoBal1 = response1.ResponseBody.ReadAsJson<ResultWrapper<AccountBalanceModel>>();

            var response2 = await _app.SetActiveUser(_jason).SystemUnderTest.Scenario(_ =>
            {
                _.Post.Url($"/promotion/pow/{promotion.Id}/889c90799e6f1bdfbc936b856c2b530f915e9d9e231ef22d2219e8961ad263dc");
                _.StatusCodeShouldBeOk();
            });
            var promoBal2 = response2.ResponseBody.ReadAsJson<ResultWrapper<AccountBalanceModel>>();

            var userBalResp = await _app.SystemUnderTest.Scenario(_ =>
            {
                _.Get.Url("/user/balance/4");
                _.StatusCodeShouldBeOk();
            });
            var jasonBalance = userBalResp.ResponseBody.ReadAsJson<ResultWrapper<AccountBalanceModel>>();

            //ASSERT
            Assert.NotNull(jasonBalance);

            Assert.NotNull(initialBalance);
            Assert.Equal(104, initialBalance.Settled);

            Assert.NotNull(promoBal1?.Data);
            Assert.Equal(52, promoBal1.Data.Settled);
            Assert.NotNull(promoBal2?.Data);
            Assert.Equal(0, promoBal2.Data.Settled);
            Assert.Equal(104, promoBal2.Data.UnSettled);

            Assert.True(promotion.Id > 0);
            Assert.Equal(_awesomeDev.Id, promotion.Client.Id);
        }

        [Fact]
        public async Task POW_Should_Pay_Two_Promoters()
        {
            //ASSIGN
            var initialBalance = FundUser(_awesomeDev.Id, 400);
            var promotion = await Create(_awesomeDev, c => c.Header.Engagements = 3);

            //ACT
            var response1 = await _app.SetActiveUser(_smartAlec).SystemUnderTest.Scenario(_ =>
            {
                _.Post.Url($"/pow/{promotion.Id}");
                _.StatusCodeShouldBeOk();
            });
            var promoBal1 = response1.ResponseBody.ReadAsJson<ResultWrapper<AccountBalanceModel>>();

            var response2 = await _app.SetActiveUser(_jason).SystemUnderTest.Scenario(_ =>
            {
                _.Post.Url($"/pow/{promotion.Id}");
                _.StatusCodeShouldBeOk();
            });
            var promoBal2 = response2.ResponseBody.ReadAsJson<ResultWrapper<AccountBalanceModel>>();

            var clientBalResp = await _app.SystemUnderTest.Scenario(_ =>
            {
                _.Get.Url($"/user/balance/{_awesomeDev.Id}/{DateTimeOffset.UtcNow.AddHours(1):yyyy-MM-ddTHH:mm:ss.fffZ}");
            });
            var awesomeDevBal = clientBalResp.ResponseBody.ReadAsJson<ResultWrapper<AccountBalanceModel>>();

            //ASSERT
            Assert.NotNull(initialBalance);
            Assert.Equal(400, initialBalance.Settled);

            Assert.NotNull(awesomeDevBal?.Data);
            Assert.Equal(296, awesomeDevBal.Data.Settled);
            Assert.Equal(0, awesomeDevBal.Data.UnSettled);
            
            Assert.NotNull(promoBal1?.Data);
            Assert.Equal(104, promoBal1.Data.Settled);
            Assert.NotNull(promoBal2?.Data);
            Assert.Equal(52, promoBal2.Data.Settled);
            Assert.Equal(104, promoBal2.Data.UnSettled);

            Assert.True(promotion.Id > 0);
            Assert.Equal(_awesomeDev.Id, promotion.Client.Id);
        }
    }
}