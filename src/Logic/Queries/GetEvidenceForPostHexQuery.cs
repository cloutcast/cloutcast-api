using System.Linq;
using FluentValidation;

namespace CloutCast.Queries
{
    public class GetEvidenceForPostHexQuery : ValidatedDapperQuery<GetEvidenceForPostHexQuery, int>
    {
        public string PostHex { get; set; }

        public override void Build(IStatementBuilder builder) => builder
            .Param("PostHex", PostHex)
            .Add($"select count(*) from  {Tables.Evidence} where PostHex = @PostHex");

        public override int Read(IDapperGridReader reader) => reader.Read<int>().SingleOrDefault();

        protected override void SetupValidation(RequestValidator v) => v.RuleFor(q => q.PostHex).NotEmpty();
    }
}