using System;

namespace CloutCast.Models
{
    using Contracts;

    public class EntityLogModel<U> : IEntityLog where U : IBitCloutUser, new()
    {
        protected EntityLogModel() {}
        
        protected EntityLogModel(IEntityLog source)
        {
            Action = source.Action;
            TimeStamp = source.TimeStamp;
            User = source.User is U user 
                ? user 
                : (U) new U().CopyFrom(source.User);
        }

        public EntityAction Action { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public U User { get; set; }

        IBitCloutUser IEntityLog.User => User;
    }
}