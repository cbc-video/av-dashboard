namespace WireCastAsync
{
    public class ShotItem
    {
        public string name { get; set; }
        public int id { get; set; }
        public string statusName { get; set; }
        public void UpdateStatusName(bool live, bool preview, bool playlist)
        {
            string previewStr = preview ? "YES" : "NO";
            string liveStr = live ? "YES" : "NO";
            string playlistStr = playlist ? " PLAYLIST" : "";
            statusName = name + playlistStr + "\t\t(PREVIEW: " + previewStr + ", LIVE: " + liveStr + ")";
        }
    }
}

