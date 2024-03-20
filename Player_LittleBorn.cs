using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_LittleBorn : Player
{
    [SerializeField] Skill_Head prefab_Head;
    [SerializeField] Transform firePos;
    [SerializeField] Transform head_Parent;
    protected Skill_Head head;
    protected IEnumerator corSkill_1;
    protected IEnumerator cor_CoolUi;

    protected override void Init()
    {
        stpd.skul = PlayerSkul.LittleBorn;
        Damage = 15;

        base.Init();
        
        if (GameObject.Find("Head_Parent"))
            head_Parent = GameObject.Find("Head_Parent").transform;
        else
        {
            GameObject head_obj = new GameObject("Head_Parent");
            head_obj.transform.SetParent(null);
            head_Parent = head_obj.transform;
        }

        StartCoroutine(Switched());
        switchIndex = 1;
    }


    #region 스킬1 (쿨타임 초기화 포함)
    protected override void InputSkill_1()
    {
        if (Input.GetKeyDown(KeyCode.A) && canSkill_1)
        {
            corSkill_1 = CSkill_1();
            StartCoroutine(corSkill_1);
        }
    }

    protected override IEnumerator CSkill_1()
    {
        rigid.velocity = new Vector2(0, rigid.velocity.y);
        canSkill_1 = false;
        animator.SetTrigger("Skill_1");
        cor_CoolUi = CCoolDown_UI(ProjectManager.Instance.ui.skill1_Mask, 3);
        StartCoroutine(cor_CoolUi);
        yield return new WaitForSeconds(3f);
        canSkill_1 = true;
        animator.runtimeAnimatorController = animators[(int)PlayerSkul.LittleBorn];
    }

    //스킬1 - 머리를 날리는 순간 event
    void EventSkill()
    {
        head = Instantiate(prefab_Head, firePos);
        head.coolTime = 3;
        head.dir = playerDir == PlayerDir.right ? 1 : -1;
        head.player = this;
        head.transform.SetParent(head_Parent);
    }

    //스킬1 - 애니메이션 끝나기 1프레임 전 event
    void EventSwitchAnimation()
    {
        canInput = true;
        animator.runtimeAnimatorController = animators[(int)PlayerSkul.NoHead];
    }

    public void ResetCool()
    {
        StopCoroutine(corSkill_1);
        StopCoroutine(cor_CoolUi);
        ProjectManager.Instance.ui.skill1_Mask.fillAmount = 0;
        canSkill_1 = true;
    }
    #endregion

    #region 스킬2
    protected override void InputSkill_2()
    {
        if (Input.GetKeyDown(KeyCode.S) && canSkill_2 && head != null)
        {
            StartCoroutine(CSkill_2());
        }
    }
    protected override IEnumerator CSkill_2()
    {
        pSound.TELEPORT();
        canSkill_2 = false;
        FxManager.Instance.CreateFx_Effect_Tp(transform, playerCol.size.x, playerCol.size.y, 3);
        transform.position = head.transform.position;
        FxManager.Instance.CreateFx_Effect_Tp(transform, playerCol.size.x, playerCol.size.y, 3);
        Destroy(head.gameObject);
        ResetCool();
        head = null;
        animator.runtimeAnimatorController = animators[(int)PlayerSkul.LittleBorn];
        StartCoroutine(CCoolDown_UI(ProjectManager.Instance.ui.skill2_Mask, 5));
        yield return new WaitForSeconds(5f);
        canSkill_2 = true;
    }
    #endregion

    #region 교대
    protected override void SkulSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            base.SkulSwitch();
            if (head != null)
                Destroy(head.gameObject);
        }
    }

    //교대 - 애니메이션 시작하자마자 event
    IEnumerator EventSwitched_LB()
    {
        canInput = false;
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            float dir = playerDir == PlayerDir.right ? 1 : -1;
            rigid.velocity = new Vector2(dir * moveSpeed, rigid.velocity.y);
            yield return new WaitForFixedUpdate();
        }
        canInput = true;
    }

    protected override void SwitchSkill()
    {
        LookDir();
        animator.SetTrigger("Switch");
        isSwitched = false;
    }
    #endregion
}
