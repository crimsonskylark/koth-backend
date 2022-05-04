namespace Server.Models
{
    internal class BanStatus
    {
        internal sbyte Banned { get; set; } = 0;
        internal long BanDuration { get; set; } = 0;

        public BanStatus(sbyte Banned, long BanDuration)
        {
            this.Banned = Banned;
            this.BanDuration = BanDuration;
        }
    }
}
