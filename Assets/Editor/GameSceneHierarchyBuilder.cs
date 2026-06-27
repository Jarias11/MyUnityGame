using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public static class GameSceneHierarchyBuilder
{
    private static int created;
    private static int configured;
    private static int already;

    [MenuItem("Tools/David/Build GameScene Hierarchy")]
    public static void BuildGameScene()
    {
        created = 0;
        configured = 0;
        already = 0;

        BuildGlobal();
        BuildWorld();
        BuildObjects();
        BuildInteractables();
        BuildPlayer();
        BuildCanvas();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log(
            "========== DAVID GAME SCENE BUILDER ==========\n" +
            $"Created: {created}\n" +
            $"Already Existed: {already}\n" +
            $"Configured: {configured}\n" +
            "Safe mode: nothing deleted, nothing renamed.\n" +
            "=============================================="
        );
    }

    private static void BuildGlobal()
    {
        GameObject global = CreateRoot("Global");

        CreateChild(global, "GameManager");
        CreateChild(global, "SaveManager");
        CreateChild(global, "TimeManager");
        CreateChild(global, "SoundManager");
        CreateChild(global, "InputManager");

        GameObject inventory = CreateChild(global, "Inventory");
        CreateChild(inventory, "InventoryManager");
        CreateChild(inventory, "ToolbarManager");
        CreateChild(inventory, "EquipmentManager");
        CreateChild(inventory, "StorageManager");

        GameObject audio = CreateChild(global, "Audio");
        CreateChild(audio, "Music");
        CreateChild(audio, "Ambient");
        CreateChild(audio, "UI");
        CreateChild(audio, "SFX");
        CreateChild(audio, "Footsteps");
        CreateChild(audio, "MachineSounds");

        GameObject saveData = CreateChild(global, "SaveData");
        CreateChild(saveData, "Player");
        CreateChild(saveData, "World");
        CreateChild(saveData, "Machines");
        CreateChild(saveData, "NPC");
        CreateChild(saveData, "Crops");
        CreateChild(saveData, "Inventory");
        CreateChild(saveData, "Settings");
    }

    private static void BuildWorld()
    {
        GameObject world = CreateRoot("World");

        GameObject grid = CreateChild(world, "Grid");
        if (grid.GetComponent<Grid>() == null)
        {
            grid.AddComponent<Grid>();
            configured++;
        }

        CreateTilemap(grid, "Ground", -100);
        CreateTilemap(grid, "Water", -90);
        CreateTilemap(grid, "GroundDecor", -50);
        CreateTilemap(grid, "Paths", -45);
        CreateTilemap(grid, "TilledGround", -40);
        CreateTilemap(grid, "CropTilemap", -35);
        CreateTilemap(grid, "Cliffs", -30);
        CreateTilemap(grid, "Bridges", -25);
        CreateTilemap(grid, "Collision", 0);
        CreateTilemap(grid, "HighlightTilemap", 100);
        CreateTilemap(grid, "WeatherEffects", 150);
    }

    private static void BuildObjects()
    {
        GameObject objects = CreateRoot("Objects");

        CreateChild(objects, "Buildings");
        CreateChild(objects, "Trees");
        CreateChild(objects, "Rocks");
        CreateChild(objects, "Decorations");

        GameObject machines = CreateChild(objects, "Machines");
        CreateChild(machines, "PlacedMachines");
        CreateChild(machines, "PreviewMachine");
        CreateChild(machines, "MachineEffects");
        CreateChild(machines, "PowerLines");
        CreateChild(machines, "MachineParticles");
    }

    private static void BuildInteractables()
    {
        GameObject interactables = CreateRoot("Interactables");

        GameObject npcs = CreateChild(interactables, "NPCs");
        CreateChild(npcs, "Villagers");
        CreateChild(npcs, "Animals");
        CreateChild(npcs, "Enemies");
        CreateChild(npcs, "Merchants");
        CreateChild(npcs, "Companions");

        GameObject crops = CreateChild(interactables, "CropSystem");
        CreateChild(crops, "CropObjects");
        CreateChild(crops, "Growing");
        CreateChild(crops, "Harvestable");
        CreateChild(crops, "Dead");
        CreateChild(crops, "CropEffects");

        CreateChild(interactables, "Items");
        CreateChild(interactables, "Chests");
        CreateChild(interactables, "Doors");
    }

    private static void BuildPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = CreateRoot("Player");
        else
            already++;

        CreateChild(player, "Sprite");
        CreateChild(player, "Shadow");
        CreateChild(player, "AnimatorRoot");
        CreateChild(player, "FootstepPoint");
        CreateChild(player, "InteractionPoint");
        CreateChild(player, "HoldItemPoint");
        CreateChild(player, "WeaponPoint");
        CreateChild(player, "HatPoint");
    }

    private static void BuildCanvas()
    {
        GameObject canvasObj = GameObject.Find("Canvas");

        if (canvasObj == null)
        {
            canvasObj = CreateRoot("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            configured++;
        }

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            configured++;
        }

        GameObject hud = CreateUIChild(canvasObj.transform, "HUD");
        CreateUIChild(hud.transform, "TopLeft");
        CreateUIChild(hud.transform, "TopRight");
        CreateUIChild(hud.transform, "BottomBar");

        CreateUIChild(canvasObj.transform, "InventoryPanel");
        CreateUIChild(canvasObj.transform, "CraftPanel");
        CreateUIChild(canvasObj.transform, "DialoguePanel");
        CreateUIChild(canvasObj.transform, "NotificationPanel");
        CreateUIChild(canvasObj.transform, "Tooltip");
        CreateUIChild(canvasObj.transform, "PauseMenu");

        GameObject fade = CreateUIChild(canvasObj.transform, "FadeOverlay");
        ConfigureFadeOverlay(fade);
        fade.transform.SetAsLastSibling();
    }

    private static GameObject CreateRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            already++;
            return existing;
        }

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        created++;
        return go;
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null)
        {
            already++;
            return existing.gameObject;
        }

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        created++;
        return go;
    }

    private static GameObject CreateUIChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            already++;
            return existing.gameObject;
        }

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create UI " + name);
        go.transform.SetParent(parent);
        go.transform.localScale = Vector3.one;

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.localRotation = Quaternion.identity;

        created++;
        return go;
    }

    private static void CreateTilemap(GameObject grid, string name, int sortingOrder)
    {
        Transform existing = grid.transform.Find(name);

        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
            already++;
        }
        else
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Tilemap " + name);
            go.transform.SetParent(grid.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            created++;
        }

        if (go.GetComponent<Tilemap>() == null)
        {
            go.AddComponent<Tilemap>();
            configured++;
        }

        TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = go.AddComponent<TilemapRenderer>();
            configured++;
        }

        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;
        configured++;
    }

    private static void ConfigureFadeOverlay(GameObject fade)
    {
        Image image = fade.GetComponent<Image>();
        if (image == null)
        {
            image = fade.AddComponent<Image>();
            configured++;
        }

        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

        RectTransform rect = fade.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        configured++;
    }

    [MenuItem("Tools/David/Validate GameScene")]
    public static void ValidateScene()
    {
        string report =
            "========== GAME SCENE VALIDATION ==========\n" +
            Check("Main Camera") +
            Check("Canvas") +
            Check("EventSystem") +
            Check("Player") +
            Check("Global") +
            Check("World") +
            Check("Objects") +
            Check("Interactables") +
            Check("World/Grid") +
            Check("World/Grid/Ground") +
            Check("World/Grid/Collision") +
            Check("World/Grid/HighlightTilemap") +
            Check("Canvas/HUD") +
            Check("Canvas/FadeOverlay") +
            "===========================================";

        Debug.Log(report);
    }

    private static string Check(string path)
    {
        GameObject found = GameObject.Find(path);
        return found != null ? $"✓ {path}\n" : $"⚠ Missing: {path}\n";
    }
}