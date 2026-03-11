using UnityEngine;
using UnityEngine.UIElements;

public class DemoMenuController : MonoBehaviour
{
    const string ThemeDark = "theme-dark";
    const string ThemeLight = "theme-light";

    [SerializeField] UIDocument _uiDocument;

    VisualElement _mainScreen;
    VisualElement _settingsScreen;
    VisualElement _root;

    void Awake()
    {
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument != null && _uiDocument.panelSettings != null)
        {
            var ps = _uiDocument.panelSettings;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.match = 0.5f;
        }
    }

    void OnEnable()
    {
        if (_uiDocument == null) return;

        var root = _uiDocument.rootVisualElement;
        _root = root.Q<VisualElement>("root");
        _mainScreen = root.Q<VisualElement>("nav-panel");
        _settingsScreen = root.Q<VisualElement>("settings-panel");

        var btnSettings = root.Q<Button>("btn-settings");
        var btnBack = root.Q<Button>("btn-back");
        var toggleTheme = root.Q<Toggle>("toggle-theme");

        if (_mainScreen != null) _mainScreen.style.display = DisplayStyle.Flex;
        if (_settingsScreen != null) _settingsScreen.style.display = DisplayStyle.None;

        if (btnSettings != null)
            btnSettings.clicked += () =>
            {
                if (_mainScreen != null) _mainScreen.style.display = DisplayStyle.None;
                if (_settingsScreen != null) _settingsScreen.style.display = DisplayStyle.Flex;
            };

        if (btnBack != null)
            btnBack.clicked += () =>
            {
                if (_mainScreen != null) _mainScreen.style.display = DisplayStyle.Flex;
                if (_settingsScreen != null) _settingsScreen.style.display = DisplayStyle.None;
            };

        if (toggleTheme != null && _root != null)
        {
            toggleTheme.SetValueWithoutNotify(false);
            toggleTheme.label = "Светлая";
            toggleTheme.RegisterValueChangedCallback(evt =>
            {
                var isLight = evt.newValue;
                _root.RemoveFromClassList(isLight ? ThemeDark : ThemeLight);
                _root.AddToClassList(isLight ? ThemeLight : ThemeDark);
                toggleTheme.label = isLight ? "Тёмная" : "Светлая";
            });
        }
    }
}
