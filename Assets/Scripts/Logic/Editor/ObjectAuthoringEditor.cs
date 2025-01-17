using Authoring;
using UnityEditor;

[CustomEditor(typeof(ObjectAuthoring))]
public class ObjectAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var pushableProp = serializedObject.FindProperty("Pushable");
        var gearProp = serializedObject.FindProperty("Gear");
        var gearMotorProp = serializedObject.FindProperty("GearMotor");
        var gearToWireProp = serializedObject.FindProperty("GearToWire");
        var wireCubeProp = serializedObject.FindProperty("WireCube");
        var shootableProp = serializedObject.FindProperty("Shootable");
        var landAudioProp = serializedObject.FindProperty("LandAudio");

        EditorGUILayout.PropertyField(pushableProp);
        EditorGUILayout.PropertyField(gearProp);
        if (gearProp.boolValue) wireCubeProp.boolValue = false;

        using (new EditorGUI.DisabledScope(!gearProp.boolValue))
        {
            EditorGUILayout.PropertyField(gearMotorProp);
            EditorGUILayout.PropertyField(gearToWireProp);
        }

        EditorGUILayout.PropertyField(wireCubeProp);
        if (wireCubeProp.boolValue) gearProp.boolValue = false;

        EditorGUILayout.PropertyField(shootableProp);

        EditorGUILayout.PropertyField(landAudioProp);

        serializedObject.ApplyModifiedProperties();
    }
}