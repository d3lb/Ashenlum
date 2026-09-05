using UnityEngine;

// Ids back into assets. Anything ownable must be listed here or it vanishes on load.
[CreateAssetMenu(fileName = "Game Asset Database", menuName = "Ashenlum/Asset Database")]
public class GameAssetDatabase : ScriptableObject {
    [SerializeField] private ShopGood[] goods;
    [SerializeField] private ActiveAbility[] abilities;

    // Presentation for the core three. Not saved - the unlock is a bool on the run.
    [SerializeField] private CoreAbilityInfo[] coreAbilities;

    public CoreAbilityInfo FindCoreAbility(AbilityType ability) {
        if (coreAbilities == null) return null;

        foreach (CoreAbilityInfo info in coreAbilities)
            if (info != null && info.ability == ability) return info;

        return null;
    }

    public T FindGood<T>(string id) where T : ShopGood {
        if (string.IsNullOrEmpty(id) || goods == null) return null;

        foreach (ShopGood good in goods)
            if (good is T match && match.Id == id) return match;

        return null;
    }

    public ActiveAbility FindAbility(string id) {
        if (string.IsNullOrEmpty(id) || abilities == null) return null;

        foreach (ActiveAbility ability in abilities)
            if (ability != null && ability.Id == id) return ability;

        return null;
    }

#if UNITY_EDITOR
    // A duplicate id silently loads the wrong asset.
    private void OnValidate() {
        WarnOnDuplicates();
    }

    private void WarnOnDuplicates() {
        if (goods == null) return;

        for (int i = 0; i < goods.Length; i++)
            for (int j = i + 1; j < goods.Length; j++)
                if (goods[i] != null && goods[j] != null && goods[i].Id == goods[j].Id)
                    Debug.LogError($"[GameAssetDatabase] Two goods share the id '{goods[i].Id}'. " +
                                   "Loading a save will pick the wrong one.", this);
    }
#endif
}
