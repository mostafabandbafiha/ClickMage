using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName = "Character";
    public float moveSpeed = 3f;
    public float harvestPower = 10f;
}
