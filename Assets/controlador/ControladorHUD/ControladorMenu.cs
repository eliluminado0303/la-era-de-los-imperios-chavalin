using UnityEngine;
using UnityEngine.SceneManagement;
 public class ControladorMenu : MonoBehaviour{
        public void CrearPatida(){
            Debug.Log("Creando partida como servidor....");
            SceneManager.LoadScene("PartidaOnline");            
        }
        public void UnirsePartida(){
            Debug.Log("Unirse a partida como cliente....");
            SceneManager.LoadScene("PartidaOnline");            
        }
        public void SalirJuego(){
            Debug.Log("Saliendo del juego....");
            Application.Quit();
        }
 }
