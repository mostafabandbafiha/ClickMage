namespace ClickMage.Animation
{
    public interface IAnimatable
    {
        void PlayAnimation(string stateName);
        void SetFloat(string param, float value);
        void SetBool(string param, bool value);
        float GetClipLength(string clipName);
    }
}
