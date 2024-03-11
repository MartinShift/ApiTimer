namespace ApiTimer.DbModels
{
    public class SoundFile
    {
        public int Id { get; set; }
        public string Filename { get; set; }
        public string RootDirectory { get; set; }


        public string Url()
        {
            return "/" + Path();
        }

        public string Path()
        {
            return RootDirectory + "/" + Filename;
        }

    }
}
