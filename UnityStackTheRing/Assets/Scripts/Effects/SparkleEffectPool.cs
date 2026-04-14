namespace HyperCasualGame.Scripts.Effects
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    /// <summary>
    /// Object pool for SparkleEffect instances.
    /// Manages reusable particle effects to avoid instantiation overhead.
    /// </summary>
    public class SparkleEffectPool : MonoBehaviour
    {
        [SerializeField] private SparkleEffect prefab;
        [SerializeField] private int poolSize = 5;

        private static SparkleEffectPool instance;

        /// <summary>
        /// Singleton instance. May be null if not in scene.
        /// </summary>
        public static SparkleEffectPool Instance => instance;

        private readonly Queue<SparkleEffect> pool = new();
        private bool isInitialized;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            instance = this;
            this.InitializePool();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void InitializePool()
        {
            if (this.isInitialized || this.prefab == null) return;

            for (var i = 0; i < this.poolSize; i++)
            {
                var effect = this.CreateEffect();
                this.pool.Enqueue(effect);
            }

            this.isInitialized = true;
        }

        private SparkleEffect CreateEffect()
        {
            var effect = Instantiate(this.prefab, this.transform);
            effect.gameObject.SetActive(false);
            effect.name = $"SparkleEffect_{this.pool.Count}";
            return effect;
        }

        /// <summary>
        /// Play sparkle effect at position with color.
        /// </summary>
        /// <param name="position">World position for effect</param>
        /// <param name="color">Ring color for particles</param>
        public async UniTask PlayAt(Vector3 position, ColorType color)
        {
            if (this.prefab == null)
            {
                Debug.LogWarning("[SparkleEffectPool] No prefab assigned, skipping VFX");
                return;
            }

            // Get or create effect
            SparkleEffect effect;
            if (this.pool.Count > 0)
            {
                effect = this.pool.Dequeue();
            }
            else
            {
                effect = this.CreateEffect();
            }

            // Play effect
            effect.gameObject.SetActive(true);
            effect.SetPosition(position);
            effect.Play(color);

            // Return to pool after particles die
            await UniTask.Delay(1000); // 1 second buffer for particles to complete

            if (effect != null)
            {
                effect.gameObject.SetActive(false);
                this.pool.Enqueue(effect);
            }
        }

        /// <summary>
        /// Fire and forget version for non-awaited usage.
        /// </summary>
        public void PlayAtFireForget(Vector3 position, ColorType color)
        {
            this.PlayAt(position, color).Forget();
        }
    }
}
