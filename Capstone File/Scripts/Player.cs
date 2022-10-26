using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class Player : MonoBehaviour
{
    public float speed;
    public GameObject weapon;
    public Camera followCamera;
    public bool hasweapon;
    public GameManager manager;
    public GameObject ChargeEffect;

    public AudioSource hammerSound;

    public int coin;
    public int health;
    public int score;

    public int maxcoin;
    public int maxhealth;

    float hAxis;
    float vAxis;

    float curChargeTime=0;

    bool iDown; //weapon
    bool wDown;
    bool fDown;
    bool fUp;
    bool gDown;
    bool rDown;
    bool jDown;
    bool sDown1;
    bool sDown2;
    bool sDown3;


    bool isJump;
    bool isDodge;
    bool isFireReady = true;
    bool isBorder;
    bool isDamaged;
    bool isShop;
    bool isDead;
    bool isClicked;
    bool isCharge;

    Vector3 movevec;
    Vector3 dodgevec;

    Animator animator;
    Rigidbody rigid;
    MeshRenderer[] meshs;

    GameObject nearobject;
    public Weapon equipWeapon; //�������� ����
    int equipWeaponIndex= -1;
    float fireDelay;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        rigid = GetComponent<Rigidbody>();
        meshs= GetComponentsInChildren<MeshRenderer>();

        PlayerPrefs.SetInt("MaxScore",11200);
        Debug.Log(PlayerPrefs.GetInt("MaxScore"));
    }

    void Start()
    {
        
    }

    void Update()
    {
        GetInput();
        Move();
        Turn();
        Jump();
        RecordFDownTime();
        Attack();
        Dodge();
        interation();
    }

    void GetInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal"); //GetAxisRaw : -1, 0 ,1 
        vAxis = Input.GetAxisRaw("Vertical");
        wDown = Input.GetButton("Walk");
        jDown = Input.GetButtonDown("Jump");
        fDown = Input.GetButtonDown("Fire1");
        fUp = Input.GetButtonUp("Fire1");
        iDown = Input.GetButtonDown("interation");
        sDown1 = Input.GetButtonDown("swap1");
        sDown2 = Input.GetButtonDown("swap2");
        sDown3 = Input.GetButtonDown("swap3");
    }

    void Move()
    {
        movevec = new Vector3(hAxis, 0, vAxis).normalized; 

        if(isDodge) 
        {
            movevec = dodgevec;
        }

        if( isDead) //!isFireReady��
        {
            movevec = Vector3.zero;
        }

        if(!isBorder)
        {
            transform.position += movevec * speed * (wDown ? 0.3f : 1f) * Time.deltaTime;
        }

        animator.SetBool("Isrun", movevec != Vector3.zero); 
        animator.SetBool("Iswalk", wDown);
    }

    void Turn()
    {
        //Ű���� ȸ��
        transform.LookAt(transform.position + movevec);

        //���콺 ȸ��
        if (fDown && !isDead)
        {
            Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit; //ray�� ���� ������Ʈ ������ �����ϴ� ����
            if (Physics.Raycast(ray, out rayHit, 100))
            {
                Vector3 nextVec = rayHit.point - transform.position;
                nextVec.y = 0;
                transform.LookAt(transform.position + nextVec);
            }
        }
    }

    void Jump()
    {
        if(jDown && movevec==Vector3.zero && !isJump && !isDodge && !isDead)
        {
            GetComponent<Rigidbody>().AddForce(Vector3.up * 20, ForceMode.Impulse);
            animator.SetBool("Isjump", true);
            animator.SetTrigger("Dojump");
            isJump = true;
        }
    }

    void Dodge()
    {
        if (jDown && movevec != Vector3.zero && !isJump && !isDodge && !isDead ) 
        {
            dodgevec = movevec; 
            animator.SetTrigger("Dododge");
            speed *= 2;
            isDodge = true;

            Invoke("DodgeOut", 0.5f);
        }
    }

    void DodgeOut()
    {
        speed *= 0.5f;
        isDodge = false;
    }

    void interation()
    {
        if (iDown && nearobject != null && !isJump && !isDodge && !isDead)
        {
            if (nearobject.tag == "Weapon")
            {
                Item item = nearobject.GetComponent<Item>();
                int weaponIndex = item.value;
                hasweapon = true;

                Destroy(nearobject);
            }
            else if (nearobject.tag == "Shop")
            {
                Shop shop = nearobject.GetComponent<Shop>();
                shop.Enter(this);
                isShop = true;
            }
        }
    }

    void Attack()
    {
        if (equipWeapon == null) return;

        fireDelay += Time.deltaTime;
        isFireReady = equipWeapon.rate < fireDelay;

        if (isCharge && isFireReady && !isDodge && !isShop && !isDead && !isClicked)
        {
            equipWeapon.ChargeAttack();
            fireDelay = 0;
            isCharge = false;
        }
        else if (fUp && isFireReady && !isDodge && !isShop && !isDead && !isCharge)
        {
            equipWeapon.MeleeAttack();
            animator.SetTrigger("Doswing");
            if (equipWeapon.type == Weapon.Type.Melee) //����� �׽�Ʈ
                hammerSound.Play();
            fireDelay = 0;
        }
    }

    void RecordFDownTime() //���ݹ�ư ���� �ð� ���
    {
        if (fDown)
        {
            isClicked = true;
        }
        if (fUp)
        {   
            ChargeEffect.SetActive(false);
            isClicked = false;
            curChargeTime = 0;
        }

        if (isClicked)
        {
            ChargeEffect.SetActive(true);
            curChargeTime += Time.deltaTime;
            Debug.Log(curChargeTime);

            if (curChargeTime >= equipWeapon.maxChargeTime)
            {
                isCharge = true;
            }
        }
    }

    void FreezeRotation()
    {
        rigid.angularVelocity = Vector3.zero;
    }

    void StopToWall()
    {
        Debug.DrawRay(transform.position, transform.forward * 5, Color.blue);
        isBorder = Physics.Raycast(transform.position, transform.forward, 5, LayerMask.GetMask("Wall"));
    }
     void FixedUpdate()
    {
        FreezeRotation();
        StopToWall();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag=="Floor")
        {
            animator.SetBool("Isjump", false);
            isJump = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag=="Item")
        {
            Item item = other.GetComponent<Item>();
            switch(item.type)
            {
                case Item.Type.Coin:
                    coin += item.value;
                    if (coin > maxcoin)
                    {
                        coin = maxcoin;
                    }
                    break;
                case Item.Type.Heart:
                    health += item.value;
                    if (health > maxhealth)
                    {
                        health=maxhealth;
                    }
                    break;
            }
            Destroy(other.gameObject);
        }
        else if(other.tag=="EnemyBullet")
        {
            if(!isDamaged)
            {
                Bullet enemyBullet = other.GetComponent<Bullet>();
                health -= enemyBullet.damage;

                bool isBossAttack = other.name == "Boss Melee Area";

                StartCoroutine(OnDamage(isBossAttack));
            }

            if (other.GetComponent<Rigidbody>() != null)
                Destroy(other.gameObject);
        }
    }

    IEnumerator OnDamage(bool isBossAttack)
    {
        isDamaged = true;
        foreach(MeshRenderer mesh in meshs)
        {
            mesh.material.color = Color.yellow;
        }

        if(isBossAttack)
        {
            rigid.AddForce(transform.forward * -25, ForceMode.Impulse);
        }

        if (health <= 0 && !isDead)
        {
            OnDie();
        }

        yield return new WaitForSeconds(1f);

        isDamaged = false;
        foreach (MeshRenderer mesh in meshs)
        {
            mesh.material.color = Color.white;
        }

        if(isBossAttack)
        {
            rigid.velocity = Vector3.zero;
        }

    }

    void OnDie()
    {
        animator.SetTrigger("Dodie");
        isDead = true;
        manager.GameOver();
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.tag=="Weapon" || other.tag == "Shop")
        {
            nearobject = other.gameObject;
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        //if(other.tag == "Weapon")
        //{
        //    nearobject = null;
        //}
        //�� �ڵ� ������, weapon Shop���� ���� ���� �� ���Ⱑ �����̿� ������ nulló���Ǿ� �Ʒ����忡 ������ ����.
        //(nearobject�� null�� �Ǿ������ �������� �������� ����)
        //�� �ڵ尡 �ʿ����� ��, �ּ��� �����ϰ�, weapon Shop�� weapon spawn pos�� �ָ� ������ ������ �ű��.
        if (other.tag == "Shop")
        {
            Shop shop = nearobject.GetComponent<Shop>();
            shop.Exit();
            isShop = false;

            nearobject = null;
        }
    }
}
