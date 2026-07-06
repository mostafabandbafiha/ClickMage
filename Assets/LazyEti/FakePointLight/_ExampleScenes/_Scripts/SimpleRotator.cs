using System;
using UnityEngine;

namespace FPL.Examples
{
    public class SimpleRotator : MonoBehaviour
    {
        [Range (-5, 5)]
        public float speed = 0.1f;
        public bool xAxis = false;
        public bool yAxis = false;
        public bool zAxis = false;

        void FixedUpdate()
        {
            transform.Rotate (speed * Convert.ToInt32 (xAxis), speed * Convert.ToInt32 (yAxis), speed * Convert.ToInt32 (zAxis));
        }
    }
}
