using System;
using Microsoft.Extensions.Logging;
using Shoko.Server.Providers.AniDB.UDP.Exceptions;
using Shoko.Server.Providers.AniDB.UDP.Generic;

namespace Shoko.Server.Providers.AniDB.UDP.User
{
    public class RequestGetEpisode : UDPBaseRequest<ResponseMyListFile>
    {
        protected override string BaseCommand
        {
            get
            {
                return $"MYLIST aid={AnimeID}&epno={EpisodeNumber}";
            }
        }

        public int AnimeID { get; set; }

        public int EpisodeNumber { get; set; }

        protected override UDPBaseResponse<ResponseMyListFile> ParseResponse(ILogger logger, UDPBaseResponse<string> response)
        {
            var code = response.Code;
            var receivedData = response.Response;
            switch (code)
            {
                case UDPReturnCode.NO_SUCH_ENTRY:
                    return new UDPBaseResponse<ResponseMyListFile>
                    {
                        Code = code,
                        Response = null,
                    };
                case UDPReturnCode.MYLIST:
                {
                    /* Response Format
                     * {int4 lid}|{int4 fid}|{int4 eid}|{int4 aid}|{int4 gid}|{int4 date}|{int2 state}|{int4 viewdate}|{str storage}|{str source}|{str other}|{int2 filestate}
                     */
                    //file already exists: read 'watched' status
                    string[] arrResult = receivedData.Split('\n');
                    if (arrResult.Length >= 2)
                    {
                        string[] arrStatus = arrResult[1].Split('|');
                        // We expect 0 for a MyListID
                        int.TryParse(arrStatus[0], out int myListID);

                        GetFile_State state = (GetFile_State) int.Parse(arrStatus[6]);

                        int viewdate = int.Parse(arrStatus[7]);
                        int updatedate = int.Parse(arrStatus[5]);
                        bool watched = viewdate > 0;
                        DateTime? updatedAt = null;
                        DateTime? watchedDate = null;
                        if (updatedate > 0)
                            updatedAt = DateTime.UnixEpoch
                            .AddSeconds(updatedate)
                            .ToLocalTime();
                        if (watched)
                            watchedDate = DateTime.UnixEpoch
                                .AddSeconds(viewdate)
                                .ToLocalTime();

                        return new UDPBaseResponse<ResponseMyListFile>
                        {
                            Code = code,
                            Response = new ResponseMyListFile
                            {
                                MyListID = myListID,
                                State = state,
                                IsWatched = watched,
                                WatchedDate = watchedDate,
                                UpdatedAt = updatedAt,
                            },
                        };
                    }
                    break;
                }
            }
            throw new UnexpectedUDPResponseException(code, receivedData);
        }
    }
}
