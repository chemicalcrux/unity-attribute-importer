using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ChemicalCrux.AttributeImporter
{
    [FilePath("chemicalcrux/Vertex Data Importer/Settings.settings", FilePathAttribute.Location.ProjectFolder)]
    public class Settings : ScriptableSingleton<Settings>
    {
        [SerializeField] internal bool debug;
        public bool Debug => debug;
    }

    class ImporterSettingsProvider : SettingsProvider
    {
        public ImporterSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {

        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new ImporterSettingsProvider("Project/Vertex Data Importer", SettingsScope.Project)
            {
                label = "Vertex Data Importer",
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = Settings.instance;
                    var settingsObject = new SerializedObject(settings);

                    // var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/settings_ui.uss");
                    // rootElement.styleSheets.Add(styleSheet);

                    var title = new Label()
                    {
                        text = "Cool Title"
                    };
                    title.AddToClassList("title");
                    rootElement.Add(title);

                    var properties = new VisualElement()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column
                        }
                    };

                    properties.AddToClassList("property-list");
                    rootElement.Add(properties);

                    properties.Add(new PropertyField(settingsObject.FindProperty(nameof(Settings.debug))));

                    rootElement.Bind(settingsObject);
                },
                keywords = new HashSet<string>(new[] { "debug" })
            };

            return provider;
        }
    }
}