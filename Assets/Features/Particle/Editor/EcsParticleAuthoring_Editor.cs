using System;
using System.Linq;
using Lib.EcsParticle;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EcsParticleAuthoring))]
public class EcsParticleAuthoring_Editor : Editor
{
    private SerializedProperty startSizeProp;
    private SerializedProperty startPositionProp;
    private SerializedProperty startRotationProp;
    private void OnEnable()
    {
        startSizeProp = serializedObject.FindProperty("startSize");
        startPositionProp = serializedObject.FindProperty("startPosition");
        startRotationProp = serializedObject.FindProperty("startRotation");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lifeTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
        }

        DrawSerializeReferenceWithTypePicker<EcsParticleStartSize>(startSizeProp, "Start Size");
        DrawSerializeReferenceWithTypePicker<EcsParticleStartPosition>(startPositionProp, "Start Position");
        DrawSerializeReferenceWithTypePicker<EcsParticleStartRotation>(startRotationProp, "Start Rotation");
        
        serializedObject.ApplyModifiedProperties();
    }
    
    static void DrawSerializeReferenceWithTypePicker<T>(SerializedProperty prop, string label)
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            Type currentType = prop.managedReferenceValue?.GetType();
            EditorGUILayout.LabelField("Type", GetTypeDisplayName(currentType));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Change Type"))
                    ShowTypeMenu(prop, typeof(T));
            }
            EditorGUILayout.EndHorizontal();

            prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, "Details", true);
            if (!prop.isExpanded) return;

            DrawChildren(prop);
        }
    }
    
    static string GetTypeDisplayName(Type t)
    {
        return t.Name.Replace("EcsParticle", "");
    }

    static void ShowTypeMenu(SerializedProperty prop, Type baseType)
    {
        var menu = new GenericMenu();

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); } // skip reflection errors
            })
            .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && t.IsClass)
            .OrderBy(t => t.Name);

        foreach (var t in types)
        {
            string name = GetTypeDisplayName(t);
            menu.AddItem(new GUIContent(name.Replace('.', '/')), false, () =>
            {
                prop.serializedObject.Update();
                prop.managedReferenceValue = Activator.CreateInstance(t);
                prop.serializedObject.ApplyModifiedProperties();
            });
        }

        if (!types.Any())
            menu.AddDisabledItem(new GUIContent("No concrete subclasses found"));

        menu.ShowAsContext();
    }

    static void DrawChildren(SerializedProperty prop)
    {
        var copy = prop.Copy();
        var end = copy.GetEndProperty();

        if (!copy.NextVisible(true)) return;

        while (!SerializedProperty.EqualContents(copy, end))
        {
            EditorGUILayout.PropertyField(copy, includeChildren: true);
            if (!copy.NextVisible(false)) break;
        }
    }
}
