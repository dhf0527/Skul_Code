using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public struct StPlayerData
{
    public PlayerType type;
    public PlayerSkul skul;
}

public enum PlayerSkul
{
    LittleBorn,
    NoHead,
    Wolf,
    Sword,
    Calleon
}

public enum PlayerType
{
    Balance,
    Speed,
    Power
}


public abstract class Player : MonoBehaviour
{
    public StPlayerData stpd;

    public enum PlayerDir
    {
        left,
        right
    }
    [HideInInspector] public PlayerDir playerDir = PlayerDir.right;
    protected PlayerDir playerDir_past;

    [HideInInspector] public Animator animator;
    protected Rigidbody2D rigid;

    //inspector에서 직접 넣어줄 것들
    public List<Player> players;
    public List<RuntimeAnimatorController> animators;
    public Attack_Box_Player atBox;
    [SerializeField] protected SpriteRenderer spriteRd;
    [SerializeField] protected CapsuleCollider2D playerCol;
    [SerializeField] Enemy enemy;
    [SerializeField] public PlayerSound pSound;
    protected bool canInput = true;
    protected float originalGravity = 6;

    //Move
    public float moveSpeed = 15f;

    //Dash
    int maxDashCount = 2;
    float dashPower = 30f;
    float dashCoolTime = 0.8f;
    float dashTime = 0.2f;
    protected bool canDash = true;
    bool isDashing = false;

    //Jump
    float jumpPower = 24f;
    bool isGround = true;
    bool jumped = false;

    //DownJump
    Collision2D collis;

    //Atack
    public float Damage { get; set; }
    public float Damage_Skill1 { get; set; }
    public float Damage_Skill2 { get; set; }

    //Damaged
    public bool isDead = false;
    protected bool isUnbeat = false;
    protected float unbeatTime = 1f;

    //Skill
    protected bool canSkill_1 = true;
    protected bool canSkill_2 = true;

    //Switch
    [HideInInspector] public bool isSwitched = false;
    protected bool canSwitch = true;
    [SerializeField] public int switchIndex;
    
    public bool isPush;

    [HideInInspector] private float inputX;
    [HideInInspector] private float inputY;

    protected virtual void Init()
    {
        animator = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();

        animator.runtimeAnimatorController = animators[(int)stpd.skul];

        ProjectManager.Instance.ui.skill1_Mask.fillAmount = 0;
        ProjectManager.Instance.ui.skill2_Mask.fillAmount = 0;
        ProjectManager.Instance.ui.switch_Mask.fillAmount = 0;

        atBox.player = this;
    }

    public void SwitchInit(Player player)
    {
        animators = player.animators;
        isSwitched = true;
        playerDir = player.playerDir;
        ProjectManager.Instance.HeadSwap();
        pSound.SWITCH();
    }

    void Start()
    {
        Init();
    }


    void Update()
    {
        if (PlayerBasket.Instance.isInven || isDead || ProjectManager.Instance.isPasue)
            return;

        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");

        JumpAnimation();

        if (!canInput)
            return;

        Move();
        Jump();
        Attack();

        if (Input.GetKeyDown(KeyCode.Z) && canDash)
            StartCoroutine("CDash");

        InputSkill_1();
        InputSkill_2();

        SkulSwitch();
        InvenActive();
    }

    #region 이동
    //바라보는 방향 설정
    protected void LookDir()
    {
        switch (playerDir)
        {
            case PlayerDir.right:
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;

            case PlayerDir.left:
                transform.rotation = Quaternion.Euler(0, 180, 0);
                break;
        }
        playerDir_past = playerDir;
    }
    
    //기본 이동
    protected void Move()
    {
        //대쉬중이거나 공격(점프공격 제외)중 이동 불가
        if (isDashing || animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack") &&
            !animator.GetCurrentAnimatorStateInfo(0).IsName("Player Jump_Attack"))
            return;

        rigid.velocity = new Vector2(inputX * moveSpeed, rigid.velocity.y);

        if (inputX == 0)
            animator.SetBool("Walk", false);
        else
        {
            animator.SetBool("Walk", true);
            playerDir = inputX > 0 ? PlayerDir.right : playerDir = PlayerDir.left;
        }

        LookDir();
    }

    #region 점프
    protected void Jump()
    {
        //점프키를 안 누르거나 아래방향+점프 
        if (!Input.GetKeyDown(KeyCode.C))
            return;
        else if ((Input.GetKeyDown(KeyCode.C) && inputY < 0))
        {
            DownJump();
            return;
        }

        if (!isGround)
        {
            if (jumped)
                return;

            jumped = true;
        }

        rigid.velocity = Vector2.zero;
        SetGravity(true);
        animator.SetBool("Dash", false);
        rigid.velocity = new Vector2(rigid.velocity.x, jumpPower);
        pSound.JUMP();
    }

    protected void DownJump()
    {
        if (Input.GetKeyDown(KeyCode.C) && inputY < 0)
        {
            RaycastHit2D rayHit = Physics2D.Raycast(transform.position, Vector3.down, 0.1f, LayerMask.GetMask("Ground"));
            if (!rayHit)
                return;

            if (rayHit.collider.GetComponent<PlatformEffector2D>())
            {
                StartCoroutine(CDownJump(rayHit.collider.GetComponent<CompositeCollider2D>()));
            }
        }
    }
    IEnumerator CDownJump(Collider2D col)
    {
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), col);
        yield return new WaitForSeconds(0.3f);
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), col, false);
    }

    //상승, 낙하에 따른 애니메이션
    protected void JumpAnimation()
    {
        if (rigid.velocity.y > 0.05f)
        {
            animator.SetBool("Jump", true);
            animator.SetBool("Fall", false);
        }
        else if (rigid.velocity.y < -0.05f)
        {
            animator.SetBool("Fall", true);
            animator.SetBool("Jump", false);
        }
        else
        {
            animator.SetBool("Jump", false);
            animator.SetBool("Fall", false);
        }
    }
    #endregion

    //대쉬
    protected IEnumerator CDash()
    {
        canDash = false;
        isDashing = true;
        isUnbeat = true;

        animator.SetBool("Dash", true);
        SetGravity(false);
        playerDir = inputX < 0 ? PlayerDir.left : inputX > 0 ? PlayerDir.right : playerDir;
        float dir = playerDir == PlayerDir.right ? 1 : -1;
        LookDir();
        rigid.velocity = new Vector2(dir * dashPower, 0);

        yield return new WaitForSeconds(dashTime);

        SetGravity(true);
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Player Dash"))
            rigid.velocity = Vector2.zero;

        isDashing = false;
        isUnbeat = false;
        animator.SetBool("Dash", false);

        yield return new WaitForSeconds(dashCoolTime);
        canDash = true;
    }
    #endregion

    #region 공격
    protected void Attack()
    {
        if (!Input.GetKeyDown(KeyCode.X) || isDashing)
            return;

        animator.SetTrigger("Attack");
    }

    //공격 A,B - 애니메이션 첫 프레임 event
    protected void EventStopMove()
    {
        rigid.velocity = new Vector2(0, rigid.velocity.y);
    }

    //공격 A,B - 무기를 휘두르는 순간 event
    protected void EventMoveAttack()
    {
        //공격시, 바라보는 방향을 입력하고있으면 전진
        if ((inputX > 0 && playerDir_past == PlayerDir.right) ||
            (inputX < 0 && playerDir_past == PlayerDir.left))
            rigid.velocity = transform.right * moveSpeed + transform.up * rigid.velocity.y;
    }

    //공격 A,B - 무기를 휘두른 직후 event
    protected void EventStopMoveAttack()
    {
        if (!isDashing)
            rigid.velocity = new Vector2(0, rigid.velocity.y);
    }

    //공격 데미지 판정 event
    protected void EventDamage()
    {
        foreach (var enemy in atBox.enemies)
        {
            SetDamage(enemy, Damage);
        }
    }

    public void SetDamage(Enemy enemy, float damage) => enemy.Damaged(damage);
    #endregion

    #region 피격
    public void Damaged(float damage)
    {
        if (isDead || isUnbeat)
            return;

        PlayerBasket.Instance.HP -= damage;
        if (PlayerBasket.Instance.HP <= 0)
        {
            PlayerBasket.Instance.HP = 0;
            animator.SetTrigger("Dead");
            rigid.velocity = Vector2.zero;
            isDead = true;
            DataManager.Instance.SaveData();
            PauseManager.Instance.Pause();
        }
        else
        {
            StartCoroutine("UnbeatTime");
        }
    }

    IEnumerator UnbeatTime()
    {
        isUnbeat = true;
        for (int i = 0; i < unbeatTime * 10; ++i)
        {
            if (i % 2 == 0)
                spriteRd.color = new Color(1f, 1f, 1f, 0.35f);
            else
                spriteRd.color = new Color(1f, 1f, 1f, 0.7f);

            yield return new WaitForSeconds(0.1f);
        }

        spriteRd.color = new Color(1f, 1f, 1f, 1f);
        isUnbeat = false;
        yield return null;
    }
    #endregion

    #region 스킬(쿨다운UI 포함)
    protected IEnumerator CCoolDown_UI(Image mask, float coolTime)
    {
        float coolDowned = 0;
        while (coolDowned < coolTime)
        {
            yield return new WaitForFixedUpdate();
            coolDowned += Time.deltaTime;
            mask.fillAmount = (coolTime - coolDowned) / coolTime;
        }
    }

    #region 스킬1
    protected abstract IEnumerator CSkill_1();
    protected virtual void InputSkill_1()
    {
        if (Input.GetKeyDown(KeyCode.A) && canSkill_1)
            StartCoroutine(CSkill_1());
    }
    #endregion

    #region 스킬2
    protected abstract IEnumerator CSkill_2();
    protected virtual void InputSkill_2()
    {
        if (Input.GetKeyDown(KeyCode.S) && canSkill_2)
            StartCoroutine(CSkill_2());
    }
    #endregion

    #endregion

    #region 교대
    protected abstract void SwitchSkill();

    //Init에서 교대 여부를 체크하고, 교대 처리하는 함수
    protected IEnumerator Switched()
    {
        if (isSwitched)
        {
            canSwitch = false;
            SwitchSkill();
            StartCoroutine(CCoolDown_UI(ProjectManager.Instance.ui.switch_Mask, 3));
            yield return new WaitForSeconds(3);
            canSwitch = true;
        }
    }

    protected virtual void SkulSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canSwitch)
        {
            Player player = Instantiate(Resources.Load<Player>($"Player/{ProjectManager.Instance.heads[1].name}"), transform);
            player.transform.SetParent(null);
            player.SwitchInit(this);
            Destroy(gameObject);
            StartCoroutine(CCoolDown_UI(ProjectManager.Instance.ui.switch_Mask, 3));
        }
    }

    #endregion
    

    public void ItemSwitch(Player plaerN)
    {
        Player player = Instantiate(Resources.Load<Player>($"Player/{plaerN.name}"), transform);
        player.transform.SetParent(null);
        animators = player.animators;
        isSwitched = true;
        playerDir = player.playerDir;
        ProjectManager.Instance.ItemHeadChange();
        Destroy(gameObject);
    }

    protected void SetGravity(bool On)
    {
        rigid.gravityScale = On ? originalGravity : 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            collis = collision;
            isGround = true;
            animator.SetBool("IsGround", true);
            jumped = false;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            collis = null;
            isGround = false;
            animator.SetBool("IsGround", false);
        }
    }
    
    void InvenActive()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            isPush = true;
        }

        else if(Input.GetKeyDown(KeyCode.Tab))
        {
            PlayerBasket.Instance.invectoryActivated = !PlayerBasket.Instance.invectoryActivated;
            pSound.InvenOpen();
            if (PlayerBasket.Instance.invectoryActivated)
            {
                ProjectManager.Instance.inven.invenCanvas.gameObject.SetActive(true);
                StartCoroutine(ProjectManager.Instance.inven.DotweenScroll());
                InvenManager.Instance.indexX = InvenManager.Instance.indexY = 0;
                PlayerBasket.Instance.isInven = true;
                pSound.InvenClose();
            }
        }
    }
}