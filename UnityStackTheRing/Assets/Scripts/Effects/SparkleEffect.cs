namespace HyperCasualGame.Scripts.Effects
{
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    /// <summary>
    /// Controls sparkle particle effect for ring landing.
    /// Emits colored burst when ring lands on bucket.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class SparkleEffect : MonoBehaviour
    {
        private ParticleSystem particles;
        private ParticleSystem.MainModule mainModule;

        private void Awake()
        {
            this.particles = this.GetComponent<ParticleSystem>();
            this.mainModule = this.particles.main;

            // Configure particle system
            this.mainModule.playOnAwake = false;
            this.mainModule.loop = false;
            this.mainModule.startLifetime = GameConstants.SparkleConfig.ParticleLifetime;
            this.mainModule.startSpeed = GameConstants.SparkleConfig.BurstSpeed;
            this.mainModule.startSize = GameConstants.SparkleConfig.SparkleScale;
            this.mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        }

        /// <summary>
        /// Play sparkle burst with ring color.
        /// </summary>
        /// <param name="ringColor">Color of the landing ring</param>
        public void Play(ColorType ringColor)
        {
            // Set color based on ring
            var color = GameConstants.GetColor(ringColor);
            var brighterColor = color * 1.2f;
            brighterColor.a = 1f;

            this.mainModule.startColor = new ParticleSystem.MinMaxGradient(color, brighterColor);

            // Emit burst
            this.particles.Emit(GameConstants.SparkleConfig.ParticleCount);
        }

        /// <summary>
        /// Set world position for the effect.
        /// </summary>
        public void SetPosition(Vector3 worldPosition)
        {
            this.transform.position = worldPosition;
        }

        /// <summary>
        /// Check if particles are still alive.
        /// </summary>
        public bool IsPlaying => this.particles.isPlaying || this.particles.particleCount > 0;
    }
}
