# Jumpy

Doodle Jump tarzı, sonsuz yukarı zıplamalı 2D platformer. Unity 6 (6000.4.4f1), URP 2D, Android hedefli bir prototip/case study.

Karakter otomatik olarak platformdan platforma zıplar; oyuncu yalnızca ekranın sağına/soluna dokunarak (veya tıklayarak) yatay yönü kontrol eder. Platformlar havuzdan (object pool) sonsuz akar, kamera yalnızca yukarı doğru takip eder, ekranın altına düşünce oyun sıfırdan başlar.

---

## İçindekiler

1. [Genel Mimari Felsefesi](#genel-mimari-felsefesi)
2. [Klasör / Namespace Yapısı](#klasör--namespace-yapısı)
3. [Kullanılan Tasarım Desenleri](#kullanılan-tasarım-desenleri)
4. [Sistem Sistem Açıklama](#sistem-sistem-açıklama)
5. [Oynanış Mekanikleri](#oynanış-mekanikleri)
6. [UI Sistemi (UI Toolkit)](#ui-sistemi-ui-toolkit)
7. [Ses Sistemi](#ses-sistemi)
8. [Veri / ScriptableObject Asset'leri](#veri--scriptableobject-assetleri)
9. [Android Build](#android-build)
10. [Kod Standartları](#kod-standartları)
11. [Bilinen Sınırlamalar / Sonraki Adımlar](#bilinen-sınırlamalar--sonraki-adımlar)

---

## Genel Mimari Felsefesi

Proje üç temel prensip üzerine kuruldu:

- **Veri odaklı (data-driven):** Oyunun tüm ayarlanabilir sayıları (hız, zıplama kuvveti, platform aralıkları, ses seviyeleri) kod içine gömülü değil, `ScriptableObject` asset'lerinde tutulur. Sahneye elle hiçbir platform/oyuncu dizilmez — her şey oyun başladığında bu verilerden **generate** edilir (bkz. `GameInitializer`).
- **Gevşek bağlı (loosely coupled) sistemler:** Sistemler birbirine mümkün olduğunca doğrudan referansla değil, `ScriptableObject` tabanlı **event channel**'lar üzerinden konuşur. Ama bu her yerde zorlanmaz — kamera→oyuncu gibi doğal 1:1 ilişkiler bilinçli olarak doğrudan referans kullanır (aşağıda "Neden Event Bus Her Yerde Değil" bölümüne bakın).
- **Küçük, tek sorumluluklu sınıflar:** Hiçbir sınıf "her şeyi yapan" bir god-object değildir. Örn. platform akışı tek bir dev sınıf yerine `PlatformSpawner` (spawn kararı), `PlatformRecycler` (havuza iade), `PlatformPoolManager` (havuz yönetimi) ve bunları yöneten ince bir `PlatformStreamManager` orkestratörüne bölünmüştür.

Bu üç prensip, `Rules/` klasöründeki iki referans kaynağa dayanıyor: Unity'nin resmi *"Create a C# Style Guide"* e-kitabı (isimlendirme, formatlama, sınıf/method tasarımı kuralları) ve Alexander Shvets'in *"Dive Into Refactoring"* kitabı (code smell kataloğu ve refactoring teknikleri — bkz. Bloaters, Object-Orientation Abusers, Dispensables vb.).

---

## Klasör / Namespace Yapısı

```
Assets/Scripts/
  Core/
    Events/      → Game.Core.Events      (SO tabanlı generic event channel altyapısı)
    Pooling/     → Game.Core.Pooling     (generic object pool altyapısı)
    Utility/     → Game.Core.Utility     (ScreenBoundsUtility gibi bağımsız yardımcılar)
  Data/          → Game.Data             (ScriptableObject veri konteynerleri)
  Platforms/     → Game.Platforms        (platform havuzu, spawn/recycle, hareket stratejileri)
  Player/        → Game.Player           (input, hareket, zıplama, ekran wrap, düşme algılama)
  CameraSystem/  → Game.CameraSystem     (kamera takip mantığı)
  Audio/         → Game.Audio            (SFX çalma, ses ayarları köprüsü)
  UI/            → Game.UI               (UI Toolkit controller'ları)
  Bootstrap/     → Game.Bootstrap        (oyunu ayağa kaldıran giriş noktası)
```

Namespace'ler arası bağımlılık yönü **tek yönlü** tutulur: `Data` hiçbir zaman `Platforms`'a bağımlı değildir (döngüsel referansı önlemek için `PlatformDataSO.Prefab` alanı `PlatformController` değil düz `GameObject` tipindedir — bkz. [Strategy / Factory](#platform-hareket-stratejisi-strategy--factory) bölümü).

---

## Kullanılan Tasarım Desenleri

### Object Pool — `Core/Pooling/`

**Nerede:** `IPoolable` arayüzü + generic `ComponentPool<T>` sınıfı (Unity'nin `UnityEngine.Pool.ObjectPool<T>`'ını sarmalıyor), kullanan yer: `Platforms/PlatformPoolManager`.

**Neden:** Oyun sonsuz yukarı akan platformlar üretir. Her zıplamada yeni bir `Instantiate`/`Destroy` çağrısı yapmak GC (garbage collector) baskısı ve frame-time dalgalanmasına yol açar — mobilde (Android hedefi) bu özellikle kritik. Object Pool deseni, ekrandan çıkan platformları yok etmek yerine devre dışı bırakıp yeniden kullanır.

```csharp
public class ComponentPool<T> where T : Component, IPoolable
```

`ComponentPool<T>` generic olduğu için sadece platformlar değil, ileride mermi/parçacık gibi başka tekrarlı objeler için de tekrar kullanılabilir (DRY). `IPoolable.OnSpawned()/OnDespawned()` her obje tipinin kendi reset mantığını tanımlamasını sağlar (örn. `PlatformController.OnDespawned()` kendi `Data` referansını temizler).

`PlatformPoolManager`, `PlatformDataSO` başına **ayrı bir havuz** tutar (`Dictionary<PlatformDataSO, ComponentPool<PlatformController>>`) — böylece farklı platform tipleri (statik/hareketli) birbirinin havuzunu kirletmez.

### Observer / Event Bus — ScriptableObject Event Channels — `Core/Events/`

**Nerede:** Generic `EventChannelSO<T>` (abstract) + somut kanallar: `FloatEventChannelSO`, `AudioClipEventChannelSO` (Core), `PlatformEventChannelSO` (Platforms, payload `PlatformController` olduğu için Data/Core'a değil kendi katmanına konuldu).

**Neden:** Ryan Hipple'ın Unity Unite konuşmasında popülerleşen "Game Architecture with ScriptableObjects" deseni. Klasik C# event/delegate yerine bir **asset** kullanmanın faydası: yayıncı (publisher) ve dinleyici (subscriber) birbirinin script'ine hiç referans vermeden, Inspector'dan aynı asset'i sürükleyerek bağlanır. Bu, sistemleri gerçekten birbirinden bağımsız test edilebilir/değiştirilebilir kılar.

**Somut örnek — mimarinin ödemesini yaptığı an:** `PlayerJumpController`, oyuncu bir platforma indiğinde `PlatformEventChannelSO`'yu (`PlayerLandedChannel` asset'i) tetikler. Bu event'i **iki bağımsız sistem** dinler:
1. Hiç — aslında şu an tek dinleyici `JumpSfxTrigger` (ses sistemi), zıplama sesini çalmak için.
2. Mimari baştan buna göre kurulduğu için, skor sistemi gibi gelecekteki bir özellik `PlayerJumpController`'ın tek satırına bile dokunmadan aynı event'e abone olabilir.

```csharp
// JumpSfxTrigger.cs — PlayerJumpController'dan tamamen habersiz
private void OnEnable() => _playerLandedChannel.OnEventRaised += HandlePlayerLanded;
private void HandlePlayerLanded(PlatformController platform) => _sfxRequestChannel.Raise(_jumpClip);
```

**Neden generic tek tip + concrete alt sınıflar (her event için ayrı sınıf değil):** `FloatEventChannelSO` hem input yönü hem de (potansiyel olarak) başka float event'ler için tekrar kullanılabilir. Her olay için özel bir sınıf açmak (`PlayerJumpedEventSO`, `ScoreChangedEventSO`, ...) *Dispensables/Speculative Generality* kod kokusuna girer — bunun yerine az sayıda **reusable payload tipi**, farklı olaylar farklı **asset instance'ları** ile temsil edilir.

**Neden her yerde değil:** Kamera'nın oyuncuyu takip etmesi (`CameraFollowController.SetTarget`) veya Ayarlar panelinin Ana Menü'yü gizlemesi gibi doğal, tek yönlü, tek-tüketicili ilişkiler için event bus yerine **doğrudan referans** kullanıldı. Event bus'ı her ilişkide zorlamak gereksiz dolaylılık (indirection) ve YAGNI ihlali olurdu.

### Strategy + Factory — Platform Hareket Stratejisi

**Nerede:** `Platforms/IPlatformMovement` arayüzü, `StaticPlatformMovement` / `HorizontalOscillatePlatformMovement` implementasyonları, `PlatformMovementFactory` (enum'dan strateji nesnesi üreten merkezi fabrika).

**Neden:** Refactoring Guru kataloğundaki **"Replace Conditional with Polymorphism"** tekniğinin doğrudan uygulaması. Platform hareketini `if/switch (movementType) { ... }` ile her `Update()` çağrısında dallandırmak yerine, her hareket tipi kendi küçük sınıfında izole edilir:

```csharp
public interface IPlatformMovement
{
    Vector3 Evaluate(Vector3 basePosition, float elapsedTime, PlatformDataSO data);
}
```

`PlatformMovementFactory` içindeki TEK `switch` merkezi bir "hangi strateji nesnesi üretilsin" kararıdır — bu, kod tabanına yayılmış çok sayıda tip kontrolünden farklıdır ve kabul edilebilir bir factory deseni kullanımıdır (kod kokusu olan, kod tabanına *dağılmış* switch'lerdir, tek bir üretim noktasındaki switch değil).

Yeni bir platform davranışı eklemek (örn. dikey salınım, kaybolan platform) mevcut kodu değiştirmeden yeni bir `IPlatformMovement` implementasyonu eklemekle olur (Open/Closed Principle).

### ScriptableObject Veri Konteynerleri (Data-Driven Design)

**Nerede:** `Data/` klasöründeki her `...SO.cs` dosyası.

| Asset | Görevi |
|---|---|
| `GameConfigSO` | Oyuncu hızı, zıplama kuvveti, ekran wrap payı, düşme-ölüm payı, kamera takip parametreleri, platform spawn/despawn mesafeleri |
| `PlatformDataSO` | Bir platform *tipini* tanımlar: prefab, sprite, dikey aralık min/max, hareket tipi + parametreleri, çökme (crumbling) davranışı + parametreleri |
| `PlatformSpawnSetSO` | `PlatformDataSO` listesi + ağırlıklı rastgele seçim (`GetRandomPlatform()`) — zorluk/çeşitlilik ayarı kod değişmeden bu asset üzerinden yapılabilir |
| `GameSettingsSO` | Kullanıcının ses tercihlerini (Master/SFX volume) `PlayerPrefs` ile kalıcı tutan runtime "canlı ayar" nesnesi |

**Neden:** Tasarımcının (ya da geliştiricinin) dengeleme/tuning yapmak için kod yazıp yeniden derlemesine gerek kalmaz — Inspector'dan sayıları değiştirip Play'e basmak yeterli. Ayrıca `PlatformDataSO.Prefab` bilinçli olarak `GameObject` tipinde tutulur (spesifik `PlatformController` tipinde değil) — aksi halde `Data` katmanı `Platforms` katmanına bağımlı olur ve **döngüsel bağımlılık** oluşurdu (`Platforms` zaten `Data`'ya bağımlı). Bu, "bağımlılık yönü tek taraflı olmalı" prensibinin somut bir uygulamasıdır.

### Single Responsibility — Platform Akış Boru Hattı

`PlatformStreamManager` (MonoBehaviour, orkestratör) → `PlatformSpawner` (hangi platformun nereye doğması gerektiğine karar verir) → `PlatformPoolManager` (havuzdan objeyi çeker) → `PlatformRecycler` (ekran altına düşenleri geri iade eder). Her sınıf **tek bir şeyi** yapar ve bağımsız test edilebilir; `PlatformStreamManager` sadece bunları doğru sırayla çağıran ince bir katmandır (Refactoring Guru: *Large Class* / *Long Method* kokularının önlenmesi).

---

## Sistem Sistem Açıklama

### Core

- **`EventChannelSO<T>`** — generic SO event channel temel sınıfı, `event Action<T> OnEventRaised` + `Raise(T value)`.
- **`ComponentPool<T>`** — generic Unity object pool sarmalayıcısı.
- **`ScreenBoundsUtility`** — `Camera.orthographicSize` + `aspect`'ten world-space ekran sınırlarını hesaplayan stateless static yardımcı. Hem `PlatformStreamManager` (spawn genişliği) hem `PlayerScreenWrapper` (ekran kenarı) hem `PlayerFallDetector` (ölüm eşiği) tarafından tekrar tekrar kullanılır (DRY).

### Platforms

- **`PlatformController`** — `IPoolable` implementasyonu, kendi `PlatformDataSO`'sunu ve aktif `IPlatformMovement` stratejisini tutar. `PlaceAt(position)` ile spawn anında konumlanır (havuzdan çıkışta pozisyon sıralaması bug'ını önlemek için `Initialize()`'dan ayrı, açık bir adım — bkz. geliştirme geçmişi). `Initialize()` ayrıca `Data.Sprite` doluysa `SpriteRenderer.sprite`'ı da ayarlar — görsel tamamen veri odaklıdır, prefab başına sabit değildir.
- **`PlatformEffector2D`** kullanılarak platformlar **tek yönlü** yapılmıştır: karakter alttan geçebilir, sadece üstten inince çarpışma oluşur. Bu, elle pivot karşılaştırması yazmak yerine Unity'nin bu tam senaryo için var olan fizik bileşenini kullanır (KISS — "tekerleği yeniden icat etme").
- **Çökme (crumbling) platformları** — `PlatformDataSO.IsCrumbling = true` olan platform tipleri. `PlayerJumpController`, bir platforma inince artık `platform.NotifyLanded()`'ı da çağırır (broadcast event'e ek olarak, spesifik örneğe **doğrudan** bir çağrı — burada tek bir dinleyici olduğu ve ilişki 1:1 olduğu için event bus yerine doğrudan referans tercih edildi). `PlatformController`, `Data.CrumbleDelay` kadar bekler, sonra collider'ını kapatır ve `Data.CrumbleFallAcceleration` ile hızlanarak düşmeye başlar. Ekranın altına düşünce **hiçbir özel havuz mantığına gerek kalmadan**, zaten var olan `PlatformRecycler` onu otomatik olarak havuza iade eder — mimarideki sistemlerin birbiriyle uyumlu çalışmasının bir örneği. `OnDespawned()` çökme durumunu (`_isCrumbling`, collider, düşüş hızı) tam olarak sıfırlar, böylece havuzdan tekrar çekilen aynı örnek düzgün şekilde yeniden kullanılabilir. Sprite'ı, orijinal `tile_half` platform sprite'ının parlaklık-korumalı biçimde turuncu-kırmızı bir "tehlike" rengine boyanmasıyla üretildi (`Assets/Sprites/Platforms/tile_half_crumbling.png`).

### Player

- **`PlayerInputReader`** — yeni Input System'in `Pointer.current`'ı üzerinden ekranın sağ/sol yarısına basılı tutmayı okur, yönü `FloatEventChannelSO` üzerinden yayınlar. Mouse ve dokunmatik ekranı aynı kodla, platforma özel dallanma olmadan destekler.
- **`PlayerMovementController`** — yön event'ini dinler, `Rigidbody2D.linearVelocity.x`'i günceller.
- **`PlayerJumpController`** — platforma çarpışmada (tek yönlü fizik zaten sadece üstten inişte tetiklendiği için ekstra hız kontrolüne gerek yoktur) zıplama kuvvetini uygular ve `PlayerLandedChannel`'ı tetikler.
- **`PlayerScreenWrapper`** — ekranın bir kenarından çıkan oyuncuyu diğer kenardan çıkarır (sonsuz yatay döngü).
- **`PlayerFallDetector`** — oyuncu kamera alt sınırının (+ pay) altına düşerse sahneyi `SceneManager.LoadScene` ile yeniden yükler.

### CameraSystem

- **`CameraFollowController`** — kamerayı yalnızca yukarı doğru takip eder (`Mathf.Max` ile asla geriye/aşağı gitmez), `SmoothDamp` ile yumuşatır, oyuncu ile arasındaki mesafe `MaxCameraLag`'ı aşarsa sert şekilde yakalar — hızlı zıplamalarda oyuncunun ekran dışına çıkmasını önler.

### Bootstrap

- **`GameInitializer`** — `BeginGame()` public metodu ile (Ana Menü'deki Play butonundan tetiklenir) oyuncuyu instantiate eder, kamera hedefini ve platform akışını başlatır. Sahne yüklendiğinde otomatik başlamaz — kullanıcı Play'e basana kadar bekler.

---

## Oynanış Mekanikleri

| Mekanik | Nasıl çalışıyor |
|---|---|
| Otomatik zıplama | `Rigidbody2D` + `PlatformEffector2D` tek yönlü platform + `PlayerJumpController` çarpışma tepkisi |
| Yön kontrolü | Ekranın sağına/soluna dokunma → `PlayerInputReader` → event → `PlayerMovementController` |
| Sonsuz platform akışı | `PlatformStreamManager` oyuncunun yüksekliğine göre önden spawn eder, kamera altına düşenleri havuza iade eder |
| Çökme platformu | Farklı renkli platform tipi — üstüne inince kısa bir gecikmeyle çöker ve düşer, tekrar basılamaz; `PlatformSpawnSetSO` içinde ağırlıklı olarak diğer tiplerle karışık spawn olur, oyun uzadıkça oyuncunun daha sık karşılaştığı bir risk haline gelir |
| Ekran wrap | `PlayerScreenWrapper`, `ScreenBoundsUtility` ile hesaplanan sınırları kullanır |
| Kamera takibi | Sadece yukarı, max-lag ile sıkı takip |
| Ölüm / yeniden başlama | Ekran altına düşme → sahne yeniden yüklenir → Ana Menü'ye dönülür |
| Duraklama | Ayarlar paneli açıkken `Time.timeScale = 0` (UI ve ses bundan etkilenmez, gerçek zamanla çalışmaya devam eder) |

---

## UI Sistemi (UI Toolkit)

Proje **UI Toolkit** (UXML/USS — XML tabanlı UI tanımı) kullanır; bu, Unity 6'da yerleşik bir modüldür (`com.unity.modules.uielements`), ekstra paket kurulumu gerekmez.

- **Ana Menü** (`Assets/UI/MainMenu/`) — başlık + Play butonu. `MainMenuController`.
- **Ayarlar Paneli** (`Assets/UI/Settings/`) — Master/SFX volume slider'ları. `SettingsMenuController`. Hem ana menüden hem oyun içinden açılabilir; nereden açıldığını hatırlayıp Geri butonunda doğru yere döner.
- **Kalıcı Ayarlar Butonu (HUD)** (`Assets/UI/Hud/`) — ekranın sağ üstünde her zaman sabit duran yuvarlak buton, `SettingsHudController`. Oyun içinde de erişilebilir olması için ayrı, hiç deaktif olmayan bir `UIDocument` olarak kurgulandı.
- **`KenneyButtonSkinner`** — herhangi bir UI Toolkit `Button`'a Kenney UI Pack sprite'larıyla normal/basılı görsel durumu ve 9-slice dilimleme uygulayan, tekrar kullanılabilir bağımsız yardımcı sınıf (MonoBehaviour değil, düz C# sınıfı — DRY, birden fazla controller'da tekrar tekrar kullanılıyor).

Panel sıralaması (`UIDocument.sortingOrder`): Ana Menü (0) < HUD butonu (1) < Ayarlar paneli (2) — ayarlar açıldığında her şeyin üstünde görünür.

---

## Ses Sistemi

- **`AudioClipEventChannelSO`** — "bu ses efektini çal" isteği için genel amaçlı event channel (Core.Events, reusable — Event Bus deseninin ses için ikinci bir uygulaması).
- **`SfxPlayer`** — bu kanalı dinleyip `AudioSource.PlayOneShot` ile çalan tek merkezi bileşen.
- **`JumpSfxTrigger`** — mevcut `PlayerLandedChannel`'ı ikinci bir dinleyici olarak kullanır, zıplama sesini `PlayerJumpController`'a hiç dokunmadan tetikler (Event Bus mimarisinin somut faydası).
- **`AudioSettingsController`** — `GameSettingsSO`'daki Master×SFX çarpımını `AudioSource.volume`'a uygular ve kalıcı hale getirir. *(Not: `Awake()` yerine bilinçli olarak `Start()` kullanılır — aynı GameObject üzerindeki `SfxPlayer.Awake()`'in kendisinden önce çalışacağının garantisi olmadığı için; `Start()` tüm `Awake()` çağrılarından sonra çalışması garanti edilen ilk noktadır.)*

Ses efektleri Kenney UI Pack'ten alınmıştır (`Assets/Audio/Kenney/`): `tap-a` (zıplama), `click-a` (buton tıklama), `switch-a` (slider/ayar değişimi).

---

## Veri / ScriptableObject Asset'leri

```
Assets/Data/
  GameConfig.asset              → GameConfigSO
  GameSettings.asset            → GameSettingsSO (PlayerPrefs destekli)
  Events/
    MoveDirectionChannel.asset  → FloatEventChannelSO
    PlayerLandedChannel.asset   → PlatformEventChannelSO
    SfxRequestChannel.asset     → AudioClipEventChannelSO
  Platforms/
    PlatformData_Static.asset     → PlatformDataSO (sabit platform)
    PlatformData_Moving.asset     → PlatformDataSO (yatay salınımlı platform)
    PlatformData_Crumbling.asset  → PlatformDataSO (çöken platform, turuncu-kırmızı sprite)
    PlatformSpawnSet.asset        → PlatformSpawnSetSO (%50 sabit / %25 hareketli / %25 çöken ağırlık)
```

---

## Android Build

- **Build hedefi:** Android, IL2CPP scripting backend, ARM64 mimari (Play Store zorunluluğu — proje varsayılanlarında zaten mevcut).
- **Paket kimliği:** `com.Jumpy.Jumpy`
- **Ekran yönü:** Portrait'e kilitli (dikey oyun).
- **Input:** Yeni Input System (`Pointer.current`) zaten dokunmatik ekranı native destekler — platforma özel ekstra kod gerekmedi.
- Build, 0 derleme hatasıyla doğrulanmıştır (`File > Build Settings > Android` üzerinden yeniden derlenebilir).

---

## Kod Standartları

Proje `Rules/` klasöründeki iki kaynağa uyar:
- Unity resmi *"Create a C# Style Guide"* — isimlendirme (PascalCase/camelCase/`_` öneki), Allman brace stili, sınıf organizasyonu, yorum kuralları.
- *"Dive Into Refactoring"* (Refactoring.Guru) — code smell kataloğu (Bloaters, Object-Orientation Abusers, Change Preventers, Dispensables, Couplers) ve karşılık gelen refactoring teknikleri.

Özet kurallar: PascalCase class/method/public üye, camelCase + `_` önekli private alan, Allman stil süslü parantez, tek satırlık `if`'lerde bile parantez kullanımı, `[SerializeField]` + `[Tooltip]` ile private alanların Inspector'a açılması, minimal yorum (sadece "neden" açıklaması gerektiğinde).

---

## Bilinen Sınırlamalar / Sonraki Adımlar

- Zorluk seviyesi ayarlardan kaldırıldı; şu an zorluk artışı, çöken platform gibi tiplerin `PlatformSpawnSetSO` içindeki ağırlıklı karışımından doğal olarak geliyor. Yüksekliğe göre ağırlıkları kademeli değiştiren (örn. yükseldikçe çöken platform oranını artıran) bir sistem ileride eklenebilir.
- Skor sistemi henüz yok — `PlayerLandedChannel` event'i zaten mevcut olduğu için eklenmesi kolay (yeni bir dinleyici yazmak yeterli, mevcut koda dokunmadan).
- Tek platform prefabı (`Platform.prefab`) var; farklı görsel/davranış varyasyonları (statik, hareketli, çöken) `PlatformDataSO` üzerinden aynı prefaba farklı sprite + `PlatformMovementType` + çökme parametreleri atanarak elde ediliyor.
