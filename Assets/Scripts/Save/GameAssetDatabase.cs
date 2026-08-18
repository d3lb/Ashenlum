using UnityEngine;

// A save file can only hold ids. This turns them back into the assets they came from.
// Anything the player can own has to be listed here or it will not survive a reload.
[CreateAssetMenu(fileName = "Game Asset Database", menuName = "Ashenlum/Asset Database")]
public class GameAssetDatabase : ScriptableObject
{
    [SerializeField] private ShopGood[] goods;
    [SerializeField] private ActiveAbility[] abilities;

    public T FindGood<T>(string id) where T : ShopGood
    {
        if (string.IsNullOrEmpty(id) || goods == null) return null;

        foreach (ShopGood good in goods)
            if (good is T match && match.Id == id) return match;

        return null;
    }

    public ActiveAbility FindAbility(string id)
    {
        if (string.IsNullOrEmpty(id) || abilities == null) return null;

        foreach (ActiveAbility ability in abilities)
            if (ability != null && ability.Id == id) return ability;

        return null;
    }

#if UNITY_EDITOR
    // Catches the mistake that costs you a save: owning something the database never
    // heard of, so it silently vanishes on load.
    private void OnValidate()
    {
        WarnOnDuplicates();
    }

    private void WarnOnDuplicates()
    {
        if (goods == null) return;

        for (int i = 0; i < goods.Length; i++)
            for (int j = i + 1; j < goods.Length; j++)
                if (goods[i] != null && goods[j] != null && goods[i].Id == goods[j].Id)
                    Debug.LogError($"[GameAssetDatabase] Two goods share the id '{goods[i].Id}'. " +
                                   "Loading a save will pick the wrong one.", this);
    }
#endif
}
