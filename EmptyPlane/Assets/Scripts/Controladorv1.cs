using System.Security.Policy;
using Assets.Scripts.Utiles;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    public class Controladorv1 : MonoBehaviour {
        private CharacterController controladorCaracter;
        public bool PersonajeControl = true;
        private float velocidad;
        public float CaminandoVelocidad=6f;
        public float SaltoVelocidad = 8f;
        public float Gravedad=20f;
        public bool LimitarDiagonalVeloc=true;
        public float FactorLimiteDiagonal = .7071f;
        public float FactorAntiBump = .75f;//Para controlar en cuesta abajo
        public int FactorantiDobleSalto = 1;
        public float LimiteVelocidadLadosAtras=3;
        public float CorriendoVelocidad=11f;
        public float CamindandoVelocidad = 6f;
        public bool DeslizarenCuesta = true;
        public bool DeslizarenObjetosMarcados = true;
        public float VelocidadDeslizandoce = 12.0f;
        public float UmbralDannoCayendo = 10.0f;//Altura desde la que al caer se hace danno
        public bool AireControl=true;
        public float AgacharseVelocidad=0.0023f;
        public bool PermitirSalto = true;
        public Animator CaracterAnimator;
        public float TiempoCorriendoPermitido=10f;

        private bool isMovimiento;
        private Vector3 direccionMovimiento = Vector3.zero;
        private Vector3 lastPos;
        private int tiempoSalto;
        private bool enTierra=true;
        private float rayDistancia;
        private RaycastHit choque;
        private float deslizarLimite;
        private Vector3 puntoContanto;
        private bool callendo;
        private float nivelPrincipioCaida;
        private bool iscorriendo;
        private float originalYCam;
        private Camera MainCam;
        private bool saltando = false;
        private bool agachado;
        private float tiempoCorriendo;
      //UI
        public Image CorriendoSHow;
        private void Awake()
        {
            controladorCaracter = GetComponent<CharacterController>();
            tiempoSalto = FactorantiDobleSalto;
            velocidad = CaminandoVelocidad;
            rayDistancia = controladorCaracter.height * .5f + controladorCaracter.radius;
            deslizarLimite = controladorCaracter.slopeLimit - .1f;
            MainCam=Camera.main;
            originalYCam = MainCam.transform.position.y;
        }

        private float GetPrecent(float tiempoparam)
        {
            return ((tiempoparam * 100) / TiempoCorriendoPermitido) / 100;
        }
        private void FixedUpdate()
        {
            var posX = Input.GetAxis("Horizontal");
            var posY = Input.GetAxis("Vertical");
            var modificarvalorentrada = 1.0f;//Modifica valor de dezplazamiento si la velocidad diagonal esta Limitada
            if (LimitarDiagonalVeloc)
            {
                modificarvalorentrada = FactorLimiteDiagonal;
            }
            if (enTierra)
            {
                bool deslizandoce = false;
                if (Physics.Raycast(transform.position, -Vector3.up, out choque, rayDistancia))
                {
                    if (Vector3.Angle(choque.normal, Vector3.up) > deslizarLimite)
                        deslizandoce = true;
                }
                else//Si el rayo anterior no detecto la pendiente
                {
                    Physics.Raycast(puntoContanto + Vector3.up, -Vector3.up, out choque);
                    if (Vector3.Angle(choque.normal, Vector3.up) > deslizarLimite)
                        deslizandoce = true;
                }
               
                //Si caemos de una distancia vertical m�s gran que el umbral permitido, manejamos una rutina descendente de da�o
                if (callendo)
                {
                    PersonajeControl = false;
                    callendo = false;
                    if (transform.position.y < nivelPrincipioCaida - UmbralDannoCayendo)
                        AlertadeDannoCayendo(nivelPrincipioCaida - transform.position.y);
                }
                    SetVelocidad();//Ajustar Velocidad

                //Si esta deslizandoce calcular velocidad hasta donde va a estar y quitar el control del caracter
                    if ((deslizandoce && DeslizarenCuesta) || (DeslizarenObjetosMarcados && choque.collider.tag == "Deslizarce"))
                    {
                        Vector3 hitNormal = choque.normal;
                        direccionMovimiento = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                        Vector3.OrthoNormalize(ref hitNormal, ref direccionMovimiento);
                        direccionMovimiento *= VelocidadDeslizandoce;
                        PersonajeControl = false; //QUitar control del playert
                        
                    }
                    else
                    {
                        //Si no Calcular direccionMovimiento directamente de los ejes.
                        //Quitando un poco de y para evitar chocar abajo con las pendientes
                        direccionMovimiento = new Vector3(posX * modificarvalorentrada, -FactorAntiBump,
                        posY * modificarvalorentrada);
                        //Vector3 en la direccion del movimiento multiplicado x la velocidad
                        direccionMovimiento = transform.TransformDirection(direccionMovimiento) * velocidad;
                        PersonajeControl = true;
                    }

                if (!Input.GetButton("Jump") && PermitirSalto)
                {
                    tiempoSalto++;
                    saltando = false;
                }

                else if (tiempoSalto >= FactorantiDobleSalto && !agachado)//Si puede saltar
                    {
                        Saltar();
                    }
                if (Input.GetButton("Agacharse") && !saltando && controladorCaracter.isGrounded && PersonajeControl)
                {
                    CaracterAnimator.SetBool("agacharse",true);
                    agachado = true;

                }
                else
                {
                    CaracterAnimator.SetBool("agacharse", false);
                    agachado = false;
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
                // Si se permite Aire Control, Comprobar movimiento pero no tocar el componente de la y
                if (AireControl && PersonajeControl)
                {
                    direccionMovimiento.x = posX * velocidad * modificarvalorentrada;
                    direccionMovimiento.z = posY * velocidad * modificarvalorentrada;
                    direccionMovimiento = transform.TransformDirection(direccionMovimiento);
                }
            }
            
            lastPos = controladorCaracter.transform.position;
            Mover();
            if (lastPos == controladorCaracter.transform.position)
                isMovimiento = false;

            if (isMovimiento)
            {
               
            }
            Debug.Log("<color=red>"+velocidad+"</color>");
        }

        private void Mover()
        {
            //Aplicar Gravedad
            direccionMovimiento.y -= Gravedad * Time.deltaTime;
            enTierra = (controladorCaracter.Move(direccionMovimiento * Time.deltaTime) & CollisionFlags.Below) != 0;
            isMovimiento = true;
        }

        private void Saltar()
        {
            if (tiempoSalto >= FactorantiDobleSalto)
            {
                direccionMovimiento.y = SaltoVelocidad;
                saltando = true;
                tiempoSalto = 0;
            }
          
        }

        private void AlertadeDannoCayendo(float distanciaCaida)
        {
            Debug.Log("Ouch! " + distanciaCaida + " unidadades!");
        }


        // Use this for initialization
        void Start () {
	
        }
	
        // Update is called once per frame
        void Update () {
            //Si esta presionado el boton de correr cambiar en caminar,correr,agacharse
            if (controladorCaracter.isGrounded && Input.GetButtonDown("Run"))
            {
                velocidad = CorriendoVelocidad;
                iscorriendo = true;
            }
            else if (controladorCaracter.isGrounded && Input.GetButton("Agacharse"))
            {
                velocidad = AgacharseVelocidad;
                iscorriendo = false;
                
            }
            if (!Input.GetButtonDown("Run"))
            {
                velocidad = CaminandoVelocidad;
                iscorriendo = false;
            }

        }

        public void SetVelocidad()
        {
            // Si apretamos la tecla  corriendo(y correr se permite), usar velocidad adecuada cuando el boton este precionado.

            
            if (Input.GetButton("Run"))
            {
                CorriendoSHow.fillAmount = GetPrecent(tiempoCorriendo);//Tiempo corriendo 
                tiempoCorriendo += Time.deltaTime;

                if (tiempoCorriendo >= TiempoCorriendoPermitido)
                {
                    tiempoCorriendo = 0;
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

                velocidad = CaminandoVelocidad;
            }
            if (Input.GetButton("Agacharse"))
            {
                velocidad = AgacharseVelocidad;
            }
           

            // Debug.Log(direccionMovimiento + " veloc:" + velocidad + " Tiempo:" + tiempo);
        }
    }
}
