using UnityEditor;
using UnityEngine;

public class QuantizePositionEditor : UnityEditor.Editor
{
    [MenuItem("GameObject/Quantize", false, 10)]
    private static void QuantizePosition()
    {
        Undo.RecordObjects(Selection.gameObjects, "Quantize");
        foreach (var obj in Selection.gameObjects)
        {
            var position = obj.transform.position;
            var rotation = obj.transform.rotation.eulerAngles;

            for (var i = 0; i < 3; i++)
            {
                position[i] = Mathf.Round(position[i]);
                rotation[i] = Mathf.Round(rotation[i] / 90) * 90;
            }

            obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(rotation);
            EditorUtility.SetDirty(obj);
        }
    }
}