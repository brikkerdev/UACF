

# Техническое задание: UI Design Framework для Unity 6.3

## 1. Общее описание

### 1.1 Назначение
UPM-пакет для Unity 6.3, предоставляющий фреймворк для проектирования игрового UI. Обёртка над Unity UI (uGUI) с системой дизайн-токенов, компонентной библиотекой, управлением экранами и интеграцией с UACF для полной автоматизации через AI-агентов.

### 1.2 Ключевые принципы
- **Token-driven** — все визуальные значения (цвета, шрифты, отступы) хранятся централизованно в ScriptableObjects, никаких хардкод-значений в компонентах
- **Prefab-based** — каждый UI-компонент — готовый префаб с привязкой к теме
- **Composable** — экраны собираются из компонентов как конструктор
- **Theme-switchable** — смена темы в runtime меняет весь UI мгновенно
- **Automation-first** — всё управляемо через UACF API без ручного взаимодействия
- **Extensible** — пользователь может добавлять свои токены, компоненты, стили

### 1.3 Зависимости
- Unity 6.3+ (6000.3.x)
- Unity UI (com.unity.ugui)
- TextMeshPro (com.unity.textmeshpro)
- UACF (com.uacf.editor) — для Editor API интеграции

---

## 2. Архитектура

### 2.1 Структура пакета

```
com.uacf.ui-framework/
├── package.json
├── README.md
├── CHANGELOG.md
│
├── Runtime/
│   ├── com.uacf.ui-framework.runtime.asmdef
│   │
│   ├── Tokens/
│   │   ├── ColorPalette.cs
│   │   ├── TypographySet.cs
│   │   ├── TypographyPreset.cs
│   │   ├── SpacingScale.cs
│   │   ├── ShapeSet.cs
│   │   ├── ShapePreset.cs
│   │   ├── ElevationSet.cs
│   │   ├── ElevationPreset.cs
│   │   ├── IconSet.cs
│   │   └── Theme.cs
│   │
│   ├── Styles/
│   │   ├── UIStyle.cs
│   │   ├── UIStyleState.cs
│   │   ├── StyleSheet.cs
│   │   ├── StyleResolver.cs
│   │   └── StyleBinding.cs
│   │
│   ├── Components/
│   │   ├── Base/
│   │   │   ├── UIComponentBase.cs
│   │   │   ├── UIInteractableBase.cs
│   │   │   └── IThemeable.cs
│   │   │
│   │   ├── Display/
│   │   │   ├── UIText.cs
│   │   │   ├── UIImage.cs
│   │   │   ├── UIIcon.cs
│   │   │   ├── UIDivider.cs
│   │   │   └── UIBadge.cs
│   │   │
│   │   ├── Input/
│   │   │   ├── UIButton.cs
│   │   │   ├── UIIconButton.cs
│   │   │   ├── UIToggle.cs
│   │   │   ├── UISlider.cs
│   │   │   ├── UIDropdown.cs
│   │   │   ├── UIInputField.cs
│   │   │   └── UICheckbox.cs
│   │   │
│   │   ├── Containers/
│   │   │   ├── UIPanel.cs
│   │   │   ├── UICard.cs
│   │   │   ├── UIScrollView.cs
│   │   │   ├── UIList.cs
│   │   │   ├── UIListItem.cs
│   │   │   ├── UIAccordion.cs
│   │   │   └── UITabContainer.cs
│   │   │
│   │   ├── Layout/
│   │   │   ├── UIHorizontalLayout.cs
│   │   │   ├── UIVerticalLayout.cs
│   │   │   ├── UIGridLayout.cs
│   │   │   ├── UIStackLayout.cs
│   │   │   ├── UISpacer.cs
│   │   │   ├── UIExpandable.cs
│   │   │   └── UIResponsiveContainer.cs
│   │   │
│   │   ├── Navigation/
│   │   │   ├── UIHeader.cs
│   │   │   ├── UIToolbar.cs
│   │   │   ├── UIBottomBar.cs
│   │   │   ├── UITabBar.cs
│   │   │   ├── UISidebar.cs
│   │   │   └── UIBreadcrumb.cs
│   │   │
│   │   ├── Overlay/
│   │   │   ├── UIModal.cs
│   │   │   ├── UIDialog.cs
│   │   │   ├── UIToast.cs
│   │   │   ├── UITooltip.cs
│   │   │   └── UIOverlay.cs
│   │   │
│   │   └── Feedback/
│   │       ├── UIProgressBar.cs
│   │       ├── UISpinner.cs
│   │       └── UIHealthBar.cs
│   │
│   ├── Screens/
│   │   ├── UIScreen.cs
│   │   ├── UIScreenRegistry.cs
│   │   ├── UINavigator.cs
│   │   ├── NavigationStack.cs
│   │   ├── ScreenTransition.cs
│   │   └── TransitionPreset.cs
│   │
│   ├── Animation/
│   │   ├── UIAnimator.cs
│   │   ├── UITween.cs
│   │   ├── TweenPreset.cs
│   │   ├── MicroInteraction.cs
│   │   └── EasingFunctions.cs
│   │
│   ├── Theming/
│   │   ├── ThemeManager.cs
│   │   ├── ThemeApplier.cs
│   │   ├── ThemeListener.cs
│   │   └── TokenReference.cs
│   │
│   └── Utility/
│       ├── SafeAreaHelper.cs
│       ├── UIExtensions.cs
│       ├── AnchorPresets.cs
│       └── UIConstants.cs
│
├── Editor/
│   ├── com.uacf.ui-framework.editor.asmdef
│   │
│   ├── UACF/
│   │   ├── UIHandlerRegistration.cs
│   │   ├── Handlers/
│   │   │   ├── UIScreenHandler.cs
│   │   │   ├── UIElementHandler.cs
│   │   │   ├── UIThemeHandler.cs
│   │   │   ├── UITokenHandler.cs
│   │   │   ├── UIStyleHandler.cs
│   │   │   ├── UILayoutHandler.cs
│   │   │   └── UIComponentListHandler.cs
│   │   │
│   │   └── Services/
│   │       ├── UIBuilderService.cs
│   │       ├── UIThemeService.cs
│   │       ├── UITokenService.cs
│   │       ├── UIStyleService.cs
│   │       └── UISerializationService.cs
│   │
│   ├── Inspectors/
│   │   ├── ThemeInspector.cs
│   │   ├── ColorPaletteInspector.cs
│   │   ├── TypographySetInspector.cs
│   │   ├── UIStyleInspector.cs
│   │   ├── UIComponentBaseInspector.cs
│   │   └── StyleBindingDrawer.cs
│   │
│   ├── Windows/
│   │   ├── ThemeEditorWindow.cs
│   │   ├── TokenBrowserWindow.cs
│   │   ├── StyleEditorWindow.cs
│   │   └── ComponentPaletteWindow.cs
│   │
│   ├── Creation/
│   │   ├── UIMenuItems.cs
│   │   ├── PrefabFactory.cs
│   │   └── ScreenTemplateFactory.cs
│   │
│   └── Validation/
│       ├── ThemeValidator.cs
│       └── StyleValidator.cs
│
├── Resources/
│   └── UACF_UI/
│       ├── DefaultTheme/
│       │   ├── DefaultTheme.asset
│       │   ├── DefaultColorPalette.asset
│       │   ├── DefaultTypography.asset
│       │   ├── DefaultSpacing.asset
│       │   └── DefaultShapes.asset
│       │
│       ├── DarkTheme/
│       │   ├── DarkTheme.asset
│       │   └── DarkColorPalette.asset
│       │
│       ├── Prefabs/
│       │   ├── Display/
│       │   │   ├── UIText.prefab
│       │   │   ├── UIImage.prefab
│       │   │   ├── UIIcon.prefab
│       │   │   ├── UIDivider.prefab
│       │   │   └── UIBadge.prefab
│       │   ├── Input/
│       │   │   ├── UIButton_Filled.prefab
│       │   │   ├── UIButton_Outlined.prefab
│       │   │   ├── UIButton_Text.prefab
│       │   │   ├── UIButton_Icon.prefab
│       │   │   ├── UIToggle.prefab
│       │   │   ├── UISlider.prefab
│       │   │   ├── UIDropdown.prefab
│       │   │   ├── UIInputField.prefab
│       │   │   └── UICheckbox.prefab
│       │   ├── Containers/
│       │   │   ├── UIPanel.prefab
│       │   │   ├── UICard.prefab
│       │   │   ├── UIScrollView.prefab
│       │   │   ├── UIList.prefab
│       │   │   └── UITabContainer.prefab
│       │   ├── Layout/
│       │   │   ├── UIHorizontalLayout.prefab
│       │   │   ├── UIVerticalLayout.prefab
│       │   │   ├── UIGridLayout.prefab
│       │   │   └── UISpacer.prefab
│       │   ├── Navigation/
│       │   │   ├── UIHeader.prefab
│       │   │   ├── UIToolbar.prefab
│       │   │   ├── UIBottomBar.prefab
│       │   │   └── UITabBar.prefab
│       │   └── Overlay/
│       │       ├── UIModal.prefab
│       │       ├── UIDialog.prefab
│       │       └── UIToast.prefab
│       │
│       ├── Screens/
│       │   ├── FullscreenTemplate.prefab
│       │   ├── HeaderContentTemplate.prefab
│       │   ├── TabsTemplate.prefab
│       │   ├── SplitTemplate.prefab
│       │   ├── DrawerTemplate.prefab
│       │   └── ModalOverlayTemplate.prefab
│       │
│       ├── Transitions/
│       │   ├── FadeTransition.asset
│       │   ├── SlideLeftTransition.asset
│       │   ├── SlideRightTransition.asset
│       │   ├── SlideUpTransition.asset
│       │   ├── SlideDownTransition.asset
│       │   └── ScaleTransition.asset
│       │
│       ├── Sprites/
│       │   ├── rounded_rect_4.png
│       │   ├── rounded_rect_8.png
│       │   ├── rounded_rect_16.png
│       │   ├── circle.png
│       │   ├── shadow_soft.png
│       │   └── divider.png
│       │
│       └── Fonts/
│           └── (placeholder for TMP fonts)
│
└── Tests/
    ├── Runtime/
    │   ├── com.uacf.ui-framework.tests.runtime.asmdef
    │   ├── ThemeManagerTests.cs
    │   ├── TokenResolutionTests.cs
    │   ├── StyleResolverTests.cs
    │   ├── NavigatorTests.cs
    │   └── ComponentBindingTests.cs
    │
    └── Editor/
        ├── com.uacf.ui-framework.tests.editor.asmdef
        ├── UIBuilderServiceTests.cs
        ├── UIThemeServiceTests.cs
        ├── PrefabFactoryTests.cs
        └── UIHandlerTests.cs
```

### 2.2 Диаграмма зависимостей

```
┌─────────────────────────────────────────────────────────────┐
│                    AI Agent (curl)                           │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                   UACF Server                               │
│  ┌──────────────────────────────────────┐                   │
│  │  UI Handlers (Editor)                │                   │
│  │  ├── UIScreenHandler                 │                   │
│  │  ├── UIElementHandler                │                   │
│  │  ├── UIThemeHandler                  │                   │
│  │  └── UITokenHandler                  │                   │
│  └──────────────┬───────────────────────┘                   │
│                 │                                           │
│  ┌──────────────▼───────────────────────┐                   │
│  │  UI Services (Editor)                │                   │
│  │  ├── UIBuilderService                │                   │
│  │  ├── UIThemeService                  │                   │
│  │  └── UITokenService                  │                   │
│  └──────────────┬───────────────────────┘                   │
└─────────────────┼───────────────────────────────────────────┘
                  │ uses
                  ▼
┌─────────────────────────────────────────────────────────────┐
│              Runtime (com.uacf.ui-framework)                │
│                                                             │
│  ┌──────────┐  ┌──────────┐  ┌────────────┐               │
│  │  Tokens  │  │  Styles  │  │  Theming   │               │
│  │ ┌──────┐ │  │ ┌──────┐ │  │ ┌────────┐ │               │
│  │ │Color │ │  │ │UIStyl│ │  │ │ThemeMgr│ │               │
│  │ │Palette│ │  │ │e     │ │  │ │        │ │               │
│  │ └──────┘ │  │ └──────┘ │  │ └────┬───┘ │               │
│  │ ┌──────┐ │  │ ┌──────┐ │  │      │     │               │
│  │ │Typo  │ │  │ │Style │ │  │ ┌────▼───┐ │               │
│  │ │graphy│ │◄─┤ │Resolv│ │◄─┤ │ThemeApp│ │               │
│  │ └──────┘ │  │ │er    │ │  │ │lier    │ │               │
│  │ ┌──────┐ │  │ └──────┘ │  │ └────────┘ │               │
│  │ │Theme │ │  │ ┌──────┐ │  └────────────┘               │
│  │ │      │─┤  │ │Style │ │                                │
│  │ └──────┘ │  │ │Bind  │ │                                │
│  └──────────┘  │ └──────┘ │                                │
│                └──────────┘                                │
│                     │                                      │
│                     ▼                                      │
│  ┌──────────────────────────────────────┐                  │
│  │          Components                  │                  │
│  │  ┌────────┐ ┌────────┐ ┌────────┐   │                  │
│  │  │Display │ │Input   │ │Layout  │   │                  │
│  │  │UIText  │ │UIButton│ │UIVert  │   │                  │
│  │  │UIImage │ │UIToggle│ │UIHoriz │   │                  │
│  │  │UIIcon  │ │UIInput │ │UIGrid  │   │                  │
│  │  └────────┘ └────────┘ └────────┘   │                  │
│  │  ┌────────┐ ┌────────┐ ┌────────┐   │                  │
│  │  │Contain │ │Navigat │ │Overlay │   │                  │
│  │  │UIPanel │ │UIHeader│ │UIModal │   │                  │
│  │  │UICard  │ │UITabBar│ │UIToast │   │                  │
│  │  └────────┘ └────────┘ └────────┘   │                  │
│  └──────────────────────────────────────┘                  │
│                     │                                      │
│                     ▼                                      │
│  ┌──────────────────────────────────────┐                  │
│  │  Screens + Navigation                │                  │
│  │  UIScreen → UINavigator → Transitions│                  │
│  └──────────────────────────────────────┘                  │
│                                                            │
│                Unity UI (uGUI) + TextMeshPro               │
└────────────────────────────────────────────────────────────┘
```

---

## 3. Design Tokens

### 3.1 ColorPalette.cs

```
ScriptableObject — централизованное хранилище цветов.

[CreateAssetMenu(menuName = "UACF UI/Tokens/Color Palette")]
class ColorPalette : ScriptableObject

Семантические цвета (фиксированные поля):
- Color primary            — основной цвет бренда
- Color primaryVariant     — вариация primary (темнее/светлее)
- Color secondary          — вторичный акцентный цвет
- Color secondaryVariant   — вариация secondary
- Color accent             — дополнительный акцент
- Color background         — фон приложения
- Color surface            — фон карточек/панелей
- Color surfaceVariant     — вариация surface
- Color error              — цвет ошибок
- Color warning            — цвет предупреждений
- Color success            — цвет успешных действий
- Color info               — цвет информационных элементов

Текстовые цвета (on-colors):
- Color onPrimary          — текст поверх primary
- Color onSecondary        — текст поверх secondary
- Color onBackground       — основной текст
- Color onBackgroundSecondary — вторичный текст (серый)
- Color onSurface          — текст на surface
- Color onError            — текст на error

Утилитарные:
- Color divider            — цвет разделителей
- Color disabled           — цвет неактивных элементов
- Color overlay            — полупрозрачный фон для модалок
- Color shadow             — цвет тени

Кастомные цвета:
- List<NamedColor> customColors
  где NamedColor = { string key, Color color }

Методы:
- Color GetColor(string key)
    — сначала ищет в семантических (через reflection/switch)
    — затем в customColors
    — если не найден → Color.magenta + warning в лог

- bool TryGetColor(string key, out Color color)
- void SetColor(string key, Color color)
- List<string> GetAllColorKeys()
- Color GetColorWithAlpha(string key, float alpha)

Валидация (Editor):
- Все семантические цвета должны быть ненулевые
- customColors не должны иметь дублирующихся key
- Предупреждение если контраст onPrimary/primary < 4.5:1 (WCAG)
```

**Структура NamedColor:**
```
[Serializable]
struct NamedColor
{
    string key;        // уникальный идентификатор ("gold", "healthBar", "xpColor")
    Color color;
    string description; // опциональное описание для UI
}
```

---

### 3.2 TypographySet.cs / TypographyPreset.cs

```
TypographyPreset — один пресет текста:

[Serializable]
class TypographyPreset
{
    string key;                    // "h1", "h2", "body", "caption"
    TMP_FontAsset font;           // ссылка на шрифт
    FontStyles fontStyle;         // Normal, Bold, Italic
    float fontSize;               // размер в pt
    float lineSpacing;            // межстрочный интервал (%)
    float characterSpacing;       // межбуквенный интервал
    float paragraphSpacing;       // межпараграфный интервал
    TextAlignmentOptions alignment; // выравнивание по умолчанию
    bool autoSize;                // авторазмер
    float autoSizeMin;            // минимум при авторазмере
    float autoSizeMax;            // максимум при авторазмере
    TextOverflowModes overflow;   // обработка переполнения
}

TypographySet — коллекция пресетов:

[CreateAssetMenu(menuName = "UACF UI/Tokens/Typography Set")]
class TypographySet : ScriptableObject

Стандартные пресеты (фиксированные поля):
- TypographyPreset h1            — заголовок 1 уровня (32pt bold)
- TypographyPreset h2            — заголовок 2 уровня (24pt bold)
- TypographyPreset h3            — заголовок 3 уровня (20pt semibold)
- TypographyPreset h4            — заголовок 4 уровня (18pt semibold)
- TypographyPreset subtitle1     — подзаголовок (16pt medium)
- TypographyPreset subtitle2     — подзаголовок мелкий (14pt medium)
- TypographyPreset body1         — основной текст (16pt regular)
- TypographyPreset body2         — малый текст (14pt regular)
- TypographyPreset caption       — подпись (12pt regular)
- TypographyPreset overline      — надпись (10pt uppercase medium)
- TypographyPreset button        — текст кнопки (14pt semibold uppercase)

Кастомные:
- List<TypographyPreset> customPresets

Методы:
- TypographyPreset GetPreset(string key)
- void ApplyTo(TMP_Text textComponent, string presetKey)
- List<string> GetAllPresetKeys()
```

---

### 3.3 SpacingScale.cs

```
[CreateAssetMenu(menuName = "UACF UI/Tokens/Spacing Scale")]
class SpacingScale : ScriptableObject

Стандартная шкала:
- float none = 0
- float xxs  = 2
- float xs   = 4
- float sm   = 8
- float md   = 16
- float lg   = 24
- float xl   = 32
- float xxl  = 48
- float xxxl = 64

Кастомные:
- List<NamedFloat> customSpacing

Методы:
- float GetSpacing(string key)
- RectOffset GetPadding(string key)
    — возвращает RectOffset(val, val, val, val)
- RectOffset GetPadding(string horizontal, string vertical)
    — возвращает RectOffset(h, h, v, v)
- RectOffset GetPadding(string left, string right, string top, string bottom)
```

---

### 3.4 ShapeSet.cs / ShapePreset.cs

```
[Serializable]
class ShapePreset
{
    string key;
    float borderRadius;        // в пикселях
    float borderWidth;         // толщина обводки (0 = нет)
    string borderColorToken;   // ключ цвета из ColorPalette ("divider", "primary")
}

[CreateAssetMenu(menuName = "UACF UI/Tokens/Shape Set")]
class ShapeSet : ScriptableObject

Пресеты:
- ShapePreset none       = { radius: 0 }
- ShapePreset small      = { radius: 4 }
- ShapePreset medium     = { radius: 8 }
- ShapePreset large      = { radius: 16 }
- ShapePreset extraLarge = { radius: 24 }
- ShapePreset circle     = { radius: 9999 }  // "pill" shape

Реализация скруглений:
- Используем 9-slice sprite с разными радиусами
- Sprite выбирается автоматически по ShapePreset
- Альтернатива: процедурная генерация через UI.Image.pixelsPerUnitMultiplier
  и sprite с большим radius, масштабированным через fillAmount
```

---

### 3.5 ElevationSet.cs / ElevationPreset.cs

```
[Serializable]
class ElevationPreset
{
    string key;
    float offsetX;
    float offsetY;
    float blur;
    float spread;
    string shadowColorToken;   // ключ цвета из ColorPalette
    float shadowAlpha;         // прозрачность тени (0-1)
}

[CreateAssetMenu(menuName = "UACF UI/Tokens/Elevation Set")]
class ElevationSet : ScriptableObject

Пресеты:
- ElevationPreset none  = { offsetY: 0, blur: 0 }
- ElevationPreset low   = { offsetY: 1, blur: 3, alpha: 0.12 }
- ElevationPreset mid   = { offsetY: 3, blur: 6, alpha: 0.16 }
- ElevationPreset high  = { offsetY: 6, blur: 12, alpha: 0.24 }

Реализация тени:
- Дочерний GameObject с Image (shadow sprite, blur через sprite assets)
- Позиция/размер высчитываются из ElevationPreset
- Shadow sprite — размытый прямоугольник (pre-rendered в Resources/Sprites/)
```

---

### 3.6 Theme.cs

```
[CreateAssetMenu(menuName = "UACF UI/Theme")]
class Theme : ScriptableObject

Поля:
- string themeName;              // "Light", "Dark", "Custom"
- string themeId;                // уникальный ID (GUID)
- ColorPalette colorPalette;     // ссылка на палитру
- TypographySet typography;      // ссылка на типографику
- SpacingScale spacing;          // ссылка на отступы
- ShapeSet shapes;               // ссылка на формы
- ElevationSet elevations;       // ссылка на тени
- IconSet icons;                 // ссылка на иконки (опционально)

Вспомогательные стили для компонентов:
- StyleSheet defaultStyles;      // стили по умолчанию для всех компонентов

Методы:
- Color GetColor(string key)         → colorPalette.GetColor(key)
- TypographyPreset GetTypography(string key) → typography.GetPreset(key)
- float GetSpacing(string key)       → spacing.GetSpacing(key)
- ShapePreset GetShape(string key)   → shapes.GetPreset(key)
- ElevationPreset GetElevation(string key) → elevations.GetPreset(key)
- UIStyle GetDefaultStyle(string componentType) → defaultStyles.GetStyle(componentType)

Валидация:
- Все ссылки не null
- Все необходимые токены присутствуют
```

---

## 4. Style System

### 4.1 UIStyleState.cs

```
[Serializable]
class UIStyleState
{
    // Фон
    OptionalToken<string> backgroundColorToken;    // ключ цвета ("primary", "surface")
    OptionalValue<Sprite> backgroundSprite;
    OptionalValue<Image.Type> backgroundImageType;

    // Текст
    OptionalToken<string> textColorToken;
    OptionalToken<string> typographyToken;         // ключ пресета ("body1", "button")

    // Форма
    OptionalToken<string> shapeToken;              // ключ ShapePreset ("medium")

    // Тень
    OptionalToken<string> elevationToken;          // ключ ElevationPreset ("low")

    // Обводка
    OptionalToken<string> borderColorToken;
    OptionalValue<float> borderWidth;

    // Отступы
    OptionalToken<string> paddingToken;            // ключ SpacingScale ("md")
    OptionalValue<RectOffset> paddingOverride;     // прямое значение

    // Прозрачность
    OptionalValue<float> opacity;

    // Размеры
    OptionalValue<Vector2> preferredSize;
    OptionalValue<Vector2> minSize;
    OptionalValue<Vector2> maxSize;

    // Трансформация
    OptionalValue<Vector3> scale;
    OptionalValue<Vector3> rotation;
}

// OptionalToken<T> — обёртка с флагом "задано ли значение":
[Serializable]
struct OptionalToken<T>
{
    bool hasValue;
    T value;
}

[Serializable]
struct OptionalValue<T>
{
    bool hasValue;
    T value;
}
```

**Почему OptionalToken/OptionalValue:**
- Позволяет отличить "не задано" от "задано как default"
- Стиль-наследник переопределяет только те свойства, где hasValue=true
- Остальные берутся из parent-стиля или default

---

### 4.2 UIStyle.cs

```
[CreateAssetMenu(menuName = "UACF UI/Style")]
class UIStyle : ScriptableObject

Поля:
- string styleKey;                    // уникальный ключ ("button_filled", "card_default")
- UIStyle parent;                     // наследование (null = корневой стиль)
- UIStyleState normal;                // состояние по умолчанию
- UIStyleState hovered;               // при наведении
- UIStyleState pressed;               // при нажатии
- UIStyleState disabled;              // когда interactable=false
- UIStyleState focused;               // когда в фокусе
- UIStyleState selected;              // когда выбран (toggle)
- float transitionDuration = 0.15f;   // длительность перехода между состояниями
- AnimationCurve transitionCurve;     // кривая перехода

Методы:
- UIStyleState ResolveState(UIComponentState state)
    — берёт соответствующее состояние
    — мержит с parent (если свойство не задано — берём из parent.normal)
    — если и в parent не задано — возвращает пустое (компонент использует свой default)

- UIStyleState GetMergedNormal()
    — рекурсивно мержит normal с parent.normal

Мерж-алгоритм (для каждого свойства):
    1. Если в текущем стиле hasValue=true → берём его
    2. Иначе если parent != null → берём parent.ResolveState(state)
    3. Иначе → свойство не задано, компонент использует fallback
```

---

### 4.3 StyleSheet.cs

```
[CreateAssetMenu(menuName = "UACF UI/Style Sheet")]
class StyleSheet : ScriptableObject

Поля:
- List<UIStyle> styles;

Методы:
- UIStyle GetStyle(string key)
- bool TryGetStyle(string key, out UIStyle style)
- List<string> GetAllKeys()
- void AddStyle(UIStyle style)
- void RemoveStyle(string key)
```

---

### 4.4 StyleBinding.cs

```
Компонент, привязывающий UI-элемент к стилю.
Добавляется на каждый GameObject с UI-компонентом.

[RequireComponent(typeof(UIComponentBase))]
class StyleBinding : MonoBehaviour

Поля:
- UIStyle style;                        // ссылка на стиль (ScriptableObject)
- string styleKey;                      // альтернатива — ключ для поиска в Theme.defaultStyles
- bool useThemeDefault = true;          // если style == null, использовать default из темы

Поведение:
- OnEnable() → подписка на ThemeManager.OnThemeChanged
- При смене темы / при инициализации:
    1. Определить активный стиль:
       style ?? Theme.GetDefaultStyle(componentType)
    2. Получить resolved UIStyleState для текущего состояния
    3. Применить к компоненту через UIComponentBase.ApplyStyle(state)

- При смене состояния компонента (hover/press/etc):
    1. Получить UIStyleState для нового состояния
    2. Запустить tween-переход от текущих значений к новым
```

---

### 4.5 StyleResolver.cs

```
Статический сервис для разрешения значений стиля.

static class StyleResolver

Методы:
- static Color ResolveColor(string token, Theme theme)
    → theme.GetColor(token)

- static TypographyPreset ResolveTypography(string token, Theme theme)
    → theme.GetTypography(token)

- static float ResolveSpacing(string token, Theme theme)
    → theme.GetSpacing(token)

- static UIStyleState MergeStates(UIStyleState child, UIStyleState parent)
    → для каждого свойства: child.hasValue ? child : parent

- static ResolvedStyle Resolve(UIStyle style, UIComponentState state, Theme theme)
    → полностью разрешённый набор конкретных значений (Color, float, etc.)
    из токенов + тема
```

**ResolvedStyle:**
```
struct ResolvedStyle
{
    Color backgroundColor;
    Color textColor;
    TypographyPreset typography;
    float borderRadius;
    Color borderColor;
    float borderWidth;
    RectOffset padding;
    float opacity;
    ElevationPreset elevation;
    bool hasBackground;
    bool hasText;
    bool hasBorder;
    bool hasElevation;
}
```

---

## 5. Component Library

### 5.1 Базовые классы

#### UIComponentBase.cs

```
Базовый класс для всех UI-компонентов фреймворка.
Наследуется от MonoBehaviour.

[RequireComponent(typeof(RectTransform))]
abstract class UIComponentBase : MonoBehaviour, IThemeable

Поля (сериализованные):
- string componentId;           // уникальный ID для UACF API
- StyleBinding styleBinding;    // ссылка на привязку стиля

Свойства:
- RectTransform RectTransform { get; }
- string ComponentType { get; }     // "UIButton", "UIText" etc.
- UIComponentState CurrentState { get; }

Методы:
- virtual void ApplyStyle(ResolvedStyle style)
    — переопределяется в каждом компоненте
    — применяет цвета, шрифты, отступы к дочерним Unity UI элементам

- virtual void OnThemeChanged(Theme newTheme)
    — вызывается ThemeManager при смене темы
    — по умолчанию вызывает ReapplyStyle()

- void ReapplyStyle()
    — пересчитывает resolved style и применяет

- virtual Dictionary<string, object> Serialize()
    — возвращает все публичные свойства как dict для UACF API

- virtual void Deserialize(Dictionary<string, object> data)
    — устанавливает свойства из dict

Жизненный цикл:
- Awake() → кэшировать компоненты, сгенерировать componentId если пусто
- OnEnable() → подписка на ThemeManager, ApplyStyle
- OnDisable() → отписка от ThemeManager
- OnValidate() → ApplyStyle в Editor
```

---

#### UIInteractableBase.cs

```
Базовый класс для интерактивных компонентов (кнопки, поля ввода, etc.).
Наследуется от UIComponentBase.

abstract class UIInteractableBase : UIComponentBase,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler

Поля:
- bool interactable = true;
- UIComponentState _currentState = UIComponentState.Normal;

События:
- event Action<UIComponentState> OnStateChanged;

Состояния:
enum UIComponentState
{
    Normal,
    Hovered,
    Pressed,
    Disabled,
    Focused,
    Selected
}

Обработка переходов:
- OnPointerEnter → state = Hovered → transition to hovered style
- OnPointerExit  → state = Normal  → transition to normal style
- OnPointerDown  → state = Pressed → transition to pressed style
- OnPointerUp    → state = Hovered (если указатель ещё над элементом)
- interactable = false → state = Disabled → transition to disabled style

Transition:
- При смене состояния — tween между текущим ResolvedStyle и новым
- Длительность и кривая берутся из UIStyle.transitionDuration / transitionCurve
- Tweening через UITween (встроенный, без DOTween)
```

---

#### IThemeable.cs

```
interface IThemeable
{
    void OnThemeChanged(Theme newTheme);
    void ApplyStyle(ResolvedStyle style);
    string ComponentType { get; }
}
```

---

### 5.2 Display Components

#### UIText.cs

```
Обёртка над TextMeshProUGUI.

class UIText : UIComponentBase

Поля:
- TMP_Text _tmpText;             // внутренний компонент
- string text;                    // текст содержимого
- string typographyToken = "body1";  // ключ пресета из TypographySet
- string colorToken = "onBackground"; // ключ цвета из ColorPalette
- TextAlignmentOptions alignment;     // переопределение выравнивания
- bool autoSize = false;

Методы:
- void SetText(string text)
- void SetTypography(string token)
- void SetColor(string colorToken)

ApplyStyle:
- Если style.hasText → применить textColor
- Если style.typography != null → применить font, size, style etc.
- Иначе → использовать typographyToken и colorToken

Структура prefab:
    UIText (GameObject)
    ├── UIText (component)
    ├── StyleBinding (component)
    └── TextMeshProUGUI (component)
```

---

#### UIImage.cs

```
Обёртка над Unity Image с поддержкой токенов.

class UIImage : UIComponentBase

Поля:
- Image _image;
- Sprite sprite;
- string colorToken = "primary";
- Image.Type imageType = Simple;
- bool preserveAspect = true;

Методы:
- void SetSprite(Sprite sprite)
- void SetColor(string colorToken)
- void SetNativeSize()
```

---

#### UIIcon.cs

```
Компонент иконки.

class UIIcon : UIComponentBase

Поля:
- Image _image;
- Sprite iconSprite;
- string iconKey;              // ключ из IconSet
- string colorToken = "onBackground";
- float size = 24;             // размер иконки (width = height = size)

Методы:
- void SetIcon(string iconKey)
- void SetIcon(Sprite sprite)
- void SetColor(string colorToken)
- void SetSize(float size)
```

---

#### UIDivider.cs

```
Горизонтальный/вертикальный разделитель.

class UIDivider : UIComponentBase

Поля:
- Image _line;
- string colorToken = "divider";
- float thickness = 1;
- DividerDirection direction = Horizontal;
- string marginToken = "md";     // отступ сверху/снизу (или слева/справа)

enum DividerDirection { Horizontal, Vertical }
```

---

#### UIBadge.cs

```
Бейдж/метка (число поверх иконки/кнопки).

class UIBadge : UIComponentBase

Поля:
- Image _background;
- TMP_Text _label;
- string backgroundColorToken = "error";
- string textColorToken = "onError";
- int value;
- int maxDisplay = 99;     // при value > max показывает "99+"
- bool showZero = false;

Методы:
- void SetValue(int value)
- void Show() / Hide()
```

---

### 5.3 Input Components

#### UIButton.cs

```
Основной компонент кнопки.

class UIButton : UIInteractableBase

Поля:
- Image _background;
- TMP_Text _label;
- Image _icon;                    // опциональная иконка
- UIButtonVariant variant = Filled;
- string labelText;
- string iconKey;
- string typographyToken = "button";
- UnityEvent onClick;

Варианты:
enum UIButtonVariant
{
    Filled,        // залитый фон (primary)
    Outlined,      // обводка, без заливки
    Text,          // только текст, без фона
    Tonal          // приглушённый фон (primaryVariant с alpha)
}

Методы:
- void SetLabel(string text)
- void SetIcon(string iconKey)
- void SetVariant(UIButtonVariant variant)
- void SetEnabled(bool enabled)

ApplyStyle по вариантам (default стили из Theme):
- Filled:   bg=primary, text=onPrimary, shape=medium
- Outlined: bg=transparent, border=primary, text=primary, shape=medium
- Text:     bg=transparent, text=primary, no shape
- Tonal:    bg=primaryVariant(alpha:0.15), text=primary, shape=medium

Структура prefab (UIButton_Filled.prefab):
    UIButton (GameObject)
    ├── UIButton (component)
    ├── StyleBinding (component)
    ├── Image (background)
    ├── HorizontalLayoutGroup (для icon + text)
    ├── Icon (GameObject, inactive по умолчанию)
    │   └── Image
    └── Label (GameObject)
        └── TextMeshProUGUI
```

---

#### UIIconButton.cs

```
Кнопка-иконка (круглая/квадратная, без текста).

class UIIconButton : UIInteractableBase

Поля:
- Image _background;
- Image _icon;
- string iconKey;
- string colorToken = "primary";
- float size = 48;
- UIIconButtonShape shape = Circle;

enum UIIconButtonShape { Circle, Square, Rounded }
```

---

#### UIToggle.cs

```
Переключатель.

class UIToggle : UIInteractableBase

Поля:
- Image _track;
- Image _thumb;
- bool isOn;
- string onColorToken = "primary";
- string offColorToken = "disabled";
- string thumbColorToken = "surface";
- UnityEvent<bool> onValueChanged;

Методы:
- void SetOn(bool value)
- bool IsOn()

Анимация:
- При переключении — thumb двигается (tween position)
- Track меняет цвет (tween color)
```

---

#### UISlider.cs

```
Ползунок.

class UISlider : UIInteractableBase

Поля:
- Slider _slider;
- Image _trackBackground;
- Image _trackFill;
- Image _thumb;
- float value;
- float minValue = 0;
- float maxValue = 1;
- bool wholeNumbers = false;
- string fillColorToken = "primary";
- string trackColorToken = "disabled";
- UnityEvent<float> onValueChanged;
- UIText _valueLabel;       // опциональная метка значения
- string valueFormat = "F1"; // формат отображения

Методы:
- void SetValue(float value)
- void SetRange(float min, float max)
```

---

#### UIDropdown.cs

```
Выпадающий список.

class UIDropdown : UIInteractableBase

Поля:
- Image _background;
- TMP_Text _selectedLabel;
- Image _arrow;
- RectTransform _dropdownPanel;
- UIVerticalLayout _optionsList;
- List<string> options;
- int selectedIndex;
- string surfaceColorToken = "surface";
- UnityEvent<int> onSelectionChanged;

Методы:
- void SetOptions(List<string> options)
- void SetSelected(int index)
- void Open() / Close()
```

---

#### UIInputField.cs

```
Поле ввода текста.

class UIInputField : UIInteractableBase

Поля:
- TMP_InputField _inputField;
- Image _background;
- TMP_Text _placeholder;
- TMP_Text _inputText;
- UIText _label;              // метка над полем
- UIText _helperText;         // подсказка под полем
- UIText _errorText;          // текст ошибки
- string labelText;
- string placeholderText;
- string helperText;
- string errorMessage;
- bool hasError = false;
- UIInputFieldVariant variant = Outlined;
- string focusColorToken = "primary";
- string errorColorToken = "error";
- TMP_InputField.ContentType contentType;
- int characterLimit;
- UnityEvent<string> onValueChanged;
- UnityEvent<string> onEndEdit;

Варианты:
enum UIInputFieldVariant
{
    Outlined,      // обводка вокруг поля
    Filled,        // заливка фона
    Underlined     // линия снизу
}

Методы:
- void SetText(string text)
- string GetText()
- void SetError(string errorMessage)
- void ClearError()
- void SetLabel(string label)
- void SetPlaceholder(string placeholder)

Состояния:
- Normal → рамка divider цвет
- Focused → рамка primary цвет, label поднимается вверх (анимация)
- Error → рамка error цвет, показывается errorText
- Disabled → рамка disabled, background disabled
```

---

#### UICheckbox.cs

```
Чекбокс.

class UICheckbox : UIInteractableBase

Поля:
- Image _box;
- Image _checkmark;
- UIText _label;
- bool isChecked;
- string checkedColorToken = "primary";
- string uncheckedColorToken = "onBackground";
- UnityEvent<bool> onValueChanged;

Методы:
- void SetChecked(bool value)
- bool IsChecked()
```

---

### 5.4 Container Components

#### UIPanel.cs

```
Базовый контейнер с фоном.

class UIPanel : UIComponentBase

Поля:
- Image _background;
- string backgroundColorToken = "surface";
- string shapeToken = "medium";
- string elevationToken = "none";
- string paddingToken = "md";
- RectOffset paddingOverride;    // null = использовать токен

ApplyStyle:
- Фон из backgroundColorToken
- Скруглённый sprite из shapeToken
- Тень из elevationToken
- Padding из paddingToken → применяется к дочернему LayoutGroup (если есть)

Структура prefab:
    UIPanel (GameObject)
    ├── UIPanel (component)
    ├── StyleBinding (component)
    ├── Image (background, 9-slice rounded rect)
    ├── Shadow (GameObject, опционально)
    │   └── Image (shadow sprite)
    └── Content (GameObject)
        └── (дочерние элементы добавляются сюда)
```

---

#### UICard.cs

```
Карточка — panel с заголовком, содержимым и actions.

class UICard : UIComponentBase

Поля:
- UIPanel _panel;
- RectTransform _headerSlot;
- RectTransform _contentSlot;
- RectTransform _actionsSlot;
- UIText _title;
- UIText _subtitle;
- UIImage _headerImage;          // опциональное изображение сверху
- string elevationToken = "low";
- bool showDividers = true;

Методы:
- void SetTitle(string title)
- void SetSubtitle(string subtitle)
- void SetHeaderImage(Sprite image)
- RectTransform GetContentSlot()    // для добавления контента
- RectTransform GetActionsSlot()    // для добавления кнопок

Структура prefab:
    UICard (GameObject)
    ├── UICard (component)
    ├── UIPanel (component) — фон
    ├── VerticalLayoutGroup
    ├── HeaderImage (GameObject, inactive)
    │   └── Image
    ├── Header (GameObject)
    │   ├── VerticalLayoutGroup
    │   ├── Title (UIText)
    │   └── Subtitle (UIText)
    ├── Divider (UIDivider)
    ├── Content (GameObject)         ← контент добавляется сюда
    ├── Divider (UIDivider)
    └── Actions (GameObject)         ← кнопки добавляются сюда
        └── HorizontalLayoutGroup (right-aligned)
```

---

#### UIScrollView.cs

```
Прокручиваемый контейнер.

class UIScrollView : UIComponentBase

Поля:
- ScrollRect _scrollRect;
- RectTransform _content;
- Image _background;
- Scrollbar _verticalScrollbar;
- Scrollbar _horizontalScrollbar;
- bool showVerticalScrollbar = true;
- bool showHorizontalScrollbar = false;
- string scrollbarColorToken = "disabled";
- ScrollRect.MovementType movementType = Elastic;
- float elasticity = 0.1f;

Методы:
- RectTransform GetContent()
- void ScrollTo(float normalizedPosition)
- void ScrollToTop() / ScrollToBottom()
```

---

#### UIList.cs

```
Список элементов (виртуализированный для больших данных — опционально).

class UIList : UIComponentBase

Поля:
- UIScrollView _scrollView;
- UIVerticalLayout _layout;
- UIListItem _itemTemplate;           // префаб элемента списка
- bool showDividers = true;
- string dividerColorToken = "divider";
- UnityEvent<int> onItemClicked;

Методы:
- void SetItems<T>(List<T> data, Action<UIListItem, T> binder)
- void AddItem(Action<UIListItem> configure)
- void RemoveItem(int index)
- void Clear()
- int Count { get; }
```

---

#### UIListItem.cs

```
Элемент списка.

class UIListItem : UIInteractableBase

Поля:
- Image _background;
- UIIcon _leadingIcon;
- UIIcon _trailingIcon;
- UIText _title;
- UIText _subtitle;
- int index;

Структура prefab:
    UIListItem (GameObject)
    ├── HorizontalLayoutGroup
    ├── LeadingIcon (UIIcon, опционально)
    ├── TextContent (GameObject)
    │   ├── VerticalLayoutGroup
    │   ├── Title (UIText, body1)
    │   └── Subtitle (UIText, caption)
    └── TrailingIcon (UIIcon, опционально)
```

---

#### UITabContainer.cs

```
Контейнер с вкладками.

class UITabContainer : UIComponentBase

Поля:
- UITabBar _tabBar;
- List<RectTransform> _tabPages;
- int selectedIndex = 0;
- TransitionPreset _tabTransition;
- UnityEvent<int> onTabChanged;

Методы:
- void AddTab(string label, string iconKey, RectTransform content)
- void SelectTab(int index)
- void RemoveTab(int index)
```

---

#### UIAccordion.cs

```
Раскрывающийся список секций.

class UIAccordion : UIComponentBase

Поля:
- UIVerticalLayout _layout;
- List<UIAccordionSection> _sections;
- bool allowMultipleOpen = false;

Методы:
- UIAccordionSection AddSection(string title, RectTransform content)
- void ExpandSection(int index)
- void CollapseSection(int index)
- void ToggleSection(int index)

UIAccordionSection:
- UIText _title;
- UIIcon _expandIcon;
- RectTransform _content;
- bool isExpanded;
- Анимация раскрытия через UITween (высота content: 0 → auto)
```

---

### 5.5 Layout Components

#### UIVerticalLayout.cs

```
Вертикальный layout — обёртка над VerticalLayoutGroup с токенами.

class UIVerticalLayout : UIComponentBase

Поля:
- VerticalLayoutGroup _layoutGroup;
- ContentSizeFitter _sizeFitter;
- string spacingToken = "md";         // расстояние между элементами
- string paddingToken = "none";       // внутренние отступы
- RectOffset paddingOverride;         // ручные отступы (приоритет над токеном)
- float spacingOverride = -1;         // ручной spacing (-1 = использовать токен)
- TextAnchor childAlignment = UpperLeft;
- bool childForceExpandWidth = true;
- bool childForceExpandHeight = false;
- bool childControlWidth = true;
- bool childControlHeight = true;
- bool reverseArrangement = false;

Методы:
- void SetSpacing(string token)
- void SetSpacing(float value)
- void SetPadding(string token)
- void SetPadding(RectOffset padding)
- void SetChildAlignment(TextAnchor alignment)

ApplyStyle:
- spacing → _layoutGroup.spacing = theme.GetSpacing(spacingToken)
- padding → _layoutGroup.padding = theme.GetSpacing(paddingToken) для всех сторон
  или paddingOverride если задан

Структура prefab:
    UIVerticalLayout (GameObject)
    ├── UIVerticalLayout (component)
    ├── StyleBinding (component)
    ├── RectTransform (stretch to parent)
    ├── VerticalLayoutGroup (component)
    └── ContentSizeFitter (component, опционально)
```

---

#### UIHorizontalLayout.cs

```
Горизонтальный layout — обёртка над HorizontalLayoutGroup с токенами.

class UIHorizontalLayout : UIComponentBase

Поля:
- HorizontalLayoutGroup _layoutGroup;
- ContentSizeFitter _sizeFitter;
- string spacingToken = "md";
- string paddingToken = "none";
- RectOffset paddingOverride;
- float spacingOverride = -1;
- TextAnchor childAlignment = MiddleLeft;
- bool childForceExpandWidth = false;
- bool childForceExpandHeight = true;
- bool childControlWidth = true;
- bool childControlHeight = true;
- bool reverseArrangement = false;

Методы:
- void SetSpacing(string token)
- void SetSpacing(float value)
- void SetPadding(string token)
- void SetPadding(RectOffset padding)
- void SetChildAlignment(TextAnchor alignment)

ApplyStyle:
- аналогично UIVerticalLayout, только HorizontalLayoutGroup

Структура prefab:
    UIHorizontalLayout (GameObject)
    ├── UIHorizontalLayout (component)
    ├── StyleBinding (component)
    ├── RectTransform (stretch to parent)
    ├── HorizontalLayoutGroup (component)
    └── ContentSizeFitter (component, опционально)
```

---

#### UIGridLayout.cs

```
Grid layout — обёртка над GridLayoutGroup с токенами.

class UIGridLayout : UIComponentBase

Поля:
- GridLayoutGroup _layoutGroup;
- ContentSizeFitter _sizeFitter;
- Vector2 cellSize = new Vector2(100, 100);
- string spacingToken = "md";               // расстояние между ячейками
- Vector2 spacingOverride = new Vector2(-1, -1); // -1 = токен
- string paddingToken = "none";
- RectOffset paddingOverride;
- GridLayoutGroup.Corner startCorner = UpperLeft;
- GridLayoutGroup.Axis startAxis = Horizontal;
- TextAnchor childAlignment = UpperLeft;
- GridLayoutGroup.Constraint constraint = Flexible;
- int constraintCount = 0;

Методы:
- void SetCellSize(Vector2 size)
- void SetCellSize(float width, float height)
- void SetSpacing(string token)
- void SetSpacing(Vector2 spacing)
- void SetPadding(string token)
- void SetPadding(RectOffset padding)
- void SetColumns(int count)
    → constraint = FixedColumnCount, constraintCount = count
- void SetRows(int count)
    → constraint = FixedRowCount, constraintCount = count
- void SetFlexible()
    → constraint = Flexible

ApplyStyle:
- spacing → float s = theme.GetSpacing(spacingToken);
             _layoutGroup.spacing = new Vector2(s, s)
- или spacingOverride если задан
- padding → из paddingToken или paddingOverride

Структура prefab:
    UIGridLayout (GameObject)
    ├── UIGridLayout (component)
    ├── StyleBinding (component)
    ├── RectTransform (stretch to parent)
    ├── GridLayoutGroup (component)
    └── ContentSizeFitter (component, опционально)
```

---

#### UIStackLayout.cs

```
Stack layout — элементы друг поверх друга (для overlay, переходов).

class UIStackLayout : UIComponentBase

Поля:
- int activeIndex = 0;         // какой дочерний элемент видим
- bool showAll = false;        // показать все (для overlay-режима)
- TransitionPreset transition; // анимация при смене активного

Методы:
- void ShowChild(int index)
- void ShowChild(string name)
- void HideAll()
- void ShowAll()
- int ActiveIndex { get; }

Поведение:
- Все дочерние элементы anchored на stretch
- При ShowChild — текущий скрывается (анимация), новый показывается
- Если showAll — все видны, управляется порядком в иерархии (sibling index)
```

---

#### UISpacer.cs

```
Пустой элемент для добавления пространства в layout.

class UISpacer : UIComponentBase

Поля:
- string sizeToken = "md";       // размер из SpacingScale
- float sizeOverride = -1;       // ручной размер (-1 = токен)
- SpacerDirection direction = Auto;

enum SpacerDirection
{
    Auto,         // определяется parent layout
    Horizontal,
    Vertical,
    Expand        // LayoutElement.flexibleWidth/Height = 1 (занять всё свободное)
}

Поведение:
- Устанавливает LayoutElement.preferredWidth/Height = resolved spacing
- При direction = Expand → LayoutElement.flexibleWidth/Height = 1
```

---

#### UIExpandable.cs

```
Элемент, расширяющийся на всё свободное место в layout.

class UIExpandable : UIComponentBase

Поля:
- LayoutElement _layoutElement;
- float flexibleWidth = 1;
- float flexibleHeight = 0;

Поведение:
- Просто LayoutElement с flexibleWidth/Height
- Используется в horizontal/vertical layout для заполнения пустого пространства
```

---

#### UIResponsiveContainer.cs

```
Контейнер, адаптирующийся под размер экрана.

class UIResponsiveContainer : UIComponentBase

Поля:
- List<ResponsiveBreakpoint> breakpoints;
- RectTransform _container;

[Serializable]
class ResponsiveBreakpoint
{
    float minWidth;               // минимальная ширина экрана
    int columns;                  // количество колонок
    string paddingToken;          // отступы на этом разрешении
    string spacingToken;          // промежутки
    float maxContentWidth;        // максимальная ширина контента (0 = без ограничения)
}

Стандартные breakpoints:
- mobile:  minWidth=0,   columns=1, padding="md", maxWidth=0
- tablet:  minWidth=768, columns=2, padding="lg", maxWidth=720
- desktop: minWidth=1200, columns=3, padding="xl", maxWidth=1140

Поведение:
- В Update (или на resize) проверяет ширину RectTransform
- Переключает layout параметры согласно активному breakpoint
- Может переключать UIGridLayout.columns или заменять
  VerticalLayout на GridLayout
```

---

### 5.6 Navigation Components

#### UIHeader.cs

```
Шапка экрана.

class UIHeader : UIComponentBase

Поля:
- Image _background;
- UIText _title;
- UIIconButton _leftAction;       // кнопка "назад" или "меню"
- UIIconButton _rightAction;      // кнопка действия
- UIText _rightActionText;        // текстовая кнопка справа
- string backgroundColorToken = "surface";
- string titleTypography = "h3";
- string elevationToken = "low";
- float height = 56;

Методы:
- void SetTitle(string title)
- void SetLeftAction(string iconKey, Action callback)
- void SetRightAction(string iconKey, Action callback)
- void ShowBackButton(Action onBack)

Структура prefab:
    UIHeader (GameObject, height=56, anchor top-stretch)
    ├── UIHeader (component)
    ├── Image (background)
    ├── Shadow (elevation)
    ├── HorizontalLayoutGroup
    ├── LeftAction (UIIconButton, 48x48)
    ├── Title (UIText, flexibleWidth=1, centered)
    └── RightAction (UIIconButton, 48x48)
```

---

#### UIToolbar.cs

```
Панель инструментов с несколькими действиями.

class UIToolbar : UIComponentBase

Поля:
- Image _background;
- UIHorizontalLayout _actionsLayout;
- string backgroundColorToken = "surface";
- List<ToolbarAction> actions;

[Serializable]
class ToolbarAction
{
    string iconKey;
    string label;
    string id;
    bool isSelected;
}

Методы:
- void SetActions(List<ToolbarAction> actions)
- void SetSelected(string actionId)
```

---

#### UIBottomBar.cs

```
Нижняя навигационная панель.

class UIBottomBar : UIComponentBase

Поля:
- Image _background;
- UIHorizontalLayout _itemsLayout;
- List<BottomBarItem> items;
- int selectedIndex = 0;
- string backgroundColorToken = "surface";
- string selectedColorToken = "primary";
- string unselectedColorToken = "onBackgroundSecondary";
- string elevationToken = "high";
- UnityEvent<int> onItemSelected;

[Serializable]
class BottomBarItem
{
    string iconKey;
    string label;
    string id;
}

Методы:
- void SetItems(List<BottomBarItem> items)
- void SelectItem(int index)

Структура prefab:
    UIBottomBar (GameObject, height=64, anchor bottom-stretch)
    ├── UIBottomBar (component)
    ├── Image (background)
    ├── Shadow (elevation, сверху)
    └── HorizontalLayoutGroup (distribute evenly)
        ├── Item0 (GameObject)
        │   ├── VerticalLayoutGroup
        │   ├── UIIcon
        │   └── UIText (caption)
        ├── Item1...
        └── Item2...
```

---

#### UITabBar.cs

```
Панель вкладок.

class UITabBar : UIComponentBase

Поля:
- Image _background;
- UIHorizontalLayout _tabsLayout;
- Image _indicator;              // полоска-индикатор под выбранной вкладкой
- List<TabItem> tabs;
- int selectedIndex = 0;
- string indicatorColorToken = "primary";
- float indicatorHeight = 3;
- UnityEvent<int> onTabSelected;

Методы:
- void SetTabs(List<TabItem> tabs)
- void SelectTab(int index)
    — анимация перемещения indicator к выбранной вкладке

[Serializable]
class TabItem
{
    string label;
    string iconKey;       // опционально
}
```

---

#### UISidebar.cs

```
Боковая панель (drawer).

class UISidebar : UIComponentBase

Поля:
- RectTransform _panel;
- Image _background;
- UIOverlay _overlay;            // полупрозрачный фон при открытии
- float width = 300;
- SidebarSide side = Left;
- bool isOpen = false;
- TransitionPreset openTransition;  // slide transition
- string backgroundColorToken = "surface";

enum SidebarSide { Left, Right }

Методы:
- void Open()
- void Close()
- void Toggle()
- RectTransform GetContent()
```

---

### 5.7 Overlay Components

#### UIModal.cs

```
Модальное окно.

class UIModal : UIComponentBase

Поля:
- UIOverlay _overlay;
- UIPanel _panel;
- RectTransform _contentSlot;
- UIHeader _header;
- RectTransform _actionsSlot;
- TransitionPreset showTransition;      // scale + fade
- bool closeOnOverlayTap = true;
- float maxWidth = 400;
- float maxHeight = 600;
- string overlayColorToken = "overlay";
- UnityEvent onClose;

Методы:
- void Show()
- void Hide()
- void SetTitle(string title)
- RectTransform GetContentSlot()
- RectTransform GetActionsSlot()
- void AddAction(string label, UIButtonVariant variant, Action callback)

Структура prefab:
    UIModal (GameObject, full screen overlay anchor)
    ├── UIModal (component)
    ├── Overlay (Image, raycast target, overlay color)
    └── Panel (UIPanel, centered, maxWidth)
        ├── Header (UIHeader variant)
        │   ├── Title (UIText)
        │   └── CloseButton (UIIconButton)
        ├── Content (ScrollView / simple)
        └── Actions (HorizontalLayoutGroup)
            ├── CancelButton (UIButton_Text)
            └── ConfirmButton (UIButton_Filled)
```

---

#### UIDialog.cs

```
Диалоговое окно (упрощённая модалка).

class UIDialog : UIComponentBase

Поля:
- UIModal _modal;
- UIText _message;
- UIButton _positiveButton;
- UIButton _negativeButton;
- UIButton _neutralButton;

static Методы:
- static UIDialog Show(string title, string message,
    string positiveLabel = "OK",
    string negativeLabel = null,
    Action onPositive = null,
    Action onNegative = null)

- static UIDialog ShowConfirm(string title, string message,
    Action onConfirm, Action onCancel = null)
```

---

#### UIToast.cs

```
Всплывающее уведомление.

class UIToast : UIComponentBase

Поля:
- Image _background;
- UIText _message;
- UIIconButton _action;
- float duration = 3f;             // автоскрытие через N секунд (0 = ручное)
- ToastPosition position = Bottom;
- TransitionPreset showTransition;

enum ToastPosition { Top, Bottom, Center }

static Методы:
- static UIToast Show(string message, float duration = 3f)
- static UIToast Show(string message, string actionLabel, Action onAction)

Поведение:
- Появляется с анимацией (slide up / fade in)
- Автоматически скрывается через duration
- Новый toast вытесняет предыдущий
```

---

#### UITooltip.cs

```
Подсказка при наведении.

class UITooltip : UIComponentBase

Поля:
- Image _background;
- UIText _text;
- RectTransform _arrow;
- string text;
- TooltipPosition position = Top;
- float showDelay = 0.5f;

enum TooltipPosition { Top, Bottom, Left, Right, Auto }

Методы:
- void Show(RectTransform anchor, string text)
- void Hide()

Позиционирование:
- Автоматический расчёт позиции относительно anchor
- Если выходит за экран — переворот на противоположную сторону
```

---

#### UIOverlay.cs

```
Полупрозрачный оверлей (фон для модалок, sidebar).

class UIOverlay : UIComponentBase

Поля:
- Image _background;
- string colorToken = "overlay";
- bool blockRaycasts = true;
- UnityEvent onTap;

Методы:
- void Show(float fadeInDuration = 0.2f)
- void Hide(float fadeOutDuration = 0.2f)
```

---

### 5.8 Feedback Components

#### UIProgressBar.cs

```
Полоса прогресса.

class UIProgressBar : UIComponentBase

Поля:
- Image _trackBackground;
- Image _fill;
- UIText _label;
- float value = 0;              // 0-1
- string fillColorToken = "primary";
- string trackColorToken = "disabled";
- bool showLabel = false;
- string labelFormat = "{0:P0}";    // "75%"
- ProgressBarVariant variant = Linear;

enum ProgressBarVariant { Linear, Indeterminate }

Методы:
- void SetValue(float normalized)   // 0-1
- void SetIndeterminate(bool indeterminate)
    — если true, запускает бесконечную анимацию
```

---

#### UISpinner.cs

```
Индикатор загрузки.

class UISpinner : UIComponentBase

Поля:
- Image _spinner;
- float rotationSpeed = 360;     // градусов в секунду
- float size = 48;
- string colorToken = "primary";

Методы:
- void Show()
- void Hide()

Поведение:
- Бесконечная вращательная анимация в Update()
```

---

#### UIHealthBar.cs

```
Игровая полоска здоровья (специализированный компонент).

class UIHealthBar : UIComponentBase

Поля:
- Image _backgroundBar;
- Image _healthFill;
- Image _damageFill;             // "замедленная" полоска (показывает потерянное HP)
- UIText _label;
- float maxHealth = 100;
- float currentHealth = 100;
- string healthColorToken = "success";
- string damageColorToken = "error";
- string lowHealthColorToken = "warning"; // цвет при HP < 25%
- float lowHealthThreshold = 0.25f;
- bool showLabel = true;
- string labelFormat = "{0}/{1}";
- float damageAnimDuration = 0.5f;   // задержка "damage fill"

Методы:
- void SetHealth(float current, float max)
- void TakeDamage(float amount)
    — currentHealth уменьшается мгновенно
    — damageFill уменьшается с задержкой (tween)
- void Heal(float amount)
```

---

## 6. Theming System

### 6.1 ThemeManager.cs

```
Синглтон, управляющий активной темой.

class ThemeManager : MonoBehaviour (или ScriptableObject + static access)

Singleton:
- static ThemeManager Instance { get; }
- Создаётся автоматически при первом обращении (DontDestroyOnLoad)

Поля:
- Theme _activeTheme;
- Theme _defaultTheme;           // загружается из Resources
- List<Theme> _availableThemes;

События:
- static event Action<Theme> OnThemeChanged;

Методы:
- static Theme ActiveTheme { get; }
- static void SetTheme(Theme theme)
    1. _activeTheme = theme
    2. OnThemeChanged?.Invoke(theme)
    3. Все IThemeable компоненты получают уведомление

- static void SetTheme(string themeId)
    — поиск в _availableThemes по themeId

- static Color GetColor(string key)
    → ActiveTheme.GetColor(key)

- static TypographyPreset GetTypography(string key)
    → ActiveTheme.GetTypography(key)

- static float GetSpacing(string key)
    → ActiveTheme.GetSpacing(key)

- static void RegisterThemeable(IThemeable component)
- static void UnregisterThemeable(IThemeable component)

- static List<Theme> GetAvailableThemes()

Инициализация:
- При старте загружает DefaultTheme из Resources/UACF_UI/DefaultTheme/
- Ищет все Theme assets в проекте
```

---

### 6.2 ThemeApplier.cs

```
Компонент, который при смене темы пересчитывает стили всех дочерних элементов.
Размещается на корневом Canvas (или на UIScreen).

class ThemeApplier : MonoBehaviour

Поля:
- Theme overrideTheme;          // null = глобальная тема
- bool applyToChildren = true;

Поведение:
- OnEnable → подписка на ThemeManager.OnThemeChanged
- При смене темы → находит все IThemeable в дочерних → вызывает OnThemeChanged
- Если overrideTheme != null → использует его вместо глобальной темы

Применение:
- Позволяет иметь разные темы для разных частей UI
- Например: основной UI в светлой теме, но модалка в тёмной
```

---

### 6.3 ThemeListener.cs

```
Лёгкий компонент для простого реагирования на смену темы
на объектах без UIComponentBase (обычные Unity UI элементы).

class ThemeListener : MonoBehaviour

Поля:
- Image targetImage;
- TMP_Text targetText;
- string imageColorToken;
- string textColorToken;

Поведение:
- Подписка на ThemeManager.OnThemeChanged
- При смене → targetImage.color = ThemeManager.GetColor(imageColorToken)
- При смене → targetText.color = ThemeManager.GetColor(textColorToken)

Применение:
- Для случаев когда не нужен полноценный UIComponentBase
- Быстрая привязка существующего UI к теме
```

---

### 6.4 TokenReference.cs

```
Сериализуемая ссылка на токен в теме.
Используется в компонентах вместо прямого Color/float.

[Serializable]
class TokenReference<T>
{
    string tokenKey;              // ключ в теме ("primary", "md", "body1")
    TokenType tokenType;          // Color, Spacing, Typography, Shape, Elevation
    bool hasOverride;
    T overrideValue;              // ручное переопределение

    T Resolve(Theme theme)
    {
        if (hasOverride) return overrideValue;
        return theme.GetToken<T>(tokenType, tokenKey);
    }
}

enum TokenType
{
    Color,
    Spacing,
    Typography,
    Shape,
    Elevation
}
```

---

## 7. Screen System

### 7.1 UIScreen.cs

```
Базовый класс экрана.

[RequireComponent(typeof(Canvas))]  // или CanvasGroup
abstract class UIScreen : MonoBehaviour

Поля:
- string screenId;                  // уникальный ID ("main_menu", "settings", "inventory")
- CanvasGroup _canvasGroup;
- RectTransform _rootTransform;
- TransitionPreset showTransition;
- TransitionPreset hideTransition;
- bool isVisible;
- ThemeApplier _themeApplier;

Lifecycle:
- virtual void OnScreenCreated()     — вызывается один раз при создании
- virtual void OnScreenShow(object data)   — перед показом (передача данных)
- virtual void OnScreenShown()       — после завершения show-анимации
- virtual void OnScreenHide()        — перед скрытием
- virtual void OnScreenHidden()      — после завершения hide-анимации
- virtual void OnScreenDestroy()     — перед уничтожением
- virtual void OnScreenFocus()       — экран получил фокус (вернулись из другого)
- virtual void OnScreenBlur()        — экран потерял фокус (перешли на другой)
- virtual bool OnBackPressed()       — обработка "назад" (return true = обработано)

Методы:
- void Show(object data = null, TransitionPreset transition = null)
- void Hide(TransitionPreset transition = null)
- void SetInteractable(bool interactable) → _canvasGroup.interactable
```

---

### 7.2 UIScreenRegistry.cs

```
Реестр доступных экранов.

class UIScreenRegistry : ScriptableObject

Поля:
- List<ScreenEntry> screens;

[Serializable]
class ScreenEntry
{
    string screenId;
    GameObject prefab;           // префаб экрана
    bool preload;                // загрузить заранее (но не показывать)
    bool singleton;              // только один экземпляр
}

Методы:
- ScreenEntry GetScreen(string screenId)
- bool HasScreen(string screenId)
- List<string> GetAllScreenIds()
```

---

### 7.3 UINavigator.cs

```
Stack-based навигация между экранами.

class UINavigator : MonoBehaviour

Поля:
- UIScreenRegistry _registry;
- NavigationStack _stack;
- RectTransform _screenContainer;   // родитель для экранов
- TransitionPreset _defaultTransition;
- UIScreen _currentScreen;

Методы:
- Task Push(string screenId, object data = null, TransitionPreset transition = null)
    1. Создать/получить экземпляр экрана из registry
    2. Вызвать OnScreenBlur() на текущем экране
    3. Запустить hide-transition текущего + show-transition нового одновременно
    4. Вызвать OnScreenShow(data) на новом
    5. Положить в стек

- Task Pop(object result = null, TransitionPreset transition = null)
    1. Снять текущий экран со стека
    2. OnScreenHide() текущий
    3. Показать предыдущий из стека
    4. OnScreenFocus() на предыдущем

- Task Replace(string screenId, object data = null)
    — Pop + Push без анимации промежуточного состояния

- Task PopToRoot()
    — снять все экраны до первого

- void PopAll()
    — закрыть все экраны

- bool CanGoBack()
    → _stack.Count > 1

Свойства:
- UIScreen CurrentScreen { get; }
- int StackDepth { get; }
- string CurrentScreenId { get; }
```

---

### 7.4 NavigationStack.cs

```
Внутренний стек навигации.

class NavigationStack

Поля:
- Stack<NavigationEntry> _stack;

class NavigationEntry
{
    string screenId;
    UIScreen instance;
    object data;
    DateTime timestamp;
}

Методы:
- void Push(NavigationEntry entry)
- NavigationEntry Pop()
- NavigationEntry Peek()
- int Count { get; }
- void Clear()
- List<string> GetHistory()   // для отладки
```

---

### 7.5 TransitionPreset.cs

```
[CreateAssetMenu(menuName = "UACF UI/Transition Preset")]
class TransitionPreset : ScriptableObject

Поля:
- TransitionType type;
- float duration = 0.3f;
- AnimationCurve curve = EaseInOut;
- float delay = 0f;

enum TransitionType
{
    None,
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    Scale,
    ScaleAndFade,
    Custom
}

Для Custom:
- AnimationClip enterAnimation;
- AnimationClip exitAnimation;

Методы:
- IEnumerator PlayShow(RectTransform target, CanvasGroup canvasGroup)
- IEnumerator PlayHide(RectTransform target, CanvasGroup canvasGroup)

Реализация (примеры):
- Fade: canvasGroup.alpha 0→1
- SlideLeft: anchoredPosition.x от Screen.width→0, одновременно alpha 0→1
- Scale: localScale от 0.8→1.0, одновременно alpha 0→1
```

---

### 7.6 Screen Templates (Prefabs)

```
Предготовые шаблоны экранов в Resources/UACF_UI/Screens/:

FullscreenTemplate:
    Screen (UIScreen)
    └── Content (stretch)

HeaderContentTemplate:
    Screen (UIScreen)
    ├── Header (UIHeader, anchor top)
    └── Content (stretch, top margin = header height)

TabsTemplate:
    Screen (UIScreen)
    ├── Header (UIHeader)
    ├── TabBar (UITabBar)
    ├── TabContent (UIStackLayout)
    │   ├── Page0
    │   ├── Page1
    │   └── Page2
    └── BottomBar (UIBottomBar, опционально)

SplitTemplate:
    Screen (UIScreen)
    ├── Header (UIHeader)
    └── HorizontalLayout
        ├── LeftPanel (width 30%)
        └── RightPanel (width 70%)

DrawerTemplate:
    Screen (UIScreen)
    ├── Header (UIHeader, с кнопкой меню)
    ├── Content (stretch)
    └── Sidebar (UISidebar, left)

ModalOverlayTemplate:
    Screen (UIScreen)
    ├── BackgroundContent (stretch, blurred/dimmed)
    └── Modal (UIModal, centered)
```

---

## 8. Animation System

### 8.1 UITween.cs

```
Встроенная система tweening (без внешних зависимостей типа DOTween).

static class UITween

Методы:
- static Coroutine To(MonoBehaviour owner, float from, float to,
    float duration, Action<float> onUpdate, AnimationCurve curve = null,
    Action onComplete = null)

- static Coroutine FadeTo(CanvasGroup target, float alpha,
    float duration, AnimationCurve curve = null)

- static Coroutine MoveTo(RectTransform target, Vector2 position,
    float duration, AnimationCurve curve = null)

- static Coroutine ScaleTo(RectTransform target, Vector3 scale,
    float duration, AnimationCurve curve = null)

- static Coroutine ColorTo(Graphic target, Color color,
    float duration, AnimationCurve curve = null)

- static Coroutine SizeTo(RectTransform target, Vector2 size,
    float duration, AnimationCurve curve = null)

- static void Cancel(Coroutine tween)
- static void CancelAll(MonoBehaviour owner)

Реализация:
- Через Coroutines (IEnumerator)
- Поддержка AnimationCurve для easing
- Unscaled time по умолчанию (для UI в паузе)
```

---

### 8.2 TweenPreset.cs

```
[CreateAssetMenu(menuName = "UACF UI/Tween Preset")]
class TweenPreset : ScriptableObject

Поля:
- float duration = 0.3f;
- float delay = 0f;
- AnimationCurve curve;
- bool useUnscaledTime = true;
```

---

### 8.3 UIAnimator.cs

```
Компонент для применения анимационных пресетов к UI-элементу.

class UIAnimator : MonoBehaviour

Поля:
- RectTransform _target;
- CanvasGroup _canvasGroup;
- TweenPreset defaultPreset;
- List<NamedAnimation> animations;

[Serializable]
class NamedAnimation
{
    string key;                  // "show", "hide", "bounce", "shake"
    AnimationType type;
    TweenPreset preset;
    // Параметры зависят от type
    Vector2 moveOffset;          // для Slide
    Vector3 scaleFrom;           // для Scale
    float fadeFrom;              // для Fade
}

enum AnimationType
{
    Fade,
    Slide,
    Scale,
    Rotate,
    Bounce,
    Shake,
    Pulse
}

Методы:
- Coroutine Play(string key)
- Coroutine PlayShow()
- Coroutine PlayHide()
- void StopAll()
```

---

### 8.4 MicroInteraction.cs

```
Компонент для hover/press микроанимаций.

class MicroInteraction : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler

Поля:
- bool scaleOnHover = true;
- float hoverScale = 1.05f;
- float hoverDuration = 0.15f;

- bool scaleOnPress = true;
- float pressScale = 0.95f;
- float pressDuration = 0.1f;

- bool colorOnHover = false;
- string hoverColorToken;

- bool soundOnClick = false;
- AudioClip clickSound;

- AnimationCurve curve;

Поведение:
- OnPointerEnter → scale to hoverScale (tween)
- OnPointerExit  → scale to 1.0 (tween)
- OnPointerDown  → scale to pressScale (tween)
- OnPointerUp    → scale to hoverScale (если ещё hover) или 1.0
```

---

### 8.5 EasingFunctions.cs

```
Библиотека функций easing.

static class EasingFunctions

Методы (все float → float, t от 0 до 1):
- static float Linear(float t)
- static float EaseInQuad(float t)
- static float EaseOutQuad(float t)
- static float EaseInOutQuad(float t)
- static float EaseInCubic(float t)
- static float EaseOutCubic(float t)
- static float EaseInOutCubic(float t)
- static float EaseInBack(float t)
- static float EaseOutBack(float t)
- static float EaseInOutBack(float t)
- static float EaseInElastic(float t)
- static float EaseOutElastic(float t)
- static float EaseOutBounce(float t)
- static float Spring(float t)

Предготовые AnimationCurve:
- static AnimationCurve EaseInOutCurve()
- static AnimationCurve EaseOutBackCurve()
- static AnimationCurve SpringCurve()
```

---

## 9. UACF Integration — API Endpoints

### 9.1 Регистрация обработчиков

```
UIHandlerRegistration.cs

[InitializeOnLoad]
static class UIHandlerRegistration

При загрузке:
- Находит UACFServer через reflection или direct reference
- Регистрирует все UI endpoints через RequestRouter

Зависимость от UACF:
- assembly reference на com.uacf.editor
- Если UACF не установлен — handlers не регистрируются, warning в лог
```

---

### 9.2 Theme API

#### `GET /api/ui/themes`

Список доступных тем.

**Response:**
```json
{
    "success": true,
    "data": {
        "themes": [
            {
                "id": "default-light",
                "name": "Default Light",
                "path": "Assets/Resources/UACF_UI/DefaultTheme/DefaultTheme.asset",
                "is_active": true
            },
            {
                "id": "default-dark",
                "name": "Default Dark",
                "path": "Assets/Resources/UACF_UI/DarkTheme/DarkTheme.asset",
                "is_active": false
            }
        ]
    }
}
```

**curl:**
```bash
curl http://localhost:7890/api/ui/themes
```

---

#### `POST /api/ui/theme/create`

Создать новую тему.

**Request body:**
```json
{
    "name": "Cyberpunk",
    "path": "Assets/Themes/CyberpunkTheme.asset",
    "base_theme": "default-dark",
    "color_overrides": {
        "primary": {"r": 0.0, "g": 1.0, "b": 0.87, "a": 1.0},
        "secondary": {"r": 1.0, "g": 0.0, "b": 0.5, "a": 1.0},
        "background": {"r": 0.05, "g": 0.05, "b": 0.1, "a": 1.0},
        "surface": {"r": 0.1, "g": 0.1, "b": 0.15, "a": 1.0}
    }
}
```

**Response:**
```json
{
    "success": true,
    "data": {
        "theme_id": "cyberpunk",
        "path": "Assets/Themes/CyberpunkTheme.asset",
        "assets_created": [
            "Assets/Themes/CyberpunkTheme.asset",
            "Assets/Themes/CyberpunkColorPalette.asset"
        ]
    }
}
```

---

#### `PUT /api/ui/theme/apply`

Применить тему к сцене.

**Request body:**
```json
{
    "theme_id": "cyberpunk",
    "target": "scene"
}
```

`target`: `"scene"` (глобально), `{"instance_id": N}` (конкретный UIScreen/ThemeApplier)

---

#### `GET /api/ui/theme/get`

Получить все токены активной темы.

**Query params:**
- `theme_id` (опционально, default: активная тема)

**Response:**
```json
{
    "success": true,
    "data": {
        "theme_id": "default-light",
        "colors": {
            "primary": {"r": 0.384, "g": 0.0, "b": 0.933, "a": 1.0},
            "secondary": {"r": 0.012, "g": 0.588, "b": 0.533, "a": 1.0},
            "background": {"r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0},
            "surface": {"r": 0.98, "g": 0.98, "b": 0.98, "a": 1.0},
            "...": "..."
        },
        "typography": {
            "h1": {"font": "Roboto-Bold", "size": 32, "style": "Bold"},
            "body1": {"font": "Roboto-Regular", "size": 16, "style": "Normal"},
            "...": "..."
        },
        "spacing": {
            "xs": 4, "sm": 8, "md": 16, "lg": 24, "xl": 32
        },
        "shapes": {
            "small": {"borderRadius": 4},
            "medium": {"borderRadius": 8},
            "large": {"borderRadius": 16}
        }
    }
}
```

---

### 9.3 Token API

#### `PUT /api/ui/tokens/colors`

Обновить цвета палитры.

**Request body:**
```json
{
    "theme_id": "default-light",
    "colors": {
        "primary": {"r": 0.2, "g": 0.4, "b": 0.9, "a": 1.0},
        "accent": {"r": 1.0, "g": 0.6, "b": 0.0, "a": 1.0}
    }
}
```

**Response:**
```json
{
    "success": true,
    "data": {
        "updated": ["primary", "accent"],
        "theme_reapplied": true
    }
}
```

**curl:**
```bash
curl -X PUT http://localhost:7890/api/ui/tokens/colors \
  -H "Content-Type: application/json" \
  -d '{
    "colors": {
      "primary": {"r": 0.2, "g": 0.4, "b": 0.9, "a": 1.0}
    }
  }'
```

---

#### `POST /api/ui/tokens/colors/add-custom`

Добавить кастомный цвет.

**Request body:**
```json
{
    "key": "gold",
    "color": {"r": 1.0, "g": 0.84, "b": 0.0, "a": 1.0},
    "description": "Gold color for achievements"
}
```

---

#### `PUT /api/ui/tokens/typography`

Обновить типографику.

**Request body:**
```json
{
    "presets": {
        "h1": {"fontSize": 36, "fontStyle": "Bold"},
        "body1": {"fontSize": 14, "lineSpacing": 1.5}
    }
}
```

---

#### `PUT /api/ui/tokens/spacing`

Обновить шкалу отступов.

**Request body:**
```json
{
    "spacing": {
        "sm": 6,
        "md": 12,
        "lg": 20
    }
}
```

---

### 9.4 Screen API

#### `POST /api/ui/screen/create`

Создать экран из шаблона.

**Request body:**
```json
{
    "screen_id": "main_menu",
    "template": "HeaderContentTemplate",
    "name": "MainMenuScreen",
    "save_as_prefab": true,
    "prefab_path": "Assets/Prefabs/Screens/MainMenuScreen.prefab",
    "theme_id": "default-light",
    "header": {
        "title": "Main Menu",
        "show_back_button": false
    }
}
```

`template` — одно из:
- `FullscreenTemplate`
- `HeaderContentTemplate`
- `TabsTemplate`
- `SplitTemplate`
- `DrawerTemplate`
- `ModalOverlayTemplate`

**Response:**
```json
{
    "success": true,
    "data": {
        "instance_id": 15000,
        "screen_id": "main_menu",
        "name": "MainMenuScreen",
        "prefab_path": "Assets/Prefabs/Screens/MainMenuScreen.prefab",
        "content_slot_id": 15001,
        "header_id": 15002
    }
}
```

**curl:**
```bash
curl -X POST http://localhost:7890/api/ui/screen/create \
  -H "Content-Type: application/json" \
  -d '{
    "screen_id": "main_menu",
    "template": "HeaderContentTemplate",
    "name": "MainMenuScreen",
    "header": {"title": "Main Menu"}
  }'
```

---

#### `GET /api/ui/screen/hierarchy`

Получить UI-иерархию экрана.

**Query params:**
- `instance_id` или `screen_id`
- `depth` (default: -1)

**Response:**
```json
{
    "success": true,
    "data": {
        "screen_id": "main_menu",
        "instance_id": 15000,
        "elements": [
            {
                "instance_id": 15002,
                "name": "Header",
                "component_type": "UIHeader",
                "properties": {
                    "title": "Main Menu",
                    "backgroundColorToken": "surface"
                },
                "children": []
            },
            {
                "instance_id": 15001,
                "name": "Content",
                "component_type": null,
                "rect": {"x": 0, "y": 56, "width": 1080, "height": 1864},
                "children": [
                    {
                        "instance_id": 15010,
                        "name": "MenuButtons",
                        "component_type": "UIVerticalLayout",
                        "properties": {
                            "spacingToken": "lg",
                            "childAlignment": "MiddleCenter"
                        },
                        "children": [
                            {
                                "instance_id": 15011,
                                "name": "PlayButton",
                                "component_type": "UIButton",
                                "properties": {
                                    "labelText": "Play",
                                    "variant": "Filled"
                                }
                            }
                        ]
                    }
                ]
            }
        ]
    }
}
```

---

### 9.5 UI Element API

#### `POST /api/ui/element/add`

Добавить UI-элемент на экран.

**Request body:**
```json
{
    "parent": {"instance_id": 15001},
    "component": "UIVerticalLayout",
    "name": "MenuButtons",
    "properties": {
        "spacingToken": "lg",
        "paddingToken": "xl",
        "childAlignment": "MiddleCenter"
    },
    "children": [
        {
            "component": "UIButton",
            "name": "PlayButton",
            "variant": "Filled",
            "properties": {
                "labelText": "Play Game",
                "typographyToken": "button"
            }
        },
        {
            "component": "UISpacer",
            "properties": {
                "sizeToken": "md"
            }
        },
        {
            "component": "UIButton",
            "name": "SettingsButton",
            "variant": "Outlined",
            "properties": {
                "labelText": "Settings"
            }
        },
        {
            "component": "UIButton",
            "name": "QuitButton",
            "variant": "Text",
            "properties": {
                "labelText": "Quit"
            }
        }
    ]
}
```

**Response:**
```json
{
    "success": true,
    "data": {
        "created": [
            {"name": "MenuButtons", "instance_id": 15010, "component": "UIVerticalLayout"},
            {"name": "PlayButton", "instance_id": 15011, "component": "UIButton"},
            {"name": "Spacer", "instance_id": 15012, "component": "UISpacer"},
            {"name": "SettingsButton", "instance_id": 15013, "component": "UIButton"},
            {"name": "QuitButton", "instance_id": 15014, "component": "UIButton"}
        ],
        "total": 5
    }
}
```

**curl:**
```bash
curl -X POST http://localhost:7890/api/ui/element/add \
  -H "Content-Type: application/json" \
  -d '{
    "parent": {"instance_id": 15001},
    "component": "UIButton",
    "name": "PlayButton",
    "properties": {"labelText": "Play Game", "variant": "Filled"}
  }'
```

---

#### `PUT /api/ui/element/modify`

Изменить свойства UI-элемента.

**Request body:**
```json
{
    "target": {"instance_id": 15011},
    "properties": {
        "labelText": "Start Game",
        "variant": "Tonal"
    },
    "style_overrides": {
        "normal": {
            "backgroundColorToken": "accent",
            "textColorToken": "onPrimary"
        },
        "hovered": {
            "backgroundColorToken": "primaryVariant"
        }
    }
}
```

---

#### `DELETE /api/ui/element/remove`

Удалить UI-элемент.

**Request body:**
```json
{
    "target": {"instance_id": 15011}
}
```

---

#### `POST /api/ui/element/reorder`

Изменить порядок элемента в parent layout.

**Request body:**
```json
{
    "target": {"instance_id": 15013},
    "sibling_index": 0
}
```

---

### 9.6 Style API

#### `POST /api/ui/style/create`

Создать стиль.

**Request body:**
```json
{
    "key": "danger_button",
    "path": "Assets/Styles/DangerButton.asset",
    "parent_style": "button_filled",
    "states": {
        "normal": {
            "backgroundColorToken": "error",
            "textColorToken": "onError"
        },
        "hovered": {
            "backgroundColorToken": "errorDark"
        },
        "pressed": {
            "backgroundColorToken": "errorLight"
        }
    },
    "transitionDuration": 0.2
}
```

---

#### `PUT /api/ui/style/apply`

Применить стиль к элементу.

**Request body:**
```json
{
    "target": {"instance_id": 15011},
    "style": "danger_button"
}
```

---

#### `GET /api/ui/style/list`

Список доступных стилей.

**Response:**
```json
{
    "success": true,
    "data": {
        "styles": [
            {"key": "button_filled", "path": "...", "has_parent": false},
            {"key": "button_outlined", "path": "...", "has_parent": false},
            {"key": "danger_button", "path": "...", "has_parent": true, "parent": "button_filled"}
        ]
    }
}
```

---

### 9.7 Layout API

#### `POST /api/ui/layout/create`

Создать layout-контейнер.

**Request body:**
```json
{
    "parent": {"instance_id": 15001},
    "type": "vertical",
    "name": "ContentColumn",
    "properties": {
        "spacingToken": "md",
        "paddingToken": "lg",
        "childAlignment": "UpperCenter",
        "childForceExpandWidth": true,
        "childForceExpandHeight": false
    }
}
```

`type`: `"vertical"` | `"horizontal"` | `"grid"` | `"stack"`

Для grid — дополнительные свойства:
```json
{
    "type": "grid",
    "properties": {
        "cellSize": {"x": 150, "y": 200},
        "spacingToken": "sm",
        "constraint": "FixedColumnCount",
        "constraintCount": 3
    }
}
```

---

### 9.8 Component List API

#### `GET /api/ui/components/list`

Список доступных UI-компонентов.

**Query params:**
- `category` — `display` | `input` | `container` | `layout` | `navigation` | `overlay` | `feedback` | `all`

**Response:**
```json
{
    "success": true,
    "data": {
        "components": [
            {
                "type": "UIButton",
                "category": "input",
                "prefab_variants": ["Filled", "Outlined", "Text", "Tonal"],
                "properties": [
                    {"name": "labelText", "type": "string", "default": "Button"},
                    {"name": "variant", "type": "enum", "values": ["Filled","Outlined","Text","Tonal"], "default": "Filled"},
                    {"name": "typographyToken", "type": "token:typography", "default": "button"},
                    {"name": "iconKey", "type": "string", "default": null},
                    {"name": "interactable", "type": "bool", "default": true}
                ]
            },
            {
                "type": "UIVerticalLayout",
                "category": "layout",
                "prefab_variants": [],
                "properties": [
                    {"name": "spacingToken", "type": "token:spacing", "default": "md"},
                    {"name": "paddingToken", "type": "token:spacing", "default": "none"},
                    {"name": "childAlignment", "type": "enum", "values": ["UpperLeft","UpperCenter","..."], "default": "UpperLeft"},
                    {"name": "childForceExpandWidth", "type": "bool", "default": true},
                    {"name": "childForceExpandHeight", "type": "bool", "default": false}
                ]
            },
            {
                "type": "UIGridLayout",
                "category": "layout",
                "properties": [
                    {"name": "cellSize", "type": "vector2", "default": {"x":100,"y":100}},
                    {"name": "spacingToken", "type": "token:spacing", "default": "md"},
                    {"name": "constraint", "type": "enum", "values": ["Flexible","FixedColumnCount","FixedRowCount"]},
                    {"name": "constraintCount", "type": "int", "default": 0}
                ]
            }
        ]
    }
}
```

**curl:**
```bash
curl "http://localhost:7890/api/ui/components/list?category=layout"
```

---

### 9.9 Screen Navigation API

#### `POST /api/ui/navigator/setup`

Настроить навигатор.

**Request body:**
```json
{
    "canvas_instance_id": 14000,
    "screen_registry_path": "Assets/Config/ScreenRegistry.asset",
    "default_transition": "FadeTransition"
}
```

---

#### `POST /api/ui/navigator/push`

Перейти на экран.

**Request body:**
```json
{
    "screen_id": "settings",
    "data": {"section": "audio"},
    "transition": "SlideLeftTransition"
}
```

---

### 9.10 Batch UI Operations

#### `POST /api/ui/batch`

Пакетное создание UI.

**Request body:**
```json
{
    "operations": [
        {
            "id": "screen",
            "action": "create_screen",
            "params": {
                "screen_id": "inventory",
                "template": "HeaderContentTemplate",
                "header": {"title": "Inventory"}
            }
        },
        {
            "id": "grid_layout",
            "action": "add_element",
            "params": {
                "parent": {"ref": "screen.content_slot_id"},
                "component": "UIScrollView",
                "name": "ItemsScroll"
            }
        },
        {
            "id": "grid",
            "action": "add_element",
            "params": {
                "parent": {"ref": "grid_layout.instance_id"},
                "component": "UIGridLayout",
                "name": "ItemsGrid",
                "properties": {
                    "cellSize": {"x": 100, "y": 120},
                    "spacingToken": "sm",
                    "constraint": "FixedColumnCount",
                    "constraintCount": 4
                }
            }
        },
        {
            "id": "item1",
            "action": "add_element",
            "params": {
                "parent": {"ref": "grid.instance_id"},
                "component": "UICard",
                "name": "ItemSlot_01",
                "properties": {
                    "elevationToken": "low"
                }
            }
        }
    ],
    "stop_on_error": true
}
```

Ссылки между операциями:
- `{"ref": "operation_id.field_name"}` — ссылка на результат предыдущей операции
- Позволяет цепочечное создание без промежуточных запросов

---

## 10. Editor Tools

### 10.1 ThemeEditorWindow.cs

```
Окно редактора тем.
Открывается через: Window > UACF UI > Theme Editor

Функциональность:
- Выбор активной темы из dropdown
- Визуальное отображение всех цветов палитры (цветные прямоугольники)
- Inline-редактирование цветов (ColorField)
- Превью типографики (отображение всех пресетов)
- Превью spacing (визуальные линейки)
- Кнопка "Apply Theme" — мгновенное применение
- Кнопка "Duplicate Theme" — создать копию для модификации
- Live preview: изменения в окне сразу отражаются в Scene View
```

---

### 10.2 TokenBrowserWindow.cs

```
Браузер токенов.
Открывается через: Window > UACF UI > Token Browser

Функциональность:
- Дерево всех токенов по категориям (Colors, Typography, Spacing, Shapes)
- Поиск по имени
- Отображение значения каждого токена
- Список компонентов, использующих конкретный токен (references)
- Drag & Drop токена в Inspector для быстрой привязки
```

---

### 10.3 ComponentPaletteWindow.cs

```
Палитра UI-компонентов.
Открывается через: Window > UACF UI > Component Palette

Функциональность:
- Список всех UI-компонентов по категориям
- Превью каждого компонента
- Drag & Drop компонента в Scene/Hierarchy для создания
- Кнопка "Create" — создание с диалогом настроек
- Показ всех свойств компонента и доступных вариантов
```

---

### 10.4 Custom Inspectors

```
UIComponentBaseInspector.cs:
- Базовый инспектор для всех UI-компонентов
- Показывает текущую привязку к стилю
- Кнопка "Detach Style" / "Attach Style"
- Превью resolved style (какие цвета/шрифты будут применены)

ColorPaletteInspector.cs:
- Визуальная сетка цветов с именами
- Проверка контрастности (WCAG)
- Color picker с HEX/RGB/HSV

TypographySetInspector.cs:
- Превью каждого пресета (текст "Aa" в соответствующем стиле)
- Inline-редактирование размеров и стилей

StyleBindingDrawer.cs:
- PropertyDrawer для StyleBinding
- Dropdown выбора стиля из доступных StyleSheet
- Превью resolved значений
```

---

### 10.5 UIMenuItems.cs

```
Пункты меню для быстрого создания.

GameObject > UACF UI > Display >
    Text
    Image
    Icon
    Divider
    Badge

GameObject > UACF UI > Input >
    Button (Filled)
    Button (Outlined)
    Button (Text)
    Icon Button
    Toggle
    Slider
    Dropdown
    Input Field
    Checkbox

GameObject > UACF UI > Layout >
    Vertical Layout
    Horizontal Layout
    Grid Layout
    Stack Layout
    Spacer

GameObject > UACF UI > Container >
    Panel
    Card
    Scroll View
    List
    Tab Container
    Accordion

GameObject > UACF UI > Navigation >
    Header
    Toolbar
    Bottom Bar
    Tab Bar
    Sidebar

GameObject > UACF UI > Overlay >
    Modal
    Dialog
    Toast
    Tooltip
    Overlay

GameObject > UACF UI > Feedback >
    Progress Bar
    Spinner
    Health Bar

GameObject > UACF UI > Screens >
    Fullscreen
    Header + Content
    Tabs
    Split
    Drawer
    Modal Overlay

Assets > Create > UACF UI >
    Theme
    Color Palette
    Typography Set
    Spacing Scale
    Shape Set
    Elevation Set
    Style
    Style Sheet
    Transition Preset
    Screen Registry

Каждый пункт:
1. Создаёт GameObject из соответствующего префаба (Resources/UACF_UI/Prefabs/)
2. Устанавливает parent = текущий выделенный объект (или Canvas)
3. Если нет Canvas — создаёт его
4. Регистрирует Undo
```

---

## 11. Utility

### 11.1 SafeAreaHelper.cs

```
Компонент для адаптации под safe area (notch, закруглённые углы экрана).

class SafeAreaHelper : MonoBehaviour

Поля:
- RectTransform _panel;
- bool applyTop = true;
- bool applyBottom = true;
- bool applyLeft = true;
- bool applyRight = true;

Поведение:
- Получает Screen.safeArea
- Пересчитывает anchorMin/anchorMax RectTransform
- Обновляет при смене ориентации
```

---

### 11.2 AnchorPresets.cs

```
Утилитарные методы для быстрой установки anchor-ов.

static class AnchorPresets

Методы:
- static void StretchFull(RectTransform rt)
    → anchor (0,0)-(1,1), offset = 0

- static void TopStretch(RectTransform rt, float height)
    → anchor (0,1)-(1,1), height

- static void BottomStretch(RectTransform rt, float height)
    → anchor (0,0)-(1,0), height

- static void CenterMiddle(RectTransform rt, Vector2 size)
    → anchor (0.5,0.5), pivot (0.5,0.5)

- static void TopLeft(RectTransform rt, Vector2 size)
- static void TopRight(RectTransform rt, Vector2 size)
- static void BottomLeft(RectTransform rt, Vector2 size)
- static void BottomRight(RectTransform rt, Vector2 size)

- static void LeftStretch(RectTransform rt, float width)
- static void RightStretch(RectTransform rt, float width)
```

---

### 11.3 UIExtensions.cs

```
Extension-методы для Unity UI.

static class UIExtensions

Методы:
- static void SetAlpha(this Graphic graphic, float alpha)
- static void SetAlpha(this CanvasGroup group, float alpha)
- static T GetOrAddComponent<T>(this GameObject go)
- static void DestroyChildren(this Transform transform)
- static void SetActive(this CanvasGroup group, bool active)
    → interactable + blocksRaycasts + alpha
- static Rect GetScreenRect(this RectTransform rt)
- static bool IsVisibleFrom(this RectTransform rt, Camera camera)
- static void SetPivotWithoutMoving(this RectTransform rt, Vector2 pivot)
```

---

## 12. Полный пример рабочего цикла агента

### Сценарий: создать экран инвентаря в стиле RPG

```bash
# 1. Проверить доступные компоненты
curl "http://localhost:7890/api/ui/components/list?category=all"

# 2. Создать кастомную тему
curl -X POST http://localhost:7890/api/ui/theme/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "RPG Dark",
    "path": "Assets/Themes/RPGDark.asset",
    "base_theme": "default-dark",
    "color_overrides": {
      "primary": {"r": 0.8, "g": 0.7, "b": 0.3, "a": 1.0},
      "secondary": {"r": 0.6, "g": 0.2, "b": 0.2, "a": 1.0},
      "background": {"r": 0.08, "g": 0.06, "b": 0.1, "a": 1.0},
      "surface": {"r": 0.12, "g": 0.1, "b": 0.15, "a": 1.0}
    }
  }'

# 3. Добавить кастомные цвета для редкости предметов
curl -X POST http://localhost:7890/api/ui/tokens/colors/add-custom \
  -H "Content-Type: application/json" \
  -d '{"key":"rarity_common","color":{"r":0.7,"g":0.7,"b":0.7,"a":1},"description":"Common item"}'

curl -X POST http://localhost:7890/api/ui/tokens/colors/add-custom \
  -H "Content-Type: application/json" \
  -d '{"key":"rarity_rare","color":{"r":0.2,"g":0.4,"b":1.0,"a":1},"description":"Rare item"}'

curl -X POST http://localhost:7890/api/ui/tokens/colors/add-custom \
  -H "Content-Type: application/json" \
  -d '{"key":"rarity_epic","color":{"r":0.6,"g":0.2,"b":0.9,"a":1},"description":"Epic item"}'

curl -X POST http://localhost:7890/api/ui/tokens/colors/add-custom \
  -H "Content-Type: application/json" \
  -d '{"key":"rarity_legendary","color":{"r":1.0,"g":0.6,"b":0.0,"a":1},"description":"Legendary item"}'

# 4. Создать экран инвентаря пакетной операцией
curl -X POST http://localhost:7890/api/ui/batch \
  -H "Content-Type: application/json" \
  -d '{
    "operations": [
      {
        "id": "screen",
        "action": "create_screen",
        "params": {
          "screen_id": "inventory",
          "template": "HeaderContentTemplate",
          "name": "InventoryScreen",
          "save_as_prefab": true,
          "prefab_path": "Assets/Prefabs/Screens/InventoryScreen.prefab",
          "header": {"title": "Inventory", "show_back_button": true}
        }
      },
      {
        "id": "main_layout",
        "action": "add_element",
        "params": {
          "parent": {"ref": "screen.content_slot_id"},
          "component": "UIVerticalLayout",
          "name": "MainLayout",
          "properties": {"spacingToken": "md", "paddingToken": "md"}
        }
      },
      {
        "id": "tab_bar",
        "action": "add_element",
        "params": {
          "parent": {"ref": "main_layout.instance_id"},
          "component": "UITabBar",
          "name": "CategoryTabs",
          "properties": {
            "tabs": [
              {"label": "All"},
              {"label": "Weapons"},
              {"label": "Armor"},
              {"label": "Potions"}
            ]
          }
        }
      },
      {
        "id": "stats_row",
        "action": "add_element",
        "params": {
          "parent": {"ref": "main_layout.instance_id"},
          "component": "UIHorizontalLayout",
          "name": "StatsRow",
          "properties": {"spacingToken": "md"}
        }
      },
      {
        "id": "gold_label",
        "action": "add_element",
        "params": {
          "parent": {"ref": "stats_row.instance_id"},
          "component": "UIText",
          "name": "GoldLabel",
          "properties": {
            "text": "Gold: 1,250",
            "typographyToken": "subtitle2",
            "colorToken": "rarity_legendary"
          }
        }
      },
      {
        "id": "weight_label",
        "action": "add_element",
        "params": {
          "parent": {"ref": "stats_row.instance_id"},
          "component": "UIText",
          "name": "WeightLabel",
          "properties": {
            "text": "Weight: 45/100",
            "typographyToken": "subtitle2",
            "colorToken": "onBackgroundSecondary"
          }
        }
      },
      {
        "id": "items_scroll",
        "action": "add_element",
        "params": {
          "parent": {"ref": "main_layout.instance_id"},
          "component": "UIScrollView",
          "name": "ItemsScroll"
        }
      },
      {
        "id": "items_grid",
        "action": "add_element",
        "params": {
          "parent": {"ref": "items_scroll.instance_id"},
          "component": "UIGridLayout",
          "name": "ItemsGrid",
          "properties": {
            "cellSize": {"x": 100, "y": 120},
            "spacingToken": "sm",
            "constraint": "FixedColumnCount",
            "constraintCount": 4,
            "paddingToken": "sm"
          }
        }
      }
    ],
    "stop_on_error": true
  }'

# 5. Добавить несколько слотов предметов
for i in $(seq 1 12); do
curl -X POST http://localhost:7890/api/ui/element/add \
  -H "Content-Type: application/json" \
  -d "{
    \"parent\": {\"name\": \"ItemsGrid\"},
    \"component\": \"UICard\",
    \"name\": \"ItemSlot_${i}\",
    \"properties\": {
      \"elevationToken\": \"low\",
      \"backgroundColorToken\": \"surface\"
    },
    \"children\": [
      {
        \"component\": \"UIImage\",
        \"name\": \"ItemIcon\",
        \"properties\": {\"colorToken\": \"onBackgroundSecondary\"}
      },
      {
        \"component\": \"UIText\",
        \"name\": \"ItemName\",
        \"properties\": {\"text\": \"Empty\", \"typographyToken\": \"caption\"}
      }
    ]
  }"
done

# 6. Проверить результат
curl "http://localhost:7890/api/ui/screen/hierarchy?screen_id=inventory"

# 7. Применить тему
curl -X PUT http://localhost:7890/api/ui/theme/apply \
  -H "Content-Type: application/json" \
  -d '{"theme_id": "rpg-dark", "target": "scene"}'

# 8. Сохранить сцену
curl -X POST http://localhost:7890/api/scene/save
```

---

## 13. Настройки фреймворка

```csharp
[CreateAssetMenu(menuName = "UACF UI/Framework Settings")]
class UIFrameworkSettings : ScriptableSingleton<UIFrameworkSettings>
{
    // Тема по умолчанию
    Theme defaultTheme;

    // Путь к ресурсам фреймворка
    string resourcesPath = "UACF_UI";

    // Авто-создание Canvas при добавлении первого UI-элемента
    bool autoCreateCanvas = true;

    // Canvas настройки по умолчанию
    RenderMode defaultRenderMode = ScreenSpaceOverlay;
    int defaultSortingOrder = 0;
    float referenceResolutionWidth = 1080;
    float referenceResolutionHeight = 1920;
    CanvasScaler.ScreenMatchMode screenMatchMode = MatchWidthOrHeight;
    float matchWidthOrHeight = 0.5f;

    // Анимация
    bool enableMicroInteractions = true;
    float defaultTransitionDuration = 0.3f;

    // UACF
    bool registerUACFHandlers = true;

    // Логирование
    bool logThemeChanges = true;
    bool logStyleApplications = false;
}
```

---

## 14. Этапы реализации

### Этап 1: Tokens Foundation
- [ ] ColorPalette
- [ ] TypographySet + TypographyPreset
- [ ] SpacingScale
- [ ] ShapeSet + ShapePreset
- [ ] ElevationSet + ElevationPreset
- [ ] Theme
- [ ] Default Light + Dark theme assets

### Этап 2: Style System
- [ ] UIStyleState
- [ ] UIStyle с наследованием
- [ ] StyleSheet
- [ ] StyleResolver
- [ ] StyleBinding
- [ ] TokenReference

### Этап 3: Theming
- [ ] ThemeManager
- [ ] ThemeApplier
- [ ] ThemeListener
- [ ] IThemeable interface

### Этап 4: Base Components
- [ ] UIComponentBase
- [ ] UIInteractableBase
- [ ] UITween
- [ ] EasingFunctions
- [ ] MicroInteraction

### Этап 5: Display Components
- [ ] UIText
- [ ] UIImage
- [ ] UIIcon
- [ ] UIDivider
- [ ] UIBadge
- [ ] Prefabs + default styles

### Этап 6: Layout Components
- [ ] UIVerticalLayout
- [ ] UIHorizontalLayout
- [ ] UIGridLayout
- [ ] UIStackLayout
- [ ] UISpacer
- [ ] UIExpandable
- [ ] UIResponsiveContainer
- [ ] Prefabs

### Этап 7: Input Components
- [ ] UIButton (все варианты)
- [ ] UIIconButton
- [ ] UIToggle
- [ ] UISlider
- [ ] UIDropdown
- [ ] UIInputField
- [ ] UICheckbox
- [ ] Prefabs + default styles

### Этап 8: Container Components
- [ ] UIPanel
- [ ] UICard
- [ ] UIScrollView
- [ ] UIList + UIListItem
- [ ] UITabContainer
- [ ] UIAccordion
- [ ] Prefabs

### Этап 9: Navigation Components
- [ ] UIHeader
- [ ] UIToolbar
- [ ] UIBottomBar
- [ ] UITabBar
- [ ] UISidebar
- [ ] UIBreadcrumb
- [ ] Prefabs

### Этап 10: Overlay Components
- [ ] UIOverlay
- [ ] UIModal
- [ ] UIDialog
- [ ] UIToast
- [ ] UITooltip
- [ ] Prefabs

### Этап 11: Feedback Components
- [ ] UIProgressBar
- [ ] UISpinner
- [ ] UIHealthBar
- [ ] Prefabs

### Этап 12: Screen System
- [ ] UIScreen
- [ ] UIScreenRegistry
- [ ] UINavigator
- [ ] NavigationStack
- [ ] TransitionPreset
- [ ] Screen Templates (prefabs)

### Этап 13: Animation
- [ ] UIAnimator
- [ ] TweenPreset
- [ ] Transition animations
- [ ] Predefined transition assets

### Этап 14: UACF Integration
- [ ] UIHandlerRegistration
- [ ] UIScreenHandler + UIBuilderService
- [ ] UIElementHandler
- [ ] UIThemeHandler + UIThemeService
- [ ] UITokenHandler + UITokenService
- [ ] UIStyleHandler + UIStyleService
- [ ] UILayoutHandler
- [ ] UIComponentListHandler
- [ ] Batch operations

### Этап 15: Editor Tools
- [ ] ThemeEditorWindow
- [ ] TokenBrowserWindow
- [ ] ComponentPaletteWindow
- [ ] Custom Inspectors
- [ ] UIMenuItems
- [ ] PrefabFactory

### Этап 16: Utilities
- [ ] SafeAreaHelper
- [ ] AnchorPresets
- [ ] UIExtensions
- [ ] UIConstants
- [ ] UIFrameworkSettings

### Этап 17: Resources
- [ ] Default sprites (rounded rects, shadows, circles)
- [ ] Default theme assets
- [ ] All component prefabs
- [ ] Screen template prefabs
- [ ] Transition preset assets

### Этап 18: Testing & Documentation
- [ ] Runtime tests (theming, tokens, navigation)
- [ ] Editor tests (UACF handlers, builders, services)
- [ ] README
- [ ] API documentation
- [ ] Cursor Rules / CLAUDE.md с полным описанием UI API