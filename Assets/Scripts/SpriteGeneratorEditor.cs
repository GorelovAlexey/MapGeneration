using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Assets.Scripts
{

    [CustomEditor(typeof(SpriteGenerator))]
    public class SpriteGeneratorEditor : Editor
    {
        #region SerializedProperties
        SerializedProperty spriteRenderer;
        SerializedProperty meshGenerator;

        SerializedProperty width;
        SerializedProperty height;
        SerializedProperty noiseScale;
        SerializedProperty seed;
        SerializedProperty octaves;
        SerializedProperty percistance;
        SerializedProperty lacunarity;


        SerializedProperty erosionSettings;
        SerializedProperty erosionSettings2;

        SerializedProperty levelValues;
        SerializedProperty levelColors;
        #endregion

        private bool showDebug = false;
        private bool oldErosionMethod = false;
        private int toolbarOption = 0;
        private string[] toolbarTexts = new[] { "ErosionSimple", "ErosionBetter" };

        // SerializedProperty NoiseSettings;
        void OnEnable()
        {
            erosionSettings = serializedObject.FindProperty("erosionSettings");
            erosionSettings2 = serializedObject.FindProperty("erosionSettings2");

            spriteRenderer = serializedObject.FindProperty("spriteRenderer");
            meshGenerator = serializedObject.FindProperty("meshGenerator");

            width = serializedObject.FindProperty("width");
            height = serializedObject.FindProperty("height");
            noiseScale = serializedObject.FindProperty("noiseScale");
            seed = serializedObject.FindProperty("seed");
            octaves = serializedObject.FindProperty("octaves");
            percistance = serializedObject.FindProperty("percistance");
            lacunarity = serializedObject.FindProperty("lacunarity");

            levelValues = serializedObject.FindProperty("levelValues");
            levelColors = serializedObject.FindProperty("levelColors");
        }

        public override void OnInspectorGUI()
        {
            var script = (SpriteGenerator)target;

            EditorGUILayout.PropertyField(spriteRenderer, new GUIContent("spriteRenderer"));
            EditorGUILayout.PropertyField(meshGenerator, new GUIContent("meshGenerator"));

            EditorGUILayout.PropertyField(width, new GUIContent("width"));
            EditorGUILayout.PropertyField(height, new GUIContent("height"));
            EditorGUILayout.PropertyField(noiseScale, new GUIContent("noiseScale"));
            EditorGUILayout.PropertyField(seed, new GUIContent("seed"));
            EditorGUILayout.PropertyField(octaves, new GUIContent("octaves"));
            EditorGUILayout.PropertyField(percistance, new GUIContent("percistance"));
            EditorGUILayout.PropertyField(lacunarity, new GUIContent("lacunarity"));

            EditorGUILayout.PropertyField(levelValues, new GUIContent("levelValues"));
            EditorGUILayout.PropertyField(levelColors, new GUIContent("levelColors"));

            GUILayout.Space(10);
            GUILayout.Label("Sprite generation: ");

            toolbarOption = GUILayout.Toolbar(toolbarOption, toolbarTexts);
            oldErosionMethod = toolbarOption == 0;

            erosionSettings.isExpanded = true;
            erosionSettings2.isExpanded = true;

            if (oldErosionMethod)
                EditorGUILayout.PropertyField(erosionSettings, new GUIContent("erosionSettings"));
            else
                EditorGUILayout.PropertyField(erosionSettings2, new GUIContent("erosionSettings2"));

            if (GUILayout.Button("Generate noise map"))
            {
                script.GenerateBasicMap();
            }

            if (GUILayout.Button("Run errosion simulator"))
            {
                if (oldErosionMethod)
                    script.ApplyErrosion();
                else
                    script.ApplyErrosion2();

            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Generate 3D mesh:");
            if (GUILayout.Button("Simple"))
            {
                script.Generate3DMesh(MeshGenerator.MeshVersion.First);
            }
            if (GUILayout.Button("Better"))
            {
                script.Generate3DMesh(MeshGenerator.MeshVersion.FirstUpdated);
            }
            if (GUILayout.Button("Smooth"))
            {
                script.Generate3DMesh(MeshGenerator.MeshVersion.Second);
            }
            GUILayout.EndHorizontal();

            showDebug = GUILayout.Toggle(showDebug, "Show debug heatMaps");
            GUILayout.BeginHorizontal();
            var debugButtonText = showDebug ? "X" : "Show debug tools";
            if (GUILayout.Button(debugButtonText))
                showDebug = !showDebug;

            if (showDebug)
            {
                if (GUILayout.Button("HeatmapVisits"))
                {
                    script.ApplyHeatMapTexture(script.LastErosionHeatMapsDebug?.heatmapVisits);
                }
                if (GUILayout.Button("HeatmapStarts"))
                {
                    script.ApplyHeatMapTexture(script.LastErosionHeatMapsDebug?.heatmapStarts);
                }
                if (GUILayout.Button("HeatmapTime"))
                {
                    script.ApplyHeatMapTexture(script.LastErosionHeatMapsDebug?.heatmapTime);
                }
                if (GUILayout.Button("HeatmapSaturations"))
                {
                    script.ApplyHeatMapTexture(script.LastErosionHeatMapsDebug?.heatmapSaturationChanges);
                }
            }
            GUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
