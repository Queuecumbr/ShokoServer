﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using JMMContracts;
using KodiMediaInfo;
using NLog;

namespace JMMFileHelper
{
    public class MediaInfoReader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static string XmlSerializeToString<T>(T objectInstance)
        {
            var serializer = new XmlSerializer(typeof(T));
            var sb = new StringBuilder();
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, objectInstance, ns);
            }

            return sb.ToString();
        }

        public static bool ReadMediaInfo(string fileNameFull, bool forceRefresh, ref MediaInfoResult info)
        {
            return ReadMediaInfo(fileNameFull, forceRefresh, ref info, false);
        }

        public static bool ReadMediaInfo(string fileNameFull, bool forceRefresh, ref MediaInfoResult info, bool useKodi)
        {
            try
            {
                if (!forceRefresh)
                {
                    // if we have populated the full info, we have already read the data
                    if (!string.IsNullOrEmpty(info.FullInfo)) return false;
                }
                if (useKodi)
                {
                    var m = MediaConvert.Convert(fileNameFull);
                    if (m != null)
                    {
                        var xml = XmlSerializeToString(m);
                        if (!string.IsNullOrEmpty(m.Width) && !string.IsNullOrEmpty(m.Height))
                            info.VideoResolution = m.Width + "x" + m.Height;
                        if (!string.IsNullOrEmpty(m.VideoCodec))
                            info.VideoCodec = m.VideoCodec;
                        if (!string.IsNullOrEmpty(m.AudioCodec))
                            info.AudioCodec = m.AudioCodec;
                        if (!string.IsNullOrEmpty(m.Duration))
                            info.Duration = int.Parse(m.Duration);
                        var vparts = m.Parts[0].Streams.Where(a => a.StreamType == "1").ToList();
                        if (vparts.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(vparts[0].Bitrate))
                                info.VideoBitrate = vparts[0].Bitrate;
                            if (!string.IsNullOrEmpty(vparts[0].BitDepth))
                                info.VideoBitDepth = vparts[0].BitDepth;
                            if (!string.IsNullOrEmpty(vparts[0].FrameRate))
                                info.VideoFrameRate = vparts[0].FrameRate;
                        }
                        var aparts = m.Parts[0].Streams.Where(a => a.StreamType == "2").ToList();
                        if (aparts.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(aparts[0].Bitrate))
                                info.AudioBitrate = aparts[0].Bitrate;
                        }
                        info.FullInfo = xml;
                    }
                    else
                    {
                        logger.Error("ERROR getting kodi media info:: {0}", fileNameFull);
                    }
                }
                else
                {
                    var m = PlexMediaInfo.MediaConvert.Convert(fileNameFull);
                    if (m != null)
                    {
                        var xml = XmlSerializeToString(m);
                        if (!string.IsNullOrEmpty(m.Width) && !string.IsNullOrEmpty(m.Height))
                            info.VideoResolution = m.Width + "x" + m.Height;
                        if (!string.IsNullOrEmpty(m.VideoCodec))
                            info.VideoCodec = m.VideoCodec;
                        if (!string.IsNullOrEmpty(m.AudioCodec))
                            info.AudioCodec = m.AudioCodec;
                        if (!string.IsNullOrEmpty(m.Duration))
                            info.Duration = int.Parse(m.Duration);
                        var vparts = m.Parts[0].Streams.Where(a => a.StreamType == "1").ToList();
                        if (vparts.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(vparts[0].Bitrate))
                                info.VideoBitrate = vparts[0].Bitrate;
                            if (!string.IsNullOrEmpty(vparts[0].BitDepth))
                                info.VideoBitDepth = vparts[0].BitDepth;
                            if (!string.IsNullOrEmpty(vparts[0].FrameRate))
                                info.VideoFrameRate = vparts[0].FrameRate;
                        }
                        var aparts = m.Parts[0].Streams.Where(a => a.StreamType == "2").ToList();
                        if (aparts.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(aparts[0].Bitrate))
                                info.AudioBitrate = aparts[0].Bitrate;
                        }
                        info.FullInfo = xml;
                    }
                    else
                    {
                        logger.Error("ERROR getting plex media info:: {0}", fileNameFull);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error reading Media Info for: {0} --- {1}", fileNameFull, ex.ToString());
            }

            return true;
        }
    }
}