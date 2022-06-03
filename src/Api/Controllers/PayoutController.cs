using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace SkunkWerx.CloutCast.Controllers
{
    using System.Data;
    using Entities;
    using PureLogicTek;
    using Queries;

    [ApiController, Route("[controller]")]
    public class PayoutController : ControllerBase
    {
        private readonly IDapperPipeline _pipeline;
        public PayoutController(IDapperPipeline pipeline) => _pipeline = pipeline;

        [HttpGet, Route("pending")]
        public List<PayoutItem> Pending()
        {
            List<PayoutItem> results = null;
            _pipeline
                .Query<IGetPayoutListQuery, List<PayoutItem>>(
                    q => {},
                    r => results = r)
                .UseIsolationLevel(IsolationLevel.Snapshot)
                .Run();

            return results;
        }
    }
}