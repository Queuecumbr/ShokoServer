﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Xml;
using JMMServer.Entities;
using JMMServer.Properties;
using JMMServer.Repositories;

namespace JMMServer.Commands
{
    [Serializable]
    public class CommandRequest_GetUpdated : CommandRequestImplementation, ICommandRequest
    {
        public CommandRequest_GetUpdated()
        {
        }

        public CommandRequest_GetUpdated(bool forced)
        {
            ForceRefresh = forced;
            CommandType = (int)CommandRequestType.AniDB_GetUpdated;
            Priority = (int)DefaultPriority;

            GenerateCommandID();
        }

        public bool ForceRefresh { get; set; }

        public CommandRequestPriority DefaultPriority
        {
            get { return CommandRequestPriority.Priority4; }
        }

        public string PrettyDescription
        {
            get
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(ServerSettings.Culture);

                return string.Format(Resources.Command_GetUpdatedAnime);
            }
        }

        public override void ProcessCommand()
        {
            logger.Info("Processing CommandRequest_GetUpdated");

            try
            {
                var animeIDsToUpdate = new List<int>();
                var repSched = new ScheduledUpdateRepository();
                var repSeries = new AnimeSeriesRepository();
                var repAnime = new AniDB_AnimeRepository();

                // check the automated update table to see when the last time we ran this command
                var sched = repSched.GetByUpdateType((int)ScheduledUpdateType.AniDBUpdates);
                if (sched != null)
                {
                    var freqHours = Utils.GetScheduledHours(ServerSettings.AniDB_Anime_UpdateFrequency);

                    // if we have run this in the last 12 hours and are not forcing it, then exit
                    var tsLastRun = DateTime.Now - sched.LastUpdate;
                    if (tsLastRun.TotalHours < freqHours)
                    {
                        if (!ForceRefresh) return;
                    }
                }


                long webUpdateTime = 0;
                long webUpdateTimeNew = 0;
                if (sched == null)
                {
                    // if this is the first time, lets ask for last 3 days
                    var localTime = DateTime.Now.AddDays(-3);
                    var utcTime = localTime.ToUniversalTime();
                    webUpdateTime = long.Parse(Utils.AniDBDate(utcTime));
                    webUpdateTimeNew = long.Parse(Utils.AniDBDate(DateTime.Now.ToUniversalTime()));

                    sched = new ScheduledUpdate();
                    sched.UpdateType = (int)ScheduledUpdateType.AniDBUpdates;
                }
                else
                {
                    logger.Trace("Last anidb info update was : {0}", sched.UpdateDetails);
                    webUpdateTime = long.Parse(sched.UpdateDetails);
                    webUpdateTimeNew = long.Parse(Utils.AniDBDate(DateTime.Now.ToUniversalTime()));

                    var timeNow = DateTime.Now.ToUniversalTime();
                    logger.Info(string.Format("{0} since last UPDATED command",
                        Utils.FormatSecondsToDisplayTime(int.Parse((webUpdateTimeNew - webUpdateTime).ToString()))));
                }

                // get a list of updates from AniDB
                // startTime will contain the date/time from which the updates apply to
                JMMService.AnidbProcessor.GetUpdated(ref animeIDsToUpdate, ref webUpdateTime);

                // now save the update time from AniDB
                // we will use this next time as a starting point when querying the web cache
                sched.LastUpdate = DateTime.Now;
                sched.UpdateDetails = webUpdateTimeNew.ToString();
                repSched.Save(sched);

                if (animeIDsToUpdate.Count == 0)
                {
                    logger.Info("No anime to be updated");
                    return;
                }


                var countAnime = 0;
                var countSeries = 0;
                foreach (var animeID in animeIDsToUpdate)
                {
                    // update the anime from HTTP
                    var anime = repAnime.GetByAnimeID(animeID);
                    if (anime == null)
                    {
                        logger.Trace("No local record found for Anime ID: {0}, so skipping...", animeID);
                        continue;
                    }

                    logger.Info("Updating CommandRequest_GetUpdated: {0} ", animeID);

                    // but only if it hasn't been recently updated
                    var ts = DateTime.Now - anime.DateTimeUpdated;
                    if (ts.TotalHours > 4)
                    {
                        var cmdAnime = new CommandRequest_GetAnimeHTTP(animeID, true, false);
                        cmdAnime.Save();
                        countAnime++;
                    }

                    // update the group status
                    // this will allow us to determine which anime has missing episodes
                    // so we wonly get by an amime where we also have an associated series
                    var ser = repSeries.GetByAnimeID(animeID);
                    if (ser != null)
                    {
                        var cmdStatus = new CommandRequest_GetReleaseGroupStatus(animeID, true);
                        cmdStatus.Save();
                        countSeries++;
                    }
                }

                logger.Info("Updating {0} anime records, and {1} group status records", countAnime, countSeries);
            }
            catch (Exception ex)
            {
                logger.Error("Error processing CommandRequest_GetUpdated: {0}", ex.ToString());
            }
        }

        public override bool LoadFromDBCommand(CommandRequest cq)
        {
            CommandID = cq.CommandID;
            CommandRequestID = cq.CommandRequestID;
            CommandType = cq.CommandType;
            Priority = cq.Priority;
            CommandDetails = cq.CommandDetails;
            DateTimeUpdated = cq.DateTimeUpdated;

            // read xml to get parameters
            if (CommandDetails.Trim().Length > 0)
            {
                var docCreator = new XmlDocument();
                docCreator.LoadXml(CommandDetails);

                // populate the fields
                ForceRefresh = bool.Parse(TryGetProperty(docCreator, "CommandRequest_GetUpdated", "ForceRefresh"));
            }

            return true;
        }

        public override void GenerateCommandID()
        {
            CommandID = "CommandRequest_GetUpdated";
        }

        public override CommandRequest ToDatabaseObject()
        {
            GenerateCommandID();

            var cq = new CommandRequest();
            cq.CommandID = CommandID;
            cq.CommandType = CommandType;
            cq.Priority = Priority;
            cq.CommandDetails = ToXML();
            cq.DateTimeUpdated = DateTime.Now;

            return cq;
        }
    }
}