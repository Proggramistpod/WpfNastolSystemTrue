using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WpfNastolSystem.Style
{
    public class WindowsFontResolver : IFontResolver
    {
        public string DefaultFontName => "Arial";

        public byte[] GetFont(string faceName)
        {
            // Определяем имя файла по запрошенному лицу
            string fontFile = faceName.ToLower() switch
            {
                "arial" => "arial.ttf",
                "arial#regular" => "arial.ttf",
                "arial#bold" => "arialbd.ttf",
                "arial#italic" => "ariali.ttf",
                "arial#bolditalic" => "arialbi.ttf",
                "verdana" => "verdana.ttf",
                "verdana#regular" => "verdana.ttf",
                "verdana#bold" => "verdanab.ttf",
                "verdana#italic" => "verdanai.ttf",
                "verdana#bolditalic" => "verdanaz.ttf",
                "microsoft sans serif" => "micross.ttf",
                "microsoft sans serif#regular" => "micross.ttf",
                _ => null
            };

            if (fontFile != null)
            {
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontFile);
                if (File.Exists(fontPath))
                    return File.ReadAllBytes(fontPath);
            }

            return null;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            string name = familyName;

            if (isBold && isItalic)
                name += "#bolditalic";
            else if (isBold)
                name += "#bold";
            else if (isItalic)
                name += "#italic";
            else
                name += "#regular";

            return new FontResolverInfo(name, isBold, isItalic);
        }
    }
}
