using UnityEditor;

internal sealed class PA4AnimatedPlayerImporter : AssetPostprocessor
{
    const string ModelPath = "Assets/Resources/PA4AnimatedPlayer/Animated Human.fbx";

    void OnPreprocessModel()
    {
        if (assetPath != ModelPath) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.optimizeGameObjects = false;
    }
}
