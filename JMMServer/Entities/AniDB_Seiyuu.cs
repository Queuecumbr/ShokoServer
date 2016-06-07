﻿using System.IO;
using JMMContracts;
using JMMServer.ImageDownload;

namespace JMMServer.Entities
{
    public class AniDB_Seiyuu
    {
        public int AniDB_SeiyuuID { get; private set; }
        public int SeiyuuID { get; set; }
        public string SeiyuuName { get; set; }
        public string PicName { get; set; }

        public string PosterPath
        {
            get
            {
                if (string.IsNullOrEmpty(PicName)) return "";

                return Path.Combine(ImageUtils.GetAniDBCreatorImagePath(SeiyuuID), PicName);
            }
        }

        public Contract_AniDB_Seiyuu ToContract()
        {
            var contract = new Contract_AniDB_Seiyuu();

            contract.AniDB_SeiyuuID = AniDB_SeiyuuID;
            contract.SeiyuuID = SeiyuuID;
            contract.SeiyuuName = SeiyuuName;
            contract.PicName = PicName;

            return contract;
        }
    }
}