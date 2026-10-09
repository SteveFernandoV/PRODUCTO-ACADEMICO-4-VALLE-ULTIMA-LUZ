using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections.Generic;

public sealed class PA3GameManager : MonoBehaviour
{
    public static PA3GameManager Instance { get; private set; }
    public Text objectiveText, statusText;
    public Text collectionTotalText;
    public Image collectionProgress;
    public int totalCollectibles = 6, collected;
    public bool cinematicFinished;
    GameObject crystalMapPanel;
    GameObject levelTransitionPanel;
    RectTransform crystalMapArea;
    RectTransform playerMapMarker;
    RectTransform portalMapMarker;
    Text mapHeader;
    readonly List<RectTransform> crystalMapMarkers = new List<RectTransform>();
    Collectible[] activeCrystals = new Collectible[0];
    Transform compassPlayer;
    Transform portalTransform;
    Vector2 mapWorldMin;
    Vector2 mapWorldSize = new Vector2(100f, 100f);
    float nextMapRefresh;

    void Awake()
    {
        Instance = this;
        RefreshUi();
        CreateCrystalMap();
        RefreshCrystalMapData();
        if (collected >= totalCollectibles) PA3PortalReadyVisual.ShowReady();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.mKey.wasPressedThisFrame && crystalMapPanel != null)
        {
            crystalMapPanel.SetActive(!crystalMapPanel.activeSelf);
            if (crystalMapPanel.activeSelf) RefreshCrystalMapData();
        }

        if (compassPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) compassPlayer = playerObject.transform;
        }
        if (Time.unscaledTime >= nextMapRefresh)
        {
            nextMapRefresh = Time.unscaledTime + 0.25f;
            RefreshCrystalMapData();
        }
        UpdateMapPlayerMarker();
    }

    void CreateCrystalMap()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("Mapa de cristales (M)", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(430f, 360f);
        panel.GetComponent<Image>().color = new Color(0.015f, 0.045f, 0.065f, 0.94f);
        crystalMapPanel = panel;

        GameObject hint = new GameObject("Ayuda para abrir mapa", typeof(RectTransform), typeof(Image));
        hint.transform.SetParent(canvas.transform, false);
        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-24f, -24f);
        hintRect.sizeDelta = new Vector2(112f, 30f);
        hint.GetComponent<Image>().color = new Color(0.015f, 0.045f, 0.065f, 0.82f);
        CreateMapText("Tecla", hint.transform, "M  |  MAPA", 14,
            new Color(0.7f, 1f, 0.93f), Vector2.zero, new Vector2(108f, 28f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        mapHeader = CreateMapText("Título", panel.transform, "MAPA DEL VALLE", 20,
            new Color(0.7f, 1f, 0.93f), new Vector2(0f, -12f), new Vector2(390f, 34f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        CreateMapText("Ayuda", panel.transform, "PRESIONA M PARA CERRAR", 12,
            new Color(0.72f, 0.82f, 0.86f), new Vector2(0f, 8f), new Vector2(300f, 20f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        CreateMapLegend(panel.transform);

        GameObject area = new GameObject("Área del mapa", typeof(RectTransform), typeof(Image));
        area.transform.SetParent(panel.transform, false);
        crystalMapArea = area.GetComponent<RectTransform>();
        crystalMapArea.anchorMin = new Vector2(0.08f, 0.18f);
        crystalMapArea.anchorMax = new Vector2(0.92f, 0.82f);
        crystalMapArea.offsetMin = Vector2.zero;
        crystalMapArea.offsetMax = Vector2.zero;
        area.GetComponent<Image>().color = new Color(0.06f, 0.13f, 0.15f, 1f);

        CreateMapGridLine("Cuadrícula vertical", crystalMapArea, true, 0.25f);
        CreateMapGridLine("Cuadrícula vertical", crystalMapArea, true, 0.5f);
        CreateMapGridLine("Cuadrícula vertical", crystalMapArea, true, 0.75f);
        CreateMapGridLine("Cuadrícula horizontal", crystalMapArea, false, 0.25f);
        CreateMapGridLine("Cuadrícula horizontal", crystalMapArea, false, 0.5f);
        CreateMapGridLine("Cuadrícula horizontal", crystalMapArea, false, 0.75f);
        CreateMapText("Norte", area.transform, "N", 14, Color.white,
            new Vector2(0f, -2f), new Vector2(22f, 20f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        playerMapMarker = CreateMapArrow(crystalMapArea, "Jugador", new Color(0.25f, 0.95f, 1f), 20f);
        ExitTrigger portal = Object.FindAnyObjectByType<ExitTrigger>();
        if (portal != null) portalTransform = portal.transform;
        portalMapMarker = CreatePortalMapIcon(crystalMapArea, "Portal");
        panel.SetActive(false);
    }

    void CreateMapLegend(Transform parent)
    {
        RectTransform playerIcon = CreateMapArrow(parent as RectTransform, "Leyenda jugador", new Color(0.25f, 0.95f, 1f), 14f);
        playerIcon.anchorMin = playerIcon.anchorMax = new Vector2(0.5f, 0f);
        playerIcon.anchoredPosition = new Vector2(-145f, 37f);
        CreateMapText("Leyenda jugador texto", parent, "TÚ", 11, Color.white,
            new Vector2(-126f, 37f), new Vector2(44f, 20f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        RectTransform crystalIcon = CreateDiamondMapIcon(parent, "Leyenda cristal", new Vector2(-22f, 37f), 12f);
        crystalIcon.anchorMin = crystalIcon.anchorMax = new Vector2(0.5f, 0f);
        CreateMapText("Leyenda cristal texto", parent, "CRISTALES", 11, Color.white,
            new Vector2(18f, 37f), new Vector2(82f, 20f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        RectTransform portalIcon = CreatePortalMapIcon(parent as RectTransform, "Leyenda portal");
        portalIcon.anchorMin = portalIcon.anchorMax = new Vector2(0.5f, 0f);
        portalIcon.anchoredPosition = new Vector2(119f, 37f);
        CreateMapText("Leyenda portal texto", parent, "PORTAL", 11, Color.white,
            new Vector2(162f, 37f), new Vector2(62f, 20f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
    }

    static Text CreateMapText(string objectName, Transform parent, string value, int fontSize,
        Color color, Vector2 position, Vector2 size, Vector2 anchor, Vector2 pivot)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    static void CreateMapGridLine(string objectName, RectTransform parent, bool vertical, float position)
    {
        GameObject line = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = vertical ? new Vector2(position, 0f) : new Vector2(0f, position);
        rect.anchorMax = vertical ? new Vector2(position, 1f) : new Vector2(1f, position);
        rect.sizeDelta = vertical ? new Vector2(1f, 0f) : new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        line.GetComponent<Image>().color = new Color(0.5f, 0.8f, 0.78f, 0.22f);
        line.GetComponent<Image>().raycastTarget = false;
    }

    static RectTransform CreateMapArrow(RectTransform parent, string objectName, Color color, float size)
    {
        GameObject arrowObject = new GameObject(objectName, typeof(RectTransform));
        arrowObject.transform.SetParent(parent, false);
        RectTransform arrow = arrowObject.GetComponent<RectTransform>();
        arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(0.5f, 0.5f);
        arrow.sizeDelta = new Vector2(size, size);
        CreateMapArrowPart(arrow, new Vector2(size * 0.17f, size * 0.55f), new Vector2(0f, -size * 0.17f), 0f, color);
        CreateMapArrowPart(arrow, new Vector2(size * 0.16f, size * 0.4f), new Vector2(-size * 0.12f, size * 0.17f), 45f, color);
        CreateMapArrowPart(arrow, new Vector2(size * 0.16f, size * 0.4f), new Vector2(size * 0.12f, size * 0.17f), -45f, color);
        return arrow;
    }

    static RectTransform CreatePortalMapIcon(RectTransform parent, string objectName)
    {
        GameObject iconObject = new GameObject(objectName, typeof(RectTransform));
        iconObject.transform.SetParent(parent, false);
        RectTransform icon = iconObject.GetComponent<RectTransform>();
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(24f, 24f);
        Color glow = new Color(0.42f, 0.9f, 1f);
        CreateMapArrowPart(icon, new Vector2(4f, 16f), new Vector2(-6f, -1f), 0f, glow);
        CreateMapArrowPart(icon, new Vector2(4f, 16f), new Vector2(6f, -1f), 0f, glow);
        CreateMapArrowPart(icon, new Vector2(16f, 4f), new Vector2(0f, 7f), 0f, glow);
        CreateMapArrowPart(icon, new Vector2(5f, 9f), new Vector2(0f, -3f), 0f,
            new Color(0.76f, 0.48f, 1f));
        return icon;
    }

    static RectTransform CreateDiamondMapIcon(Transform parent, string objectName, Vector2 position, float size)
    {
        GameObject iconObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(parent, false);
        RectTransform icon = iconObject.GetComponent<RectTransform>();
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(size, size);
        icon.anchoredPosition = position;
        icon.localRotation = Quaternion.Euler(0f, 0f, 45f);
        iconObject.GetComponent<Image>().color = new Color(1f, 0.75f, 0.16f);

        GameObject glint = new GameObject("Destello", typeof(RectTransform), typeof(Image));
        glint.transform.SetParent(icon, false);
        RectTransform glintRect = glint.GetComponent<RectTransform>();
        glintRect.anchorMin = glintRect.anchorMax = glintRect.pivot = new Vector2(0.5f, 0.5f);
        glintRect.sizeDelta = new Vector2(size * 0.25f, size * 0.25f);
        glintRect.anchoredPosition = new Vector2(-size * 0.16f, size * 0.16f);
        glint.GetComponent<Image>().color = new Color(1f, 1f, 0.82f);
        glint.GetComponent<Image>().raycastTarget = false;
        return icon;
    }

    static void CreateMapArrowPart(RectTransform parent, Vector2 size, Vector2 position, float rotation, Color color)
    {
        GameObject part = new GameObject("Parte de flecha", typeof(RectTransform), typeof(Image));
        part.transform.SetParent(parent, false);
        RectTransform rect = part.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        Image image = part.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    void RefreshCrystalMapData()
    {
        if (crystalMapArea == null) return;
        activeCrystals = Object.FindObjectsByType<Collectible>();
        UpdateMapWorldBounds();
        EnsureCrystalMapMarkers(activeCrystals.Length);
        for (int i = 0; i < crystalMapMarkers.Count; i++)
        {
            bool hasCrystal = i < activeCrystals.Length && activeCrystals[i] != null;
            crystalMapMarkers[i].gameObject.SetActive(hasCrystal);
            if (hasCrystal) crystalMapMarkers[i].anchoredPosition = WorldToMap(activeCrystals[i].transform.position);
        }
        if (portalTransform == null)
        {
            ExitTrigger portal = Object.FindAnyObjectByType<ExitTrigger>();
            if (portal != null) portalTransform = portal.transform;
        }
        if (portalMapMarker != null)
        {
            bool hasPortal = portalTransform != null;
            portalMapMarker.gameObject.SetActive(hasPortal);
            if (hasPortal) portalMapMarker.anchoredPosition = WorldToMap(portalTransform.position);
        }
        if (mapHeader != null)
            mapHeader.text = activeCrystals.Length == 0
                ? "NO HAY CRISTALES ACTIVOS"
                : "MAPA DEL VALLE · " + activeCrystals.Length + " CRISTALES";
        UpdateMapPlayerMarker();
    }

    void EnsureCrystalMapMarkers(int count)
    {
        while (crystalMapMarkers.Count < count)
        {
            crystalMapMarkers.Add(CreateDiamondMapIcon(crystalMapArea, "Cristal en el mapa", Vector2.zero, 14f));
        }
    }

    void UpdateMapWorldBounds()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains.Length > 0)
        {
            float minX = float.PositiveInfinity, minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxZ = float.NegativeInfinity;
            foreach (Terrain terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null) continue;
                Vector3 origin = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                minX = Mathf.Min(minX, origin.x);
                minZ = Mathf.Min(minZ, origin.z);
                maxX = Mathf.Max(maxX, origin.x + size.x);
                maxZ = Mathf.Max(maxZ, origin.z + size.z);
            }
            if (maxX > minX && maxZ > minZ)
            {
                const float zoomedWorldFraction = 0.15f;
                Vector2 terrainCenter = new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);
                mapWorldSize = new Vector2((maxX - minX) * zoomedWorldFraction,
                                           (maxZ - minZ) * zoomedWorldFraction);
                Vector2 viewCenter = compassPlayer != null
                    ? new Vector2(compassPlayer.position.x, compassPlayer.position.z)
                    : terrainCenter;
                mapWorldMin = viewCenter - mapWorldSize * 0.5f;
                return;
            }
        }

        float lowX = compassPlayer != null ? compassPlayer.position.x : 0f;
        float highX = lowX;
        float lowZ = compassPlayer != null ? compassPlayer.position.z : 0f;
        float highZ = lowZ;
        foreach (Collectible crystal in activeCrystals)
        {
            if (crystal == null) continue;
            Vector3 position = crystal.transform.position;
            lowX = Mathf.Min(lowX, position.x); highX = Mathf.Max(highX, position.x);
            lowZ = Mathf.Min(lowZ, position.z); highZ = Mathf.Max(highZ, position.z);
        }
        float width = Mathf.Max(40f, highX - lowX);
        float height = Mathf.Max(40f, highZ - lowZ);
        mapWorldMin = new Vector2((lowX + highX - width) * 0.5f, (lowZ + highZ - height) * 0.5f);
        mapWorldSize = new Vector2(width * 1.25f, height * 1.25f);
        mapWorldMin -= mapWorldSize * 0.1f;
    }

    Vector2 WorldToMap(Vector3 worldPosition)
    {
        float width = crystalMapArea.rect.width;
        float height = crystalMapArea.rect.height;
        float x = (worldPosition.x - mapWorldMin.x) / Mathf.Max(1f, mapWorldSize.x);
        float y = (worldPosition.z - mapWorldMin.y) / Mathf.Max(1f, mapWorldSize.y);
        const float edgeInset = 0.04f;
        return new Vector2(Mathf.Clamp(x, edgeInset, 1f - edgeInset) * width - width * 0.5f,
                           Mathf.Clamp(y, edgeInset, 1f - edgeInset) * height - height * 0.5f);
    }

    void UpdateMapPlayerMarker()
    {
        if (playerMapMarker == null || compassPlayer == null) return;
        playerMapMarker.anchoredPosition = WorldToMap(compassPlayer.position);
        float heading = Vector3.SignedAngle(Vector3.forward, compassPlayer.forward, Vector3.up);
        playerMapMarker.localRotation = Quaternion.Euler(0f, 0f, -heading);
        playerMapMarker.SetAsLastSibling();
    }
    public void Collect() { collected++; RefreshUi(); if (collected >= totalCollectibles) { if (statusText != null) statusText.text = "Ruta despejada: llega al portal de salida"; PA3PortalReadyVisual.ShowReady(); } }
    public void CompleteCinematic() { cinematicFinished = true; if (statusText != null) statusText.text = "Explora el valle y recupera los cristales"; }
    public void ReachExit()
    {
        if (collected < totalCollectibles)
        {
            if (statusText != null) statusText.text = "Aún faltan cristales por recuperar";
            return;
        }
        if (levelTransitionPanel != null) return;
        ShowLevelTransition();
    }

    void ShowLevelTransition()
    {
        bool hasNextLevel = SceneManager.GetActiveScene().name == PA3MenuController.GameplaySceneName;
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            if (hasNextLevel) LoadNextLevel();
            else PA3MenuController.ReturnToMainMenu("¡Completaste el juego y recuperaste los cristales!");
            return;
        }

        CameraFollow3D cameraMode = Object.FindAnyObjectByType<CameraFollow3D>();
        if (cameraMode != null && cameraMode.IsFirstPerson) cameraMode.SetFirstPerson(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        levelTransitionPanel = new GameObject("Panel de nivel completado", typeof(RectTransform), typeof(Image));
        levelTransitionPanel.transform.SetParent(canvas.transform, false);
        RectTransform overlay = levelTransitionPanel.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        levelTransitionPanel.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.04f, 0.88f);

        GameObject card = new GameObject("Tarjeta de resultado", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(levelTransitionPanel.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(490f, 310f);
        card.GetComponent<Image>().color = new Color(0.045f, 0.09f, 0.11f, 0.98f);

        string title = hasNextLevel ? "¡NIVEL COMPLETADO!" : "¡JUEGO COMPLETADO!";
        string message = hasNextLevel
            ? "Recuperaste los cristales. El siguiente nivel ya está disponible."
            : "Recuperaste los cristales y restauraste el valle.";
        CreateMapText("Resultado", card.transform, title, 24, new Color(0.67f, 1f, 0.9f),
            new Vector2(0f, -34f), new Vector2(450f, 46f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        CreateMapText("Mensaje", card.transform, message, 16, Color.white,
            new Vector2(0f, -98f), new Vector2(420f, 54f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        if (hasNextLevel)
            CreateTransitionButton(card.transform, "Siguiente nivel", "IR AL NIVEL 2", new Vector2(0f, -178f), LoadNextLevel);
        else
            CreateTransitionButton(card.transform, "Volver al menú", "VOLVER AL MENÚ", new Vector2(0f, -178f),
                () => PA3MenuController.ReturnToMainMenu("¡Juego completado! Recuperaste los cristales y restauraste el valle."));

        if (hasNextLevel)
            CreateTransitionButton(card.transform, "Menú principal", "MENÚ PRINCIPAL", new Vector2(0f, -240f),
                () => PA3MenuController.ReturnToMainMenu("Nivel 1 completado. Puedes continuar al nivel 2 cuando quieras."), true);
    }

    void LoadNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SEGUNDO NIVEL");
    }

    static void CreateTransitionButton(Transform parent, string objectName, string label,
        Vector2 position, UnityAction action, bool secondary = false)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(270f, 48f);
        Image image = buttonObject.GetComponent<Image>();
        image.color = secondary ? new Color(0.09f, 0.16f, 0.18f) : new Color(0.08f, 0.62f, 0.53f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        CreateMapText("Texto", buttonObject.transform, label, 16, Color.white,
            Vector2.zero, new Vector2(260f, 42f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    }
    void RefreshUi()
    {
        if (objectiveText != null) objectiveText.text = collected.ToString("00");
        if (collectionTotalText != null) collectionTotalText.text = totalCollectibles.ToString("00");
        if (collectionProgress != null) collectionProgress.fillAmount = totalCollectibles <= 0 ? 0f : Mathf.Clamp01((float)collected / totalCollectibles);
        if (statusText != null && collected == 0) statusText.text = "Reúne los cristales de luz para abrir el portal";
    }
}
