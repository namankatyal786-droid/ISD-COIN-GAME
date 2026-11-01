using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
public static class CoinSnatcherSetup
{
    [MenuItem("Coin Snatcher/Create Game Scene")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObj = new GameObject("Main Camera", typeof(Camera),typeof(AudioListener)); cameraObj.tag="MainCamera";
        cameraObj.transform.position=new Vector3(0,0,-10); var cam=cameraObj.GetComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=5; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.063f,.102f,.125f);
        System.IO.Directory.CreateDirectory("Assets/CoinSnatcher/Art");
        var texture = new Texture2D(128,128,TextureFormat.RGBA32,false);
        for(int y=0;y<128;y++) for(int x=0;x<128;x++) { float distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(64,64)); texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(63-distance))); }
        texture.Apply(); System.IO.File.WriteAllBytes("Assets/CoinSnatcher/Art/Disc.png",texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset("Assets/CoinSnatcher/Art/Disc.png"); var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/CoinSnatcher/Art/Disc.png"); importer.textureType=TextureImporterType.Sprite; importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.SaveAndReimport();
        var disc=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CoinSnatcher/Art/Disc.png");
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if(font==null) throw new System.Exception("Import TMP Essentials before creating scene.");
        var root=new GameObject("Coin Snatcher"); var game=root.AddComponent<CoinSnatcher>(); game.BuildLayout(disc,font); EditorUtility.SetDirty(game);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,"Assets/Scenes/CoinSnatcher.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/CoinSnatcher.unity",true)};
        Selection.activeGameObject=root; AssetDatabase.SaveAssets();
        Debug.Log("Coin Snatcher ready. Press Play to play.");
    }
}
