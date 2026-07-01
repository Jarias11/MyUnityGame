using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class EntityId : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id
    {
        get
        {
            EnsureId();
            return id;
        }
    }

    public bool HasId => !string.IsNullOrWhiteSpace(id);

    private void Awake()
    {
        EnsureId();
    }

    private void Reset()
    {
        EnsureId();
    }

    private void OnValidate()
    {
        EnsureId();
    }

    public void EnsureId()
    {
        if (!string.IsNullOrWhiteSpace(id))
            return;

        id = Guid.NewGuid().ToString("N");

#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Generate New Entity Id")]
    private void GenerateNewId()
    {
        id = Guid.NewGuid().ToString("N");

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public override string ToString()
    {
        return Id;
    }
}