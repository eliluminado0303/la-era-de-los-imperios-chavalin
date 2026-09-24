using UnityEngine;
using UnityEngine.SceneManagement;
 public class ControladorMenu : MonoBehaviour{
        public void CrearPatida(){
            Debug.Log("Creando partida como servidor....");
            SceneManager.LoadScene("PartidaOnline");            
        }
        public void CombateLibre(){
            Debug.Log("Unirse a combate libre....");
            SceneManager.LoadScene("CombateLibre");            
        }
        public void SalirJuego(){
            Debug.Log("Saliendo del juego....");
            Application.Quit();
        }
 }
