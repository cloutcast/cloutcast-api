using System;
using System.Threading.Tasks;
using MediatR;

namespace CloutCast
{
    public static class MediatorExtensions
    {
        public static Task Send<R>(this IMediator mediator, Action<R> setupAction = null) where R : IRequest, new()
        {
            var request = new R();
            setupAction?.Invoke(request);
            return mediator.Send(request);
        }

        public static Task<V> Send<R, V>(this IMediator mediator, Action<R> setupAction = null) where R : IRequest<V>, new()
        {
            var request = new R();
            setupAction?.Invoke(request);
            return mediator.Send(request);
        }
    }
}