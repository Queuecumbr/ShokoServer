namespace Shoko.Server
{
    /// <summary>
    /// this is triggered when either an Ed2k or Progress change is detected
    /// </summary>
    public class AVDumpProgressEventArgs
    {
        /// <summary>
        /// Progress out of 100
        /// </summary>
        public decimal Progress { get; set; }
    }
}
