using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using Cadena.Api.Parameters;
using Cadena.Api.Rest;
using Cadena.Data;
using StarryEyes.Globalization.Filters;
using StarryEyes.Settings;

namespace StarryEyes.Filters.Sources
{
    public class FilterUser : FilterSourceBase
    {
        private readonly string _targetIdOrScreenName;

        public FilterUser()
        {
        }

        public FilterUser(string screenName)
        {
            _targetIdOrScreenName = screenName.Trim();
            if (!screenName.StartsWith("#")) return;
            // if specified by ID, check variable is numerics
            long result;
            if (!Int64.TryParse(screenName.Substring(1), out result))
            {
                throw new ArgumentException(FilterObjectResources.FilterUserInvalidArgument);
            }
        }

        public override string FilterKey => "user";

        public override string FilterValue => _targetIdOrScreenName;

        public override Func<TwitterStatus, bool> GetEvaluator()
        {
            return _targetIdOrScreenName.StartsWith("#")
                ? (Func<TwitterStatus, bool>)(s => s.User.Id == Int64.Parse(_targetIdOrScreenName.Substring(1)))
                : (s => s.User.ScreenName.Equals(_targetIdOrScreenName,
                    StringComparison.CurrentCultureIgnoreCase));
        }

        public override string GetSqlQuery()
        {
            if (_targetIdOrScreenName.StartsWith("#"))
            {
                // extract by id
                return "BaseUserId = " + _targetIdOrScreenName.Substring(1);
            }
            // extract by screen name
            return "EXISTS (select ScreenName from User where Id = status.BaseUserId AND " +
                   "LOWER(ScreenName) = '" + _targetIdOrScreenName.ToLower() + "' limit 1)";
        }

        protected override IObservable<TwitterStatus> ReceiveSink(long? maxId)
        {
            var parameter = _targetIdOrScreenName.StartsWith("#")
                ? new UserParameter(Int64.Parse(_targetIdOrScreenName.Substring(1)))
                : new UserParameter(_targetIdOrScreenName);

            return Observable.Start(() => Setting.Accounts.GetRandomOne())
                             .Where(a => a != null)
                             .SelectMany(a => a.CreateAccessor().GetUserTimelineAsync(
                                                   parameter, null, null, null, null, null, CancellationToken.None)
                                               .ToObservable())
                             .SelectMany(o => o.Result);
        }
    }
}