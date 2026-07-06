#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility that creates the EnemyRoster ScriptableObject pre-filled
/// with all 9 enemy entries and their counter-pick weights.
///
/// Run via: Tools → Enemy Commander → Create Enemy Roster
///
/// After running, open the created asset at Assets/Data/EnemyCommander/EnemyRoster.asset
/// and assign your EnemyCharacter prefabs to each entry's Prefab field.
/// </summary>
public static class EnemyRosterFactory
{
    private const string OUTPUT_PATH = "Assets/Data/EnemyCommander/EnemyRoster.asset";

    [MenuItem("Tools/Enemy Commander/Create Enemy Roster")]
    public static void CreateRoster()
    {
        // Ensure directory exists
        System.IO.Directory.CreateDirectory("Assets/Data/EnemyCommander");
        AssetDatabase.Refresh();

        var roster = ScriptableObject.CreateInstance<EnemyRosterSO>();
        roster.Entries = new System.Collections.Generic.List<EnemyRosterEntry>
        {
            BuildGoblinScavenger(),
            BuildSkeletonArcher(),
            BuildOgreBrute(),
            BuildPlagueCaster(),
            BuildShadowRunner(),
            BuildSiegeGolem(),
            BuildNightmareShaman(),
            BuildDreadKnight(),
            BuildAbyssalHorror(),
        };

        AssetDatabase.CreateAsset(roster, OUTPUT_PATH);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = roster;

        Debug.Log($"[EnemyRosterFactory] Created roster with {roster.Entries.Count} entries at {OUTPUT_PATH}. " +
                  "Assign prefabs in the Inspector.");
    }

    // ── Entry builders ────────────────────────────────────────────────────────

    /// <summary>
    /// GOBLIN SCAVENGER
    /// Role: cheap disposable swarm. Floods weak zones and punishes no-AoE.
    /// The commander's default probe unit — sent first every night to find gaps.
    ///
    /// Counter-pick logic:
    ///   - Strongly preferred when player has NO AoE (single-target towers waste shots on swarms)
    ///   - Preferred vs Physical dominance (goblins have no resistance but their volume overwhelms)
    ///   - Preferred when a zone has damaged towers (easy to reach them in numbers)
    ///   - Loses preference when player HAS AoE dominance (splash wipes entire wave)
    ///
    /// Stats: Low HP, Moderate speed, No armor, drops Dark Essence
    /// </summary>
    static EnemyRosterEntry BuildGoblinScavenger() => new()
    {
        DisplayName = "Goblin Scavenger",
        UnlockNight = 1,
        BudgetCost = 0.8f,      // cheapest unit — commander can field many

        BaseHP = 60f,
        BaseArmor = 0f,
        IsRanged = false,
        HasEvasion = false,
        IsSeige = false,

        Resistances = DamageTypeMask.None,

        // ── Counter-pick weights ──────────────────────────────────────────────
        // Thrive against single-target towers — volume overwhelms focused fire
        WeightVsAoE = -1.5f,     // STRONGLY avoid when player has AoE — splash kills whole wave
        WeightVsSlow = -0.5f,     // slow hurts swarms (gives towers more shots per goblin)
        WeightVsPhysical = 1.0f,     // single-target physical just shoots one goblin at a time
        WeightVsFire = -0.3f,     // fire DoT lingers, kills even fast units
        WeightVsFrost = -0.8f,     // slow + frost = swarm shredded before reaching base
        WeightVsLightning = -1.0f,     // chain lightning deletes swarms
        WeightVsBleed = 0.2f,     // bleed is slow to kill — goblins may still leak
        WeightVsDamagedTowers = 1.2f,     // damaged zone = flood it before repairs
        WeightAwayFromHero = 0.8f,     // hero AoE melts swarms — route around
        LateGameBonus = 0.3f,     // still useful late as budget filler + distraction
    };

    /// <summary>
    /// SKELETON ARCHER
    /// Role: ranged harasser that stays at distance, whittles towers from range.
    /// Tests whether the player has enough range to reach back-line enemies.
    ///
    /// Counter-pick logic:
    ///   - Preferred when player range is low (archer can fire without being hit)
    ///   - Preferred vs Physical-dominant towers (archers have bone armor vs physical)
    ///   - Paired well with Ogres (archers stand behind tanks)
    ///   - Weak to Frost (slowed archers die before repositioning)
    ///   - Weak to Lightning (chain jumps back row)
    ///
    /// Stats: Low HP, Moderate speed, No armor, Ranged, drops Metal
    /// </summary>
    static EnemyRosterEntry BuildSkeletonArcher() => new()
    {
        DisplayName = "Skeleton Archer",
        UnlockNight = 1,
        BudgetCost = 1.0f,

        BaseHP = 70f,
        BaseArmor = 0f,
        IsRanged = true,
        HasEvasion = false,
        IsSeige = false,

        Resistances = DamageTypeMask.Physical,   // bones shrug off some physical

        WeightVsAoE = 0.0f,     // AoE hits them wherever they stand
        WeightVsSlow = -1.0f,     // slowed archers are dead archers
        WeightVsPhysical = 1.5f,     // resistant to physical — thrive vs sword towers
        WeightVsFire = -0.5f,     // fire DoT works even at range
        WeightVsFrost = -1.2f,     // frost stops repositioning — lethal for archers
        WeightVsLightning = -0.8f,     // chain hits the back line directly
        WeightVsBleed = -0.3f,     // bleed ticks away even from distance
        WeightVsDamagedTowers = 0.5f,     // soft tower = archer can focus it safely
        WeightAwayFromHero = 0.6f,     // hero closes distance — nullifies ranged advantage
        LateGameBonus = 0.5f,     // range advantage matters more late when towers cluster
    };

    /// <summary>
    /// OGRE BRUTE
    /// Role: high-HP tank that walks through damage and tests concentrated DPS.
    /// Forces the player to stack damage items — one arrow-tower won't cut it.
    ///
    /// Counter-pick logic:
    ///   - Strongly preferred when player has AoE (single tank wastes splash radius)
    ///   - Preferred vs Bleed (ogre HP pool absorbs bleeds better than instant damage)
    ///   - High armor means preferred vs Physical-only builds
    ///   - Weak to Armor-Piercing (Piercing Tip, Shatter Point), Frost (slowed tank = easy focus)
    ///   - Preferred on damaged-tower zones (ogre reaches the tower)
    ///
    /// Stats: High HP, Slow, High armor, targets structures, drops Corrupted Metal
    /// </summary>
    static EnemyRosterEntry BuildOgreBrute() => new()
    {
        DisplayName = "Ogre Brute",
        UnlockNight = 3,
        BudgetCost = 2.5f,

        BaseHP = 400f,
        BaseArmor = 15f,
        IsRanged = false,
        HasEvasion = false,
        IsSeige = false,    // not a dedicated siege but will punch towers

        Resistances = DamageTypeMask.Physical | DamageTypeMask.Bleed,

        WeightVsAoE = 2.0f,    // BEST vs AoE — one fat target wastes splash
        WeightVsSlow = -0.5f,    // slow hurts — ogre must reach towers to matter
        WeightVsPhysical = 1.8f,    // high armor shrugs off arrows
        WeightVsFire = 0.5f,    // fire DoT bothers ogre but doesn't stop it
        WeightVsFrost = -1.5f,    // frozen ogre accomplishes nothing
        WeightVsLightning = -0.3f,    // lightning doesn't chain on a solo tank
        WeightVsBleed = 1.0f,    // big HP pool absorbs bleed ticks
        WeightVsDamagedTowers = 1.5f,    // damaged tower = ogre finishes it
        WeightAwayFromHero = -0.2f,    // hero vs ogre is fine — hero still needs sustained DPS
        LateGameBonus = 0.8f,    // late game ogres get stat multiplier — become walls
    };

    /// <summary>
    /// PLAGUE CASTER
    /// Role: debuff support that weakens towers (reduces their DPS) and slows the player hero.
    /// Does not deal much direct damage — value comes from making other enemies survive longer.
    ///
    /// Counter-pick logic:
    ///   - MOST valuable when paired with tanks or swarms (extends their survival window)
    ///   - Preferred when player relies on fast-firing towers (debuff attack speed wrecks them)
    ///   - Preferred vs Lightning (disruption counters chain-reaction builds)
    ///   - Weak to fast burst damage (low HP dies before aura lands)
    ///   - Deploying Plague Caster in a zone effectively increases every other enemy's weight
    ///
    /// Stats: Low-Moderate HP, Slow, No armor, AoE debuff aura, drops Soul Fragments
    /// </summary>
    static EnemyRosterEntry BuildPlagueCaster() => new()
    {
        DisplayName = "Plague Caster",
        UnlockNight = 5,
        BudgetCost = 2.0f,

        BaseHP = 120f,
        BaseArmor = 0f,
        IsRanged = true,     // casts from range
        HasEvasion = false,
        IsSeige = false,

        Resistances = DamageTypeMask.Bleed | DamageTypeMask.Fire,  // disease-immune to DoT

        WeightVsAoE = 0.5f,    // debuffing AoE towers is very valuable
        WeightVsSlow = 0.3f,    // extra slow stacks = enemy horde crawls to victory
        WeightVsPhysical = 0.8f,    // reducing physical tower output = high value
        WeightVsFire = 0.0f,    // fire-dominant = burn caster fast
        WeightVsFrost = 0.5f,    // slowing an already-slowed zone = combo
        WeightVsLightning = 1.2f,    // disrupting chain builds = spike in value
        WeightVsBleed = 0.3f,    // bleed ticks through plague immunity
        WeightVsDamagedTowers = 0.8f,    // soft zone = caster survives long enough to debuff
        WeightAwayFromHero = 1.0f,    // hero kills caster instantly — reroute
        LateGameBonus = 1.2f,    // late game debuff scaling becomes crucial
    };

    /// <summary>
    /// SHADOW RUNNER
    /// Role: high-speed evasion unit that slips through gaps in coverage.
    /// Stealth means ThreatScanner can't "see" them (already handled by TargetRegistry.IsInvisible).
    /// Forces the player to use detection items (Watchtower, Precision Scope reveals).
    ///
    /// Counter-pick logic:
    ///   - Strongly preferred vs slow-firing towers (misses between attacks)
    ///   - Preferred vs Physical (evasion dodge chance vs projectiles)
    ///   - Preferred on low-tower zones (fewer targeting options = easier slip-through)
    ///   - Weak to AoE (can't dodge splash) and Frost (can't outrun frozen status)
    ///   - Weak to Bleed (DoT tracks even invisible units)
    ///
    /// Stats: Moderate HP, Very High speed, No armor, Evasion/Stealth, drops Soul Fragments
    /// </summary>
    static EnemyRosterEntry BuildShadowRunner() => new()
    {
        DisplayName = "Shadow Runner",
        UnlockNight = 7,
        BudgetCost = 1.8f,

        BaseHP = 150f,
        BaseArmor = 0f,
        IsRanged = false,
        HasEvasion = true,     // triggers TargetRegistry invisibility logic
        IsSeige = false,

        Resistances = DamageTypeMask.Physical,   // evasion vs projectiles

        WeightVsAoE = -2.0f,    // WORST vs AoE — splash ignores evasion
        WeightVsSlow = -1.8f,    // speed is its only defense — slow = death
        WeightVsPhysical = 2.0f,    // BEST vs physical — dodge chance on projectiles
        WeightVsFire = -0.5f,    // fire tracks invisible units
        WeightVsFrost = -2.0f,    // frost = game over for a speed unit
        WeightVsLightning = -0.5f,    // chain can hit invisible secondary targets
        WeightVsBleed = -0.8f,    // bleed follows stealth units relentlessly
        WeightVsDamagedTowers = 1.0f,    // soft zone = faster slip-through
        WeightAwayFromHero = 1.5f,    // hero will spot and pursue — reroute
        LateGameBonus = 0.6f,    // still useful as distraction + detection check
    };

    /// <summary>
    /// SIEGE GOLEM
    /// Role: dedicated structure-killer with massive bonus damage to buildings.
    /// Slow but nearly unkillable without armor-piercing. Tests whether player has Piercing Tip / Shatter Point.
    ///
    /// Counter-pick logic:
    ///   - Highest weight vs AoE (AoE towers = prime target for structure-killer)
    ///   - Preferred vs high-HP towers (golem bonus damage multiplies on fortified towers)
    ///   - Preferred when player has high armor (golem ignores % of structure armor)
    ///   - Weak to Shatter Point (enemies with <5 armor take +100% dmg — but golem has high armor)
    ///   - Weak to Frost (frozen golem = 0 damage output)
    ///   - Pairs catastrophically well with Plague Caster (debuffed tower + golem attack)
    ///
    /// Stats: Very High HP, Very Slow, High armor, bonus vs buildings, drops Corrupted Metal
    /// </summary>
    static EnemyRosterEntry BuildSiegeGolem() => new()
    {
        DisplayName = "Siege Golem",
        UnlockNight = 10,
        BudgetCost = 4.0f,     // expensive — but destroys towers when it arrives

        BaseHP = 800f,
        BaseArmor = 25f,
        IsRanged = false,
        HasEvasion = false,
        IsSeige = true,     // CounterPickSelector uses this for boss night phase 2

        Resistances = DamageTypeMask.Physical | DamageTypeMask.Bleed,

        WeightVsAoE = 2.5f,    // golem eats AoE towers for breakfast
        WeightVsSlow = -2.0f,    // frozen golem literally cannot function
        WeightVsPhysical = 2.0f,    // high armor walls physical damage
        WeightVsFire = 0.8f,    // stone golem — fire does moderate work
        WeightVsFrost = -2.5f,    // WORST matchup — freeze stops all building damage
        WeightVsLightning = -0.5f,    // chain reaches golem even with escort
        WeightVsBleed = 1.5f,    // golem barely notices bleed ticks
        WeightVsDamagedTowers = 2.0f,    // already-damaged tower = golem finishes job fast
        WeightAwayFromHero = -0.3f,    // hero needs armor-pierce to hurt golem — fair fight
        LateGameBonus = 1.0f,    // late golems get stat multiplier — near-unkillable walls
    };

    /// <summary>
    /// NIGHTMARE SHAMAN
    /// Role: aura buffer that multiplies nearby enemy effectiveness.
    /// Does not attack directly — presence alone makes every other enemy in zone more dangerous.
    /// Player MUST eliminate shaman first or the entire wave buffs itself.
    ///
    /// Counter-pick logic:
    ///   - Preferred in any zone with other enemies present (aura value = 0 alone)
    ///   - EXTREMELY preferred in late game where budget allows full escort squads
    ///   - Preferred vs Physical (aura damage boost makes goblins hit like brutes)
    ///   - Weak to burst damage (low HP — any focus fire kills it)
    ///   - Weak to Bleed (pierces shaman's low defense quickly)
    ///   - Pairs with: every other unit type — shaman multiplies them all
    ///
    /// Stats: Moderate HP, Slow, Low armor, AoE aura buff, drops Crafting Materials
    /// </summary>
    static EnemyRosterEntry BuildNightmareShaman() => new()
    {
        DisplayName = "Nightmare Shaman",
        UnlockNight = 14,
        BudgetCost = 3.0f,

        BaseHP = 200f,
        BaseArmor = 5f,
        IsRanged = true,
        HasEvasion = false,
        IsSeige = false,

        Resistances = DamageTypeMask.Magic,     // magic-resistant (ironic for a caster)

        WeightVsAoE = 1.5f,    // buffing a swarm vs AoE = race condition player must win
        WeightVsSlow = -0.5f,    // shaman doesn't need to move — slow is minor penalty
        WeightVsPhysical = 2.0f,    // aura damage boost turns weak goblins into threats
        WeightVsFire = -0.3f,    // fire burns shaman out quickly
        WeightVsFrost = -0.3f,    // frost doesn't hurt much but slows aura positioning
        WeightVsLightning = -1.0f,    // chain can jump to shaman through the escort
        WeightVsBleed = -1.2f,    // bleed melts shaman's low HP fast
        WeightVsDamagedTowers = 1.0f,    // soft zone = shaman survives long enough to stack aura
        WeightAwayFromHero = 1.5f,    // hero focus-kills shaman instantly — reroute
        LateGameBonus = 2.0f,    // late game aura multipliers are devastating with stat mult
    };

    /// <summary>
    /// DREAD KNIGHT
    /// Role: elite frontliner with AoE slam that damages multiple towers simultaneously.
    /// Moderate-High armor, Very High HP — requires both DPS AND armor-piercing to handle.
    ///
    /// Counter-pick logic:
    ///   - Preferred vs clustered tower setups (AoE slam hits adjacents)
    ///   - Preferred when player has NO armor-piercing (Dread Knight shrugs off raw damage)
    ///   - Preferred vs Bleed (high HP absorbs bleed ticks)
    ///   - Weak to burst + armor-shred combo (Piercing Tip + high DPS melts it)
    ///   - Weak to Frost (frozen knight can't slam)
    ///   - Solo Dread Knight is still a major threat — full budget entry in mid-to-late game
    ///
    /// Stats: Very High HP, Moderate speed, Mod-High armor, AoE Slam, drops Tier 3 Component
    /// </summary>
    static EnemyRosterEntry BuildDreadKnight() => new()
    {
        DisplayName = "Dread Knight",
        UnlockNight = 18,
        BudgetCost = 5.0f,

        BaseHP = 600f,
        BaseArmor = 20f,
        IsRanged = false,
        HasEvasion = false,
        IsSeige = false,

        Resistances = DamageTypeMask.Physical | DamageTypeMask.Bleed | DamageTypeMask.Fire,

        WeightVsAoE = 1.0f,    // AoE towers are clustered — slam hits them all
        WeightVsSlow = -1.0f,    // frozen knight can't reach slam range
        WeightVsPhysical = 2.5f,    // BEST vs physical — armor walls it completely
        WeightVsFire = 0.5f,    // fire resistant — partial counter
        WeightVsFrost = -1.5f,    // frost slows and prevents slam
        WeightVsLightning = -0.5f,    // chain can reach knight even behind swarm
        WeightVsBleed = 1.8f,    // huge HP absorbs bleed — barely notices
        WeightVsDamagedTowers = 1.2f,    // damaged towers die to one slam
        WeightAwayFromHero = -0.5f,    // Dread Knight vs hero is interesting — intentional
        LateGameBonus = 2.5f,    // with stat multiplier, Dread Knight is near-boss tier
    };

    /// <summary>
    /// ABYSSAL HORROR (FINAL BOSS — Night 30 only)
    /// Role: three-phase encounter that uses everything the commander learned.
    /// NOT fielded by the regular counter-pick system.
    /// SmartEnemyCommander.BuildBossNight() handles this separately.
    ///
    /// Weight values are set conservatively (0) since this unit bypasses the picker.
    /// UnlockNight = 30 ensures it NEVER appears in regular waves.
    ///
    /// Stats: Extreme HP, Moderate speed, Very High armor, multi-ability + summons
    /// </summary>
    static EnemyRosterEntry BuildAbyssalHorror() => new()
    {
        DisplayName = "The Abyssal Horror",
        UnlockNight = 30,       // hard lock — only appears via BuildBossNight()
        BudgetCost = 999f,     // cost so high it can never be accidentally picked

        BaseHP = 5000f,
        BaseArmor = 40f,
        IsRanged = false,
        HasEvasion = false,
        IsSeige = true,     // attacks structures during phase 2

        // Resists everything — player must rely on armor-pierce and elemental combos
        Resistances = DamageTypeMask.Physical
                             | DamageTypeMask.Fire
                             | DamageTypeMask.Bleed,

        // All weights 0 — not used by regular picker
        WeightVsAoE = 0f,
        WeightVsSlow = 0f,
        WeightVsPhysical = 0f,
        WeightVsFire = 0f,
        WeightVsFrost = 0f,
        WeightVsLightning = 0f,
        WeightVsBleed = 0f,
        WeightVsDamagedTowers = 0f,
        WeightAwayFromHero = 0f,
        LateGameBonus = 0f,
    };
}
#endif