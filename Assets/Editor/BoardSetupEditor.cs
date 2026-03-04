using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// 씬에서 7x7 보드 UI를 에디터에서 생성하는 도구.
/// GridCell 프리팹을 7x7로 배치.
/// </summary>
public static class BoardSetupEditor
{
    private const string GridCellPrefabPath = "Assets/Prefabs/GridCell.prefab";

    private static GameObject GetOrCreateGridCellPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GridCellPrefabPath);
        if (prefab != null) return prefab;

        var cell = CreateCellObject(0, 0);
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        prefab = PrefabUtility.SaveAsPrefabAsset(cell, GridCellPrefabPath);
        Object.DestroyImmediate(cell);
        return prefab;
    }

    [MenuItem("Board Game/Create GridCell Prefab")]
    public static void CreateGridCellPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(GridCellPrefabPath);
        if (existing != null)
        {
            Debug.Log($"GridCell 프리팹이 이미 존재합니다: {GridCellPrefabPath}");
            Selection.activeObject = existing;
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        var cell = CreateCellObject(0, 0);
        var prefab = PrefabUtility.SaveAsPrefabAsset(cell, GridCellPrefabPath);
        Object.DestroyImmediate(cell);
        AssetDatabase.SaveAssets();
        Debug.Log($"GridCell 프리팹 생성됨: {GridCellPrefabPath}");
        Selection.activeObject = prefab;
    }
    private static System.Type GetBoardResolutionAdapterType()
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType("BoardResolutionAdapter");
            if (type != null) return type;
        }
        return null;
    }

    private static Component AddOrGetResolutionAdapter(GameObject go, RectTransform rect, GridLayoutGroup grid)
    {
        var type = GetBoardResolutionAdapterType();
        if (type == null)
        {
            Debug.LogWarning("BoardResolutionAdapter 타입을 찾을 수 없습니다.");
            return null;
        }
        var adapter = go.GetComponent(type) ?? go.AddComponent(type);
        var so = new SerializedObject(adapter);
        so.FindProperty("boardRect").objectReferenceValue = rect;
        so.FindProperty("gridLayout").objectReferenceValue = grid;
        so.ApplyModifiedPropertiesWithoutUndo();
        return adapter;
    }

    [MenuItem("Board Game/Add Resolution Adapter to Selected")]
    public static void AddResolutionAdapterToSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogWarning("보드 오브젝트를 선택한 뒤 실행하세요.");
            return;
        }
        var rect = go.GetComponent<RectTransform>();
        var grid = go.GetComponent<GridLayoutGroup>();
        if (rect == null || grid == null)
        {
            Debug.LogWarning("RectTransform과 GridLayoutGroup이 있는 보드 오브젝트를 선택하세요.");
            return;
        }
        if (AddOrGetResolutionAdapter(go, rect, grid) != null)
            Debug.Log("BoardResolutionAdapter 추가됨.");
    }

    [MenuItem("Board Game/Create 7x7 Board in Scene")]
    public static void CreateBoardInScene()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100;

        var boardObj = new GameObject("Board");
        boardObj.transform.SetParent(canvas.transform, false);

        var rect = boardObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600, 600);

        var gridLayout = boardObj.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(80, 80);
        gridLayout.spacing = new Vector2(4, 4);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 7;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        AddOrGetResolutionAdapter(boardObj, rect, gridLayout);

        var cellPrefab = GetOrCreateGridCellPrefab();
        if (cellPrefab == null)
        {
            Debug.LogError("GridCell 프리팹을 찾을 수 없습니다. Board Game > Create GridCell Prefab을 먼저 실행하세요.");
            return;
        }

        for (int i = 0; i < 49; i++)
        {
            var cellObj = (GameObject)PrefabUtility.InstantiatePrefab(cellPrefab);
            cellObj.transform.SetParent(boardObj.transform, false);
            cellObj.name = $"Cell_{i / 7}_{i % 7}";
        }

        var gameObj = new GameObject("BoardGame");
        var boardManager = gameObj.AddComponent<BoardManager>();
        gameObj.AddComponent<GameManager>();

        var so = new SerializedObject(boardManager);
        so.FindProperty("boardContainer").objectReferenceValue = rect;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = boardObj;
        Undo.RegisterCreatedObjectUndo(boardObj, "Create Board");
        Undo.RegisterCreatedObjectUndo(gameObj, "Create Board");
    }

    private static GameObject CreateCellObject(int row, int col)
    {
        var obj = new GameObject("GridCell");
        obj.AddComponent<RectTransform>();

        var bg = obj.AddComponent<Image>();
        bg.color = (row + col) % 2 == 0 ? new Color(0.9f, 0.85f, 0.75f) : new Color(0.75f, 0.7f, 0.6f);

        var button = obj.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 0.8f);
        colors.pressedColor = new Color(0.9f, 0.9f, 0.7f);
        button.colors = colors;

        var pieceObj = new GameObject("Piece");
        pieceObj.transform.SetParent(obj.transform, false);
        var pieceRect = pieceObj.AddComponent<RectTransform>();
        pieceRect.anchorMin = Vector2.zero;
        pieceRect.anchorMax = Vector2.one;
        pieceRect.offsetMin = new Vector2(8, 8);
        pieceRect.offsetMax = new Vector2(-8, -8);
        var pieceImg = pieceObj.AddComponent<Image>();
        pieceImg.raycastTarget = false;
        pieceImg.color = Color.clear;

        var cell = obj.AddComponent<GridCell>();
        cell.Row = row;
        cell.Col = col;
        cell.SetImageReferences(bg, pieceImg);

        return obj;
    }
}
