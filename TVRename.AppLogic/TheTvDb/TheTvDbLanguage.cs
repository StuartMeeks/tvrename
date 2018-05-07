namespace TVRename.AppLogic.TheTvDb
{
    public class TheTvDbLanguage
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; }
        public string Name { get; set; }
        public string EnglishName { get; set; }

        public TheTvDbLanguage(int id, string abbreviation, string name, string englishName)
        {
            Id = id;
            Abbreviation = abbreviation;
            Name = name;
            EnglishName = englishName;
        }
    }
}
