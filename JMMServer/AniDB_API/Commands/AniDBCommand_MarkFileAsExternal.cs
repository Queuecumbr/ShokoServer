﻿using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AniDBAPI.Commands
{
    public class AniDBCommand_MarkFileAsExternal : AniDBUDPCommand, IAniDBUDPCommand
    {
        public string Hash = "";
        public bool ReturnIsWatched = false;

        public AniDBCommand_MarkFileAsExternal()
        {
            commandType = enAniDBCommandType.MarkFileExternal;
        }

        public string GetKey()
        {
            return "AniDBCommand_MarkFileAsExternal" + Hash;
        }

        public virtual enHelperActivityType GetStartEventType()
        {
            return enHelperActivityType.MarkingFileExternal;
        }

        public virtual enHelperActivityType Process(ref Socket soUDP,
            ref IPEndPoint remoteIpEndPoint, string sessionID, Encoding enc)
        {
            ProcessCommand(ref soUDP, ref remoteIpEndPoint, sessionID, enc);

            // handle 555 BANNED and 598 - UNKNOWN COMMAND
            if (ResponseCode == 598) return enHelperActivityType.UnknownCommand_598;
            if (ResponseCode == 555) return enHelperActivityType.Banned_555;

            if (errorOccurred) return enHelperActivityType.NoSuchFile;

            var sMsgType = socketResponse.Substring(0, 3);
            switch (sMsgType)
            {
                case "210":
                    return enHelperActivityType.FileMarkedAsDeleted;
                case "310":
                    return enHelperActivityType.FileMarkedAsDeleted;
                case "311":
                    return enHelperActivityType.FileMarkedAsDeleted;
                case "320":
                    return enHelperActivityType.NoSuchFile;
                case "411":
                    return enHelperActivityType.NoSuchFile;

                case "502":
                    return enHelperActivityType.LoginFailed;
                case "501":
                    {
                        return enHelperActivityType.LoginRequired;
                    }
            }

            return enHelperActivityType.FileDoesNotExist;
        }

        public void Init(string hash, long fileSize)
        {
            Hash = hash;
            commandID = "MarkingFileExternal File: " + hash;

            commandText = "MYLISTADD size=" + fileSize;
            commandText += "&ed2k=" + hash;
            commandText += "&state=" + (int)AniDBFileStatus.DVD;
            commandText += "&edit=1";
        }
    }
}