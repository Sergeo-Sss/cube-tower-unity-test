using UnityEngine.EventSystems;

namespace CubeTower.Runtime.Gameplay.Cubes
{
    /// <summary>
    /// Owner of a <see cref="CubeView"/> that decides what a drag gesture on the cube means.
    /// </summary>
    public interface ICubeDragReceiver
    {
        void OnInitializePotentialDrag(CubeView cube, PointerEventData eventData);

        void OnBeginDrag(CubeView cube, PointerEventData eventData);

        void OnDrag(CubeView cube, PointerEventData eventData);

        void OnEndDrag(CubeView cube, PointerEventData eventData);
    }
}
