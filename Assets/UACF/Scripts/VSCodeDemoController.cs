using UnityEngine;
using UnityEngine.UIElements;

public class VSCodeDemoController : MonoBehaviour
{
    [SerializeField] UIDocument _uiDocument;

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
}
