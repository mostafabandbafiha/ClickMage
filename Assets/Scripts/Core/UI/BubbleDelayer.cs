using System.Collections;
using UnityEngine;

/// <summary>Helper that shows a speech bubble on another character after a delay.</summary>
public class BubbleDelayer : MonoBehaviour
{
    public void ShowDelayed(BaseCharacter target, float delay)
    {
        StartCoroutine(Routine(target, delay));
    }

    private IEnumerator Routine(BaseCharacter target, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (target == null) yield break;
        if (target.StateMachine.CurrentState is not CharacterRestingState) yield break;

        var bubble = target.GetComponentInChildren<SpeechBubble>();
        if (bubble == null)
        {
            var go = new GameObject("SpeechBubble");
            go.transform.SetParent(target.transform);
            go.transform.localPosition = new Vector3(0, 2.2f, 0);
            bubble = go.AddComponent<SpeechBubble>();
        }

        bubble.Show(duration: 2.5f);
    }
}