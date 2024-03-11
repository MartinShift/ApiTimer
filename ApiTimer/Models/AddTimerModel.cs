namespace ApiTimer.Models
{
    public class AddTimerModel
    {
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public string Title { get; set; }
        public DateTime ? Date { get;set; }
    }
}
