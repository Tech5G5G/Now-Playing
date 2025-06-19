using System.Windows.Markup;

namespace Now_Playing.Markup
{
    public class FontIcon : MarkupExtension
    {
        public string Glyph { get; set; }

        public double FontSize { get; set; } = 12;

        public FontFamily FontFamily { get; set; } = SystemFonts.MessageFontFamily;

        public FontWeight FontWeight { get; set; } = FontWeights.Normal;

        public FontStyle FontStyle { get; set; } = FontStyles.Normal;

        public override object ProvideValue(IServiceProvider serviceProvider) => new TextBlock
        {
            Text = Glyph,
            FontSize = FontSize,
            FontFamily = FontFamily,
            FontWeight = FontWeight,
            FontStyle = FontStyle
        };
    }
}
