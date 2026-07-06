using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPL.Examples
{

    public class RandomFlicker : MonoBehaviour
    {
        [SerializeField] bool refresh;

        private void Start()
        {
            SetRandomFlickering ();
        }

        private void Update()
        {
            if (refresh)
            {
                refresh = false;
                SetRandomFlickering ();
            }
        }

        //find all FPL_Controllers component in childrens and apply random flickering values:
        private void SetRandomFlickering()
        {
            FPL_Controller[] controllers = GetComponentsInChildren<FPL_Controller> ();

            foreach (FPL_Controller c in controllers)
            {
                if (c == null) continue;
                c.SetProperty (FPL_Properties._RandomOffset, Random.value*5);
                c.SetProperty (FPL_Properties._FlickerSpeed, Random.value*2);
            }
        }


    }
}
