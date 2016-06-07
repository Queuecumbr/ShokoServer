﻿using System.Xml;

namespace AniDBAPI
{
    public class Raw_AniDB_Recommendation
    {
        public Raw_AniDB_Recommendation()
        {
            InitFields();
        }

        public int AnimeID { get; set; }
        public int UserID { get; set; }
        public string RecommendationTypeText { get; set; }
        //public int RecommendationType { get; set; }
        public string RecommendationText { get; set; }

        private void InitFields()
        {
            AnimeID = 0;
            UserID = 0;
            //RecommendationType = 0;

            RecommendationTypeText = string.Empty;
            RecommendationText = string.Empty;
        }

        public void ProcessFromHTTPResult(XmlNode node, int anid)
        {
            InitFields();

            AnimeID = anid;

            RecommendationTypeText = AniDBHTTPHelper.TryGetAttribute(node, "type");
            RecommendationText = node.InnerText.Trim();

            var uid = 0;
            int.TryParse(AniDBHTTPHelper.TryGetAttribute(node, "uid"), out uid);
            UserID = uid;
        }
    }
}