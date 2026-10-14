using UnityEngine;

namespace CubeTower.Core.Gestures
{
    public enum DragGesture
    {
        /// <summary>The finger hasn't moved far enough to tell the intent.</summary>
        Undecided,

        /// <summary>Take a cube out of the palette.</summary>
        PickCube,

        /// <summary>Scroll the palette.</summary>
        Scroll
    }

    /// <summary>
    /// Tells "pick a cube" from "scroll the palette" for a gesture started on a palette cube.
    /// - A finger that rested on the cube before moving wants the cube, whatever the direction.
    /// - Otherwise the direction is judged only after <see cref="DecisionDistance"/>: the first pixels
    ///   of a touch are noisy and must not decide the gesture.
    /// - Steep enough movement picks the cube (everything the cube can go to is above the palette),
    ///   flat movement scrolls.
    /// </summary>
    public sealed class DragGestureClassifier
    {
        public DragGestureClassifier(float pickAngle, float decisionDistance, float holdToPick)
        {
            PickAngle = Mathf.Clamp(pickAngle, 0f, 90f);
            DecisionDistance = Mathf.Max(0f, decisionDistance);
            HoldToPick = holdToPick;
        }

        /// <summary>Min angle from the horizontal (degrees) that picks a cube.</summary>
        public float PickAngle { get; }

        /// <summary>Distance (pixels) from the press point at which the direction is judged.</summary>
        public float DecisionDistance { get; }

        /// <summary>Hold time (seconds) before moving that picks a cube in any direction; non-positive disables it.</summary>
        public float HoldToPick { get; }

        /// <param name="delta">Pointer offset from the press point, in pixels.</param>
        /// <param name="heldBeforeMove">Seconds between the press and the start of the movement.</param>
        public DragGesture Classify(Vector2 delta, float heldBeforeMove)
        {
            if (HoldToPick > 0f && heldBeforeMove >= HoldToPick)
                return DragGesture.PickCube;

            if (delta.sqrMagnitude < DecisionDistance * DecisionDistance || delta.sqrMagnitude <= Mathf.Epsilon)
                return DragGesture.Undecided;

            var angle = Mathf.Atan2(Mathf.Abs(delta.y), Mathf.Abs(delta.x)) * Mathf.Rad2Deg;
            return angle >= PickAngle ? DragGesture.PickCube : DragGesture.Scroll;
        }
    }
}
