namespace YapiLabCadTools.UI
{
    /// <summary>
    /// Every user-facing string of the application, in Turkish, in one place.
    /// (If the product ever needs another language, this class becomes a .resx.)
    /// </summary>
    public static class Texts
    {
        // Window
        public const string WindowTitle = "YapıLab CAD Tools — Koordinattan Çizim";

        // Toolbar
        public const string OpenFile = "Dosya Aç";
        public const string Paste = "Yapıştır";
        public const string InsertRow = "Satır Ekle";
        public const string DeleteRows = "Satır Sil";
        public const string Undo = "Geri Al";
        public const string Clear = "Temizle";
        public const string FormatLabel = "Format:";
        public const string FormatAuto = "Otomatik";
        public const string FormatNoYX = "No Y X";
        public const string FormatNoXY = "No X Y";
        public const string FormatYX = "Y X";
        public const string FormatXY = "X Y";
        public const string FormatNoEnlemBoylam = "No Enlem Boylam";
        public const string FormatNoBoylamEnlem = "No Boylam Enlem";

        public const string Hint =
            "Excel veya Notepad'den kopyalayıp Ctrl+V ile yapıştırın, TXT/CSV/JSON dosyasını pencereye sürükleyin " +
            "ya da hücreleri elle düzenleyin. (Y = Sağa/Doğu, X = Yukarı/Kuzey) " +
            "Tapu/GPS'ten alınan Enlem/Boylam (WGS84°) listeleri ile TKGM parsel sorgu JSON çıktıları " +
            "(birden fazla parsel dahil) da otomatik tanınıp UTM metreye çevrilir.";

        // Grid columns
        public const string ColumnNo = "Nokta No";
        public const string ColumnEast = "Y (Sağa)";
        public const string ColumnNorth = "X (Yukarı)";

        // Options
        public const string OptionsTitle = "Seçenekler";
        public const string OptionClose = "Kapalı polyline";
        public const string OptionPointNumbers = "Nokta numaralarını yaz";
        public const string OptionMarkers = "Nokta işaretleri çiz";
        public const string OptionSummaryText = "Alan/çevre yazısı ekle";
        public const string OptionCreateLayer = "Katman otomatik oluştur";
        public const string OptionZoom = "Çizim sonrası yakınlaş";
        public const string LayerNameLabel = "Katman adı:";
        public const string TextHeightLabel = "Yazı yüksekliği:";
        public const string PointSymbolLabel = "Nokta sembolü:";
        public const string SymbolDot = "Nokta (·)";
        public const string SymbolPlus = "Artı (+)";
        public const string SymbolCross = "Çarpı (×)";
        public const string SymbolCircle = "Daire (○)";

        // Preview
        public const string PreviewTitle = "Önizleme";
        public const string PreviewPointCount = "Nokta sayısı:";
        public const string PreviewArea = "Alan (yaklaşık):";
        public const string PreviewPerimeter = "Çevre (yaklaşık):";
        public const string PreviewBounds = "Sınır kutusu:";
        public const string PreviewFormat = "Algılanan format:";
        public const string PreviewErrors = "Hatalı satır:";
        public const string PreviewShapeCount = "Şekil sayısı:";
        public const string PreviewNone = "—";

        // Draw / result
        public const string DrawButton = "ÇİZ";
        public const string DrawButtonWithCount = "ÇİZ  ({0} nokta)";
        public const string ResultTitle = "Sonuç";
        public const string ResultIdle = "Henüz çizim yapılmadı.";

        public const string ResultSuccessClosed =
            "✓ Kapalı polyline başarıyla oluşturuldu.\r\nNokta: {0}\r\nAlan: {1}\r\nÇevre: {2}\r\nKatman: {3}";

        public const string ResultSuccessOpen =
            "✓ Polyline başarıyla oluşturuldu.\r\nNokta: {0}\r\nUzunluk: {1}\r\nKatman: {2}";

        public const string ResultSuccessClosedMulti =
            "✓ {1} şekil çizildi (ana şekil kapalı polyline).\r\nToplam nokta: {0}\r\n" +
            "Ana şekil alanı: {2}\r\nAna şekil çevresi: {3}\r\nAna katman: {4}  •  Diğerleri: {5}";

        public const string ResultSuccessOpenMulti =
            "✓ {1} şekil çizildi.\r\nToplam nokta: {0}\r\n" +
            "Ana şekil uzunluğu: {2}\r\nAna katman: {3}  •  Diğerleri: {4}";

        // Errors
        public const string ErrorDrawFailed = "Çizim yapılamadı: {0}";
        public const string ErrorNotEnoughPoints = "Çizim için en az 2 geçerli nokta gerekli.";
        public const string ErrorClipboardEmpty = "Panoda metin bulunamadı.";
        public const string ErrorFileRead = "Dosya okunamadı: {0}";

        // File dialog
        public const string OpenFileTitle = "Koordinat dosyası aç";
        public const string OpenFileFilter =
            "Koordinat dosyaları (*.txt;*.csv;*.json)|*.txt;*.csv;*.json|Tüm dosyalar (*.*)|*.*";
    }
}
