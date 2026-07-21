# YapıLab CAD Tools — Arayüz Tasarımı

Modern, düz (flat), Office benzeri açık tema. Renkler ve fontlar `UI/Theme.cs` içinde
merkezidir: beyaz zemin, `#2563EB` vurgu mavisi, Segoe UI 9.5pt, hatalar için açık
kırmızı hücre arka planı.

## Ana pencere (940 × 760, yeniden boyutlandırılabilir)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  YapıLab CAD Tools — Koordinattan Çizim                              ─ □ ×   │
├──────────────────────────────────────────────────────────────────────────────┤
│ [Dosya Aç] [Yapıştır] │ [Satır Ekle] [Satır Sil] [Geri Al] [Temizle] │       │
│                                                       Format: [Otomatik ▼]  │
├──────────────────────────────────────────────────────────────────────────────┤
│  Excel veya Notepad'den kopyalayıp Ctrl+V ile yapıştırın, TXT/CSV dosyasını  │
│  pencereye sürükleyin ya da hücreleri elle düzenleyin. (Y=Sağa, X=Yukarı)    │
├──────────────────────────────────────────────────────────────────────────────┤
│ ┌──────────┬───────────────────────────┬───────────────────────────┐         │
│ │ Nokta No │ Y (Sağa)                  │ X (Yukarı)                │         │
│ ├──────────┼───────────────────────────┼───────────────────────────┤         │
│ │ 1        │ 456712.45                 │ 4423388.10                │         │
│ │ 2        │ 456798.32                 │ 4423401.75                │         │
│ │ 3        │ 456845.90                 │ 4423355.20                │         │
│ │ 5        │ ███ ---  (kırmızı satır — ipucu: "Y değeri sayı değil")│        │
│ │ 6        │ 456695.08                 │ 4423320.35                │         │
│ │          │                           │                           │  ▲      │
│ │          │   (sanal mod: 100.000+ satır akıcı kayar)             │  █      │
│ │          │                           │                           │  ▼      │
│ └──────────┴───────────────────────────┴───────────────────────────┘         │
├──────────────────────────────────────────────────────────────────────────────┤
│ ┌─ Seçenekler ──────────────────┐┌─ Önizleme ─────────────┐┌───────────────┐ │
│ │ ☑ Kapalı polyline             ││ Nokta sayısı: 6        ││ ┌───────────┐ │ │
│ │ ☑ Nokta numaralarını yaz      ││ Alan: 14.280,52 m²     ││ │  ÇİZ (6)  │ │ │
│ │ ☑ Alan/çevre yazısı ekle      ││ Çevre: 462,80 m        ││ └───────────┘ │ │
│ │ ☑ Çizim sonrası yakınlaş      ││ Sınır: 150,8 × 138,9 m ││┌─ Sonuç ─────┐│ │
│ │ ☐ Nokta işaretleri [Artı (+)▼]││ Format: No Y X • Sekme ││ ✓ Polyline   ││ │
│ │ ☑ Katman oluştur   [PARSEL  ] ││        • ondalık nokta ││ oluşturuldu. ││ │
│ │ Yazı yüksekliği:   [1,00  ⇅ ] ││ Hatalı satır: 1        ││ Alan: … m²   ││ │
│ └───────────────────────────────┘└────────────────────────┘└──────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Etkileşim ilkeleri

1. **Sıfır yapılandırma:** pencere açılır açılmaz Ctrl+V çalışır; format, ayraç ve
   sütun sırası otomatik algılanır ve önizlemede açıkça yazılır.
2. **Hata asla engel değildir:** bozuk satırlar kırmızı gösterilir, geri kalanı
   çizilebilir; ÇİZ düğmesi geçerli nokta sayısını üzerinde taşır.
3. **Geri alınabilirlik:** her yapıştırma/silme/düzenleme adımı Geri Al (Ctrl+Z)
   yığınına girer; AutoCAD tarafında da tüm çizim tek UNDO adımıdır.
4. **Canlı geri bildirim:** her değişiklikte (300 ms geciktirme ile) alan, çevre ve
   sınır kutusu yeniden hesaplanır — çizmeden önce sonuç bellidir.
5. **Modeless pencere:** AutoCAD ile yan yana çalışır; çizim sırasında belge
   kilitlenir, sonra kullanıcı kaldığı yerden devam eder.
