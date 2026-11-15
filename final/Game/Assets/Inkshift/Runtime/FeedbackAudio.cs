using UnityEngine;

namespace Inkshift
{
    public enum SoundCue { Flip, Jump, AirJump, Land, Shift, Fall, Checkpoint, Clear, Shutter }

    [RequireComponent(typeof(AudioSource))]
    public sealed class FeedbackAudio : MonoBehaviour
    {
        [SerializeField] AudioClip[] cues;
        AudioSource speaker;

        void Awake() => speaker = GetComponent<AudioSource>();

        public void Play(SoundCue cue)
        {
            int index = (int)cue;
            AudioClip clip = cues != null && index < cues.Length ? cues[index] : null;
            if (clip)
                speaker.PlayOneShot(clip);
        }
    }
}
