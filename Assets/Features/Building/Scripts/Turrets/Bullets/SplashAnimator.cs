using UnityEngine;

namespace NaGaDeMon.Features.Building.Turrets.Bullets
{
    public class SplashAnimator : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float targetScale;

        // Tunable settings
        private float growDuration = 0.2f;     // time to expand
        private float lingerDuration = .3f;   // time fully expanded
        private float fadeDuration = 0.35f;     // time to fade

        private float timer;
        private enum State { Growing, Lingering, Fading }
        private State state;

        public void Initialize(SpriteRenderer renderer, float radius)
        {
            sr = renderer;

            // Scale calculation (circle sprite radius = 0.5 → scale * 0.5 = radius)
            targetScale = radius * 2f;

            Debug.Log(radius);

            state = State.Growing;
            timer = 0f;

            transform.localScale = Vector3.zero;

            // Start mostly visible
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0.05f;
                sr.color = c;
            }
        }

        private void Update()
        {
            switch (state)
            {
                case State.Growing:
                    UpdateGrowing();
                    break;
                case State.Lingering:
                    UpdateLingering();
                    break;
                case State.Fading:
                    UpdateFading();
                    break;
            }
        }

        private void UpdateGrowing()
        {
            timer += Time.deltaTime;
            float t = timer / growDuration;

            float scale = Mathf.Lerp(0f, targetScale, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            if (t >= 1f)
            {
                state = State.Lingering;
                timer = 0f;
            }
        }

        private void UpdateLingering()
        {
            timer += Time.deltaTime;

            // Stay fully expanded, do nothing
            if (timer >= lingerDuration)
            {
                state = State.Fading;
                timer = 0f;
            }
        }

        private void UpdateFading()
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.8f, 0f, t);
                sr.color = c;
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
