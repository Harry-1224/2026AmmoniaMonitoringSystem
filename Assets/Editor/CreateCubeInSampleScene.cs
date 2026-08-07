using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateCubeInSampleScene
{
    public static void CreateCube()
    {
        var basePath = "D:/GitHub/2026AmmoniaMonitoringSystem/Logs";
        Directory.CreateDirectory(basePath);
        File.WriteAllText(Path.Combine(basePath, "execute_method_test.txt"), "execute-method-ran");

        var scenePath = "Assets/Scenes/SampleScene.unity";
        File.WriteAllText(Path.Combine(basePath, "after_open_scene_attempt.txt"), scenePath);

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        File.WriteAllText(Path.Combine(basePath, "after_open_scene_success.txt"), scene.path);

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "RuntimeCreatedCube";
        cube.transform.position = new Vector3(0f, 0.5f, 0f);
        cube.transform.localScale = Vector3.one;
        File.WriteAllText(Path.Combine(basePath, "after_create_cube.txt"), cube.name);

        var saved = EditorSceneManager.SaveScene(scene, scenePath);
        File.WriteAllText(Path.Combine(basePath, "after_save_scene.txt"), saved ? "saved" : "not-saved");
        Debug.Log($"Created cube in {scenePath}");
    }
}
