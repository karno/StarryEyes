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
        private readonly ListInfo _list;
        private readonly Action<IEnumerable<TwitterUser>> _usersHandler;
        private readonly IApiAccessor _accessor;

        public long? _listId;

        public ListMemberReceiver(IApiAccessor accessor, ListInfo listInfo,
            [NotNull] Action<IEnumerable<TwitterUser>> usersHandler,
            [NotNull] Action<IEnumerable<long>> handler,
            [CanBeNull] Action<Exception> exceptionHandler) : base(handler,
            exceptionHandler)
        {
            _accessor = accessor;
            _list = listInfo;
            _usersHandler = usersHandler;
        }

        public long? ListId => _listId;

        protected override async Task<RateLimitDescription> Execute(CancellationToken token)
        {
            long listId;
            if (_listId == null)
            {
                // get description (not required to receive, but important operation)
                var list = await ListProxy.GetOrReceiveListDescription(_accessor, _list);
                _listId = listId = list.Id;
            }
            else
            {
                listId = _listId.Value;
            }
            var idParam = new ListParameter(listId);
            var result = await RetrieveCursoredResult(_accessor,
                    (a, i) => a.GetListMembersAsync(idParam, i, token), CallExceptionHandler, token)
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