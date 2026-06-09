using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShooterDem
{
    /// <summary>
    /// PUENTE (Camino A): congela al player del Low Poly Shooter Pack en PAUSA y GAME OVER.
    /// El sistema del pack no conoce nuestro `GameManager`, así que seguía moviendo la cámara
    /// (mouse-look usa delta, no Time.deltaTime) durante la pantalla de fin. Este componente
    /// escucha los eventos estáticos de `GameManager` y desactiva `PlayerInput` + `CameraLook`
    /// mientras el juego está congelado, y los re-activa al reanudar.
    /// Se agrega al GameObject raíz del player del pack.
    /// </summary>
    public class LpspGameStateFreeze : MonoBehaviour
    {
        private PlayerInput playerInput;
        private readonly List<Behaviour> cameraLooks = new List<Behaviour>();

        void Awake()
        {
            playerInput = GetComponentInChildren<PlayerInput>(true);
            // CameraLook vive en el namespace del pack; lo buscamos por nombre de tipo.
            foreach (var b in GetComponentsInChildren<Behaviour>(true))
                if (b.GetType().Name == "CameraLook")
                    cameraLooks.Add(b);
        }

        void OnEnable()
        {
            GameManager.GameOverShown += OnGameOver;
            GameManager.PauseChanged += OnPauseChanged;
        }

        void OnDisable()
        {
            GameManager.GameOverShown -= OnGameOver;
            GameManager.PauseChanged -= OnPauseChanged;
        }

        // Game over (ganar o perder): congela y no vuelve (la partida terminó).
        private void OnGameOver(string _) => SetFrozen(true);

        // Pausa: congela; al reanudar, descongela.
        private void OnPauseChanged(bool paused) => SetFrozen(paused);

        private void SetFrozen(bool frozen)
        {
            if (playerInput != null) playerInput.enabled = !frozen;
            for (int i = 0; i < cameraLooks.Count; i++)
                if (cameraLooks[i] != null) cameraLooks[i].enabled = !frozen;
        }
    }
}
