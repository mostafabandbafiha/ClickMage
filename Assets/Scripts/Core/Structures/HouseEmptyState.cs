// HouseEmptyState.cs
using ClickMage.StateMachine;

public class HouseEmptyState : IState<HouseStructure>
{
    public void Enter(HouseStructure house) { }
    public void Tick(HouseStructure house, float dt)
    {
        if (house.Residents.Count > 0)
            house.ChangeState(new HouseOccupiedDayState());
    }
    public void Exit(HouseStructure house) { }
}

// HouseOccupiedDayState.cs — residents are out working
public class HouseOccupiedDayState : IState<HouseStructure>
{
    public void Enter(HouseStructure house)
    {
        UnityEngine.Debug.Log($"[House] {house.name} residents are out working.");
    }
    public void Tick(HouseStructure house, float dt) { }
    public void Exit(HouseStructure house) { }
}

// HouseCallingResidentsState.cs — sunset, send everyone home
public class HouseCallingResidentsState : IState<HouseStructure>
{
    public void Enter(HouseStructure house)
    {
        house.SendResidentsHome();
    }
    public void Tick(HouseStructure house, float dt)
    {
        // Once all residents are inside, move to night state
        if (house.AllResidentsHome)
            house.ChangeState(new HouseNightState());
    }
    public void Exit(HouseStructure house) { }
}

// HouseNightState.cs — everyone is home and resting
public class HouseNightState : IState<HouseStructure>
{
    public void Enter(HouseStructure house)
    {
        UnityEngine.Debug.Log($"[House] {house.name} all residents home for the night.");
    }
    public void Tick(HouseStructure house, float dt) { }
    public void Exit(HouseStructure house) { }
}

// HouseReleasingResidentsState.cs — day started, release everyone
public class HouseReleasingResidentsState : IState<HouseStructure>
{
    public void Enter(HouseStructure house)
    {
        house.ReleaseResidents();
    }
    public void Tick(HouseStructure house, float dt)
    {
        if (house.IsEmpty)
            house.ChangeState(new HouseOccupiedDayState());
    }
    public void Exit(HouseStructure house) { }
}