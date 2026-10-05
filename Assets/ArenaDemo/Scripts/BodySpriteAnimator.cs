using UnityEngine;
namespace PhaseArena
{
    public class BodySpriteAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public SpriteRenderer visual;
        public ThermoBody body;
        // Down, up, left, right. Empty for existing animated enemies.
        public Sprite[] directionalFrames;
        public Vector2[] directionalOffsets;
        public Sprite[] directionalWalkFrames;
        public Vector2[] walkFrameOffsets;
        public float walkFramesPerSecond=8;
        int directionIndex;
        float timer;
        bool walking;
        public bool HasDirections => directionalFrames!=null && directionalFrames.Length==4;
        public void SetFacing(Vector2 direction)
        {
            walking=direction.sqrMagnitude>=.01f;
            if(!HasDirections || !walking) return;
            directionIndex=Mathf.Abs(direction.y)>=Mathf.Abs(direction.x) ? (direction.y>0 ? 1 : 0) : (direction.x<0 ? 2 : 3);
            ApplyDirection();
        }
        void ApplyDirection()
        {
            if(!HasDirections || visual==null) return;
            int frame=directionIndex*4+(walking ? (int)timer%4 : 0);
            bool hasWalk=directionalWalkFrames!=null && directionalWalkFrames.Length==16;
            visual.sprite=hasWalk ? directionalWalkFrames[frame] : directionalFrames[directionIndex]; visual.flipX=false;
            if(hasWalk && walkFrameOffsets!=null && walkFrameOffsets.Length==16)
                visual.transform.localPosition=walkFrameOffsets[frame]*(body!=null ? body.ShapeScale : 1);
            else if(directionalOffsets!=null && directionalOffsets.Length==4)
                visual.transform.localPosition=directionalOffsets[directionIndex]*(body!=null ? body.ShapeScale : 1);
        }
        void LateUpdate() { if(HasDirections) ApplyDirection(); }
        void Update()
        {
            if(HasDirections) { if(walking) timer+=Time.deltaTime*walkFramesPerSecond; else timer=0; return; }
            if(frames==null || frames.Length==0 || visual==null || body==null || body.Body==null) return;
            timer+=Time.deltaTime*(body.Body.velocity.magnitude>.3f ? 9 : 4);
            visual.sprite=frames[(int)timer%frames.Length];
        }
    }
}
