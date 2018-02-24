using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cadena;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Data;
using Cadena.Engine.CyclicReceivers.Relations;
using JetBrains.Annotations;
using StarryEyes.Models.Databases;

namespace StarryEyes.Models.Receiving.Ex
{
    public sealed class ListMemberReceiver : CyclicRelationInfoReceiverBase
    {
        private readonly ListParameter _list;
        private readonly Action<IEnumerable<TwitterUser>> _usersHandler;
        private readonly IApiAccessor _accessor;
        private long? _listId;

        public ListMemberReceiver(IApiAccessor accessor, ListParameter parameter,
            [NotNull] Action<IEnumerable<TwitterUser>> usersHandler, [NotNull] Action<IEnumerable<long>> handler,
            [CanBeNull] Action<Exception> exceptionHandler) : base(handler,
            exceptionHandler)
        {
            _accessor = accessor;
            _list = parameter;
            _usersHandler = usersHandler;
            _listId = parameter.ListId;
        }

        protected override async Task<RateLimitDescription> Execute(CancellationToken token)
        {
            if (_listId == null)
            {
                // get description (not required to receive, but important operation)
                var list = await ReceiveListDescription(_accessor, _list).ConfigureAwait(false);
                await ListProxy.SetListDescription(list.Result).ConfigureAwait(false);
                _listId = list.Result.Id;
            }
            var result = await RetrieveCursoredResult(_accessor,
                    (a, i) => a.GetListMembersAsync(_list, i, token), CallExceptionHandler, token)
                .ConfigureAwait(false);
            _usersHandler(result.Result);
            CallHandler(result.Result.Select(u => u.Id));
            return result.RateLimit;
        }

        private async Task<IApiResult<TwitterList>> ReceiveListDescription(IApiAccessor accessor,
            ListParameter parameter)
        {
            return await accessor.ShowListAsync(parameter, CancellationToken.None);
        }
    }
}