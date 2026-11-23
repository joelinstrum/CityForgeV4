using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private static PrototypeRegionView zoningPauseOwner;
        public static bool ZoningSimulationPaused => zoningPauseOwner;
        private float zoningPreviousTimeScale;
        private bool zoningPreviousAudioPause;
        private readonly List<Animator> zoningPausedAnimators=new();
        private readonly List<ParticleSystem> zoningPausedParticles=new();

        private void OnDisable()=>SetZoningGameplayPause(false);

        // Called by menu transitions, never a recurring scene scan. Keeping a stamp
        // attached to the pointer leaves the Zones section open and retains this lease.
        private void SetZoningGameplayPause(bool paused)
        {
            if(paused)
            {
                if(zoningPauseOwner)return;
                zoningPauseOwner=this;
                zoningPreviousTimeScale=Time.timeScale;
                zoningPreviousAudioPause=AudioListener.pause;
                Time.timeScale=0;AudioListener.pause=true;
                foreach(var animator in GetComponentsInChildren<Animator>())
                    if(animator.enabled){zoningPausedAnimators.Add(animator);animator.enabled=false;}
                foreach(var particles in GetComponentsInChildren<ParticleSystem>())
                    if(particles.isPlaying){zoningPausedParticles.Add(particles);particles.Pause(false);}
            }
            else
            {
                if(zoningPauseOwner!=this)return;
                zoningPauseOwner=null;
                Time.timeScale=zoningPreviousTimeScale;AudioListener.pause=zoningPreviousAudioPause;
                foreach(var animator in zoningPausedAnimators)if(animator)animator.enabled=true;
                foreach(var particles in zoningPausedParticles)if(particles)particles.Play(false);
                zoningPausedAnimators.Clear();zoningPausedParticles.Clear();
            }
        }
    }
}
