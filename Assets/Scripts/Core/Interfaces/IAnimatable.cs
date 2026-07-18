namespace ClickMage.Animation
{
    public interface IAnimatable
    {
        void PlayAnimation(string stateName);
        void PlayAnimation(string stateName, bool forceRestart); // NEW
        void SetFloat(string param, float value);
        void SetBool(string param, bool value);
        float GetClipLength(string clipName);
    }
}
