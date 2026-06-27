using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to a fixed building or large obstacle.
/// Its Collider2D bounds are converted into blocked Ground Tilemap cells.
///
/// This does not move or paint the building. The building remains a normal
/// GameObject placed at a fixed location in the Scene.
/// </summary>
public class GroundBlocker : MonoBehaviour
{
    [SerializeField] private Collider2D footprintCollider;

    private Coroutine registrationRoutine;
    private bool isRegistered;
    private EntityId blockerId;

    private void Awake()
    {
        blockerId = GetEntityId();

        if (footprintCollider == null)
            footprintCollider = GetComponent<Collider2D>();
    }

    private void Reset()
    {
        footprintCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        registrationRoutine =
            StartCoroutine(RegisterWhenGroundIsReady());
    }

    private void OnDisable()
    {
        if (registrationRoutine != null)
        {
            StopCoroutine(registrationRoutine);
            registrationRoutine = null;
        }

        Unregister();
    }

    private IEnumerator RegisterWhenGroundIsReady()
    {
        while (GroundManager.Instance == null ||
               !GroundManager.Instance.IsInitialized)
        {
            yield return null;
        }

        RegisterNow();
        registrationRoutine = null;
    }

    [ContextMenu("Refresh Blocked Ground Cells")]
    public void RefreshBlockedCells()
    {
        if (!isActiveAndEnabled)
            return;

        if (GroundManager.Instance == null ||
            !GroundManager.Instance.IsInitialized)
        {
            return;
        }

        RegisterNow();
    }

    private void RegisterNow()
    {
        if (footprintCollider == null)
        {
            Debug.LogError(
                "GroundBlocker needs a Collider2D footprint.",
                this);
            return;
        }

        GroundManager groundManager = GroundManager.Instance;

        List<Vector3Int> occupiedCells =
            groundManager.GetCellsOverlappingBounds(
                footprintCollider.bounds);

        groundManager.RegisterBlocker(
            blockerId,
            occupiedCells);

        isRegistered = true;
    }

    private void Unregister()
    {
        if (!isRegistered)
            return;

        if (GroundManager.Instance != null)
        {
            GroundManager.Instance.UnregisterBlocker(
                blockerId);
        }

        isRegistered = false;
    }
}
