using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Utiles;
using System.Collections;
using System.ComponentModel;

[RequireComponent(typeof (CharacterController))]
public class Controlador : MonoBehaviour
{

    public float CaminandoVeloc;
    public float CorriendoVelocidad;
    public float RotacionVeloc;
    public bool LimitarDiagonalVeloc = true;
    public bool ToggleCorrer = false;
    public float SaltoVelocidad = 8.0f;
    public float Gravedad = 20.0f;
    public float UmbralDannoCayendo = 10.0f;
    public bool DeslizarenCuesta = false;
    public bool DeslizarenObjetosMarcados = false;
    public float VelocidadDeslizandoce = 12.0f;
    public bool AireControl = false;
    public float FactorAntiBump = .75f;
    public int FactorantiDobleSalto = 1;
    public float LimiteVelocidadLadosAtras = 0;
    public float TiempoCorriendoPermitido;
    public float TiempoDescanso = 0;
    
    private Vector3 direccionMovimiento = Vector3.zero;
    private bool entierra = false;
    private CharacterController controlador;

    private float velocidad;
    private RaycastHit hit;
    private float nivelPrincipioCaida;
    private bool callendo;
    private float deslizarLimite;
    private float rayDistance;
    private Vector3 puntoContanto;
    private bool playerControl = false;
    private int tiempoSalto;
    private float tiempo = 0; //Tiempo COrriendo
    private bool corriendo = false;
   
    
   
    //UI
    public Image CorriendoSHow;

    private void Awake()
    {
     
        controlador = GetComponent<CharacterController>();
        velocidad = CaminandoVeloc;
        rayDistance = controlador.height*.5f + controlador.radius;
        deslizarLimite = controlador.slopeLimit - .1f;
        tiempoSalto = FactorantiDobleSalto;
    }

  

    private void Start()
    {

    }


    private void FixedUpdate()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");
        float modificarvalorentrada = (inputX != 0.0f && inputY != 0.0f && LimitarDiagonalVeloc) ? .7071f : 1.0f;

        if (entierra)
        {
            bool deslizandoce = false;
            if (Physics.Raycast(transform.position, -Vector3.up, out hit, rayDistance))
            {
                if (Vector3.Angle(hit.normal, Vector3.up) > deslizarLimite)
                    deslizandoce = true;
            }
            else
            {
                Physics.Raycast(puntoContanto + Vector3.up, -Vector3.up, out hit);
                if (Vector3.Angle(hit.normal, Vector3.up) > deslizarLimite)
                    deslizandoce = true;
            }
            //Si caemos de una distancia vertical m�s gran que el umbral permitido, manejamos una rutina descendente de da�o
            if (callendo)
            {
                callendo = false;
                if (transform.position.y < nivelPrincipioCaida - UmbralDannoCayendo)
                    AlertadeDannoCayendo(nivelPrincipioCaida - transform.position.y);
            }
           SetVelocidad();
            
            // Si estamos delizandonos(y esta permitido), o estamos en un orbejo con tag "Deslizarce", obtengo un vecot apuntando a la base de la pendiente
            if ((deslizandoce && DeslizarenCuesta) || (DeslizarenObjetosMarcados && hit.collider.tag == "Deslizarce"))
            {
                Vector3 hitNormal = hit.normal;
                direccionMovimiento = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                Vector3.OrthoNormalize(ref hitNormal, ref direccionMovimiento);
                direccionMovimiento *= VelocidadDeslizandoce;
                playerControl = false; //QUitar control del playert
                Debug.Log("Deslizandoce");
            }
            // Si no recalcular direccionMovimiento directamente de los ejes , Quitando un poco de y para evitar chocar abajo con las pendientes
            else
            {
                direccionMovimiento = new Vector3(inputX*modificarvalorentrada, -FactorAntiBump,
                    inputY*modificarvalorentrada);
                direccionMovimiento = transform.TransformDirection(direccionMovimiento)*velocidad;
                playerControl = true;
                // Debug.Log("Caminado "+direccionMovimiento.x);
            }
            // Saltar solo  si el boton de salto a sido presionado y el jugador a estado en tierra el numero de frames permitidos
            if (!Input.GetButton("Jump"))
                tiempoSalto++;
            else if (tiempoSalto >= FactorantiDobleSalto)
            {
                direccionMovimiento.y = SaltoVelocidad;
                tiempoSalto = 0;
            }
        }
        else
        {
            // Si pas�ramos por encima de un acantilado o algo por el estilo, colocar la altura en la cual comenzamos a caer
            //Si se usara
            if (!callendo)
            {
                callendo = true;
                nivelPrincipioCaida = transform.position.y;
            }
            // Si se permite Aire Control, Compruebe movimiento pero no tocar el componente de la y
            if (AireControl && playerControl)
            {
                direccionMovimiento.x = inputX*velocidad*modificarvalorentrada;
                direccionMovimiento.z = inputY*velocidad*modificarvalorentrada;
                direccionMovimiento = transform.TransformDirection(direccionMovimiento);
            }
        }

        // AplicarGravedad
        direccionMovimiento.y -= Gravedad*Time.deltaTime;
        //Mover el controlador poner entierra a verdadero o falso dependiendo si estamos de pie o no
        entierra = (controlador.Move(direccionMovimiento * Time.deltaTime) & CollisionFlags.Below) != 0;
       
    }

  

    private void Update()
    {
        //Si esta presionado el boton de correr cambiar en caminar y correr
        if (ToggleCorrer && entierra && Input.GetButtonDown("Run"))
          velocidad = (velocidad == CaminandoVeloc ? CorriendoVelocidad : CaminandoVeloc);
    }

    // Guarda las colisiones por si las necesito mas tarde
    private void OnControllerColliderHit(ControllerColliderHit Hit)
    {
        puntoContanto = Hit.point;
        //   Debug.Log(Hit.collider.name);
    }


    private void AlertadeDannoCayendo(float distanciaCaida)
    {
        Debug.Log("Ouch! " + distanciaCaida + " unidadades!");
    }

   

    private float GetPrecent(float tiempoparam)
    {
        return ((tiempoparam*100)/TiempoCorriendoPermitido)/100;
    }

    public void SetVelocidad()
    {
        // Si apretamos la tecla  corriendo(y correr se permite), usar velocidad adecuada cuando el boton este precionado.

        CorriendoSHow.fillAmount = GetPrecent(tiempo);//Tiempo corriendo 
        if (Input.GetButton("Run") && !corriendo && ToggleCorrer)
        {
            tiempo += Time.deltaTime;

            if (tiempo >= TiempoCorriendoPermitido)
            {
                tiempo = 0;
            }

            if (Input.GetButton("Horizontal")) //Liminar velocidad corriendo al lado
            {
                velocidad = CorriendoVelocidad - LimiteVelocidadLadosAtras;
            }

            else
            {
                velocidad = CorriendoVelocidad;
            }
        }
        else
        {
            
            velocidad = CaminandoVeloc;
        }

       Debug.Log(direccionMovimiento + " veloc:" + velocidad + " Tiempo:" + tiempo);
    }




}
