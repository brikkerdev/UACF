using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// C# logic to approximate CORTEX HTML demo: matrix scanner, blink, slider sync, wave selector, animated graphs.
/// </summary>
public class NeuralCodeController : MonoBehaviour
{
    const int RowCount = 14;
    const float ScannerInterval = 0.12f;
    const float BlinkInterval = 0.5f;
    const int GraphDataLength = 50;

    [SerializeField] UIDocument _uiDocument;

    VisualElement _scannerBar;
    VisualElement _blinkIndicator;
    List<Label> _rowDataLabels = new List<Label>();
    List<Slider> _sliders = new List<Slider>();
    List<Label> _paramValLabels = new List<Label>();
    List<VisualElement> _waveOpts = new List<VisualElement>();
    VisualElement _graphCanvas1;
    VisualElement _graphCanvas2;

    int _currentScannerRow;
    float _scannerTimer;
    bool _blinkVisible = true;
    float _blinkTimer;
    float[] _graphDataLoss = new float[GraphDataLength];
    float[] _graphDataAcc = new float[GraphDataLength];
    float _graphTime;

    static readonly (float min, float max, int decimals)[] SliderRanges =
    {
        (0.001f, 0.1f, 3),
        (0.1f, 1f, 2),
        (16f, 128f, 0),
        (0f, 255f, 0)
    };

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

        _scannerBar = root.Q<VisualElement>("scanner-bar");
        _blinkIndicator = root.Q<Label>("header-status-indicator");

        for (int i = 0; i < RowCount; i++)
        {
            var pathLabel = root.Q<Label>($"tree-path-{i}");
            if (pathLabel != null) _rowDataLabels.Add(pathLabel);
        }

        var paramVal1 = root.Q<Label>("param-val-1");
        var paramVal2 = root.Q<Label>("param-val-2");
        var paramVal3 = root.Q<Label>("param-val-3");
        var paramVal4 = root.Q<Label>("param-val-4");
        if (paramVal1 != null) _paramValLabels.Add(paramVal1);
        if (paramVal2 != null) _paramValLabels.Add(paramVal2);
        if (paramVal3 != null) _paramValLabels.Add(paramVal3);
        if (paramVal4 != null) _paramValLabels.Add(paramVal4);

        var slider1 = root.Q<Slider>("slider-1");
        var slider2 = root.Q<Slider>("slider-2");
        var slider3 = root.Q<Slider>("slider-3");
        var slider4 = root.Q<Slider>("slider-4");
        if (slider1 != null) { _sliders.Add(slider1); BindSlider(slider1, 0); UpdateParamLabel(0, slider1.value); }
        if (slider2 != null) { _sliders.Add(slider2); BindSlider(slider2, 1); UpdateParamLabel(1, slider2.value); }
        if (slider3 != null) { _sliders.Add(slider3); BindSlider(slider3, 2); UpdateParamLabel(2, slider3.value); }
        if (slider4 != null) { _sliders.Add(slider4); BindSlider(slider4, 3); UpdateParamLabel(3, slider4.value); }

        var wave1 = root.Q<Button>("wave-opt-1");
        var wave2 = root.Q<Button>("wave-opt-2");
        var wave3 = root.Q<Button>("wave-opt-3");
        if (wave1 != null) { _waveOpts.Add(wave1); wave1.clicked += () => SetWaveActive(0); }
        if (wave2 != null) { _waveOpts.Add(wave2); wave2.clicked += () => SetWaveActive(1); }
        if (wave3 != null) { _waveOpts.Add(wave3); wave3.clicked += () => SetWaveActive(2); }

        _graphCanvas1 = root.Q<VisualElement>("graph-canvas-1");
        _graphCanvas2 = root.Q<VisualElement>("graph-canvas-2");

        for (int i = 0; i < GraphDataLength; i++)
        {
            _graphDataLoss[i] = 0.5f;
            _graphDataAcc[i] = 0.5f;
        }

        if (_graphCanvas1 != null) _graphCanvas1.generateVisualContent += OnGenerateGraphContent1;
        if (_graphCanvas2 != null) _graphCanvas2.generateVisualContent += OnGenerateGraphContent2;
    }

    void OnDisable()
    {
        if (_graphCanvas1 != null) _graphCanvas1.generateVisualContent -= OnGenerateGraphContent1;
        if (_graphCanvas2 != null) _graphCanvas2.generateVisualContent -= OnGenerateGraphContent2;
    }

    void BindSlider(Slider slider, int index)
    {
        if (index >= _paramValLabels.Count) return;
        slider.RegisterValueChangedCallback(evt => UpdateParamLabel(index, evt.newValue));
    }

    void UpdateParamLabel(int index, float sliderValue)
    {
        if (index >= _paramValLabels.Count) return;
        var (min, max, decimals) = SliderRanges[index];
        var val = min + (sliderValue / 100f) * (max - min);
        _paramValLabels[index].text = val.ToString(decimals == 0 ? "0" : "0." + new string('0', decimals));
    }

    void SetWaveActive(int index)
    {
        for (int i = 0; i < _waveOpts.Count; i++)
            _waveOpts[i].EnableInClassList("wave-opt-active", i == index);
    }

    void Update()
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;

        _scannerTimer += Time.deltaTime;
        if (_scannerTimer >= ScannerInterval)
        {
            _scannerTimer = 0;
            UpdateMatrixScanner();
        }

        _blinkTimer += Time.deltaTime;
        if (_blinkTimer >= BlinkInterval)
        {
            _blinkTimer = 0;
            _blinkVisible = !_blinkVisible;
            if (_blinkIndicator != null)
                _blinkIndicator.style.opacity = _blinkVisible ? 1f : 0f;
        }

        _graphTime += Time.deltaTime;
        UpdateGraphData();
        if (_graphCanvas1 != null) _graphCanvas1.MarkDirtyRepaint();
        if (_graphCanvas2 != null) _graphCanvas2.MarkDirtyRepaint();
    }

    void UpdateMatrixScanner()
    {
        if (_rowDataLabels.Count == 0 || _scannerBar == null) return;

        var prevRow = (_currentScannerRow - 1 + RowCount) % RowCount;

        if (_currentScannerRow < _rowDataLabels.Count)
        {
            _rowDataLabels[_currentScannerRow].text = $"{GenerateHex().Substring(0, 4)} {GenerateHex().Substring(0, 4)} [ACT]";
            _rowDataLabels[_currentScannerRow].style.color = new Color(1, 1, 1);
        }
        if (prevRow < _rowDataLabels.Count)
        {
            _rowDataLabels[prevRow].text = "-- -- -- --";
            _rowDataLabels[prevRow].style.color = new Color(0.69f, 0.69f, 0.69f);
        }

        var rowHeight = 24f;
        _scannerBar.style.top = _currentScannerRow * rowHeight;
        _currentScannerRow = (_currentScannerRow + 1) % RowCount;
    }

    static string GenerateHex()
    {
        return "0x" + UnityEngine.Random.Range(0, 0x1000000).ToString("X6");
    }

    void UpdateGraphData()
    {
        var noiseLoss = (UnityEngine.Random.value - 0.5f) * 0.1f;
        var trendLoss = Mathf.Sin(_graphTime) * 0.3f + 0.5f;
        var newLoss = Mathf.Clamp01(trendLoss + noiseLoss);

        var noiseAcc = (UnityEngine.Random.value - 0.5f) * 0.08f;
        var trendAcc = Mathf.Sin(_graphTime * 0.4f) * 0.25f + 0.55f;
        var newAcc = Mathf.Clamp01(trendAcc + noiseAcc);

        ShiftArray(_graphDataLoss, newLoss);
        ShiftArray(_graphDataAcc, newAcc);
    }

    static void ShiftArray(float[] arr, float newVal)
    {
        for (int i = 0; i < arr.Length - 1; i++)
            arr[i] = arr[i + 1];
        arr[arr.Length - 1] = newVal;
    }

    void OnGenerateGraphContent1(MeshGenerationContext ctx) => DrawGraph(ctx, _graphDataLoss);
    void OnGenerateGraphContent2(MeshGenerationContext ctx) => DrawGraph(ctx, _graphDataAcc);

    void DrawGraph(MeshGenerationContext ctx, float[] data)
    {
        var el = ctx.visualElement;
        var w = el.contentRect.width;
        var h = el.contentRect.height;
        if (w <= 0 || h <= 0) return;

        var painter = ctx.painter2D;
        painter.lineWidth = 1.5f;
        painter.strokeColor = Color.white;
        painter.BeginPath();
        painter.MoveTo(new Vector2(0, h - data[0] * h));
        var step = w / (data.Length - 1);
        for (int i = 1; i < data.Length; i++)
            painter.LineTo(new Vector2(i * step, h - data[i] * h));
        painter.Stroke();
    }
}
