// IEffectTarget.cs
using ClickMage.Stats;

namespace ClickMage.Items
{
    public interface IEffectTarget
    {
        IStatHolder StatHolder { get; }
    }
}
