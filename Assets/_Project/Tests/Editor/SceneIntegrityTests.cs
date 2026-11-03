using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Проверка сцен этажей и магазина без запуска игры: меню паузы, кнопки, магазин, трофейное оружие врагов.
/// Сцена открывается рядом с текущей (Additive) и закрывается после проверки, открытая у разработчика не трогается.
/// </summary>
public class SceneIntegrityTests
{
    private const string Folder = "Assets/_Project/Scenes/";

    private static readonly string[] PlayerScenes =
    {
        "Lobby", "TheFirstLevel", "Basement", "HighFloor", "HappyRoomAndSmallBossfight", "BossFight",
    };

    private static readonly string[] FloorScenes =
    {
        "TheFirstLevel", "Basement", "HighFloor", "HappyRoomAndSmallBossfight", "BossFight",
    };

    // Этажи, которые завершаются зачисткой (в BossFight — вертолёт и свой сценарий победы)
    private static readonly string[] ClearableFloorScenes =
    {
        "TheFirstLevel", "Basement", "HighFloor", "HappyRoomAndSmallBossfight",
    };

    private Scene scene;
    private bool openedByTest;

    [TearDown]
    public void TearDown()
    {
        if (openedByTest && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        openedByTest = false;
    }

    private Scene Open(string name)
    {
        string path = Folder + name + ".unity";
        scene = SceneManager.GetSceneByPath(path);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            openedByTest = true;
        }
        return scene;
    }

    private static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        var result = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
        return result;
    }

    [Test]
    public void BuildSettings_ContainAllScenesInOrder()
    {
        string[] expected = { "LobbyScene", "Lobby", "TheFirstLevel", "Basement", "HighFloor", "HappyRoomAndSmallBossfight", "BossFight" };
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        Assert.AreEqual(expected.Length, scenes.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(Folder + expected[i] + ".unity", scenes[i].path);
            Assert.IsTrue(scenes[i].enabled, expected[i]);
        }
        Assert.AreEqual(GameState.UpgradeShopScene, 1);
        Assert.AreEqual(GameState.FirstFloorScene, 2);
    }

    [TestCaseSource(nameof(PlayerScenes))]
    public void PauseMenu_IsWorldSpaceAndClickableByController(string name)
    {
        Scene s = Open(name);
        List<PlayerHealth> players = FindInScene<PlayerHealth>(s);
        Assert.AreEqual(1, players.Count, "игрок в сцене один");

        GameObject menu = players[0].MenuCanvas;
        Assert.IsNotNull(menu, "MenuCanvas не назначен");
        Canvas canvas = menu.GetComponentInParent<Canvas>(true);
        Assert.IsNotNull(canvas);
        Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, "в VR меню — объект в мире, а не оверлей");
        Assert.IsNotNull(canvas.GetComponent<TrackedDeviceGraphicRaycaster>(), "без него контроллер не нажмёт кнопки");
        Assert.Less(canvas.transform.lossyScale.x, 0.01f, "мировой канвас должен быть уменьшен до размеров руки");

        foreach (Button button in menu.GetComponentsInChildren<Button>(true))
        {
            AssertHasPersistentListener(button);
        }
    }

    [TestCaseSource(nameof(PlayerScenes))]
    public void AllButtons_HaveWorkingListeners(string name)
    {
        Scene s = Open(name);
        foreach (Button button in FindInScene<Button>(s))
        {
            // Кнопки магазина подписываются из кода (UpgradeCardView, UpgradeShop)
            if (button.GetComponentInParent<UpgradeCardView>(true) != null) continue;
            if (button.GetComponentInParent<UpgradeShop>(true) != null) continue;
            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                Object target = button.onClick.GetPersistentTarget(i);
                string method = button.onClick.GetPersistentMethodName(i);
                Assert.IsNotNull(target, $"{Path(button.transform)}: слушатель {i} без объекта");
                Assert.IsFalse(string.IsNullOrEmpty(method), $"{Path(button.transform)}: слушатель {i} без метода");
                Assert.IsNotNull(UnityEventBase.GetValidMethodInfo(target, method, new System.Type[0]) ??
                                 FindAnyOverload(target, method), $"{Path(button.transform)}: нет метода {target.GetType().Name}.{method}");
            }
        }
    }

    [Test]
    public void Lobby_ShopHasFiveFilledPlaces()
    {
        Scene s = Open("Lobby");
        List<UpgradeShop> shops = FindInScene<UpgradeShop>(s);
        Assert.AreEqual(1, shops.Count);

        var serialized = new SerializedObject(shops[0]);
        SerializedProperty places = serialized.FindProperty("places");
        Assert.AreEqual(5, places.arraySize);
        int itemPlaces = 0;
        for (int i = 0; i < places.arraySize; i++)
        {
            SerializedProperty place = places.GetArrayElementAtIndex(i);
            Assert.IsNotNull(place.FindPropertyRelative("view").objectReferenceValue, $"место {i + 1} без карточки");
            if (place.FindPropertyRelative("requiresFoundItem").boolValue) itemPlaces++;
        }
        Assert.AreEqual(1, itemPlaces, "одно место открывается за найденный предмет");

        foreach (string field in new[] { "eddiesText", "rerollButton", "rerollText", "healButton", "healText" })
        {
            Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, $"не назначено {field}");
        }
    }

    [TestCaseSource(nameof(FloorScenes))]
    public void EnemyGuns_DropAPickup(string name)
    {
        Scene s = Open(name);
        foreach (EnemyGun gun in FindInScene<EnemyGun>(s))
        {
            Object pickup = new SerializedObject(gun).FindProperty("pickupPrefab").objectReferenceValue;
            Assert.IsNotNull(pickup, $"{Path(gun.transform)}: нет оружия-трофея");
        }
    }

    [TestCaseSource(nameof(ClearableFloorScenes))]
    public void Floor_LevelManagerListsEveryEnemy(string name)
    {
        Scene s = Open(name);
        List<LevelManager> managers = FindInScene<LevelManager>(s);
        Assert.AreEqual(1, managers.Count);

        GameObject[] enemies = managers[0].enemies;
        Assert.IsNotEmpty(enemies, "пустой список врагов: награда за зачистку выдастся сразу");
        for (int i = 0; i < enemies.Length; i++)
        {
            Assert.IsNotNull(enemies[i], $"враг {i} в списке пустой");
        }

        // Враг, которого нет в списке, не мешает «зачистить» этаж, пока он жив
        foreach (EnemyStateManager enemy in FindInScene<EnemyStateManager>(s))
        {
            bool listed = System.Array.Exists(enemies, e => e != null && (enemy.transform.IsChildOf(e.transform) || e.transform.IsChildOf(enemy.transform)));
            Assert.IsTrue(listed, $"{Path(enemy.transform)} не внесён в LevelManager.enemies");
        }
    }

    [TestCaseSource(nameof(PlayerScenes))]
    public void PlayerRig_HasValidCharacterController(string name)
    {
        Scene s = Open(name);
        foreach (CharacterController controller in FindInScene<CharacterController>(s))
        {
            // При нуле Unity пишет в консоль «Step Offset must be positive» на каждой загрузке сцены
            Assert.Greater(controller.stepOffset, 0f, Path(controller.transform));
            Assert.LessOrEqual(controller.stepOffset, controller.height + controller.radius * 2f, Path(controller.transform));
        }
    }

    private static void AssertHasPersistentListener(Button button)
    {
        Assert.Greater(button.onClick.GetPersistentEventCount(), 0, $"{Path(button.transform)}: кнопка ничего не делает");
    }

    private static System.Reflection.MethodInfo FindAnyOverload(Object target, string method)
    {
        foreach (var info in target.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (info.Name == method) return info;
        }
        return null;
    }

    private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
}
