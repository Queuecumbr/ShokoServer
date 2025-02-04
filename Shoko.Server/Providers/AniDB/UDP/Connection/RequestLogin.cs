using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shoko.Server.Providers.AniDB.UDP.Exceptions;
using Shoko.Server.Providers.AniDB.UDP.Generic;

namespace Shoko.Server.Providers.AniDB.UDP.Connection
{
    public class RequestLogin : UDPBaseRequest<ResponseLogin>
    {
        public string Username { get; set; }
        public string Password { get; set; }

        protected override string BaseCommand => $"AUTH user={Username}&pass={Password}&protover=3&client=ommserver&clientver=2&comp=1&imgserver=1&enc=utf-16";

        protected override UDPBaseResponse<ResponseLogin> ParseResponse(ILogger logger, UDPBaseResponse<string> response)
        {
            var code = response.Code;
            var receivedData = response.Response;
            int i = receivedData.IndexOf("LOGIN", StringComparison.Ordinal);
            if (i < 0) throw new UnexpectedUDPResponseException(code, receivedData);
            // after response code, before "LOGIN"
            string sessionID = receivedData.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(sessionID)) throw new UnexpectedUDPResponseException(code, receivedData);
            string imageServer = receivedData.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return new UDPBaseResponse<ResponseLogin>
            {
                Response = new ResponseLogin {SessionID = sessionID, ImageServer = imageServer}, Code = code
            };
        }

        protected override void PreExecute(string sessionID)
        {
            // Override to prevent attaching our non-existent sessionID
        }
        
        public override UDPBaseResponse<ResponseLogin> Execute(AniDBUDPConnectionHandler handler)
        {
            Command = BaseCommand;
            PreExecute(handler.SessionID);
            // LOGIN commands have special needs, so we want to handle this differently
            UDPBaseResponse<string> rawResponse = handler.CallAniDBUDPDirectly(Command, false, true, false, true);
            var factory = handler.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = factory.CreateLogger(GetType());
            var response = ParseResponse(logger, rawResponse);
            PostExecute(logger, handler.SessionID, response);
            return response;
        }
    }
}
