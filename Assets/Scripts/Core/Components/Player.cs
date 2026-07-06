using UnityEngine;

public class Player : MonoBehaviour
{
    public static Transform Point { get; private set; }

    private void Awake() => Point = transform;
    private void OnDestroy() => Point = null;
}