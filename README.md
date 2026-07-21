# Arazi Oluşturucu

**Koordinat listesinden tek tıkla parsel/arazi çizimi** yapan bir AutoCAD eklentisi.

Elinizdeki koordinat listesini (Excel, tapu/kadastro çıktısı, TXT, CSV — hatta artık **Enlem/Boylam (GPS)** olarak da) AutoCAD'e yapıştırın, **ÇİZ**'e basın; kapalı bir polyline, alan/çevre yazısı ve nokta numaralarıyla parseliniz hazır.

> Format bilmenize gerek yok. Sıra ister `No Y X`, ister `X Y`, ister `Enlem Boylam` olsun — program veriyi kendisi tanır.

---

## 📥 Kurulum (derleme gerekmez)

Bu bölüm hiç kod bilmeyen, sadece AutoCAD kullanan mimar/mühendis arkadaşlar için yazıldı. Beş dakika sürer.

### 1) DLL dosyasını indirin

**[⬇ YapiLabCadTools.dll indir](../../releases/latest/download/YapiLabCadTools.dll)**

(Bu link her zaman en güncel sürümü indirir. Alternatif olarak [Releases](../../releases) sayfasından da indirebilirsiniz.)

İndirdiğiniz dosyayı kolayca bulacağınız sabit bir klasöre koyun, örneğin:

```
C:\YapiLab\YapiLabCadTools.dll
```

> **Windows uyarısı çıkarsa:** İndirilen dosyaya sağ tıklayıp **Özellikler**'i açın, alt kısımda "Bu dosya başka bir bilgisayardan geldi, engellemeyi kaldır" gibi bir kutu varsa işaretleyip **Tamam**'a basın. Bu, internetten indirilen dosyalar için Windows'un standart bir uyarısıdır, dosyayla ilgili bir sorun değildir.

### 2) AutoCAD'e yükleyin

1. AutoCAD'i açın.
2. Komut satırına `NETLOAD` yazıp **Enter**'a basın.
3. Açılan pencereden 1. adımda indirdiğiniz `YapiLabCadTools.dll` dosyasını seçin.
4. Komut satırına `YAPILAB` yazıp **Enter**'a basın (kısayolu: `YL`).

Karşınıza koordinat penceresi açılacak. Bu kadar — kurulum bitti.

### 3) Her AutoCAD açılışında otomatik yüklensin

Yukarıdaki `NETLOAD` adımını her seferinde tekrar etmemek için:

1. Komut satırına `APPLOAD` yazın.
2. **Startup Suite** (Başlangıç Takımı) bölümüne `YapiLabCadTools.dll` dosyasını ekleyin.

Bundan sonra AutoCAD her açıldığında eklenti otomatik yüklenir, sadece `YAPILAB` yazmanız yeterli olur.

### Gereksinimler

| | |
|---|---|
| AutoCAD | **2025, 2026 veya 2027** (daha eski sürümler desteklenmez) |
| Ekstra kurulum | **Yok** — DLL tek başına çalışır, ek bir program/kütüphane kurmanıza gerek yok |

---

## 🖊 Kullanım

1. Koordinatları Excel'den, tapu/kadastro çıktısından veya herhangi bir metin dosyasından kopyalayın.
2. Arazi Oluşturucu penceresinde **Ctrl+V** ile yapıştırın (ya da TXT/CSV dosyasını pencereye sürükleyin / **Dosya Aç**'ı kullanın).
3. Alttaki **Önizleme**'de nokta sayısını, alanı ve çevreyi kontrol edin.
4. **ÇİZ**'e tıklayın.

Bir hücre yanlış görünüyorsa (kırmızı), üzerine gelince sebebini gösterir; tablo üzerinde elle düzeltebilirsiniz. Format yanlış algılandıysa araç çubuğundaki **Format** listesinden doğru sırayı elle seçin.

### Desteklenen koordinat biçimleri

- **Projeksiyonlu (TM/UTM) koordinatlar:** `No Y X`, `No X Y`, `Y X`, `X Y` — sıra otomatik algılanır.
- **Enlem/Boylam (WGS84°, GPS/tapu kadastro):** Örneğin
  ```
  No    Enlem      Boylam
  1     41.1886    28.8750
  2     41.1890    28.8747
  ...
  ```
  Bu tür bir liste yapıştırıldığında program otomatik olarak tanır ve çizim için gereken **UTM metre** koordinatlarına çevirir — elle hiçbir dönüşüm yapmanıza gerek yoktur.
- **Ayraçlar:** Tab, virgül, noktalı virgül, boşluk — hepsi otomatik tanınır.
- **Ondalık ayraç:** Hem `4423456,78` hem `4423456.78` çalışır.

### Örnek dosyalarla deneyin

Bu depodaki [`samples/`](samples) klasöründe gerçekçi örnek koordinat dosyaları var — indirip pencereye sürükleyerek programı hemen test edebilirsiniz.

---

## 🛠 Geliştiriciler için

Aşağısı yalnızca kaynak koddan derlemek isteyenler içindir — normal kullanım için gerekli değildir.

### Kaynak koddan derleme

```powershell
git clone https://github.com/samikurtulmus/arazi-olusturucu.git
cd arazi-olusturucu
dotnet build YapiLabCadTools.sln -c Release
```

Çıktı: `src\YapiLabCadTools\bin\Release\YapiLabCadTools.dll` — tek DLL, başka dosya gerekmez.

Testler:

```powershell
dotnet test
```

Gerekenler: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) veya üzeri; Visual Studio 2022 (17.8+) de kullanılabilir. Eklenti `net8.0-windows` hedefler ve derlemede NuGet üzerinden `AutoCAD.NET 25.0.1` başvurusu kullanır (yalnızca derleme zamanı — çalışma zamanında AutoCAD kendi DLL'lerini sağlar).

### Özellikler (teknik özet)

- **Akıllı format algılama:** `No Y X`, `No X Y`, `Y X`, `X Y`, **Enlem/Boylam (WGS84°)**, CSV, TSV, noktalı virgül, boşluk/çoklu boşluk/karışık ayraçlar, başlık satırları, boş satırlar — hepsi otomatik tanınır.
- **Enlem/Boylam → UTM dönüşümü:** GPS/tapu kadastrodan alınan WGS84 koordinatları, standart Transverse Mercator (UTM) formülleriyle metre cinsine çevrilir; UTM dilimi otomatik seçilir.
- **Ondalık virgül ve ondalık nokta** birlikte desteklenir; binlik ayraçlar da çözülür.
- **Sağa/Yukarı (easting/northing) sırası** koordinat değer aralıklarından otomatik belirlenir (Türkiye TM/UTM aralıkları) ve doğru CAD eksenlerine yerleştirilir.
- **Hatalı satırlar programı asla çökertmez:** kırmızı vurgulanır, açıklaması hücre ipucunda gösterilir, elle düzeltilebilir.
- **Canlı önizleme:** nokta sayısı, yaklaşık alan (m²), çevre (m), sınır kutusu ve algılanan format çizimden önce görünür.
- **Çizim seçenekleri:** kapalı/açık polyline, nokta numaraları, nokta işaretleri (·, +, ×, ○), alan/çevre yazısı, otomatik katman, yazı yüksekliği, çizim sonrası yakınlaşma.
- **Performans:** 100.000+ satır desteklenir (sanal tablo + tek AutoCAD transaction'ı); ayrıştırma arka planda çalışır.
- Girdi yolları: **Ctrl+V**, Excel/Notepad yapıştırma, **TXT/CSV açma**, **sürükle-bırak**, elle hücre düzenleme, satır ekle/sil, **geri al**.

### Mimari

```
src/YapiLabCadTools/            → tek DLL olarak dağıtılan eklenti
├── Plugin/                     AutoCAD giriş noktası (IExtensionApplication, komutlar)
├── Core/                       ★ AutoCAD bağımsız çekirdek — birim testleri buraya bağlanır
│   ├── Models/                 CoordinatePoint, ParseResult, DrawOptions, GeometryStats…
│   ├── Parsing/                SmartCoordinateParser + Delimiter/Header/Column/Enlem-Boylam dedektörleri
│   ├── Geometry/                PolygonMath (alan, çevre, sınır kutusu) + GeographicProjection (WGS84 → UTM)
│   └── Utils/                  Sayı biçimlendirme
├── Drawing/                    AutoCAD çizim motoru (PolylineDrawingService, LayerService)
├── Services/                   ServiceContainer (composition root), FileService
└── UI/                         WinForms arayüzü (MainForm, Theme, Texts, PointRow)

tests/YapiLabCadTools.Tests/    → Core kaynaklarını doğrudan derleyen xUnit testleri
```

Katman kuralları:

- **Core** hiçbir AutoCAD ya da WinForms türüne başvurmaz → testler AutoCAD olmadan çalışır.
- **UI** çizimi `IDrawingService`, ayrıştırmayı `ICoordinateParser` arayüzleri üzerinden yapar; somut sınıflar `ServiceContainer` içinde bağlanır (constructor injection).
- **Komutlar incedir:** yalnızca servisleri çözüp pencereyi açar.

#### Gelecek modüller nasıl eklenir?

Yeni bir modül (ör. "polyline'dan koordinat dök", "Excel dışa aktar", "parsel raporu"):

1. İş mantığını `Core/` altına (AutoCAD'siz, test edilebilir) yazın.
2. AutoCAD tarafı gerekiyorsa `Drawing/` altına yeni bir servis + arayüz ekleyin.
3. Servisi `ServiceContainer`'a kaydedin.
4. `Plugin/` altına yeni bir `[CommandMethod]` sınıfı ekleyin ve `AssemblyAttributes.cs`'e `[assembly: CommandClass(...)]` satırını ilave edin.

Çekirdek sistemde değişiklik gerekmez.

### Teknik notlar

- Tüm çizim **tek `DocumentLock` + tek `Transaction`** içinde yapılır → 100k köşe hızlı ve AutoCAD'de tek UNDO adımı.
- Tablo **sanal modda** çalışır (satırlar bellekte, ekranda yalnız görünenler çizilir).
- Dosya okumada UTF-8/UTF-16 içerikten algılanır; eski dosyalar için **Windows-1254** (Türkçe ANSI) devreye girer.
- Alan/çevre önizlemesi AutoCAD'e dokunmadan `PolygonMath` ile hesaplanır (shoelace formülü).
- Enlem/Boylam listeleri, WGS84 elipsoidi üzerinde standart Transverse Mercator formülleriyle UTM metreye çevrilir (bkz. `Core/Geometry/GeographicProjection.cs`).

---

## Lisans

[MIT](LICENSE) — ücretsiz, ticari/kişisel her amaçla kullanabilir, değiştirebilir ve dağıtabilirsiniz.
