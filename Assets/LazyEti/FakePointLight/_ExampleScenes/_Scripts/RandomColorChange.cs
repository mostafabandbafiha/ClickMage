using UnityEngine;

namespace FPL.Examples
{
    public class RandomColorChange : MonoBehaviour
    {
        [SerializeField] FPL_Controller fplController;

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                if(fplController == null) return;

                Color randomColor = Random.ColorHSV () * Random.Range(1,3);
                randomColor.a = 1;

                fplController.SetProperty (FPL_Properties._LightTint, randomColor);   
                fplController.SetProperty (FPL_Properties._HaloTint, randomColor * 3);
            }
        }
    }
}

