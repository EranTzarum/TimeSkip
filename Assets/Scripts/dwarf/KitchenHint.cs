using UnityEngine;
using UnityEngine.SceneManagement;

// The kitchen opens after the memory game with no instructions; tell the child how to get back outside. UI only.
public static class KitchenHint
{
    const string Text = "Well done! Use the arrow keys to walk down through the door and go back outside.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += (scene, _) => { if (scene.name == "kitchen") ScreenMessage.Show(Text); };
        if (SceneManager.GetActiveScene().name == "kitchen") ScreenMessage.Show(Text);
    }
}
