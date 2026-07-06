using UnityEngine;

namespace FPL.Examples
{
    [ExecuteInEditMode]
    public class LightInstancer : MonoBehaviour
    {
        [SerializeField] GameObject lightPrefab;
        [SerializeField] int instances = 10;
        [SerializeField] float offset = 1, lightSize = 50;
        [SerializeField] bool refresh;
        Vector2[] phyllotaxisOffset;

        void GenerateGrid()
        {

            ClearGrid ();


            phyllotaxisOffset = new Vector2[instances];
            for (int i = 0; i < instances; i++) phyllotaxisOffset[i] = CalculatePhyllotaxis (i, offset);

            for (int i = 0; i < instances; i++)
            {

                Vector3 position = new Vector3 (phyllotaxisOffset[i].x, 0, phyllotaxisOffset[i].y);
                //position -= new Vector3 (spacing * (gridSize.x *.5f), 0, spacing * ( gridSize.i * .5f ));
                position += transform.position;

                GameObject gridObject = Instantiate (lightPrefab,transform);
                gridObject.transform.position = position;
                gridObject.transform.localScale = Vector3.one * lightSize;
                //gridObjects[x, i] = gridObject;

            }

        }

        static float _degree = 137.5f;
        public static Vector2 CalculatePhyllotaxis(int pointCount, float pointRadius)
        {
            double angle = pointCount * ( _degree * Mathf.Deg2Rad );
            float r = pointRadius * Mathf.Sqrt (pointCount);

            float x = r * (float)System.Math.Cos (angle);
            float y = r * (float)System.Math.Sin (angle);
            Vector2 vec2 = new Vector2 (x, y);
            return vec2;
        }


 void ClearGrid()
        {
            // Destroy all previous grid objects
            var tempArray = new GameObject[transform.childCount];

            for (int i = 0; i < tempArray.Length; i++)
            {
                tempArray[i] = transform.GetChild (i).gameObject;
            }

            foreach (var child in tempArray)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    DestroyImmediate (child);
                };
#endif
            }
        }

        void OnValidate()
        {
            if (Application.isPlaying) return;


            if (refresh)
            {
                refresh = false;
                GenerateGrid ();
            }

        }

    }
}
