namespace ILI9341Test1
{
    public abstract class Font
    {
        public abstract byte SpaceWidth { get; }
        public abstract FontCharacter GetFontData(char character);
    }
}
