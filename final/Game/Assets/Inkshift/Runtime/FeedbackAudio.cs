using UnityEngine;

namespace Inkshift
{
    public enum SoundCue { Flip, Jump, AirJump, Land, Shift, Fall, Checkpoint, Clear }

    [RequireComponent(typeof(AudioSource))]
    public sealed class FeedbackAudio : MonoBehaviour
    {
        [SerializeField] AudioClip[] cues;
        AudioSource speaker;

        void Awake() => speaker = GetComponent<AudioSource>();

        public void Play(SoundCue cue)
        {
            AudioClip clip = cues[(int)cue];
            if (clip)
                speaker.PlayOneShot(clip);
        }
    }
}
