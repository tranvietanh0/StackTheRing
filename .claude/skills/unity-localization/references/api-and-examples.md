---

origin: theonekit-unity
repository: The1Studio/theonekit-unity
module: unity-base
protected: false
---
# Unity Localization — API & Code Examples

## C# Usage

```csharp
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizedUI : MonoBehaviour {
    // Inspector reference:
    [SerializeField] LocalizedString startText = new("UI", "menu_start");

    void OnEnable() {
        startText.StringChanged += UpdateText;
    }

    void OnDisable() {
        startText.StringChanged -= UpdateText;
    }

    void UpdateText(string value) {
        label.text = value;  // Auto-updates on locale change
    }

    // One-shot load:
    async void LoadString() {
        string text = await LocalizationSettings.StringDatabase
            .GetLocalizedStringAsync("UI", "menu_start").Task;
    }

    // With arguments:
    async void LoadFormatted() {
        string text = await LocalizationSettings.StringDatabase
            .GetLocalizedStringAsync("Gameplay", "health_label",
                arguments: new object[] { currentHealth }).Task;
    }
}
```

## Smart Strings (Advanced Formatting)

Enable Smart String on entries for powerful formatting:

```
// Placeholder:
"Hello, {player-name}!"

// Plural:
"{count:plural:There is {} item|There are {} items}"

// Choose:
"{gender:choose(m|f|n):He|She|They} picked up the item"

// Conditional:
"{health:choose(>50|>20|>0):Healthy|Wounded|Critical}"

// Nested:
"You found {item-count} {item-count:plural:{item-name}|{item-name}s}"
```

## Locale Management

```csharp
// Get available locales:
var locales = LocalizationSettings.AvailableLocales.Locales;

// Get current locale:
var current = LocalizationSettings.SelectedLocale;

// Change locale:
LocalizationSettings.SelectedLocale =
    LocalizationSettings.AvailableLocales.GetLocale("vi");

// Startup locale selector (Project Settings → Localization):
// 1. CommandLineSelector (--language=vi)
// 2. SystemLocaleSelector (OS language)
// 3. SpecificLocaleSelector (fallback default)
```

## Asset Tables (Localized Assets)

```csharp
// Localized sprites, audio, prefabs:
[SerializeField] LocalizedSprite flagSprite;
[SerializeField] LocalizedAudioClip voiceLine;

async void LoadLocalizedAsset() {
    Sprite flag = await flagSprite.LoadAssetAsync().Task;
    AudioClip clip = await voiceLine.LoadAssetAsync().Task;
}
```

## TMPro Integration

```
1. Add "Localize String Event" component to TextMeshProUGUI
2. Assign String Reference (table + key)
3. Wire "Update String" event to TMPro.text setter
4. Per-locale fonts: Use Font Asset fallback list or locale-specific font overrides
```

## Import/Export

```
Window → Asset Management → Localization Tables
├── Export → CSV, Google Sheets, XLIFF
└── Import → Same formats

// Google Sheets Extension:
// Install: com.unity.localization (includes Google Sheets support)
// Configure: Service Account + Sheet ID in table editor
```

## Common Gotchas

1. **Async loading**: Strings load asynchronously. Use `StringChanged` event, not direct access
2. **Missing entries**: Falls back to key name. Set fallback locale in settings
3. **Smart String escaping**: Use `\{` for literal braces
4. **Locale codes**: Use ISO 639-1 (en, vi, ja, ko, zh-Hans, zh-Hant)
5. **Font coverage**: CJK languages need large font atlases — use SDF fonts + fallback chains
6. **RTL**: TMPro supports RTL via `isRightToLeftText`. Test with Arabic/Hebrew early
