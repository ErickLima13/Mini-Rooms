using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;

namespace App.Editor.Screens
{
    public readonly struct CreateScreenSettings
    {
        public readonly string AppScreensRoot;
        public readonly string ScreenManagerPrefabPath;
        public readonly string PanelSettingsPath;

        public CreateScreenSettings(string appScreensRoot, string screenManagerPrefabPath, string panelSettingsPath)
        {
            AppScreensRoot = appScreensRoot;
            ScreenManagerPrefabPath = screenManagerPrefabPath;
            PanelSettingsPath = panelSettingsPath;
        }

        public static CreateScreenSettings Default => new(
            DefaultAppScreensRoot,
            DefaultScreenManagerPrefabPath,
            DefaultPanelSettingsPath);

        public const string DefaultAppScreensRoot = "Assets/Code/Scripts/App";
        public const string DefaultScreenManagerPrefabPath = "Assets/Level/Prefabs/ScreenManager.prefab";
        public const string DefaultPanelSettingsPath = "Assets/SandBox/NewScreens/Screens/NewResolutionPanelSettings.asset";
    }

    public sealed class CreateScreenWindow : EditorWindow
    {
        private const string PrefAppScreensRoot = "App.Editor.CreateScreen.AppScreensRoot";
        private const string PrefScreenManagerPrefabPath = "App.Editor.CreateScreen.ScreenManagerPrefabPath";
        private const string PrefPanelSettingsPath = "App.Editor.CreateScreen.PanelSettingsPath";

        private string _screenName = string.Empty;
        private DefaultAsset _screensRootFolder;
        private GameObject _screenManagerPrefab;
        private PanelSettings _panelSettings;

        [MenuItem("Tools/Create New Screen...", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<CreateScreenWindow>("New Screen");
            window.minSize = new Vector2(460, 280);
            window.Show();
        }

        private void OnEnable()
        {
            LoadPreferences();
        }

        private void LoadPreferences()
        {
            var screensRootPath = EditorPrefs.GetString(
                PrefAppScreensRoot,
                CreateScreenSettings.DefaultAppScreensRoot);
            var screenManagerPath = EditorPrefs.GetString(
                PrefScreenManagerPrefabPath,
                CreateScreenSettings.DefaultScreenManagerPrefabPath);
            var panelSettingsPath = EditorPrefs.GetString(
                PrefPanelSettingsPath,
                CreateScreenSettings.DefaultPanelSettingsPath);

            _screensRootFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(screensRootPath);
            _screenManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(screenManagerPath);
            _panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);
        }

        private void SavePreferences()
        {
            var settings = BuildSettings();
            EditorPrefs.SetString(PrefAppScreensRoot, settings.AppScreensRoot);
            EditorPrefs.SetString(PrefScreenManagerPrefabPath, settings.ScreenManagerPrefabPath);
            EditorPrefs.SetString(PrefPanelSettingsPath, settings.PanelSettingsPath);
        }

        private CreateScreenSettings BuildSettings()
        {
            return new CreateScreenSettings(
                AssetDatabase.GetAssetPath(_screensRootFolder),
                AssetDatabase.GetAssetPath(_screenManagerPrefab),
                AssetDatabase.GetAssetPath(_panelSettings));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Nova tela (MVVM + ScreenManager)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _screenName = EditorGUILayout.TextField("Nome", _screenName);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Destinos", EditorStyles.boldLabel);

            _screensRootFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Pasta das telas",
                _screensRootFolder,
                typeof(DefaultAsset),
                false);

            _screenManagerPrefab = (GameObject)EditorGUILayout.ObjectField(
                "ScreenManager Prefab",
                _screenManagerPrefab,
                typeof(GameObject),
                false);

            _panelSettings = (PanelSettings)EditorGUILayout.ObjectField(
                "Panel Settings",
                _panelSettings,
                typeof(PanelSettings),
                false);

            EditorGUILayout.HelpBox(
                "Cria scripts, UXML/USS, prefab na pasta escolhida e registra no ScreenManager.",
                MessageType.Info);

            var settings = BuildSettings();
            var canCreate = string.IsNullOrWhiteSpace(_screenName) == false
                && CreateScreenTool.TryValidateSettings(settings, out _);

            EditorGUI.BeginDisabledGroup(!canCreate);
            if (GUILayout.Button("Create Screen", GUILayout.Height(28)))
            {
                SavePreferences();
                CreateScreenTool.StartCreation(_screenName, settings);
            }
            EditorGUI.EndDisabledGroup();

            if (!canCreate
                && string.IsNullOrWhiteSpace(_screenName) == false
                && CreateScreenTool.TryValidateSettings(settings, out var validationError) == false)
            {
                EditorGUILayout.HelpBox(validationError, MessageType.Warning);
            }
        }
    }

    [InitializeOnLoad]
    public static class CreateScreenTool
    {
        private const string PendingBaseNameKey = "App.Editor.CreateScreen.PendingBaseName";
        private const string PendingAppScreensRootKey = "App.Editor.CreateScreen.Pending.AppScreensRoot";
        private const string PendingScreenManagerPrefabPathKey = "App.Editor.CreateScreen.Pending.ScreenManagerPrefabPath";
        private const string PendingPanelSettingsPathKey = "App.Editor.CreateScreen.Pending.PanelSettingsPath";

        static CreateScreenTool()
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            EditorApplication.delayCall += TryCompletePendingCreation;
        }

        public static void StartCreation(string rawName, CreateScreenSettings settings)
        {
            if (!TryValidateSettings(settings, out var settingsError))
            {
                EditorUtility.DisplayDialog("Create Screen", settingsError, "OK");
                return;
            }

            if (!TryNormalizeBaseName(rawName, out var baseName, out var error))
            {
                EditorUtility.DisplayDialog("Create Screen", error, "OK");
                return;
            }

            var folderPath = $"{settings.AppScreensRoot}/{baseName}";
            var screenClassName = $"{baseName}Screen";
            var prefabPath = $"{folderPath}/{screenClassName}.prefab";

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog(
                    "Create Screen",
                    $"A pasta já existe:\n{folderPath}",
                    "OK");
                return;
            }

            if (File.Exists(GetAbsolutePath(prefabPath)))
            {
                EditorUtility.DisplayDialog(
                    "Create Screen",
                    $"O prefab já existe:\n{prefabPath}",
                    "OK");
                return;
            }

            try
            {
                Directory.CreateDirectory(GetAbsolutePath(folderPath));
                WriteScreenFiles(baseName, folderPath);
                SavePendingSettings(baseName, settings);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog(
                    "Create Screen",
                    $"Arquivos criados para {baseName}.\n\nAguardando compilação para gerar prefab e registrar no ScreenManager...",
                    "OK");
            }
            catch (Exception exception)
            {
                ClearPendingSettings();
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Create Screen",
                    $"Falha ao criar arquivos:\n{exception.Message}",
                    "OK");
            }
        }

        public static bool TryValidateSettings(CreateScreenSettings settings, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(settings.AppScreensRoot)
                || !AssetDatabase.IsValidFolder(settings.AppScreensRoot))
            {
                error = "Selecione uma pasta válida em Assets para criar as telas.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.ScreenManagerPrefabPath))
            {
                error = "Selecione o prefab do ScreenManager.";
                return false;
            }

            var screenManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(settings.ScreenManagerPrefabPath);
            if (screenManagerPrefab == null || !settings.ScreenManagerPrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                error = "O ScreenManager precisa ser um prefab válido.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.PanelSettingsPath))
            {
                error = "Selecione um asset de Panel Settings.";
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<PanelSettings>(settings.PanelSettingsPath) == null)
            {
                error = "Panel Settings inválido ou não encontrado.";
                return false;
            }

            return true;
        }

        private static void SavePendingSettings(string baseName, CreateScreenSettings settings)
        {
            SessionState.SetString(PendingBaseNameKey, baseName);
            SessionState.SetString(PendingAppScreensRootKey, settings.AppScreensRoot);
            SessionState.SetString(PendingScreenManagerPrefabPathKey, settings.ScreenManagerPrefabPath);
            SessionState.SetString(PendingPanelSettingsPathKey, settings.PanelSettingsPath);
        }

        private static void ClearPendingSettings()
        {
            SessionState.EraseString(PendingBaseNameKey);
            SessionState.EraseString(PendingAppScreensRootKey);
            SessionState.EraseString(PendingScreenManagerPrefabPathKey);
            SessionState.EraseString(PendingPanelSettingsPathKey);
        }

        private static bool TryLoadPendingSettings(out string baseName, out CreateScreenSettings settings)
        {
            baseName = SessionState.GetString(PendingBaseNameKey, string.Empty);
            if (string.IsNullOrEmpty(baseName))
            {
                settings = default;
                return false;
            }

            settings = new CreateScreenSettings(
                SessionState.GetString(PendingAppScreensRootKey, CreateScreenSettings.DefaultAppScreensRoot),
                SessionState.GetString(PendingScreenManagerPrefabPathKey, CreateScreenSettings.DefaultScreenManagerPrefabPath),
                SessionState.GetString(PendingPanelSettingsPathKey, CreateScreenSettings.DefaultPanelSettingsPath));
            return true;
        }

        private static void OnCompilationFinished(object _)
        {
            EditorApplication.delayCall += TryCompletePendingCreation;
        }

        private static void TryCompletePendingCreation()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (!TryLoadPendingSettings(out var baseName, out var settings))
            {
                return;
            }

            ClearPendingSettings();

            if (EditorUtility.scriptCompilationFailed)
            {
                EditorUtility.DisplayDialog(
                    "Create Screen",
                    "A compilação falhou. Corrija os erros antes de tentar novamente.",
                    "OK");
                return;
            }

            try
            {
                CompleteCreation(baseName, settings);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Create Screen",
                    $"Falha ao finalizar a tela:\n{exception.Message}",
                    "OK");
            }
        }

        private static void CompleteCreation(string baseName, CreateScreenSettings settings)
        {
            var folderPath = $"{settings.AppScreensRoot}/{baseName}";
            var viewModelClassName = $"{baseName}ViewModel";
            var screenClassName = $"{baseName}Screen";
            var installerClassName = $"{baseName}ScreenInstaller";
            var uxmlPath = $"{folderPath}/{baseName}UXML.uxml";
            var prefabPath = $"{folderPath}/{screenClassName}.prefab";

            var viewModelType = FindTypeByName(viewModelClassName);
            var screenType = FindTypeByName(screenClassName);
            var installerType = FindTypeByName(installerClassName);

            if (viewModelType == null || screenType == null || installerType == null)
            {
                throw new InvalidOperationException(
                    $"Tipos não encontrados após compilação: {viewModelClassName}, {screenClassName}, {installerClassName}.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                RegisterScreenPrefab(prefabPath, settings.ScreenManagerPrefabPath);
                EditorUtility.DisplayDialog(
                    "Create Screen",
                    $"Prefab já existia e foi verificado no ScreenManager:\n{prefabPath}",
                    "OK");
                PingAsset(prefabPath);
                return;
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settings.PanelSettingsPath);
            if (panelSettings == null)
            {
                throw new InvalidOperationException($"PanelSettings não encontrado em {settings.PanelSettingsPath}.");
            }

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTree == null)
            {
                throw new InvalidOperationException($"UXML não encontrado em {uxmlPath}.");
            }

            var root = new GameObject(screenClassName)
            {
                layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 5
            };

            try
            {
                var uiDocument = root.AddComponent<UIDocument>();
                ConfigureUiDocument(uiDocument, panelSettings, visualTree);

                var screenComponent = (MonoBehaviour)root.AddComponent(screenType);
                var installerComponent = (MonoBehaviour)root.AddComponent(installerType);
                WireInstallerView(installerComponent, screenComponent);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterScreenPrefab(prefabPath, settings.ScreenManagerPrefabPath);
            PingAsset(prefabPath);

            EditorUtility.DisplayDialog(
                "Create Screen",
                $"Tela criada com sucesso.\n\nPrefab:\n{prefabPath}\n\nRegistrada em:\n{settings.ScreenManagerPrefabPath}",
                "OK");
        }

        private static void WriteScreenFiles(string baseName, string folderPath)
        {
            var viewModelClassName = $"{baseName}ViewModel";
            var screenClassName = $"{baseName}Screen";
            var installerClassName = $"{baseName}ScreenInstaller";

            WriteTextAsset($"{folderPath}/{baseName}USS.uss", BuildUssContent());
            WriteTextAsset($"{folderPath}/{baseName}UXML.uxml", BuildUxmlContent(baseName));
            WriteTextAsset($"{folderPath}/{viewModelClassName}.cs", BuildViewModelContent(viewModelClassName));
            WriteTextAsset($"{folderPath}/{screenClassName}.cs", BuildScreenContent(screenClassName, viewModelClassName));
            WriteTextAsset($"{folderPath}/{installerClassName}.cs", BuildInstallerContent(installerClassName, viewModelClassName));
        }

        private static void WriteTextAsset(string assetPath, string contents)
        {
            File.WriteAllText(GetAbsolutePath(assetPath), contents, Encoding.UTF8);
        }

        private static string BuildUssContent()
        {
            return "#container {\n    width: 100%;\n    height: 100%;\n    flex-grow: 1;\n}\n";
        }

        private static string BuildUxmlContent(string baseName)
        {
            return
                "<engine:UXML xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:engine=\"UnityEngine.UIElements\" xmlns:editor=\"UnityEditor.UIElements\" noNamespaceSchemaLocation=\"../../../../../UIElementsSchema/UIElements.xsd\" editor-extension-mode=\"False\">\n" +
                $"    <Style src=\"{baseName}USS.uss\" />\n" +
                "    <engine:VisualElement name=\"container\" style=\"flex-grow: 1; width: 100%; height: 100%;\" />\n" +
                "</engine:UXML>\n";
        }

        private static string BuildViewModelContent(string viewModelClassName)
        {
            return
                "using Maneuver.ScreenManager;\n\n" +
                "namespace App.UI.Screens.Behaviour\n" +
                "{\n" +
                $"    public class {viewModelClassName} : ScreenViewModelBase\n" +
                "    {\n" +
                "    }\n" +
                "}\n";
        }

        private static string BuildScreenContent(string screenClassName, string viewModelClassName)
        {
            return
                "using Maneuver.ScreenManager;\n\n" +
                "namespace App.UI.Screens.Behaviour\n" +
                "{\n" +
                $"    public class {screenClassName} : ScreenBase<{viewModelClassName}>\n" +
                "    {\n" +
                "        public override void Initialize()\n" +
                "        {\n" +
                "        }\n\n" +
                $"        protected override void BindViewModel({viewModelClassName} viewModel)\n" +
                "        {\n" +
                "        }\n" +
                "    }\n" +
                "}\n";
        }

        private static string BuildInstallerContent(string installerClassName, string viewModelClassName)
        {
            return
                "using App.UI.Screens.Behaviour;\n" +
                "using Maneuver.ScreenManager;\n\n" +
                "namespace ConfigAutomation.Installers\n" +
                "{\n" +
                $"    public class {installerClassName} : ScreenInstaller<{viewModelClassName}>\n" +
                "    {\n" +
                "    }\n" +
                "}\n";
        }

        private static void ConfigureUiDocument(UIDocument uiDocument, PanelSettings panelSettings, VisualTreeAsset visualTree)
        {
            var serializedObject = new SerializedObject(uiDocument);
            serializedObject.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
            serializedObject.FindProperty("sourceAsset").objectReferenceValue = visualTree;
            serializedObject.FindProperty("m_WorldSpaceSizeMode").enumValueIndex = 1;
            serializedObject.FindProperty("m_WorldSpaceWidth").floatValue = 1920f;
            serializedObject.FindProperty("m_WorldSpaceHeight").floatValue = 1080f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireInstallerView(MonoBehaviour installerComponent, MonoBehaviour screenComponent)
        {
            var serializedObject = new SerializedObject(installerComponent);
            var viewProperty = serializedObject.FindProperty("_view");
            if (viewProperty == null)
            {
                throw new InvalidOperationException("Campo _view não encontrado em ScreenInstaller.");
            }

            viewProperty.objectReferenceValue = screenComponent;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterScreenPrefab(string prefabPath, string screenManagerPrefabPath)
        {
            var screenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (screenPrefab == null)
            {
                throw new InvalidOperationException($"Prefab não encontrado em {prefabPath}.");
            }

            using var editScope = new PrefabUtility.EditPrefabContentsScope(screenManagerPrefabPath);
            var screenManagerComponent = FindScreenManagerComponent(editScope.prefabContentsRoot);
            if (screenManagerComponent == null)
            {
                throw new InvalidOperationException("Componente ScreenManager não encontrado no prefab ScreenManager.");
            }

            var serializedObject = new SerializedObject(screenManagerComponent);
            var screenPrefabsProperty = serializedObject.FindProperty("_screenPrefabs");
            if (screenPrefabsProperty == null || !screenPrefabsProperty.isArray)
            {
                throw new InvalidOperationException("Lista _screenPrefabs não encontrada em ScreenManager.");
            }

            for (var i = 0; i < screenPrefabsProperty.arraySize; i++)
            {
                if (screenPrefabsProperty.GetArrayElementAtIndex(i).objectReferenceValue == screenPrefab)
                {
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            screenPrefabsProperty.InsertArrayElementAtIndex(screenPrefabsProperty.arraySize);
            screenPrefabsProperty
                .GetArrayElementAtIndex(screenPrefabsProperty.arraySize - 1)
                .objectReferenceValue = screenPrefab;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MonoBehaviour FindScreenManagerComponent(GameObject prefabRoot)
        {
            foreach (var component in prefabRoot.GetComponents<MonoBehaviour>())
            {
                if (component != null && component.GetType().FullName == "Maneuver.ScreenManager.ScreenManager")
                {
                    return component;
                }
            }

            return null;
        }

        private static Type FindTypeByName(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var behaviourType = assembly.GetType($"App.UI.Screens.Behaviour.{typeName}");
                if (behaviourType != null)
                {
                    return behaviourType;
                }

                var installerType = assembly.GetType($"ConfigAutomation.Installers.{typeName}");
                if (installerType != null)
                {
                    return installerType;
                }
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(t => t != null).ToArray();
                }

                var match = types.FirstOrDefault(type => type.Name == typeName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool TryNormalizeBaseName(string rawName, out string baseName, out string error)
        {
            baseName = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(rawName))
            {
                error = "Informe um nome para a tela.";
                return false;
            }

            var parts = Regex
                .Split(rawName.Trim(), @"[^a-zA-Z0-9]+")
                .Where(part => !string.IsNullOrEmpty(part))
                .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant())
                .ToArray();

            if (parts.Length == 0)
            {
                error = "Nome inválido.";
                return false;
            }

            baseName = string.Concat(parts);

            if (baseName.EndsWith("Screen", StringComparison.Ordinal))
            {
                baseName = baseName.Substring(0, baseName.Length - "Screen".Length);
            }

            if (string.IsNullOrEmpty(baseName))
            {
                error = "Nome inválido após normalização.";
                return false;
            }

            if (!Regex.IsMatch(baseName, @"^[A-Z][a-zA-Z0-9]*$"))
            {
                error = $"Nome normalizado inválido: {baseName}";
                return false;
            }

            return true;
        }

        private static string GetAbsolutePath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void PingAsset(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
