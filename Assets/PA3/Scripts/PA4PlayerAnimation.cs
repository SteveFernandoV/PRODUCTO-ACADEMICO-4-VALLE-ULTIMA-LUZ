using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>Loads a rigged, animated player visual at runtime without changing scene layouts.</summary>
public sealed class PA4PlayerAnimation : MonoBehaviour
{
    const string ModelResourcePath = "PA4AnimatedPlayer/Animated Human";
    const string ClipResourcePath = "PA4AnimatedPlayer/Animated Human";
    const string VisualName = "PA4_AnimatedPlayerVisual";

    CharacterController characterController;
    Animator animator;
    PlayableGraph animationGraph;
    AnimationMixerPlayable movementMixer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeForLoadedScenes()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstallInScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => InstallInScene(scene);

    static void InstallInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded ||
            (scene.name != "PA3_VerticalSlice" && scene.name != "SEGUNDO NIVEL" && scene.name != "TERCER NIVEL"))
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PlayerController3D[] players = root.GetComponentsInChildren<PlayerController3D>(true);
            foreach (PlayerController3D player in players)
            {
                if (player != null && player.gameObject.scene == scene &&
                    player.GetComponent<PA4PlayerAnimation>() == null)
                    player.gameObject.AddComponent<PA4PlayerAnimation>();
            }
        }
    }

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (!TryCreateAnimatedVisual()) return;
        CreateMovementGraph();
    }

    bool TryCreateAnimatedVisual()
    {
        GameObject modelAsset = Resources.Load<GameObject>(ModelResourcePath);
        if (modelAsset == null)
        {
            Debug.LogWarning("PA4_PLAYER_ANIMATION: no se encontró el modelo en Resources/" + ModelResourcePath);
            return false;
        }

        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(ClipResourcePath);
        AnimationClip idle = FindClip(clips, "Idle");
        AnimationClip walk = FindClip(clips, "Walk");
        if (idle == null || walk == null)
        {
            Debug.LogWarning("PA4_PLAYER_ANIMATION: no se encontraron los clips Idle y Walk dentro del FBX.");
            return false;
        }

        GameObject visual = Instantiate(modelAsset, transform);
        visual.name = VisualName;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        animator = visual.GetComponentInChildren<Animator>(true);
        if (animator == null) animator = visual.AddComponent<Animator>();
        if (animator.avatar == null)
        {
            Debug.LogWarning("PA4_PLAYER_ANIMATION: el modelo no tiene Avatar riggeado; se conserva el personaje anterior.");
            Destroy(visual);
            animator = null;
            return false;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("PA4_PLAYER_ANIMATION: el modelo no contiene mallas visibles.");
            Destroy(visual);
            animator = null;
            return false;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.y <= 0.01f)
        {
            Destroy(visual);
            animator = null;
            return false;
        }
        visual.transform.localScale = Vector3.one * (1.9f / bounds.size.y);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        visual.transform.localPosition = Vector3.up * (transform.position.y - bounds.min.y);

        foreach (MeshRenderer oldRenderer in GetComponentsInChildren<MeshRenderer>(true))
            if (!oldRenderer.transform.IsChildOf(visual.transform)) oldRenderer.enabled = false;
        foreach (SkinnedMeshRenderer oldRenderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (!oldRenderer.transform.IsChildOf(visual.transform)) oldRenderer.enabled = false;

        idle.wrapMode = WrapMode.Loop;
        walk.wrapMode = WrapMode.Loop;
        Debug.Log("PA4_PLAYER_ANIMATION: personaje animado cargado; clips Idle/Walk listos.");
        return true;
    }

    static AnimationClip FindClip(AnimationClip[] clips, string fragment)
    {
        foreach (AnimationClip clip in clips)
            if (clip != null && clip.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                return clip;
        return null;
    }

    void CreateMovementGraph()
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(ClipResourcePath);
        AnimationClip idle = FindClip(clips, "Idle");
        AnimationClip walk = FindClip(clips, "Walk");
        if (idle == null || walk == null || animator == null) return;

        animationGraph = PlayableGraph.Create("PA4 Player Walk Animation");
        animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        movementMixer = AnimationMixerPlayable.Create(animationGraph, 2, true);
        AnimationClipPlayable idlePlayable = AnimationClipPlayable.Create(animationGraph, idle);
        AnimationClipPlayable walkPlayable = AnimationClipPlayable.Create(animationGraph, walk);
        animationGraph.Connect(idlePlayable, 0, movementMixer, 0);
        animationGraph.Connect(walkPlayable, 0, movementMixer, 1);
        movementMixer.SetInputWeight(0, 1f);
        movementMixer.SetInputWeight(1, 0f);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Player Animation", animator);
        output.SetSourcePlayable(movementMixer);
        animationGraph.Play();
    }

    void Update()
    {
        if (!animationGraph.IsValid() || !movementMixer.IsValid() || characterController == null) return;
        Vector3 velocity = characterController.velocity;
        velocity.y = 0f;
        float walkWeight = Mathf.Clamp01(velocity.magnitude / 2.8f);
        if (walkWeight < 0.08f) walkWeight = 0f;
        movementMixer.SetInputWeight(0, 1f - walkWeight);
        movementMixer.SetInputWeight(1, walkWeight);
    }

    void OnDestroy()
    {
        if (animationGraph.IsValid()) animationGraph.Destroy();
    }
}
