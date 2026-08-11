using UnityEngine;

/// <summary>
/// Syncs this gear piece's animator with its parent's animator (usually legs).
/// Attach to chest and head gear pieces.
/// </summary>
[RequireComponent(typeof(Animator))]
public class GearAnimatorSync : MonoBehaviour
{
    private Animator myAnimator;
    private Animator parentAnimator;
    private bool _initializedExplicitly;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    /// <summary>
    /// Explicitly set the animator to sync from. Call this immediately after instantiation
    /// so syncing works in the same frame rather than waiting for the deferred Start() search.
    /// </summary>
    public void Initialize(Animator parent)
    {
        parentAnimator = parent;
        _initializedExplicitly = true;
    }

    private void Start()
    {
        // If Initialize() was already called by PlayerGearManager at instantiation time, skip the search.
        if (_initializedExplicitly) return;

        // Fallback: search upward for a parent animator (editor/test scenes without explicit init).
        Transform current = transform.parent;
        while (current != null)
        {
            Animator foundAnimator = current.GetComponent<Animator>();
            if (foundAnimator != null)
            {
                parentAnimator = foundAnimator;
                break;
            }
            current = current.parent;
        }

        if (parentAnimator == null)
        {
            Debug.LogWarning($"[GearAnimatorSync] {gameObject.name} couldn't find parent animator to sync with!");
        }
    }
    
    private void LateUpdate()
    {
        if (parentAnimator == null || myAnimator == null) return;
        
        // Copy animation state from parent
        AnimatorStateInfo stateInfo = parentAnimator.GetCurrentAnimatorStateInfo(0);
        
        // Log every 60 frames (about once per second at 60fps)
        if (Time.frameCount % 60 == 0)
        {
            AnimatorClipInfo[] parentClips = parentAnimator.GetCurrentAnimatorClipInfo(0);
            string parentClip = parentClips.Length > 0 ? parentClips[0].clip.name : "(none)";
            AnimatorClipInfo[] myClips = myAnimator.GetCurrentAnimatorClipInfo(0);
            string myClip = myClips.Length > 0 ? myClips[0].clip.name : "(none)";
        }
        
        myAnimator.Play(stateInfo.fullPathHash, 0, stateInfo.normalizedTime);
        
        // Copy parameters
        foreach (AnimatorControllerParameter param in parentAnimator.parameters)
        {
            if (!myAnimator.parameters.HasParameter(param.nameHash)) continue;
            
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    myAnimator.SetFloat(param.name, parentAnimator.GetFloat(param.name));
                    break;
                case AnimatorControllerParameterType.Int:
                    myAnimator.SetInteger(param.name, parentAnimator.GetInteger(param.name));
                    break;
                case AnimatorControllerParameterType.Bool:
                    myAnimator.SetBool(param.name, parentAnimator.GetBool(param.name));
                    break;
            }
        }
    }
}

// Extension method to check if parameter exists
public static class AnimatorExtensions
{
    public static bool HasParameter(this AnimatorControllerParameter[] parameters, int nameHash)
    {
        foreach (var param in parameters)
        {
            if (param.nameHash == nameHash) return true;
        }
        return false;
    }
}
